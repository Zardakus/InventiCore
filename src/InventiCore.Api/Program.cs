using System.Text;
using InventiCore.Api.Middleware;
using InventiCore.Api.Services;
using InventiCore.Application;
using InventiCore.Application.Common.Interfaces;
using InventiCore.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console();
});

// ── Dependency Injection (Clean Architecture layers) ─────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Context & Auth Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "MinhaSuperChaveSecretaMuitoLongaParaOJWTAqui2024!";
var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true
    };
});

builder.Services.AddAuthorization();

// Register Background Worker
builder.Services.AddHostedService<InventiCore.Api.Workers.StockLowDiscordAlertWorker>();

// ── Controllers ──────────────────────────────────────────
builder.Services.AddControllers();

// ── CORS ─────────────────────────────────────────────────
builder.Services.AddCors();

// ── Swagger ──────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "InventiCore API",
        Version = "v1",
        Description = "SaaS B2B de Gestão de Estoque Distribuído"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta forma: Bearer {seu_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// ── Global Exception Handler (deve ser o primeiro middleware) ──
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

// ── Pipeline HTTP ────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();

// Use CORS before Auth
app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
