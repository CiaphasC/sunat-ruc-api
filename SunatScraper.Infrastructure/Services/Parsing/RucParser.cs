/// <summary>
/// Facilita la elección del parser adecuado para cada tipo de consulta.
/// </summary>
namespace SunatScraper.Infrastructure.Services;
using SunatScraper.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class RucParser
{
    /// <summary>
    /// Devuelve la información de un RUC a partir del HTML obtenido.
    /// </summary>
    public static RucInfo Parse(string html, bool porDocumento = false)
    {
        return porDocumento ? DocumentHtmlParser.Parse(html) : RucHtmlParser.Parse(html);
    }

    public static Task<RucInfo> ParseAsync(string html, bool porDocumento = false)
    {
        return porDocumento ? DocumentHtmlParser.ParseAsync(html) : RucHtmlParser.ParseAsync(html);
    }

    /// <summary>
    /// Obtiene la lista de resultados encontrados en el HTML.
    /// </summary>
    public static IEnumerable<SearchResultItem> ParseList(string html, bool porDocumento = false)
    {
        return porDocumento ? DocumentHtmlParser.ParseList(html) : RucHtmlParser.ParseList(html);
    }

    public static Task<IReadOnlyList<SearchResultItem>> ParseListAsync(string html, bool porDocumento = false)
    {
        return porDocumento ? DocumentHtmlParser.ParseListAsync(html) : RucHtmlParser.ParseListAsync(html);
    }
}
