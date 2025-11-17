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
using CareWork.Infrastructure.Data;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/carework-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CareWork API",
        Version = "v1",
        Description = @"
## 🎯 API RESTful para Plataforma de Bem-estar Emocional

A **CareWork API** é uma API completa desenvolvida em .NET 8 para gerenciamento de bem-estar emocional em ambientes de trabalho híbridos.

### 📋 Funcionalidades Principais

- ✅ **Check-ins Emocionais**: Registro diário de humor, stress e qualidade do sono
- ✅ **Análises e Insights**: Tendências, sequências (streaks) e comparações de períodos
- ✅ **Relatórios Detalhados**: Relatórios semanais e mensais com análises completas
- ✅ **Dicas Personalizadas**: Recomendações inteligentes baseadas no estado do usuário
- ✅ **Gestão de Perfil**: Atualização de perfil, senha e exclusão de conta

### 🔐 Autenticação

Esta API utiliza **JWT (JSON Web Tokens)** para autenticação. 

**Como usar:**
1. Faça login ou registro através dos endpoints `/api/v1/auth/login` ou `/api/v1/auth/register`
2. Copie o `token` retornado na resposta
3. Clique no botão **Authorize** acima e cole o token no formato: `Bearer {seu-token}`
4. Agora você pode testar todos os endpoints autenticados

### 📚 Documentação Completa

Para mais informações, consulte o README do projeto ou a documentação completa em `ENDPOINTS_MOBILE.md`.

### 🚀 Base URL

```
http://localhost:8080/api/v1
```

### ⚠️ Importante

- Todos os endpoints autenticados requerem o header `Authorization: Bearer {token}`
- O token expira em 24 horas
- Use HTTPS em produção
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

    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "CareWork API",
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

    // Habilitar XML comments para documentação
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Adicionar tags para organizar endpoints
    c.TagActionsBy(api =>
    {
        if (api.GroupName != null)
        {
            return new[] { api.GroupName };
        }

        var controllerName = api.ActionDescriptor.RouteValues["controller"];
        return new[] { controllerName ?? "Default" };
    });

    // Ordenar endpoints por tags
    c.DocInclusionPredicate((name, api) => true);

    // Add JWT authentication to Swagger
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

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<CareWorkDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        // SQLite para desenvolvimento (especialmente macOS)
        options.UseSqlite(connectionString);
    }
    else
    {
        // SQL Server para produção
        options.UseSqlServer(connectionString);
    }
});

// Configure JWT Authentication
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

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Register services
builder.Services.AddScoped<ICheckinService, CheckinService>();
builder.Services.AddScoped<ITipService, TipService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IInsightsService, InsightsService>();

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CareWorkDbContext>();

// Configure OpenTelemetry (sem console exporter para reduzir verbosidade)
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

// Configure CORS
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

// Configure the HTTP request pipeline
// Swagger sempre habilitado para facilitar desenvolvimento e testes
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CareWork API V1");
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "CareWork API V2");
    c.RoutePrefix = "swagger"; // Swagger UI em /swagger
    c.DisplayRequestDuration(); // Mostra tempo de requisição
    c.EnableDeepLinking(); // Permite links diretos para endpoints
    c.EnableFilter(); // Habilita filtro de busca
});

// CORS deve vir ANTES de tudo (exceto Swagger)
app.UseCors("AllowAll");

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Custom exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapControllers();

// Health Check endpoint
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

