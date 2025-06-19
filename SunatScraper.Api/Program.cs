// Punto de entrada principal de la API.
// Aquí se configuran los servicios y se definen los endpoints HTTP.
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

// Consulta información detallada mediante número de RUC.
app.MapGet("/ruc/{ruc}", async ([FromServices] ISunatClient client, string ruc) =>
    Results.Json(await client.GetByRucAsync(ruc)));

// Búsqueda por tipo y número de documento de identidad.
app.MapGet("/doc/{tipo}/{numero}", async ([FromServices] ISunatClient client, string tipo, string numero) =>
    Results.Json(await client.GetByDocumentAsync(tipo, numero)));

// Devuelve la lista completa de coincidencias para un documento específico.
app.MapGet("/doc/{tipo}/{numero}/lista", async ([FromServices] ISunatClient client, string tipo, string numero) =>
    Results.Json(await client.SearchByDocumentAsync(tipo, numero)));

// Obtiene las coincidencias de razón social sin datos de ubicación.
app.MapGet("/rs/lista", async ([FromServices] ISunatClient client,
        [FromQuery(Name = "q")] string razonSocial) =>
    Results.Json(await client.SearchByNameAsync(razonSocial)));

// Consulta por razón social retornando ubicación cuando está disponible.
app.MapGet("/rs", async ([FromServices] ISunatClient client,
        [FromQuery(Name = "q")] string razonSocial) =>
    Results.Json(await client.GetByNameAsync(razonSocial)));
app.Run();
