namespace SunatScraper.Core.Services;
using SunatScraper.Core.Models;
using SunatScraper.Core.Validation;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using System.Net;
public sealed class SunatClient
{
    private readonly HttpClient _http;
    private readonly SunatSecurity _sec;
    private readonly IMemoryCache _mem;
    private readonly IDatabase? _redis;
    private SunatClient(HttpClient h,IMemoryCache m,IDatabase? r){
        _http=h;_mem=m;_redis=r;_sec=new SunatSecurity(h);
        Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }
    public static SunatClient Create(string? redis=null){
        var handler=new HttpClientHandler{CookieContainer=new CookieContainer(),AutomaticDecompression=DecompressionMethods.All};
        var http=new HttpClient(handler){BaseAddress=new Uri("https://e-consultaruc.sunat.gob.pe/")};
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        var mem=new MemoryCache(new MemoryCacheOptions{SizeLimit=2048});
        IDatabase? db= redis is null ? null : ConnectionMultiplexer.Connect(redis).GetDatabase();
        return new SunatClient(http,mem,db);
    }
    public Task<RucInfo> ByRucAsync(string r)=>SendAsync("consPorRuc",("nroRuc",r));
    public Task<RucInfo> ByDocumentoAsync(string t,string n){
        if(!InputGuards.IsValidDocumento(t,n)) throw new ArgumentException("Doc inválido");
        return SendAsync("consPorTipdoc",("tipdoc",t),("nrodoc",n));
    }
    public Task<RucInfo> ByRazonAsync(string q){
        if(!InputGuards.IsValidTexto(q)) throw new ArgumentException("Texto inválido");
        return SendAsync("consPorRazonSoc",("razSoc",q));
    }
    private async Task<RucInfo> SendAsync(string accion, params (string k,string v)[] ex){
        var form=new Dictionary<string,string>{{"accion",accion}};
        foreach(var (k,v) in ex)form[k]=v;
        string key=JsonSerializer.Serialize(form);
        if(_mem.TryGetValue(key,out RucInfo c))return c;
        if(_redis!=null){
            var j=_redis.StringGet(key);
            if(j.HasValue) return JsonSerializer.Deserialize<RucInfo>(j.ToString())!;
        }
        await _http.GetAsync("cl-ti-itmrconsruc/FrameCriterioBusquedaWeb.jsp");
        form["token"]=_sec.GenerateToken();
        form["codigo"]=await _sec.SolveCaptchaAsync();
        form["contexto"]="ti-it";form["modo"]="1";
        using var res=await _http.PostAsync("cl-ti-itmrconsruc/jcrS00Alias",new FormUrlEncodedContent(form));
        res.EnsureSuccessStatusCode();
        var html=Encoding.GetEncoding("ISO-8859-1").GetString(await res.Content.ReadAsByteArrayAsync());
        var info=RucParser.Parse(html);
        _mem.Set(key,info,new MemoryCacheEntryOptions{Size=1,SlidingExpiration=TimeSpan.FromHours(6)});
        _redis?.StringSet(key,JsonSerializer.Serialize(info),TimeSpan.FromHours(12));
        return info;
    }
}
