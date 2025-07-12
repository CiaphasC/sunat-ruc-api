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

// Habilita controladores MVC
builder.Services.AddControllers();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull);

var app = builder.Build();
Console.WriteLine($"Listening on port {port}");

// Usa controladores para manejar las rutas
app.MapControllers();

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
