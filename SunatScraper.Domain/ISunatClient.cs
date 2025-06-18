// Contrato que define las operaciones de consulta a la SUNAT.
namespace SunatScraper.Domain;
using SunatScraper.Domain.Models;

public interface ISunatClient
{
    /// <summary>
    /// Obtiene la información detallada de un número de RUC.
    /// </summary>
    Task<RucInfo> ByRucAsync(string ruc);

    /// <summary>
    /// Realiza la búsqueda por tipo y número de documento de identidad.
    /// </summary>
    Task<RucInfo> ByDocumentoAsync(string tipo, string numero);

    /// <summary>
    /// Devuelve la lista de contribuyentes asociados a un documento.
    /// </summary>
    Task<IReadOnlyList<SearchResultItem>> SearchDocumentoAsync(string tipo, string numero);

    /// <summary>
    /// Obtiene el listado de resultados filtrando por razón social.
    /// </summary>
    Task<IReadOnlyList<SearchResultItem>> SearchRazonAsync(string query);

    /// <summary>
    /// Devuelve el detalle de la primera coincidencia encontrada por razón social.
    /// </summary>
    Task<RucInfo> ByRazonAsync(string query);
}
