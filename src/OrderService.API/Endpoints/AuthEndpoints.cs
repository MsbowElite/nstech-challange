using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OrderService.API.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/token", GenerateToken)
            .WithName("GenerateToken")
            .AllowAnonymous()
            .WithOpenApi();
    }

    private static IResult GenerateToken(
        [FromBody] TokenRequest request,
        IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.BadRequest(new { error = "Username and password are required" });
        }

        // For demo purposes, accept any username/password
        var token = GenerateJwtToken(request.Username, configuration);
        return Results.Ok(new TokenResponse(token, "bearer", 3600));
    }

    private static string GenerateJwtToken(string username, IConfiguration configuration)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public record TokenRequest(string Username, string Password);
public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);
