using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinterRose.Web.Problems;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddGracefulProblemDetails()
        {
            services.AddExceptionHandler<ProblemExceptionHandler>();

            services.AddProblemDetails(options =>
             {
                 options.CustomizeProblemDetails = context =>
                 {
                     context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                     context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);

                     var activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
                     context.ProblemDetails.Extensions.TryAdd("traceId", activity?.TraceId.ToString() ?? "traceID could not be provided");

                         Endpoint? endpoint = context.HttpContext.GetEndpoint();
                         var action = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();

                     string? source =
                            action is null
                                ? null
                                : $"{action.ControllerName}.{action.ActionName}";

                     if(source is not null)
                        context.ProblemDetails.Extensions["source"] = source;
                 };
             });
            return services;
        }
    }
}
