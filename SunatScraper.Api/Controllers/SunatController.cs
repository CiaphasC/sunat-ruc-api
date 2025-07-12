using Microsoft.AspNetCore.Mvc;
using SunatScraper.Domain;

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

    [HttpGet("")]
    public string Index() => "SUNAT RUC API ok";

    [HttpGet("ruc/{ruc}")]
    public Task<IResult> GetByRuc(string ruc) =>
        ApiHelpers.Execute(async () => ApiHelpers.ToResult(await _client.GetByRucAsync(ruc)));

    [HttpGet("rucs")]
    public Task<IResult> GetByRucs([FromQuery(Name = "r")] string[] rucs) =>
        ApiHelpers.Execute(async () => Results.Json(await _client.GetByRucsAsync(rucs)));

    [HttpGet("doc/{tipo}/{numero}")]
    public Task<IResult> GetByDocument(string tipo, string numero) =>
        ApiHelpers.Execute(async () => ApiHelpers.ToResult(await _client.GetByDocumentAsync(tipo, numero)));

    [HttpGet("doc/{tipo}/{numero}/lista")]
    public Task<IResult> SearchByDocument(string tipo, string numero) =>
        ApiHelpers.Execute(async () => ApiHelpers.ToResult(await _client.SearchByDocumentAsync(tipo, numero)));

    [HttpGet("rs/lista")]
    public Task<IResult> SearchByNameList([FromQuery(Name = "q")] string razonSocial) =>
        ApiHelpers.Execute(async () => ApiHelpers.ToResult(await _client.SearchByNameAsync(razonSocial)));

    [HttpGet("rs")]
    public Task<IResult> GetByName([FromQuery(Name = "q")] string razonSocial) =>
        ApiHelpers.Execute(async () => ApiHelpers.ToResult(await _client.GetByNameAsync(razonSocial)));
}
