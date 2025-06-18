using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Json;
using System.Text.Json.Serialization;
using SunatScraper.Infrastructure.Services;
using SunatScraper.Domain;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console());

builder.Services.AddSingleton<ISunatClient>(_ =>
    SunatClient.Create(builder.Configuration["Redis"]));

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var app = builder.Build();

app.MapGet("/", () => "SUNAT RUC API ok");

app.MapGet("/ruc/{ruc}", async ([FromServices] ISunatClient client, string ruc) =>
    Results.Json(await client.ByRucAsync(ruc)));

app.MapGet("/doc/{tipo}/{numero}", async ([FromServices] ISunatClient client, string tipo, string numero) =>
    Results.Json(await client.ByDocumentoAsync(tipo, numero)));

app.MapGet("/doc/{tipo}/{numero}/lista", async ([FromServices] ISunatClient client, string tipo, string numero) =>
    Results.Json(await client.SearchDocumentoAsync(tipo, numero)));

app.MapGet("/rs/lista", async ([FromServices] ISunatClient client, [FromQuery] string razonSocial) =>
    Results.Json(await client.SearchRazonAsync(razonSocial)));

app.MapGet("/rs", async ([FromServices] ISunatClient client, [FromQuery] string razonSocial) =>
    Results.Json(await client.ByRazonAsync(razonSocial)));
app.Run();
