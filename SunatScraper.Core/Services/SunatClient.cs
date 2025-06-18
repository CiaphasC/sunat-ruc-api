namespace SunatScraper.Core.Services;
using SunatScraper.Core.Models;
using SunatScraper.Core.Validation;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using System.Net;
using System.Linq;
public sealed class SunatClient
{
    private readonly HttpClient _http;
    private readonly SunatSecurity _sec;
    private readonly IMemoryCache _mem;
    private readonly IDatabase? _redis;
    private readonly CookieContainer _cookie;
    private SunatClient(HttpClient h,IMemoryCache m,IDatabase? r,CookieContainer c){
        _http=h;_mem=m;_redis=r;_cookie=c;_sec=new SunatSecurity(h);
        Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }
    public static SunatClient Create(string? redis=null){
        var jar=new CookieContainer();
        var handler=new HttpClientHandler{CookieContainer=jar,AutomaticDecompression=DecompressionMethods.All};
        var http=new HttpClient(handler){BaseAddress=new Uri("https://e-consultaruc.sunat.gob.pe/")};
        http.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) " +
            "Chrome/122.0.0.0 Safari/537.36");
        http.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        http.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests","1");
        http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("es-PE,es;q=0.9");
        var mem=new MemoryCache(new MemoryCacheOptions{SizeLimit=2048});
        IDatabase? db= redis is null ? null : ConnectionMultiplexer.Connect(redis).GetDatabase();
        return new SunatClient(http,mem,db,jar);
    }
    public Task<RucInfo> ByRucAsync(string r)=>SendAsync("consPorRuc",("nroRuc",r));
    public Task<RucInfo> ByDocumentoAsync(string t,string n){
        if(!InputGuards.IsValidDocumento(t,n)) throw new ArgumentException("Doc inválido");
        return SendAsync("consPorTipdoc",("tipdoc",t),("nrodoc",n));
    }
    public async Task<IReadOnlyList<SearchResultItem>> SearchDocumentoAsync(string t,string n)
    {
        if(!InputGuards.IsValidDocumento(t,n)) throw new ArgumentException("Doc inválido");
        var html = await SendRawAsync("consPorTipdoc",("tipdoc",t),("nrodoc",n));
        return RucParser.ParseList(html).ToList();
    }
    public Task<RucInfo> ByRazonAsync(string q){
        if(!InputGuards.IsValidTexto(q)) throw new ArgumentException("Texto inválido");
        return SendAsync("consPorRazonSoc",("razSoc",q));
    }
    private async Task<string> SendRawAsync(string accion, params (string k,string v)[] ex){
        var form=new Dictionary<string,string>{{"accion",accion}};
        foreach(var (k,v) in ex)form[k]=v;
        string key=JsonSerializer.Serialize(form);
        if(_mem.TryGetValue(key,out string? cachedHtml) && cachedHtml is not null)
            return cachedHtml;
        if(_redis!=null)
        {
            var j=_redis.StringGet(key);
            if(j.HasValue) return j.ToString();
        }
        var initRes=await _http.GetAsync("cl-ti-itmrconsruc/FrameCriterioBusquedaWeb.jsp");
        if(initRes.IsSuccessStatusCode){
            var body=await initRes.Content.ReadAsStringAsync();
            const string pat = @"document\.cookie\s*=\s*""([^""]+)""";
            foreach(System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(body,pat)){
                var cookie=m.Groups[1].Value.Split(';',2)[0];
                var p=cookie.IndexOf('=');
                if(p>0){
                    _cookie.Add(new Uri(_http.BaseAddress!,"/"),new Cookie(cookie[..p],cookie[(p+1)..]));
                }
            }
        }
        form["token"]=_sec.GenerateToken();
        form["codigo"]=await _sec.SolveCaptchaAsync();
        form["numRnd"]=_sec.LastRandom.ToString();
        form["contexto"]="ti-it";form["modo"]="1";
        using var req=new HttpRequestMessage(HttpMethod.Post,"cl-ti-itmrconsruc/jcrS00Alias"){
            Content=new FormUrlEncodedContent(form)
        };
        req.Headers.Referrer=new Uri(_http.BaseAddress!,"cl-ti-itmrconsruc/FrameCriterioBusquedaWeb.jsp");
        using var res=await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var html=Encoding.GetEncoding("ISO-8859-1").GetString(await res.Content.ReadAsByteArrayAsync());
        _mem.Set(key,html,new MemoryCacheEntryOptions{Size=1,SlidingExpiration=TimeSpan.FromHours(6)});
        _redis?.StringSet(key,html,TimeSpan.FromHours(12));
        return html;
    }

    private async Task<RucInfo> SendAsync(string accion, params (string k,string v)[] ex){
        var html = await SendRawAsync(accion,ex);
        var info = RucParser.Parse(html);
        return info;
    }
}
