using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using projects_menagment.Api.Middleware;
using projects_menagment.Application.DependencyInjection;
using projects_menagment.Infrastructure.DependencyInjection;
using projects_menagment.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);
var frontendOrigin = builder.Configuration["Frontend:Origin"] ?? "http://localhost:5173";

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(frontendOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtOptions = builder.Configuration
                             .GetSection(JwtOptions.SectionName)
                             .Get<JwtOptions>()
                         ?? throw new InvalidOperationException("JWT configuration is missing.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/test", () => new { message = "Hello from test endpoint", timestamp = DateTime.UtcNow })
    .WithName("GetTest")
    .WithOpenApi()
    .RequireAuthorization()
    .Produces<object>(StatusCodes.Status200OK);

app.Run();

public partial class Program;
