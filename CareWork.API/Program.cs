using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using CareWork.API.Middleware;
using CareWork.API.Services;
using CareWork.API.Swagger;
using CareWork.Infrastructure.Data;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CONFIGURAÇÃO DO SERILOG (STRUCTURED LOGGING)
// ============================================================================
// Configuração do Serilog através do appsettings.json
// UseSerilog() automaticamente substitui os providers padrão, evitando duplicação
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

// Usa Serilog como único provider de logging (substitui providers padrão automaticamente)
builder.Host.UseSerilog(dispose: true);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ============================================================================
// CONFIGURAÇÃO DO SWAGGER/OPENAPI
// ============================================================================
// Configuração de documentação da API com suporte a versionamento (V1 e V2)
// Cada versão aparece isolada no Swagger UI através do ApiExplorerSettings
// ============================================================================
builder.Services.AddSwaggerGen(c =>
{
    // ------------------------------------------------------------------------
    // Documento Swagger para V1 (Versão Completa e Estável)
    // ------------------------------------------------------------------------
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CareWork API V1",
        Version = "v1",
        Description = @"
## 🎯 API RESTful para Plataforma de Bem-estar Emocional - Versão 1

A **CareWork API V1** é a versão completa e estável da API desenvolvida em .NET 8 para gerenciamento de bem-estar emocional em ambientes de trabalho híbridos.

### 📋 Funcionalidades Principais

- ✅ **Check-ins Emocionais**: Registro diário de humor, stress e qualidade do sono
- ✅ **Análises e Insights**: Tendências, sequências (streaks) e comparações de períodos
- ✅ **Relatórios Detalhados**: Relatórios semanais e mensais com análises completas
- ✅ **Dicas Personalizadas**: Recomendações inteligentes baseadas no estado do usuário
- ✅ **Gestão de Perfil**: Atualização de perfil, senha e exclusão de conta
- ✅ **Tips Pré-cadastradas**: 20 dicas de bem-estar categorizadas (Stress, Sleep, Mood, Wellness)

### 🔐 Autenticação

Esta API utiliza **JWT (JSON Web Tokens)** para autenticação. 

**Como usar:**
1. Faça login ou registro através dos endpoints `/api/v1/auth/login` ou `/api/v1/auth/register`
2. Copie o `token` retornado na resposta
3. Clique no botão **Authorize** acima e cole o token no formato: `Bearer {seu-token}`
4. Agora você pode testar todos os endpoints autenticados

### 🗄️ Banco de Dados

- **SQL Server** (padrão - conforme requisito)
- **Entity Framework Core 8** com Code First e Migrations
- Suporte também a SQLite (desenvolvimento local) e Oracle/MongoDB (configurável)
- Migrations executadas automaticamente na primeira execução

### 🛠️ Tecnologias

- **.NET 8** - Framework mais recente da Microsoft
- **Entity Framework Core 8** - ORM com Code First
- **SQL Server** - Banco de dados relacional (padrão)
- **JWT Authentication** - Autenticação stateless
- **Serilog** - Logging estruturado
- **OpenTelemetry** - Distributed tracing
- **Swagger/OpenAPI 3.0** - Documentação interativa

### 📚 Documentação Completa

Para mais informações, consulte o README do projeto.

### 🚀 URLs Importantes

- **Swagger UI**: `http://localhost:8080/swagger`
- **Health Check**: `http://localhost:8080/health`
- **Base API**: `http://localhost:8080/api/v1` (prefixo para todos os endpoints)

### ⚠️ Importante

- Todos os endpoints autenticados requerem o header `Authorization: Bearer {token}`
- O token expira em 24 horas
- Use HTTPS em produção
- Banco de dados criado automaticamente na primeira execução
        ",
        Contact = new OpenApiContact
        {
            Name = "CareWork Team",
            Email = "support@carework.com",
            Url = new Uri("https://github.com/carework")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        },
        TermsOfService = new Uri("https://carework.com/terms")
    });

    // ------------------------------------------------------------------------
    // Documento Swagger para V2 (Versão com Melhorias)
    // ------------------------------------------------------------------------
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "CareWork API V2",
        Version = "v2",
        Description = @"
## 🎯 API RESTful para Plataforma de Bem-estar Emocional - Versão 2

Versão 2 da API CareWork com melhorias e novas funcionalidades.

**Status:** Em desenvolvimento - Alguns endpoints ainda estão na V1

### 📋 Endpoints Disponíveis na V2

- ✅ Autenticação (`/api/v2/auth`)
- ✅ Check-ins (`/api/v2/checkins`)

