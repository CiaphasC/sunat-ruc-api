namespace SunatScraper.Infrastructure.Services;

using HtmlAgilityPack;
using SunatScraper.Domain.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

/// <summary>
/// Funciones compartidas para analizar HTML de resultados SUNAT.
/// </summary>
internal static class HtmlParserCommon
{
    static Dictionary<string, string> BuildMap(HtmlDocument document)
    {
        var labelValueMap = document.DocumentNode
            .SelectNodes("//td[@class='bgn']")
            ?.Select(labelCell =>
            {
                var valueNode = labelCell.NextSibling;
                while (valueNode is { Name: not "td" })
                    valueNode = valueNode.NextSibling;
                return new KeyValuePair<string, string>(
                    labelCell.InnerText.Trim(),
                    WebUtility.HtmlDecode(valueNode?.InnerText.Trim() ?? string.Empty));
            })
            .ToDictionary(k => k.Key, v => v.Value)
            ?? new Dictionary<string, string>();

        if (labelValueMap.Count == 0)
        {
            foreach (var item in document.DocumentNode
                        .SelectNodes("//div[contains(@class,'list-group-item')]")
                        ?? Enumerable.Empty<HtmlNode>())
            {
                var itemText = WebUtility.HtmlDecode(item.InnerText.Trim());
                int colonIndex = itemText.IndexOf(':');
                if (colonIndex > 0)
                    labelValueMap[itemText[..colonIndex].Trim()] = itemText[(colonIndex + 1)..].Trim();
            }
        }
        return labelValueMap;
    }

    static string? GetValue(IDictionary<string, string> map, string label)
    {
        static string Normalize(string s)
        {
            var form = s.Normalize(NormalizationForm.FormD);
            Span<char> buffer = stackalloc char[form.Length];
            int bufferIndex = 0;
            foreach (var c in form)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    buffer[bufferIndex++] = char.ToLowerInvariant(c);
            return new string(buffer[..bufferIndex]);
        }

        var normLabel = Normalize(label);
        foreach (var (currentLabel, value) in map)
        {
            if (currentLabel.Contains(label, StringComparison.OrdinalIgnoreCase) ||
                Normalize(currentLabel).Contains(normLabel, StringComparison.OrdinalIgnoreCase))
                return value;
        }
        return null;
    }

    internal static Task<RucInfo> ParseRucInfoAsync(string html) =>
        Task.Run(() => ParseRucInfo(html));

    internal static RucInfo ParseRucInfo(string html)
    {
        var document = new HtmlDocument();
        document.LoadHtml(html);

        var infoMap = BuildMap(document);
        var plainText = WebUtility.HtmlDecode(document.DocumentNode.InnerText);

        string? rucNumber = null, razonSocial = null;

        var rucLine = GetValue(infoMap, "RUC");
        if (rucLine != null)
        {
            var m = Regex.Match(rucLine, "\\d{11}");
            if (m.Success)
            {
                rucNumber = m.Value;
                var trailingText = rucLine[(m.Index + m.Length)..].TrimStart('-', ' ').Trim();
                razonSocial = string.IsNullOrEmpty(trailingText) ? null : trailingText;
            }
            else
                rucNumber = rucLine.Trim();
        }

        razonSocial ??= GetValue(infoMap, "Razón") ?? GetValue(infoMap, "Nombre");

        if (string.IsNullOrWhiteSpace(rucNumber))
        {
            var m = Regex.Match(plainText, "\\b(\\d{11})\\b");
            if (m.Success) rucNumber = m.Groups[1].Value;
        }
        if (string.IsNullOrWhiteSpace(razonSocial) || razonSocial == "-")
        {
            var m = Regex.Match(plainText, "\\b\\d{11}\\s*-\\s*([^\\n]+)");
            if (m.Success) razonSocial = m.Groups[1].Value.Trim();
        }

        var estado = GetValue(infoMap, "Estado") ??
                     Regex.Match(plainText, "Estado\\s*:\\s*([^\\n]+)", RegexOptions.IgnoreCase)
                         .Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(estado)) estado = null;

        var condicion = GetValue(infoMap, "Condición") ??
                        Regex.Match(plainText, "Condici(?:\\u00f3|o)n\\s*:\\s*([^\\n]+)", RegexOptions.IgnoreCase)
                            .Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(condicion)) condicion = null;

        var direccion = GetValue(infoMap, "Dirección") ?? GetValue(infoMap, "Domicilio");
        if (string.IsNullOrWhiteSpace(direccion))
        {
            var m = Regex.Match(plainText, "Direcci(?:\\u00f3|o)n\\s*:\\s*([^\\n]+)", RegexOptions.IgnoreCase);
            if (m.Success) direccion = m.Groups[1].Value.Trim();
        }
        if (string.IsNullOrWhiteSpace(direccion)) direccion = null;

        var ubicacion = GetValue(infoMap, "Ubicación") ??
                        Regex.Match(plainText, "Ubicaci(?:\\u00f3|o)n\\s*:\\s*([^\\n]+)", RegexOptions.IgnoreCase)
                       .Groups[1].Value.Trim();
        if (string.IsNullOrWhiteSpace(ubicacion)) ubicacion = null;

        var tipoDocumento = GetValue(infoMap, "Tipo de Documento");
        if (tipoDocumento == null)
        {
            var m = Regex.Match(plainText, "Tipo de Documento\\s*:\\s*([^\\n]+)", RegexOptions.IgnoreCase);
            if (m.Success) tipoDocumento = m.Groups[1].Value.Trim();
        }
        if (tipoDocumento != null)
        {
            int dash = tipoDocumento.IndexOf('-');
            tipoDocumento = (dash > 0 ? tipoDocumento[..dash] : tipoDocumento).Trim();
        }

        var tipoContribuyente = GetValue(infoMap, "Tipo Contribuyente");
        if (tipoContribuyente == null)
        {
            var m = Regex.Match(plainText, "Tipo Contribuyente\\s*:\\s*([^\\n]+)", RegexOptions.IgnoreCase);
            if (m.Success) tipoContribuyente = m.Groups[1].Value.Trim();
        }
        if (tipoContribuyente != null)
        {
            int dash = tipoContribuyente.IndexOf('-');
            tipoContribuyente = (dash > 0 ? tipoContribuyente[..dash] : tipoContribuyente).Trim();
        }

        return new RucInfo(
            rucNumber,
            razonSocial,
            estado,
            condicion,
            direccion,
            ubicacion,
            tipoDocumento,
            tipoContribuyente);
    }
}
