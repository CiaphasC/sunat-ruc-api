namespace SunatScraper.Core.Models;
using System.Text.Json;
public sealed record SearchResultItem(
    string? Ruc,
    string? RazonSocial,
    string? Ubicacion,
    string? Estado)
{
    public string ToJson() => JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
}
