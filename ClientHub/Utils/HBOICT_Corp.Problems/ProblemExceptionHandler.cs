using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Collections;
using System.Diagnostics;

namespace WinterRose.Web.Problems;

/// <summary>
/// Handles exceptions of type APIProblem by generating and writing a standardized problem details response to the HTTP
/// context.
/// </summary>
/// <remarks>Only <see cref="ApiProblem"/> exceptions are handled by this class</remarks>
public class ProblemExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IWebHostEnvironment env) : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetailsService = problemDetailsService;
    private readonly IWebHostEnvironment env = env;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var result = ProblemDetailsFactory.Build(
            exception,
            httpContext,
            env);

        return await problemDetailsService.TryWriteAsync(result);
    }
}

public static class ProblemDetailsFactory
{
    public static ProblemDetailsContext Build(
        Exception exception,
        HttpContext? httpContext = null,
        IWebHostEnvironment? env = null)
    {
        ProblemDetails problemDetails;

        if (exception is ApiProblem apiProblem)
        {
            problemDetails = BuildApiProblem(apiProblem);

            if (env?.IsDevelopment() == true)
            {
                problemDetails.Extensions["debug"] = new Dictionary<string, object>
                {
                    ["exceptionInfo"] = ExceptionFormatting.BuildException(exception)
                };
            }

            if (httpContext != null)
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        }
        else
        {
            problemDetails = BuildGenericProblem(exception, env);

            if (env?.IsDevelopment() == true)
            {
                problemDetails.Extensions["debug"] = new Dictionary<string, object>
                {
                    ["exceptionInfo"] = ExceptionFormatting.BuildException(exception)
                };
            }

            if (httpContext != null)
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }

        return new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        };
    }

    private static ProblemDetails BuildGenericProblem(
        Exception exception,
        IWebHostEnvironment? env)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError
        };

        if (env?.IsDevelopment() == true)
        {
            var info = ExceptionFormatting.GetThrowLocation(exception);

            problem.Title =
                $"A {exception.GetType().Name} was thrown at {info.file}:{info.line} in {info.member}";

            problem.Detail = exception.Message;
        }

        return problem;
    }

    private static ProblemDetails BuildApiProblem(ApiProblem apiProblem)
    {
        var problem = new ProblemDetails
        {
            Title = apiProblem.Error,
            Detail = apiProblem.Message,
            Status = StatusCodes.Status400BadRequest
        };

        if (apiProblem.Details is { Count: > 0 })
        {
            var detailMap = new Dictionary<string, List<object>>();

            foreach (var detail in apiProblem.Details)
            {
                var type = detail.Type ?? "general";

                if (!detailMap.TryGetValue(type, out var list))
                {
                    list = new List<object>();
                    detailMap[type] = list;
                }

                var issue = detail.Issue;
                if (issue is not string and System.Collections.IEnumerable enumerable)
                {
                    foreach (var item in enumerable)
                        list.Add(item);
                }
                else
                {
                    list.Add(issue ?? "unknown issue");
                }
            }

            problem.Extensions["issues"] = detailMap;
        }

        return problem;
    }
}

public static class ExceptionFormatting
{
    public static object BuildException(Exception ex, bool includeStackTrace = true)
    {
        if (ex is AggregateException agg)
        {
            return new
            {
                type = ex.GetType().FullName,
                message = ex.Message,
                inner = agg.InnerExceptions
                    .Select(e => BuildException(e, true))
                    .ToArray()
            };
        }

        return new
        {
            type = ex.GetType().FullName,
            message = ex.Message,
            stackTrace = includeStackTrace
                ? TrimStackTrace(ex.StackTrace ?? "", 5)
                : null,
            inner = ex.InnerException is null
                ? null
                : BuildException(ex.InnerException, false)
        };

        static string[] TrimStackTrace(string stackTrace, int maxLines)
        {
            if (string.IsNullOrWhiteSpace(stackTrace))
                return Array.Empty<string>();

            var lines = stackTrace
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            var newLines = lines
                .Take(maxLines)
                .Select(line => line.Trim())
                .ToArray();

            if (lines.Length > maxLines)
                newLines = newLines
                    .Append($"{lines.Length - maxLines} stacktrace lines hidden for brevity")
                    .ToArray();

            return newLines;
        }
    }

    public static (string? file, int? line, string? member) GetThrowLocation(Exception exception)
    {
        if (exception == null)
            return (null, null, null);

        var stackTrace = new StackTrace(exception, true);
        var frames = stackTrace.GetFrames();

        if (frames == null || frames.Length == 0)
            return (null, null, null);

        const string SOLUTION_ROOT_MARKER = "hbo-ict-corp";

        foreach (var frame in frames)
        {
            var method = frame.GetMethod();
            if (method == null)
                continue;

            var file = frame.GetFileName();
            var line = frame.GetFileLineNumber();

            if (string.IsNullOrEmpty(file))
                continue;

            var normalizedFile = file.Replace('\\', '/');

            var markerIndex = normalizedFile.IndexOf(
                SOLUTION_ROOT_MARKER,
                StringComparison.OrdinalIgnoreCase);

            if (markerIndex >= 0)
                normalizedFile = normalizedFile[markerIndex..];

            return (normalizedFile, line, method.DeclaringType?.FullName + "." + method.Name);
        }

        return (null, null, null);
    }
}