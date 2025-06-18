/// <summary>
/// Representa un elemento en los resultados de búsqueda de la SUNAT.
/// </summary>
namespace SunatScraper.Domain.Models;
using System.Text.Json;
public sealed record SearchResultItem(
    string? Ruc,
    string? RazonSocial,
    string? Ubicacion,
    string? Estado)
{
    /// <summary>
    /// Convierte la instancia a JSON con formato legible.
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(this,
        new JsonSerializerOptions { WriteIndented = true });
}
