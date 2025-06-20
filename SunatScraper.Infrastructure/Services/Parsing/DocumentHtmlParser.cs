namespace SunatScraper.Infrastructure.Services;

using SunatScraper.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Analiza la página HTML devuelta por consultas de documento.
/// </summary>
internal static class DocumentHtmlParser
{
    internal static RucInfo Parse(string html)
    {
        return HtmlParserCommon.ParseRucInfo(html);
    }

    internal static Task<RucInfo> ParseAsync(string html)
    {
        return HtmlParserCommon.ParseRucInfoAsync(html);
    }

    internal static IEnumerable<SearchResultItem> ParseList(string html)
    {
        return RucHtmlParser.ParseList(html);
    }

    internal static Task<IReadOnlyList<SearchResultItem>> ParseListAsync(string html)
    {
        return RucHtmlParser.ParseListAsync(html);
    }
}
