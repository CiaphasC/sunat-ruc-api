/// <summary>
/// Facilita la elección del parser adecuado para cada tipo de consulta.
/// </summary>
namespace SunatScraper.Infrastructure.Services;
using SunatScraper.Domain.Models;
using System.Collections.Generic;

public static class RucParser
{
    /// <summary>
    /// Devuelve la información de un RUC a partir del HTML obtenido.
    /// </summary>
    public static RucInfo Parse(string html, bool porDocumento = false)
    {
        return porDocumento ? DocumentPageParser.Parse(html) : RucPageParser.Parse(html);
    }

    /// <summary>
    /// Obtiene la lista de resultados encontrados en el HTML.
    /// </summary>
    public static IEnumerable<SearchResultItem> ParseList(string html, bool porDocumento = false)
    {
        return porDocumento ? DocumentPageParser.ParseList(html) : RucPageParser.ParseList(html);
    }
}
