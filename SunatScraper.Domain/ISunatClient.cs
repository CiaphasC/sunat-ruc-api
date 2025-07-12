// Contrato que define las operaciones de consulta a la SUNAT.
namespace SunatScraper.Domain;
using SunatScraper.Domain.Models;

public interface ISunatClient
{
    /// <summary>
    /// Obtiene la información detallada de un número de RUC.
    /// </summary>
    Task<RucInfo> GetByRucAsync(string ruc);

    /// <summary>
    /// Obtiene la información de varios RUCs de forma concurrente.
    /// </summary>
    Task<IReadOnlyList<RucInfo>> GetByRucsAsync(IEnumerable<string> rucs);

    /// <summary>
    /// Realiza la búsqueda por tipo y número de documento de identidad.
    /// </summary>
    Task<RucInfo> GetByDocumentAsync(string tipo, string numero);

    /// <summary>
    /// Devuelve la lista de contribuyentes asociados a un documento.
    /// </summary>
    Task<IReadOnlyList<SearchResultItem>> SearchByDocumentAsync(string tipo, string numero);

    /// <summary>
    /// Obtiene el listado de resultados filtrando por razón social.
    /// </summary>
    Task<IReadOnlyList<SearchResultItem>> SearchByNameAsync(string query);

    /// <summary>
    /// Devuelve el detalle de la primera coincidencia encontrada por razón social.
    /// </summary>
    Task<RucInfo> GetByNameAsync(string query);
}
