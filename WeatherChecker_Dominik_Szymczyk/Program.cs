using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using WeatherChecker_Dominik_Szymczyk.Services;
using WeatherChecker_Dominik_Szymczyk.Repositories;
using WeatherChecker_Dominik_Szymczyk.Middlewares;
using WeatherChecker_Dominik_Szymczyk.Logging;

var builder = WebApplication.CreateBuilder(args);

// Rejestracja serwisu do generowania tokenów JWT
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddSingleton<UserRepository>();

// Dodanie kontrolerów
builder.Services.AddControllers();

// Swagger + konfiguracja JWT w dokumentacji
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WeatherChecker API",
        Version = "v1"
    });

    // Dodanie JWT do Swaggera
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Wpisz token w formacie: Bearer {twój_token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

// Konfiguracja JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseMiddleware<WeatherChecker_Dominik_Szymczyk.Middlewares.RateLimitMiddleware>();


// Swagger tylko w trybie developerskim
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRequestLogging();

app.UseSqlInjectionProtection();

app.UseAuthentication(); // Uwierzytelnianie JWT
app.UseAuthorization();  // Autoryzacja

app.MapControllers();
app.Run();
