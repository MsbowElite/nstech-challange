using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OrderService.API.Endpoints;
using OrderService.API.Middleware;
using OrderService.Application.Behaviors;
using OrderService.Application.Commands.Handlers;
using OrderService.Application.Interfaces;
using OrderService.Application.Validators;
using OrderService.Infrastructure;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.UnitOfWork;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext with tracking (for commands/modifications)
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add read-only DbContext wrapper (for queries - no change tracking)
builder.Services.AddScoped<IOrderDbContextReadOnly, OrderDbContextReadOnly>();

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();

// Add MediatR with validation behavior
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CreateOrderCommandHandler).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// Add DbContext (required by Unit of Work and repositories)
// Note: AddDbContext is already called above in the file

// Add Unit of Work (coordinates all repositories and transaction management)
builder.Services.AddScoped<IUnitOfWork, OrderService.Infrastructure.UnitOfWork.UnitOfWork>();

// Add Repositories for query handlers (they don't participate in UnitOfWork transactions)
builder.Services.AddScoped(sp => sp.GetRequiredService<IUnitOfWork>().Orders);
builder.Services.AddScoped(sp => sp.GetRequiredService<IUnitOfWork>().Products);

// Add Domain Services
builder.Services.AddScoped<OrderService.Domain.Services.OrderDomainService>();

// Add JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "ThisIsASecretKeyForDevelopmentOnlyDoNotUseInProduction123456";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "OrderServiceAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "OrderServiceClients";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Order Service API Test", 
        Version = "v1.1",
        Description = "A REST API for managing orders with stock validation (Minimal API)"
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
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
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Apply migrations and seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<OrderDbContext>();
        await context.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add Exception Handling middleware
app.UseExceptionHandling();

// Add Correlation ID middleware
app.UseCorrelationId();

app.UseAuthentication();
app.UseAuthorization();

// Map Minimal API endpoints
app.MapAuthEndpoints();
app.MapOrderEndpoints();

app.Run();

// Make Program class accessible for testing
public partial class Program { }
