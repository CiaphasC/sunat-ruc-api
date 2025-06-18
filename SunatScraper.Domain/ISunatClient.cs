// Contrato de operaciones para consultar información en la SUNAT.
namespace SunatScraper.Domain;
using SunatScraper.Domain.Models;

public interface ISunatClient
{
    Task<RucInfo> ByRucAsync(string ruc);
    Task<RucInfo> ByDocumentoAsync(string tipo, string numero);
    Task<IReadOnlyList<SearchResultItem>> SearchDocumentoAsync(string tipo, string numero);
    Task<IReadOnlyList<SearchResultItem>> SearchRazonAsync(string query);
    Task<RucInfo> ByRazonAsync(string query);
}
