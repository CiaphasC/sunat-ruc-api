using Microsoft.AspNetCore.Mvc;
using SunatScraper.Core.Services;
using Serilog;

var b=WebApplication.CreateBuilder(args);
b.Host.UseSerilog((ctx,lc)=>lc.WriteTo.Console());
b.Services.AddSingleton(_=>SunatClient.Create(b.Configuration["Redis"]));
var app=b.Build();

app.MapGet("/",()=> "SUNAT RUC API ok");
app.MapGet("/ruc/{r}",async([FromServices]SunatClient s,string r)=>Results.Json(await s.ByRucAsync(r)));
app.MapGet("/doc/{t}/{n}",async([FromServices]SunatClient s,string t,string n)=>Results.Json(await s.ByDocumentoAsync(t,n)));
app.MapGet("/rs",async([FromServices]SunatClient s,[FromQuery]string q)=>Results.Json(await s.ByRazonAsync(q)));
app.Run();
