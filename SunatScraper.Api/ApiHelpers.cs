using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SunatScraper.Domain.Models;
using System.Net;

internal static class ApiHelpers
{
    internal record ApiError(string Mensaje);

    internal static IResult ToResult(RucInfo info) =>
        string.IsNullOrWhiteSpace(info.Ruc)
            ? Results.Json(new ApiError("Registro no encontrado"),
                statusCode: StatusCodes.Status404NotFound)
            : Results.Json(info);

    internal static IResult ToResult<T>(IReadOnlyList<T> list) =>
        list.Count == 0
            ? Results.Json(new ApiError("Registro no encontrado"),
                statusCode: StatusCodes.Status404NotFound)
            : Results.Json(list);

    internal static async Task<IResult> Execute(Func<Task<IResult>> action)
    {
        try
        {
            return await action();
        }
        catch (ArgumentException ex)
        {
            return Results.Json(new ApiError(ex.Message),
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (HttpRequestException ex)
        {
            string msg = ex.StatusCode switch
            {
                HttpStatusCode.ServiceUnavailable => "Portal de SUNAT no disponible (503)",
                HttpStatusCode.GatewayTimeout => "Tiempo de espera agotado (504)",
                _ => "No se obtuvo respuesta del portal de SUNAT"
            };
            return Results.Json(new ApiError(msg),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
