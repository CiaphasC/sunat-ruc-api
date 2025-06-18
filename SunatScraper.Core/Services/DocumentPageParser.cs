namespace SunatScraper.Core.Services;
using SunatScraper.Core.Models;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;

internal static class DocumentPageParser
{
    internal static RucInfo Parse(string html)
    {
        var plain = WebUtility.HtmlDecode(html);

        string? ruc = Regex.Match(plain, @"\b(\d{11})\b").Groups[1].Value;
        if (string.IsNullOrWhiteSpace(ruc)) ruc = null;

        string? razon = Regex.Match(plain, @"\b\d{11}\s*-\s*([^\n]+)").Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(razon)) razon = null;

        string? estado = Regex.Match(plain, @"Estado\s*:\s*([^\n]+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(estado)) estado = null;

        string? condicion = Regex.Match(plain, @"Condici(?:\u00f3|o)n\s*:\s*([^\n]+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(condicion)) condicion = null;

        string? direccion = Regex.Match(plain, @"Direcci(?:\u00f3|o)n\s*:\s*([^\n]+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(direccion)) direccion = null;

        string? ubic = Regex.Match(plain, @"Ubicaci(?:\u00f3|o)n\s*:\s*([^\n]+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(ubic)) ubic = null;

        string? documento = Regex.Match(plain, @"Tipo de Documento\s*:\s*([^\n]+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(documento)) documento = null;

        string? contribuyente = Regex.Match(plain, @"Tipo Contribuyente\s*:\s*([^\n]+)", RegexOptions.IgnoreCase).Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(contribuyente)) contribuyente = null;

        return new RucInfo(
            ruc,
            razon,
            estado,
            condicion,
            direccion,
            ubic,
            documento,
            contribuyente);
    }

    internal static IEnumerable<SearchResultItem> ParseList(string html)
    {
        return RucPageParser.ParseList(html);
    }
}
