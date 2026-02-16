# Docker HTTPS Configuration Fix

## Problem
The Docker container was failing to start with the following error:

```
fail: Microsoft.Extensions.Hosting.Internal.Host[11]
      Hosting failed to start
      System.InvalidOperationException: Unable to configure HTTPS endpoint. 
      No server certificate was specified, and the default developer certificate 
      could not be found or is out of date.
```

Additionally, there was a warning about DataProtection keys:
```
warn: Microsoft.AspNetCore.DataProtection.Repositories.FileSystemXmlRepository[60]
      Storing keys in a directory '/root/.aspnet/DataProtection-Keys' that may 
      not be persisted outside of the container. Protected data will be unavailable 
      when container is destroyed.
```

## Root Cause

### HTTPS Certificate Issue (Critical)
The docker-compose.yml was configured to run the API with both HTTP and HTTPS:
```yaml
ASPNETCORE_URLS=http://+:8080;https://+:8081
```

However, Docker containers don't have access to:
- Developer certificates created by `dotnet dev-certs https`
- System certificate stores
- User-specific certificate locations

When ASP.NET Core tries to configure an HTTPS endpoint without a certificate, it fails during startup.

### DataProtection Keys (Warning)
ASP.NET Core's Data Protection system stores encryption keys in the filesystem by default. In containers, these are stored in ephemeral storage and lost when the container is recreated. This is a warning, not an error, and doesn't prevent startup.

## Solution

### 1. Remove HTTPS Configuration
For containerized applications, HTTPS should typically be handled at the infrastructure level (reverse proxy, load balancer, ingress controller) rather than in the application container.

**Changes to docker-compose.yml:**
```yaml
# Before
ports:
  - "8080:8080"
  - "8081:8081"
environment:
  - ASPNETCORE_URLS=http://+:8080;https://+:8081
  - ASPNETCORE_HTTP_PORTS=8080
  - ASPNETCORE_HTTPS_PORTS=8081

# After
ports:
  - "8080:8080"
environment:
  - ASPNETCORE_URLS=http://+:8080
```

**Changes to Dockerfile:**
```dockerfile
# Before
EXPOSE 8080
EXPOSE 8081

# After
EXPOSE 8080
```

### 2. DataProtection Keys (Optional Fix)
The DataProtection warning can be addressed in several ways:

**Option A: Accept ephemeral keys** (Current approach)
- Keys are regenerated on container restart
- Acceptable for development and stateless applications
- No changes needed

**Option B: Persist keys to volume** (For production)
```yaml
api:
  volumes:
    - dataprotection-keys:/root/.aspnet/DataProtection-Keys
```

**Option C: Use Redis/Database for key storage** (For distributed systems)
```csharp
services.AddDataProtection()
    .PersistKeysToDbContext<OrderDbContext>();
```

## Impact

### Before Fix
- ❌ Container failed to start
- ❌ Application unavailable
- ❌ Error logs every startup attempt

### After Fix
- ✅ Container starts successfully
- ✅ API accessible on http://localhost:8080
- ✅ Swagger UI available at http://localhost:8080/swagger
- ✅ Clean startup logs (only DataProtection warning, which is non-critical)

## Best Practices for Containerized Applications

### HTTPS/SSL in Containers
1. **Development**: Use HTTP only in containers
2. **Production**: Terminate SSL at:
   - Reverse proxy (nginx, Traefik, Caddy)
   - Load balancer (AWS ALB, Azure Application Gateway)
   - Kubernetes Ingress Controller
   - API Gateway

### Why Not HTTPS in Container?
- Certificate management complexity
- Certificate rotation challenges
- No benefit (reverse proxy already handles it)
- Follows 12-factor app principles
- Simplifies container orchestration

### Production SSL Setup Example
```
Internet → HTTPS → [Load Balancer/Reverse Proxy] → HTTP → [Container]
         (SSL Term)                                 (8080)
```

## Verification

To verify the fix works:

```bash
# Start the containers
docker compose up

# Check that API is running
curl http://localhost:8080/swagger/index.html

# Should return Swagger UI HTML without errors
```

Expected startup logs:
```
orderservice_api | info: Microsoft.Hosting.Lifetime[14]
orderservice_api |       Now listening on: http://[::]:8080
orderservice_api | info: Microsoft.Hosting.Lifetime[0]
orderservice_api |       Application started. Press Ctrl+C to shut down.
```

## Related Files Modified
- `docker-compose.yml` - Removed HTTPS port and configuration
- `Dockerfile` - Removed EXPOSE 8081

## References
- [ASP.NET Core HTTPS in Docker](https://docs.microsoft.com/en-us/aspnet/core/security/docker-https)
- [Data Protection in ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/)
- [12-Factor App - Port Binding](https://12factor.net/port-binding)