### 🔄 Compatibilidade

A V2 mantém compatibilidade com a V1, permitindo migração gradual.

**Recomendação:** Para aplicações em produção, use a **V1** que possui todos os endpoints completos.
        ",
        Contact = new OpenApiContact
        {
            Name = "CareWork Team",
            Email = "support@carework.com",
            Url = new Uri("https://github.com/carework")
        }
    });

    // ------------------------------------------------------------------------
    // Configuração de Documentação XML
    // ------------------------------------------------------------------------
    // Habilita comentários XML dos controllers para aparecer no Swagger
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // ------------------------------------------------------------------------
    // Organização de Tags (Agrupamento de Endpoints)
    // ------------------------------------------------------------------------
    // Organiza os endpoints por nome do controller no Swagger UI
    c.TagActionsBy(api =>
    {
        var controllerName = api.ActionDescriptor.RouteValues["controller"];
        return new[] { controllerName ?? "Default" };
    });

    // ------------------------------------------------------------------------
    // Exemplos para DTOs de Atualização (PUT)
    // ------------------------------------------------------------------------
    // Adiciona exemplos pré-preenchidos nos request bodies dos métodos PUT
    c.SchemaFilter<ExampleSchemaFilter>();

    // ------------------------------------------------------------------------
    // Filtro de Inclusão por Versão (CRÍTICO PARA SEPARAÇÃO)
    // ------------------------------------------------------------------------
    // Este filtro garante que cada versão do Swagger mostre APENAS os endpoints
    // da sua respectiva versão, baseado no GroupName definido nos controllers
    // através do atributo [ApiExplorerSettings(GroupName = "v1" ou "v2")]
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        // Obtém o GroupName do ApiDescription
        // O GroupName é definido pelo atributo [ApiExplorerSettings(GroupName = "v1" ou "v2")]
        // nos controllers
        var groupName = apiDesc.GroupName;

        // Se não tem GroupName definido, não inclui em nenhuma versão
        if (string.IsNullOrEmpty(groupName))
            return false;

        // Inclui o endpoint apenas se o GroupName corresponder ao documento solicitado
        // Exemplo: se docName = "v1" e groupName = "v1", retorna true
        return docName == groupName;
    });

    // ------------------------------------------------------------------------
    // Configuração de Autenticação JWT no Swagger
    // ------------------------------------------------------------------------
    // Permite testar endpoints autenticados diretamente no Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = @"
**JWT Authentication**

Para autenticar suas requisições:

1. Faça login ou registro através dos endpoints `/api/v1/auth/login` ou `/api/v1/auth/register`
2. Copie o `token` retornado na resposta (campo `data.token`)
3. Cole o token no campo abaixo no formato: **Bearer {seu-token}**

**Exemplo:**
```
Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI...
```

**Importante:**
- O token expira em 24 horas
- Você precisará fazer login novamente após a expiração
- Todos os endpoints marcados com 🔒 requerem autenticação
        ",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================================
// CONFIGURAÇÃO DO ENTITY FRAMEWORK CORE
// ============================================================================
// REQUISITO: Integração com SQL Server, Oracle ou MongoDB
// Implementado: SQL Server (padrão) e SQLite (opcional para desenvolvimento local)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<CareWorkDbContext>(options =>
{
    // Verifica se é SQLite (para desenvolvimento local, especialmente macOS)
    // Se a connection string começar com "Data Source=", assume SQLite
    if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
    {
        // SQLite para desenvolvimento local (opcional)
        // Para usar SQL Server, configure a connection string no appsettings.json
        options.UseSqlite(connectionString);
    }
    else
    {
        // SQL Server (padrão - conforme requisito)
        // Suporta: SQL Server, SQL Server LocalDB, Azure SQL
        options.UseSqlServer(connectionString);
    }
});

// ============================================================================
// CONFIGURAÇÃO DE AUTENTICAÇÃO JWT
// ============================================================================
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "CareWork";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "CareWork";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// ============================================================================
// CONFIGURAÇÃO DO AUTOMAPPER
// ============================================================================
builder.Services.AddAutoMapper(typeof(Program));

// ============================================================================
// REGISTRO DE SERVIÇOS (DEPENDENCY INJECTION)
// ============================================================================
builder.Services.AddScoped<ICheckinService, CheckinService>();
builder.Services.AddScoped<ITipService, TipService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IInsightsService, InsightsService>();

// ============================================================================
// CONFIGURAÇÃO DE HEALTH CHECKS
// ============================================================================
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CareWorkDbContext>();

