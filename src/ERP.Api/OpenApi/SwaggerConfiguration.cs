using ERP.Api.Extensions;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ERP.Api.OpenApi;

/// <summary>
/// Adds an Accept-Language selector to every operation in Swagger UI, so the
/// localized responses can be exercised without a REST client.
/// </summary>
public sealed class AcceptLanguageHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Accept-Language",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Response language.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Default = new OpenApiString(PresentationServices.DefaultCulture),
                Enum = [.. PresentationServices.SupportedCultures.Select(c => (IOpenApiAny)new OpenApiString(c))]
            }
        });
    }
}
