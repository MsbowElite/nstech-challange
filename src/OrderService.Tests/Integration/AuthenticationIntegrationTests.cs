using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NUnit.Framework;

namespace OrderService.Tests.Integration;

/// <summary>
/// Integration tests for Authentication endpoints
/// </summary>
[TestFixture]
public class AuthenticationIntegrationTests : IntegrationTestBase
{
    [Test]
    public async Task GenerateToken_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var tokenRequest = new
        {
            username = "testuser",
            password = "anypassword" // API accepts any credentials for demo
        };

        // Act
        var response = await Client.PostAsJsonAsync("/auth/token", tokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.TokenType.Should().Be("bearer");
        result.ExpiresIn.Should().Be(3600);
    }

    [Test]
    public async Task GenerateToken_WithEmptyUsername_ShouldReturnBadRequest()
    {
        // Arrange
        var tokenRequest = new
        {
            username = "",
            password = "password"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/auth/token", tokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GenerateToken_WithEmptyPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var tokenRequest = new
        {
            username = "testuser",
            password = ""
        };

        // Act
        var response = await Client.PostAsJsonAsync("/auth/token", tokenRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task GenerateToken_WithDifferentUsernames_ShouldReturnDifferentTokens()
    {
        // Arrange
        var request1 = new { username = "user1", password = "pass" };
        var request2 = new { username = "user2", password = "pass" };

        // Act
        var response1 = await Client.PostAsJsonAsync("/auth/token", request1);
        var response2 = await Client.PostAsJsonAsync("/auth/token", request2);

        // Assert
        var token1 = await response1.Content.ReadFromJsonAsync<TokenResponse>();
        var token2 = await response2.Content.ReadFromJsonAsync<TokenResponse>();
        
        token1!.AccessToken.Should().NotBe(token2!.AccessToken);
    }

    [Test]
    public async Task AccessProtectedEndpoint_WithValidToken_ShouldSucceed()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task AccessProtectedEndpoint_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange - Don't set authentication header

        // Act
        var response = await Client.GetAsync("/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AccessProtectedEndpoint_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid.token.here");

        // Act
        var response = await Client.GetAsync("/orders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

}
