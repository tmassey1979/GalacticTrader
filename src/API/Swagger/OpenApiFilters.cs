namespace GalacticTrader.API.Swagger;

using GalacticTrader.API.Contracts;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

public sealed class DefaultErrorResponsesOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Responses ??= new OpenApiResponses();

        AddIfMissing(operation.Responses, "500", "Unexpected server error.");

        var method = context.ApiDescription.HttpMethod;
        if (method is "POST" or "PUT" or "PATCH" or "DELETE")
        {
            AddIfMissing(operation.Responses, "400", "Invalid request payload or domain validation failed.");
        }

        if (context.ApiDescription.RelativePath?.Contains("auth", StringComparison.OrdinalIgnoreCase) == true)
        {
            AddIfMissing(operation.Responses, "401", "Authentication failed or token is invalid.");
            AddIfMissing(operation.Responses, "409", "Registration conflict for duplicate account identity.");
        }
        else
        {
            AddIfMissing(operation.Responses, "404", "Requested resource was not found.");
        }
    }

    private static void AddIfMissing(OpenApiResponses responses, string statusCode, string description)
    {
        if (!responses.ContainsKey(statusCode))
        {
            responses[statusCode] = new OpenApiResponse { Description = description };
        }
    }
}

public sealed class SwaggerExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        schema.Example = context.Type.Name switch
        {
            nameof(RegisterPlayerApiRequest) => new OpenApiObject
            {
                ["username"] = new OpenApiString("captain_hera"),
                ["email"] = new OpenApiString("hera@galactictrader.test"),
                ["password"] = new OpenApiString("WarpDrive9000")
            },
            nameof(LoginPlayerApiRequest) => new OpenApiObject
            {
                ["username"] = new OpenApiString("captain_hera"),
                ["password"] = new OpenApiString("WarpDrive9000")
            },
            nameof(CreateSectorRequest) => new OpenApiObject
            {
                ["name"] = new OpenApiString("Orion Hub"),
                ["x"] = new OpenApiDouble(12.5),
                ["y"] = new OpenApiDouble(-4.2),
                ["z"] = new OpenApiDouble(89.9)
            },
            nameof(CreateRouteRequest) => new OpenApiObject
            {
                ["fromSectorId"] = new OpenApiString("66ec3a2d-9efd-4e76-b84d-cf4f7d95f8e9"),
                ["toSectorId"] = new OpenApiString("0f660267-7fca-4912-a457-35ec08af9de9"),
                ["legalStatus"] = new OpenApiString("Legal"),
                ["warpGateType"] = new OpenApiString("Stable")
            },
            _ => schema.Example
        };
    }
}
