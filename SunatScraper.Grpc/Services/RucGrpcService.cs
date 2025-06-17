using Grpc.Core;
using SunatScraper.Core.Services;
public class RucGrpcService : Sunat.SunatBase
{
    private readonly SunatClient _svc;
    public RucGrpcService(SunatClient s)=>_svc=s;
    public override async Task<RucReply> GetByRuc(RucRequest r,ServerCallContext _)=>
        Map(await _svc.ByRucAsync(r.Ruc));
    static RucReply Map(SunatScraper.Core.Models.RucInfo i)=>new(){
        Ruc=i.Ruc??"",RazonSocial=i.RazonSocial??"",Estado=i.Estado??"",
        Condicion=i.Condicion??"",Direccion=i.Direccion??""
    };
}
