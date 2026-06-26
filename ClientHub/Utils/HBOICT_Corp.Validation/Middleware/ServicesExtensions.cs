using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace WinterRose.Web.Validation.Middleware;

/// <summary>
/// Provides extension methods for registering validation services in the dependency injection container.
/// </summary>
public static class ServicesExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers all validation definitions and the generic validator in the dependency injection container.
        /// </summary>
        /// <remarks>Call this method during application startup to enable model validation via dependency injection.
        /// <br></br><br></br><br></br>
        /// You must call <see cref="UseValidation"/> on 'app' after building.
        /// </remarks>
        /// <returns>The current <see cref="IServiceCollection"/> instance with validation services registered.</returns>
        public IServiceCollection AddValidation()
        {
            services.AddHttpContextAccessor();

            // Suppress the default model state invalid filter to allow
            // this validation middleware to give a more detailed response.
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            services.AddControllers(options =>
            {
                options.Filters.Add<ValidationRequestFilter>();
                options.Filters.Add<ValidationResponseFilter>();
            });

            services.AddScoped<IValidationStore, ValidationStore>();
            services.AddScoped<TrackingJsonInputFormatter>();
            services.AddTransient<IConfigureOptions<MvcOptions>, ConfigureMvcOptions>();

            Type interfaceType = typeof(IValidationDefinition<>);
            Type[] types = TypeWorker.FindTypesWithInterface(interfaceType);

            services.AddSingleton<Validator>();
            services.AddScoped(typeof(Validation<>));

            foreach (Type type in types.Where(t => !t.IsAbstract && !t.IsInterface))
            {
                var definitionType = type.GetInterfaces()
                    .First(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IValidationDefinition<>));
                Type modelType = definitionType.GetGenericArguments()[0];
                services.AddSingleton(definitionType, type);
            }


            return services;
        }
    }

    extension (WebApplication app)
    {
        /// <summary>
        /// Configures the application to use validation middleware for handling query params model validation.
        /// </summary>
        public IApplicationBuilder UseValidation()
        {
            app.UseMiddleware<TrackingQueryMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                // this is used to catch 405 errors and return
                // a more informative response to the client.
                // normally it just returns an empty forbidden response,
                // which tells you the developer nothing about why its forbidden.
                // now it tells you that the method the call used is not allowed on this endpoint.
                // this code is only enabled in development, because in production you dont want to 
                // expose this information
                app.UseStatusCodePages(async context =>
                {
                    var response = context.HttpContext.Response;

                    if (response.StatusCode == StatusCodes.Status405MethodNotAllowed)
                    {
                        var usedMethod = context.HttpContext.Request.Method;
                        var endpoint = context.HttpContext.Request.Path;

                        var message = $"Endpoint '{endpoint}' does not support '{usedMethod}'.";

                        response.ContentType = "application/json";

                        await response.WriteAsJsonAsync(new
                        {
                            type = "METHOD_NOT_ALLOWED",
                            title = "Method Not Allowed",
                            detail = message
                        });
                    }
                });
            }

            return app;
        }
    }
}
