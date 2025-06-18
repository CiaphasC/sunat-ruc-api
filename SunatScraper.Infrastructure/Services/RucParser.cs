namespace SunatScraper.Infrastructure.Services;
using SunatScraper.Domain.Models;
using System.Collections.Generic;

public static class RucParser
{
    public static RucInfo Parse(string html, bool porDocumento = false)
    {
        return porDocumento ? DocumentPageParser.Parse(html) : RucPageParser.Parse(html);
    }

    public static IEnumerable<SearchResultItem> ParseList(string html, bool porDocumento = false)
    {
        return porDocumento ? DocumentPageParser.ParseList(html) : RucPageParser.ParseList(html);
    }
}
