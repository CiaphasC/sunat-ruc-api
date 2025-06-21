/// <summary>
/// Cliente HTTP encargado de interactuar con el portal de SUNAT.
/// </summary>
namespace SunatScraper.Infrastructure.Services;

using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using SunatScraper.Domain.Models;
using SunatScraper.Domain.Validation;
using SunatScraper.Domain;
using System.Net;
using System.Text;
using System.Text.Json;

/// <summary>
/// Implementación de <see cref="ISunatClient"/> basada en <see cref="HttpClient"/>.
/// </summary>
public sealed class SunatClient : ISunatClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly CaptchaSolver _security;
    private readonly IMemoryCache _memoryCache;
    private readonly IDatabase? _redisDatabase;
    private readonly CookieContainer _cookieJar;

    private SunatClient(HttpClient httpClient, IMemoryCache memoryCache, IDatabase? redisDatabase, CookieContainer cookieJar)
    {
        _httpClient = httpClient;
        _memoryCache = memoryCache;
        _redisDatabase = redisDatabase;
        _cookieJar = cookieJar;
        _security = new CaptchaSolver(httpClient);

        Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }
    /// <summary>
    /// Crea una instancia de <see cref="ISunatClient"/> configurada y lista para usar.
    /// </summary>
    public static ISunatClient Create(string? redisConnection = null)
    {
        var cookieJar = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieJar,
            AutomaticDecompression = DecompressionMethods.All
        };

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://e-consultaruc.sunat.gob.pe/")
        };

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/122.0.0.0 Safari/537.36");
        httpClient.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        httpClient.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es-PE,es;q=0.9");

        var memory = new MemoryCache(new MemoryCacheOptions { SizeLimit = 2048 });
        IDatabase? db = redisConnection is null ? null : ConnectionMultiplexer.Connect(redisConnection).GetDatabase();

        return new SunatClient(httpClient, memory, db, cookieJar);
    }
    /// <summary>
    /// Obtiene la información correspondiente a un RUC.
    /// </summary>
    public Task<RucInfo> GetByRucAsync(string ruc) =>
        SendAsync("consPorRuc", ("nroRuc", ruc));

    /// <summary>
    /// Obtiene los datos de varios RUCs en paralelo manteniendo el orden.
    /// </summary>
    public async Task<IReadOnlyList<RucInfo>> GetByRucsAsync(IEnumerable<string> rucs)
    {
        var tasks = rucs.Select(ruc =>
        {
            if (!InputValidators.IsValidRuc(ruc))
                throw new ArgumentException("RUC inválido");
            return GetByRucAsync(ruc);
        }).ToArray();

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Realiza la búsqueda de contribuyente por tipo y número de documento.
    /// </summary>
    public async Task<RucInfo> GetByDocumentAsync(string tipo, string numero)
    {
        if (!InputValidators.IsValidDocumento(tipo, numero))
            throw new ArgumentException("Doc inválido");

        var html = await SendRawAsync("consPorTipdoc", ("tipdoc", tipo), ("nrodoc", numero));
        var results = RucParser.ParseList(html, true).ToList();
        string? ubicacion = results.Count > 0 ? results[0].Ubicacion : null;

        if (results.Count > 0 && !string.IsNullOrWhiteSpace(results[0].Ruc))
        {
            var details = await GetByRucAsync(results[0].Ruc!);
            return !string.IsNullOrWhiteSpace(ubicacion)
                ? details with { Ubicacion = ubicacion }
                : details;
        }

        var parsed = RucParser.Parse(html, true);
        return !string.IsNullOrWhiteSpace(ubicacion)
            ? parsed with { Ubicacion = ubicacion }
            : parsed;
    }

    /// <summary>
    /// Devuelve el listado de contribuyentes asociados a un documento.
    /// </summary>
    public async Task<IReadOnlyList<SearchResultItem>> SearchByDocumentAsync(string tipo, string numero)
    {
        if (!InputValidators.IsValidDocumento(tipo, numero))
            throw new ArgumentException("Doc inválido");

        var html = await SendRawAsync("consPorTipdoc", ("tipdoc", tipo), ("nrodoc", numero));
        return RucParser.ParseList(html, true).ToList();
    }

    /// <summary>
    /// Obtiene coincidencias a partir de la razón social.
    /// </summary>
    public async Task<IReadOnlyList<SearchResultItem>> SearchByNameAsync(string query)
    {
        if (!InputValidators.IsValidTexto(query))
            throw new ArgumentException("Texto inválido");

        var html = await SendRawAsync("consPorRazonSoc", ("razSoc", query));
        return RucParser.ParseList(html).ToList();
    }

    /// <summary>
    /// Devuelve la información de la primera coincidencia por razón social.
    /// </summary>
    public async Task<RucInfo> GetByNameAsync(string query)
    {
        if (!InputValidators.IsValidTexto(query))
            throw new ArgumentException("Texto inválido");

        var html = await SendRawAsync("consPorRazonSoc", ("razSoc", query));
        var results = RucParser.ParseList(html).ToList();
        string? ubicacion = results.Count > 0 ? results[0].Ubicacion : null;

        if (results.Count > 0 && !string.IsNullOrWhiteSpace(results[0].Ruc))
        {
            var details = await GetByRucAsync(results[0].Ruc!);
            return !string.IsNullOrWhiteSpace(ubicacion) ? details with { Ubicacion = ubicacion } : details;
        }

        var parsed = RucParser.Parse(html);
        return !string.IsNullOrWhiteSpace(ubicacion) ? parsed with { Ubicacion = ubicacion } : parsed;
    }
    /// <summary>
    /// Envía la solicitud al portal de SUNAT y devuelve el HTML resultante.
    /// </summary>
    private async Task<string> SendRawAsync(string accion, params (string k, string v)[] extras)
    {
        var form = new Dictionary<string, string> { { "accion", accion } };
        foreach (var (k, v) in extras)
            form[k] = v;

        var key = JsonSerializer.Serialize(form);

        if (_memoryCache.TryGetValue(key, out string? cachedHtml) && cachedHtml is not null)
            return cachedHtml;

        if (_redisDatabase != null)
        {
            var j = _redisDatabase.StringGet(key);
            if (j.HasValue) return j.ToString();
        }

        var initRes = await _httpClient.GetAsync("cl-ti-itmrconsruc/FrameCriterioBusquedaWeb.jsp");
        if (initRes.IsSuccessStatusCode)
        {
            var body = await initRes.Content.ReadAsStringAsync();
            const string pat = @"document\.cookie\s*=\s*""([^""]+)""";
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(body, pat))
            {
                var cookie = m.Groups[1].Value.Split(';', 2)[0];
                var p = cookie.IndexOf('=');
                if (p > 0)
                {
                    _cookieJar.Add(new Uri(_httpClient.BaseAddress!, "/"), new Cookie(cookie[..p], cookie[(p + 1)..]));
                }
            }
        }

        form["token"] = _security.GenerateToken();
        form["codigo"] = await _security.SolveCaptchaAsync();
        form["numRnd"] = _security.LastRandom.ToString();
        form["contexto"] = "ti-it";
        form["modo"] = "1";

        using var req = new HttpRequestMessage(HttpMethod.Post, "cl-ti-itmrconsruc/jcrS00Alias")
        {
            Content = new FormUrlEncodedContent(form)
        };
        req.Headers.Referrer = new Uri(_httpClient.BaseAddress!, "cl-ti-itmrconsruc/FrameCriterioBusquedaWeb.jsp");

        using var res = await _httpClient.SendAsync(req);
        res.EnsureSuccessStatusCode();

        var html = Encoding.GetEncoding("ISO-8859-1").GetString(await res.Content.ReadAsByteArrayAsync());
        _memoryCache.Set(key, html, new MemoryCacheEntryOptions { Size = 1, SlidingExpiration = TimeSpan.FromHours(6) });
        _redisDatabase?.StringSet(key, html, TimeSpan.FromHours(12));
        return html;
    }

    /// <summary>
    /// Método auxiliar que procesa el HTML obtenido y lo convierte en un modelo de dominio.
    /// </summary>
    private async Task<RucInfo> SendAsync(string accion, params (string k, string v)[] extras)
    {
        var html = await SendRawAsync(accion, extras);
        var parsedInfo = RucParser.Parse(html, accion == "consPorTipdoc");
        return parsedInfo;
    }

    /// <summary>
    /// Libera los recursos utilizados por el cliente HTTP y la memoria caché.
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
        _memoryCache.Dispose();
    }
}
