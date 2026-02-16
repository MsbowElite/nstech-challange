# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY OrderService.Domain/OrderService.Domain.csproj OrderService.Domain/
COPY OrderService.Application/OrderService.Application.csproj OrderService.Application/
COPY OrderService.Infrastructure/OrderService.Infrastructure.csproj OrderService.Infrastructure/
COPY OrderService.API/OrderService.API.csproj OrderService.API/

# Restore dependencies
RUN dotnet restore OrderService.API/OrderService.API.csproj

# Copy source code
COPY OrderService.Domain/ OrderService.Domain/
COPY OrderService.Application/ OrderService.Application/
COPY OrderService.Infrastructure/ OrderService.Infrastructure/
COPY OrderService.API/ OrderService.API/

# Build the application
WORKDIR /src/OrderService.API
RUN dotnet build -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "OrderService.API.dll"]
