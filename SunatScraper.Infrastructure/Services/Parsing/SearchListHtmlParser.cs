namespace SunatScraper.Infrastructure.Services;

using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using SunatScraper.Domain.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

/// <summary>
/// Analiza el HTML de listados de contribuyentes.
/// </summary>
internal static class SearchListHtmlParser
{
    internal static IReadOnlyList<SearchResultItem> ParseList(string html)
    {
        var parser = new HtmlParser();
        var document = parser.ParseDocument(html);

        var items = document.QuerySelectorAll("a[class*=aRucs]")
            .Select(anchor =>
            {
                string? rucNumber = null, razonSocial = null, ubicacion = null, estado = null;

                var rucNode = anchor.QuerySelectorAll("h4").FirstOrDefault(h => h.TextContent.Contains("RUC"));
                if (rucNode != null)
                {
                    var match = Regex.Match(rucNode.TextContent, "\\d{11}");
                    if (match.Success) rucNumber = match.Value;
                }

                var headingNodes = anchor.QuerySelectorAll("h4").ToList();
                if (headingNodes.Count > 1)
                    razonSocial = WebUtility.HtmlDecode(headingNodes[1].TextContent.Trim());

                var anchorText = WebUtility.HtmlDecode(anchor.TextContent);
                var ubicacionNode = anchor.QuerySelectorAll("p").FirstOrDefault(p => p.TextContent.Contains("Ubicación"));
                if (ubicacionNode != null)
                    ubicacion = ubicacionNode.TextContent.Split(':', 2).Last().Trim();
                else
                {
                    var match = Regex.Match(anchorText, @"Ubicaci(?:\u00f3|o)n\s*:\s*([^\n]+)", RegexOptions.IgnoreCase);
                    if (match.Success) ubicacion = match.Groups[1].Value.Trim();
                }

                var estadoNode = anchor.QuerySelectorAll("p").FirstOrDefault(p => p.TextContent.Contains("Estado"));
                if (estadoNode != null)
                    estado = estadoNode.TextContent.Split(':', 2).Last().Trim();
                else
                {
                    var match = Regex.Match(anchorText, @"Estado\s*:\s*([^\n]+)", RegexOptions.IgnoreCase);
                    if (match.Success) estado = match.Groups[1].Value.Trim();
                }

                return new SearchResultItem(rucNumber, razonSocial, ubicacion, estado);
            })
            ?.ToList()
            ?? new List<SearchResultItem>();

        return items;
    }

    internal static Task<IReadOnlyList<SearchResultItem>> ParseListAsync(string html) =>
        Task.Run(() => ParseList(html));
}
