using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Dtos.Common;
using Shared.Exceptions;

namespace Infrastructure.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(x => x.PropertyName ?? string.Empty)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).Distinct().ToArray());

            if (errors.Count == 0)
            {
                errors[string.Empty] = [ex.Message];
            }

            var response = new ValidationProblemDetails(errors)
            {
                Type = "https://httpstatuses.com/400",
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more validation errors occurred.",
                Instance = context.Request.Path
            };

            logger.LogInformation(
                ex,
                "Validation failed. Path: {Path}, TraceId: {TraceId}",
                context.Request.Path,
                context.TraceIdentifier);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
        }
        catch (ApplicationExceptionBase ex)
        {
            var response = new ApiErrorResponse
            {
                Type = ex.Type,
                Title = ex.Title,
                Status = ex.StatusCode,
                Detail = ex.Message,
                TraceId = context.TraceIdentifier,
                RequiresVerification = ex is EmailVerificationRequiredException ? true : null,
                NextAction = ex is EmailVerificationRequiredException ? "verify_email" : null
            };

            logger.LogInformation(
                ex,
                "Application exception handled. Path: {Path}, TraceId: {TraceId}, Status: {Status}",
                context.Request.Path,
                context.TraceIdentifier,
                ex.StatusCode);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = ex.StatusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException postgresException)
        {
            var mapped = MapPostgresException(postgresException, context.TraceIdentifier);

            logger.LogInformation(
                ex,
                "Database update exception mapped. SqlState: {SqlState}, Status: {Status}, TraceId: {TraceId}",
                postgresException.SqlState,
                mapped.Status,
                context.TraceIdentifier);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = mapped.Status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(mapped, SerializerOptions));
        }
        catch (PostgresException ex)
        {
            var mapped = MapPostgresException(ex, context.TraceIdentifier);

            logger.LogInformation(
                ex,
                "PostgreSQL exception mapped. SqlState: {SqlState}, Status: {Status}, TraceId: {TraceId}",
                ex.SqlState,
                mapped.Status,
                context.TraceIdentifier);

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = mapped.Status;
            await context.Response.WriteAsync(JsonSerializer.Serialize(mapped, SerializerOptions));
        }
        catch (Exception ex)
        {
            logger.LogInformation(ex, "Unhandled exception occurred while processing request.");

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            var response = new ApiErrorResponse
            {
                Type = "https://httpstatuses.com/500",
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = environment.IsDevelopment() ? ex.Message : "An unexpected error occurred.",
                TraceId = context.TraceIdentifier
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
        }
    }

    private static ApiErrorResponse MapPostgresException(PostgresException ex, string traceId)
    {
        return ex.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => new ApiErrorResponse
            {
                Type = "https://httpstatuses.com/409",
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = "A record with the same unique value already exists.",
                TraceId = traceId
            },
            PostgresErrorCodes.ForeignKeyViolation => new ApiErrorResponse
            {
                Type = "https://httpstatuses.com/409",
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = "Related data constraint failed.",
                TraceId = traceId
            },
            PostgresErrorCodes.NotNullViolation => new ApiErrorResponse
            {
                Type = "https://httpstatuses.com/400",
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "Required data is missing.",
                TraceId = traceId
            },
            PostgresErrorCodes.StringDataRightTruncation => new ApiErrorResponse
            {
                Type = "https://httpstatuses.com/400",
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "One or more values exceed allowed length.",
                TraceId = traceId
            },
            _ => new ApiErrorResponse
            {
                Type = "https://httpstatuses.com/500",
                Title = "Internal Server Error",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "A database error occurred.",
                TraceId = traceId
            }
        };
    }
}
