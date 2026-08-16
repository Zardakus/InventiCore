using System.Net;
using System.Text.Json;
using FluentValidation;
using InventiCore.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace InventiCore.Api.Middleware;

/// <summary>
/// Middleware global que captura exceções não tratadas e retorna respostas
/// padronizadas usando ProblemDetails (RFC 7807).
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = exception switch
        {
            ValidationException validationEx => CreateValidationProblemDetails(validationEx),
            KeyNotFoundException notFoundEx => CreateProblemDetails(
                HttpStatusCode.NotFound,
                "Recurso não encontrado",
                notFoundEx.Message),
            InsufficientStockException stockEx => CreateProblemDetails(
                HttpStatusCode.Conflict,
                "Estoque insuficiente",
                stockEx.Message),
            InvalidOperationException invalidOpEx => CreateProblemDetails(
                HttpStatusCode.Conflict,
                "Operação inválida",
                invalidOpEx.Message),
            _ => CreateProblemDetails(
                HttpStatusCode.InternalServerError,
                "Erro interno do servidor",
                "Ocorreu um erro inesperado. Tente novamente mais tarde.")
        };

        // Log contextualizado por severidade
        if (exception is ValidationException or KeyNotFoundException)
        {
            _logger.LogWarning(exception, "Exceção de negócio: {Message}", exception.Message);
        }
        else
        {
            _logger.LogError(exception, "Exceção não tratada: {Message}", exception.Message);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static ProblemDetails CreateProblemDetails(HttpStatusCode statusCode, string title, string detail)
    {
        return new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail
        };
    }

    private static ProblemDetails CreateValidationProblemDetails(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = (int)HttpStatusCode.BadRequest,
            Title = "Erro de validação",
            Detail = "Um ou mais erros de validação ocorreram."
        };
    }
}
