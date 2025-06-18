namespace SunatScraper.Core.Services;

using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using SunatScraper.Core.Models;
using SunatScraper.Core.Validation;
using System.Net;
using System.Text;
using System.Text.Json;

/// <summary>
/// Abstraction for consult operations against the SUNAT website.
/// </summary>
public interface ISunatClient
{
    Task<RucInfo> ByRucAsync(string ruc);
    Task<RucInfo> ByDocumentoAsync(string tipo, string numero);
    Task<IReadOnlyList<SearchResultItem>> SearchDocumentoAsync(string tipo, string numero);
    Task<IReadOnlyList<SearchResultItem>> SearchRazonAsync(string query);
    Task<RucInfo> ByRazonAsync(string query);
}

/// <summary>
/// HTTP client implementation for <see cref="ISunatClient"/>.
/// </summary>
public sealed class SunatClient : ISunatClient
{
    private readonly HttpClient _httpClient;
    private readonly SunatSecurity _security;
    private readonly IMemoryCache _memoryCache;
    private readonly IDatabase? _redisDatabase;
    private readonly CookieContainer _cookieJar;

    private SunatClient(HttpClient httpClient, IMemoryCache memoryCache, IDatabase? redisDatabase, CookieContainer cookieJar)
    {
        _httpClient = httpClient;
        _memoryCache = memoryCache;
        _redisDatabase = redisDatabase;
        _cookieJar = cookieJar;
        _security = new SunatSecurity(httpClient);

        Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }
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
    public Task<RucInfo> ByRucAsync(string ruc) =>
        SendAsync("consPorRuc", ("nroRuc", ruc));

    public async Task<RucInfo> ByDocumentoAsync(string tipo, string numero)
    {
        if (!InputGuards.IsValidDocumento(tipo, numero))
            throw new ArgumentException("Doc inválido");

        var html = await SendRawAsync("consPorTipdoc", ("tipdoc", tipo), ("nrodoc", numero));
        var list = RucParser.ParseList(html, true).ToList();

        if (list.Count > 0 && !string.IsNullOrWhiteSpace(list[0].Ruc))
            return await ByRucAsync(list[0].Ruc!);

        return RucParser.Parse(html, true);
    }

    public async Task<IReadOnlyList<SearchResultItem>> SearchDocumentoAsync(string tipo, string numero)
    {
        if (!InputGuards.IsValidDocumento(tipo, numero))
            throw new ArgumentException("Doc inválido");

        var html = await SendRawAsync("consPorTipdoc", ("tipdoc", tipo), ("nrodoc", numero));
        return RucParser.ParseList(html, true).ToList();
    }

    public async Task<IReadOnlyList<SearchResultItem>> SearchRazonAsync(string query)
    {
        if (!InputGuards.IsValidTexto(query))
            throw new ArgumentException("Texto inválido");

        var html = await SendRawAsync("consPorRazonSoc", ("razSoc", query));
        return RucParser.ParseList(html).ToList();
    }

    public async Task<RucInfo> ByRazonAsync(string query)
    {
        if (!InputGuards.IsValidTexto(query))
            throw new ArgumentException("Texto inválido");

        var html = await SendRawAsync("consPorRazonSoc", ("razSoc", query));
        var list = RucParser.ParseList(html).ToList();
        string? ubicacion = list.Count > 0 ? list[0].Ubicacion : null;

        if (list.Count > 0 && !string.IsNullOrWhiteSpace(list[0].Ruc))
        {
            var info = await ByRucAsync(list[0].Ruc!);
            return !string.IsNullOrWhiteSpace(ubicacion) ? info with { Ubicacion = ubicacion } : info;
        }

        var parsed = RucParser.Parse(html);
        return !string.IsNullOrWhiteSpace(ubicacion) ? parsed with { Ubicacion = ubicacion } : parsed;
    }
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

    private async Task<RucInfo> SendAsync(string accion, params (string k, string v)[] extras)
    {
        var html = await SendRawAsync(accion, extras);
        var info = RucParser.Parse(html, accion == "consPorTipdoc");
        return info;
    }
}
