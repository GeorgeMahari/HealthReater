using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HealthRater.Api.Swagger;

/// <summary>
/// Marks operations that require a signed-in user (controller- or action-level
/// <see cref="AuthorizeAttribute"/>) with the session-cookie requirement and a 401 response,
/// so Swagger UI shows which endpoints need a login first.
/// </summary>
public class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var requiresAuth = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();
        if (!requiresAuth) return;

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Not signed in, or the session has expired." });
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "sessionCookie" },
            }] = Array.Empty<string>(),
        });
    }
}
