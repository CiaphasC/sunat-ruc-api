// Servicio gRPC que expone las consultas de RUC.
using System.Threading.Tasks;
using Grpc.Core;
using SunatScraper.Domain;
using SunatScraper.Grpc;

public class RucGrpcService : Sunat.SunatBase
{
    private readonly ISunatClient _client;

    public RucGrpcService(ISunatClient client) => _client = client;

    public override async Task<RucReply> GetByRuc(RucRequest request, ServerCallContext _) =>
        Map(await _client.ByRucAsync(request.Ruc));

    private static RucReply Map(SunatScraper.Domain.Models.RucInfo info) => new()
    {
        Ruc = info.Ruc ?? string.Empty,
        RazonSocial = info.RazonSocial ?? string.Empty,
        Estado = info.Estado ?? string.Empty,
        Condicion = info.Condicion ?? string.Empty,
        Direccion = info.Direccion ?? string.Empty
    };
}

