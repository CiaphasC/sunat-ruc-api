namespace SunatScraper.Core.Services;
using SunatScraper.Core.Models;
using System.Collections.Generic;

internal static class DocumentPageParser
{
    internal static RucInfo Parse(string html)
    {
        return RucPageParser.Parse(html);
    }

    internal static IEnumerable<SearchResultItem> ParseList(string html)
    {
        return RucPageParser.ParseList(html);
    }
}
