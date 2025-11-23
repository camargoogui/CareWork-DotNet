using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using CareWork.API.Models.DTOs;

namespace CareWork.API.Swagger;

public class ExampleSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Exemplo para UpdateCheckinDto
        if (context.Type == typeof(UpdateCheckinDto))
        {
            schema.Example = new OpenApiObject
            {
                ["mood"] = new OpenApiInteger(4),
                ["stress"] = new OpenApiInteger(2),
                ["sleep"] = new OpenApiInteger(5),
                ["notes"] = new OpenApiString("Dia produtivo"),
                ["tags"] = new OpenApiArray
                {
                    new OpenApiString("trabalho"),
                    new OpenApiString("produtivo")
                }
            };
        }

        // Exemplo para UpdateTipDto
        if (context.Type == typeof(UpdateTipDto))
        {
            schema.Example = new OpenApiObject
            {
                ["title"] = new OpenApiString("Título da Dica"),
                ["description"] = new OpenApiString("Descrição da dica"),
                ["category"] = new OpenApiString("Stress"),
                ["icon"] = new OpenApiString("breath"),
                ["color"] = new OpenApiString("#FF5722")
            };
        }

        // Exemplo para UpdateProfileDto
        if (context.Type == typeof(UpdateProfileDto))
        {
            schema.Example = new OpenApiObject
            {
                ["name"] = new OpenApiString("João Silva"),
                ["email"] = new OpenApiString("joao@example.com")
            };
        }
    }
}

