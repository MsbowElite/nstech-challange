#!/bin/bash

# Create solution
dotnet new sln -n OrderService

# Create projects
dotnet new classlib -n OrderService.Domain -f net8.0
dotnet new classlib -n OrderService.Application -f net8.0
dotnet new classlib -n OrderService.Infrastructure -f net8.0
dotnet new webapi -n OrderService.API -f net8.0
dotnet new xunit -n OrderService.Tests -f net8.0

# Add projects to solution
dotnet sln add OrderService.Domain/OrderService.Domain.csproj
dotnet sln add OrderService.Application/OrderService.Application.csproj
dotnet sln add OrderService.Infrastructure/OrderService.Infrastructure.csproj
dotnet sln add OrderService.API/OrderService.API.csproj
dotnet sln add OrderService.Tests/OrderService.Tests.csproj

# Setup project references
dotnet add OrderService.Application/OrderService.Application.csproj reference OrderService.Domain/OrderService.Domain.csproj
dotnet add OrderService.Infrastructure/OrderService.Infrastructure.csproj reference OrderService.Application/OrderService.Application.csproj
dotnet add OrderService.API/OrderService.API.csproj reference OrderService.Application/OrderService.Application.csproj
dotnet add OrderService.API/OrderService.API.csproj reference OrderService.Infrastructure/OrderService.Infrastructure.csproj
dotnet add OrderService.Tests/OrderService.Tests.csproj reference OrderService.Domain/OrderService.Domain.csproj
dotnet add OrderService.Tests/OrderService.Tests.csproj reference OrderService.Application/OrderService.Application.csproj
dotnet add OrderService.Tests/OrderService.Tests.csproj reference OrderService.Infrastructure/OrderService.Infrastructure.csproj
dotnet add OrderService.Tests/OrderService.Tests.csproj reference OrderService.API/OrderService.API.csproj

echo "Solution structure created successfully!"