// ============================================================================
// CONFIGURAÇÃO DO OPENTELEMETRY (DISTRIBUTED TRACING)
// ============================================================================
// Configurado sem console exporter para reduzir verbosidade em desenvolvimento
// Em produção, configure para exportar para Application Insights ou outro sistema
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddSource("CareWork.API")
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CareWork.API"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
        // Removido AddConsoleExporter() para reduzir logs verbosos
        // Em produção, configure para exportar para Application Insights ou outro sistema
    });

// ============================================================================
// CONFIGURAÇÃO DE CORS (CROSS-ORIGIN RESOURCE SHARING)
// ============================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("*"); // Permite expor todos os headers na resposta
    });
});

var app = builder.Build();

// ============================================================================
// CONFIGURAÇÃO DO PIPELINE HTTP
// ============================================================================

// ------------------------------------------------------------------------
// Swagger/OpenAPI (Sempre habilitado para desenvolvimento e testes)
// ------------------------------------------------------------------------
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Configuração dos documentos Swagger para cada versão
    // Cada versão aparece como um seletor no topo do Swagger UI
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CareWork API V1");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "CareWork API V2");
    
    // Configurações de UI
    c.RoutePrefix = "swagger"; // Swagger UI em /swagger
    c.DisplayRequestDuration(); // Mostra tempo de requisição
    c.EnableDeepLinking(); // Permite links diretos para endpoints
    c.EnableFilter(); // Habilita filtro de busca
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List); // Expande lista por padrão
});

// ------------------------------------------------------------------------
// CORS (Deve vir ANTES de autenticação e autorização)
// ------------------------------------------------------------------------
app.UseCors("AllowAll");

// ------------------------------------------------------------------------
// Logging de Requisições (Serilog)
// ------------------------------------------------------------------------
// Configurado para logar apenas requisições HTTP (não duplica logs de inicialização)
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) => ex != null || elapsed > 1000
        ? Serilog.Events.LogEventLevel.Warning
        : Serilog.Events.LogEventLevel.Information;
});

// ------------------------------------------------------------------------
// HTTPS Redirection (apenas se HTTPS estiver disponível)
// ------------------------------------------------------------------------
// Só usa HTTPS redirection se houver porta HTTPS configurada
// Isso evita o warning "Failed to determine the https port for redirect"
var applicationUrls = builder.Configuration["ASPNETCORE_URLS"] 
    ?? builder.Configuration["applicationUrl"] 
    ?? string.Empty;

// Verifica se há alguma URL HTTPS configurada
var hasHttps = applicationUrls.Contains("https://", StringComparison.OrdinalIgnoreCase) ||
               (builder.Environment.IsProduction() && !applicationUrls.Contains("http://", StringComparison.OrdinalIgnoreCase));

if (hasHttps)
{
    app.UseHttpsRedirection();
}

// ------------------------------------------------------------------------
// Autenticação e Autorização (Ordem importante!)
// ------------------------------------------------------------------------
app.UseAuthentication();
app.UseAuthorization();

// ------------------------------------------------------------------------
// Middleware Customizado de Tratamento de Exceções
// ------------------------------------------------------------------------
app.UseMiddleware<ExceptionHandlingMiddleware>();

// ------------------------------------------------------------------------
// Mapeamento de Controllers
// ------------------------------------------------------------------------
app.MapControllers();

// ------------------------------------------------------------------------
// Health Check Endpoint
// ------------------------------------------------------------------------
app.MapHealthChecks("/health");

// Ensure database is created and seed initial data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CareWorkDbContext>();
        context.Database.EnsureCreated();
        
        // Seed initial tips
        await DbSeeder.SeedTipsAsync(context);
        
        var dbLogger = services.GetRequiredService<ILogger<Program>>();
        dbLogger.LogInformation("✅ Banco de dados inicializado e populado com tips iniciais");
    }
    catch (Exception ex)
    {
        var dbLogger = services.GetRequiredService<ILogger<Program>>();
        dbLogger.LogError(ex, "An error occurred creating the DB.");
    }
}

// Exibir informações de inicialização
var appLogger = app.Services.GetRequiredService<ILogger<Program>>();
appLogger.LogInformation("🚀 CareWork API iniciada com sucesso!");
appLogger.LogInformation("📚 Swagger UI: http://localhost:8080/swagger");
appLogger.LogInformation("🏥 Health Check: http://localhost:8080/health");
appLogger.LogInformation("🌐 API Base: http://localhost:8080");
appLogger.LogInformation("═══════════════════════════════════════════");
appLogger.LogInformation("");

app.Run();

// Make Program accessible for testing
public partial class Program { }

