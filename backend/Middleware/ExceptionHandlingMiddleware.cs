using System.Net;
using System.Text.Json;
using FluentValidation;
using PatientSyncHealth.Domain.Exceptions;
using PatientSyncHealth.Middleware.Exceptions;

namespace PatientSyncHealth.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        var (statusCode, response) = exception switch
        {
            NotFoundException ex => (
                HttpStatusCode.NotFound,
                new ErrorResponse("Not Found", ex.Message)),

            BusinessException ex => (
                HttpStatusCode.BadRequest,
                new ErrorResponse("Business Rule Violation", ex.Message)),

            DomainException ex => (
                HttpStatusCode.BadRequest,
                new ErrorResponse("Domain Rule Violation", ex.Message)),

            ValidationException ex => (
                HttpStatusCode.BadRequest,
                new ErrorResponse("Validation Failed", "One or more validation errors occurred.",
                    ex.Errors.Select(e => new ValidationError(e.PropertyName, e.ErrorMessage)).ToList())),

            ForbiddenException ex => (
                HttpStatusCode.Forbidden,
                new ErrorResponse("Forbidden", ex.Message)),

            _ => (
                HttpStatusCode.InternalServerError,
                new ErrorResponse("Internal Server Error", "An unexpected error occurred."))
        };

        // Log the exception
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred");
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception: {ExceptionType}", exception.GetType().Name);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }
}

public record ErrorResponse(
    string Title,
    string Detail,
    List<ValidationError>? Errors = null);

public record ValidationError(
    string Property,
    string Message);

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
