# PostgreSQL Healthcheck Fix

## Problem
The PostgreSQL container was logging a database error every 10 seconds:
```
orderservice_postgres  | FATAL:  database "orderuser" does not exist
```

## Root Cause
The Docker healthcheck was using:
```yaml
test: ["CMD-SHELL", "pg_isready -U orderuser"]
```

When `pg_isready` is called with only the `-U` (username) parameter and no `-d` (database) parameter, PostgreSQL defaults to checking for a database with the same name as the username. In this case, it was trying to connect to a database named "orderuser", which doesn't exist.

The actual database name is "orderservicedb" as defined in:
- `POSTGRES_DB: orderservicedb` in docker-compose.yml
- Connection string in the API configuration

## Solution
Updated the healthcheck to explicitly specify the database name:
```yaml
test: ["CMD-SHELL", "pg_isready -U orderuser -d orderservicedb"]
```

## Impact
- ✅ Eliminates the recurring error messages
- ✅ Healthcheck now correctly validates PostgreSQL readiness
- ✅ No changes to application code or database configuration
- ✅ Minimal change: one parameter added to healthcheck command

## Files Changed
- `docker-compose.yml` - Line 14

## Testing
The fix was validated by:
1. Verifying the docker-compose configuration syntax with `docker compose config`
2. Confirming the healthcheck command now includes the correct database name
