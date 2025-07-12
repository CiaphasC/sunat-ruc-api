using Microsoft.AspNetCore.Mvc;
using SunatScraper.Domain;
using SunatScraper.Domain.Models;

namespace SunatScraper.Api.Controllers;

[ApiController]
[Route("")]
public sealed class SunatController : ControllerBase
{
    private readonly ISunatClient _client;

    public SunatController(ISunatClient client)
    {
        _client = client;
    }

    [HttpGet]
    public string Root() => "SUNAT RUC API ok";

    [HttpGet("ruc/{ruc}")]
    public async Task<ActionResult<RucInfo>> GetByRuc(string ruc)
    {
        try
        {
            var info = await _client.GetByRucAsync(ruc);
            if (string.IsNullOrWhiteSpace(info.Ruc))
                return NotFound(new { Mensaje = "Registro no encontrado" });
            return Ok(info);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { Mensaje = "No se obtuvo respuesta del portal de SUNAT" });
        }
    }

    [HttpGet("rucs")]
    public async Task<ActionResult<IReadOnlyList<RucInfo>>> GetByRucs([FromQuery(Name="r")] string[] rucs)
    {
        try
        {
            var info = await _client.GetByRucsAsync(rucs);
            return Ok(info);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { Mensaje = "No se obtuvo respuesta del portal de SUNAT" });
        }
    }

    [HttpGet("doc/{tipo}/{numero}")]
    public async Task<ActionResult<RucInfo>> GetByDocument(string tipo, string numero)
    {
        try
        {
            var info = await _client.GetByDocumentAsync(tipo, numero);
            if (string.IsNullOrWhiteSpace(info.Ruc))
                return NotFound(new { Mensaje = "Registro no encontrado" });
            return Ok(info);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { Mensaje = "No se obtuvo respuesta del portal de SUNAT" });
        }
    }

    [HttpGet("doc/{tipo}/{numero}/lista")]
    public async Task<ActionResult<IReadOnlyList<SearchResultItem>>> SearchByDocument(string tipo, string numero)
    {
        try
        {
            var list = await _client.SearchByDocumentAsync(tipo, numero);
            if (list.Count == 0)
                return NotFound(new { Mensaje = "Registro no encontrado" });
            return Ok(list);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { Mensaje = "No se obtuvo respuesta del portal de SUNAT" });
        }
    }

    [HttpGet("rs/lista")]
    public async Task<ActionResult<IReadOnlyList<SearchResultItem>>> SearchByName([FromQuery(Name="q")] string razonSocial)
    {
        try
        {
            var list = await _client.SearchByNameAsync(razonSocial);
            if (list.Count == 0)
                return NotFound(new { Mensaje = "Registro no encontrado" });
            return Ok(list);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { Mensaje = "No se obtuvo respuesta del portal de SUNAT" });
        }
    }

    [HttpGet("rs")]
    public async Task<ActionResult<RucInfo>> GetByName([FromQuery(Name="q")] string razonSocial)
    {
        try
        {
            var info = await _client.GetByNameAsync(razonSocial);
            if (string.IsNullOrWhiteSpace(info.Ruc))
                return NotFound(new { Mensaje = "Registro no encontrado" });
            return Ok(info);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Mensaje = ex.Message });
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { Mensaje = "No se obtuvo respuesta del portal de SUNAT" });
        }
    }
}
