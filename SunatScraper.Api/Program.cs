// Punto de entrada principal de la API.
// Aquí se configuran los servicios y se definen los endpoints HTTP.
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using SunatScraper.Domain;
using SunatScraper.Infrastructure.Services;
using System.Text.Json.Serialization;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

int port = builder.Configuration.GetValue<int?>("PORT")
    ?? GetAvailablePort(5000);
builder.WebHost.ConfigureKestrel(o => o.ListenAnyIP(port));

builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console());

// Configuración de servicios
builder.Services.AddSingleton<ISunatClient>(_ =>
    SunatClient.Create(builder.Configuration["Redis"]));

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var app = builder.Build();
Console.WriteLine($"Listening on port {port}");

// Endpoints HTTP
app.MapGet("/", () => "SUNAT RUC API ok");

// Consulta información detallada mediante número de RUC.
app.MapGet(
    "/ruc/{ruc}",
    ([FromServices] ISunatClient client, string ruc) =>
        ApiHelpers.Execute(async () => ApiHelpers.ToResult(await client.GetByRucAsync(ruc))));

// Consulta múltiples RUCs en paralelo.
app.MapGet(
    "/rucs",
    ([FromServices] ISunatClient client, [FromQuery(Name = "r")] string[] rucs) =>
        ApiHelpers.Execute(async () => Results.Json(await client.GetByRucsAsync(rucs))));

// Búsqueda por tipo y número de documento de identidad.
app.MapGet(
    "/doc/{tipo}/{numero}",
    ([FromServices] ISunatClient client, string tipo, string numero) =>
        ApiHelpers.Execute(async () => ApiHelpers.ToResult(await client.GetByDocumentAsync(tipo, numero))));

// Devuelve la lista completa de coincidencias para un documento específico.
app.MapGet(
    "/doc/{tipo}/{numero}/lista",
    ([FromServices] ISunatClient client, string tipo, string numero) =>
        ApiHelpers.Execute(async () => ApiHelpers.ToResult(await client.SearchByDocumentAsync(tipo, numero))));

// Obtiene las coincidencias de razón social sin datos de ubicación.
app.MapGet(
    "/rs/lista",
    ([FromServices] ISunatClient client, [FromQuery(Name = "q")] string razonSocial) =>
        ApiHelpers.Execute(async () => ApiHelpers.ToResult(await client.SearchByNameAsync(razonSocial))));

// Consulta por razón social retornando ubicación cuando está disponible.
app.MapGet(
    "/rs",
    ([FromServices] ISunatClient client, [FromQuery(Name = "q")] string razonSocial) =>
        ApiHelpers.Execute(async () => ApiHelpers.ToResult(await client.GetByNameAsync(razonSocial))));

app.Run();

static int GetAvailablePort(int start)
{
    for (int p = start; p < start + 20; p++)
    {
        try
        {
            var test = new TcpListener(IPAddress.Loopback, p);
            test.Start();
            test.Stop();
            return p;
        }
        catch (SocketException)
        {
            // puerto en uso
        }
    }

    var fallback = new TcpListener(IPAddress.Loopback, 0);
    fallback.Start();
    int port = ((IPEndPoint)fallback.LocalEndpoint).Port;
    fallback.Stop();
    return port;
}
