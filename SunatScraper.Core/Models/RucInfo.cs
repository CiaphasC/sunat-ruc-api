namespace SunatScraper.Core.Models;
using System.Text.Json;
public sealed record RucInfo(
    string? Ruc,
    string? RazonSocial,
    string? Estado,
    string? Condicion,
    string? Direccion)
{
    public string ToJson() => JsonSerializer.Serialize(this,
        new JsonSerializerOptions { WriteIndented = true });
}
