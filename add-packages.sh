#!/bin/bash

# Infrastructure - EF Core + PostgreSQL
dotnet add OrderService.Infrastructure/OrderService.Infrastructure.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add OrderService.Infrastructure/OrderService.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Design

# API - JWT Authentication
dotnet add OrderService.API/OrderService.API.csproj package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add OrderService.API/OrderService.API.csproj package Microsoft.IdentityModel.Tokens
dotnet add OrderService.API/OrderService.API.csproj package System.IdentityModel.Tokens.Jwt

# Application - MediatR for CQRS
dotnet add OrderService.Application/OrderService.Application.csproj package MediatR

# Tests - Additional testing packages
dotnet add OrderService.Tests/OrderService.Tests.csproj package FluentAssertions
dotnet add OrderService.Tests/OrderService.Tests.csproj package Moq
dotnet add OrderService.Tests/OrderService.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing

echo "Packages added successfully!"
