namespace SunatScraper.Infrastructure.Services;

using HtmlAgilityPack;
using SunatScraper.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// Analiza el HTML obtenido en las búsquedas por RUC.
/// </summary>
internal static class RucHtmlParser
{
    internal static RucInfo Parse(string html)
    {
        return HtmlParserCommon.ParseRucInfo(html);
    }

    internal static Task<RucInfo> ParseAsync(string html)
    {
        return HtmlParserCommon.ParseRucInfoAsync(html);
    }

    /// <summary>
    /// Obtiene una colección de coincidencias desde la página de resultados de búsqueda.
    /// </summary>
    internal static IEnumerable<SearchResultItem> ParseList(string html)
    {
        return SearchListHtmlParser.ParseList(html);
    }

    internal static Task<IReadOnlyList<SearchResultItem>> ParseListAsync(string html)
    {
        return SearchListHtmlParser.ParseListAsync(html);
    }
}
