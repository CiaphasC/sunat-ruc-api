namespace SunatScraper.Infrastructure.Services;

using HtmlAgilityPack;
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
    internal static IEnumerable<SearchResultItem> ParseList(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        return document.DocumentNode
            .SelectNodes("//a[contains(@class,'aRucs')]")
            ?.Select(anchor =>
            {
                string? rucNumber = null, razonSocial = null, ubicacion = null, estado = null;

                var rucNode = anchor.SelectSingleNode(".//h4[contains(text(),'RUC')]");
                if (rucNode != null)
                {
                    var match = Regex.Match(rucNode.InnerText, "\\d{11}");
                    if (match.Success) rucNumber = match.Value;
                }

                var headingNodes = anchor.SelectNodes(".//h4");
                if (headingNodes != null && headingNodes.Count > 1)
                    razonSocial = WebUtility.HtmlDecode(headingNodes[1].InnerText.Trim());

                var anchorText = WebUtility.HtmlDecode(anchor.InnerText);
                var ubicacionNode = anchor.SelectSingleNode(".//p[contains(text(),'Ubicación')]");
                if (ubicacionNode != null)
                    ubicacion = ubicacionNode.InnerText.Split(':', 2).Last().Trim();
                else
                {
                    var match = Regex.Match(anchorText, @"Ubicaci(?:\u00f3|o)n\s*:\s*([^\n]+)", RegexOptions.IgnoreCase);
                    if (match.Success) ubicacion = match.Groups[1].Value.Trim();
                }

                var estadoNode = anchor.SelectSingleNode(".//p[contains(text(),'Estado')]");
                if (estadoNode != null)
                    estado = estadoNode.InnerText.Split(':', 2).Last().Trim();
                else
                {
                    var match = Regex.Match(anchorText, @"Estado\s*:\s*([^\n]+)", RegexOptions.IgnoreCase);
                    if (match.Success) estado = match.Groups[1].Value.Trim();
                }

                return new SearchResultItem(rucNumber, razonSocial, ubicacion, estado);
            })
            ?? Enumerable.Empty<SearchResultItem>();
    }

    internal static Task<IReadOnlyList<SearchResultItem>> ParseListAsync(string html) =>
        Task.Run(() => (IReadOnlyList<SearchResultItem>)ParseList(html).ToList());
}
