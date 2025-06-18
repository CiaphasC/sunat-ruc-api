namespace SunatScraper.Core.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
public sealed record RucInfo(
    string? Ruc,
    string? RazonSocial,
    string? Estado,
    string? Condicion,
    string? Direccion,
    string? Ubicacion = null,
    [property: JsonPropertyName("documento")] string? Documento = null,
    [property: JsonPropertyName("contribuyente")] string? Contribuyente = null)
{
    public string ToJson() => JsonSerializer.Serialize(this,
        new JsonSerializerOptions {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
}
