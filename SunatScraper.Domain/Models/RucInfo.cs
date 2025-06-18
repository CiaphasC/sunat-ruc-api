/// <summary>
/// Modelo que representa la información detallada de un RUC consultado.
/// </summary>
namespace SunatScraper.Domain.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
public sealed record RucInfo(
    string? Ruc,
    [property: JsonPropertyName("razonsocial")] string? RazonSocial,
    string? Estado,
    [property: JsonPropertyName("condicion")] string? Condicion,
    string? Direccion,
    string? Ubicacion = null,
    [property: JsonPropertyName("documento")] string? Documento = null,
    [property: JsonPropertyName("contribuyente")] string? Contribuyente = null)
{
    /// <summary>
    /// Serializa el modelo a formato JSON con identación.
    /// </summary>
    public string ToJson() => JsonSerializer.Serialize(this,
        new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
}
