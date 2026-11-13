# RIGX — Running the project

## Requirements
.NET SDK 10, Docker (PostgreSQL + tests use Testcontainers), Node 24 (frontend, phase 4).

## Local setup
```bash
cp .env.example .env            # then set local passwords in .env
docker compose up -d postgres   # PostgreSQL on 127.0.0.1:5433
dotnet tool restore             # dotnet-ef
dotnet build HARDWARE_STORE.slnx
```

## Database migrations
Each module has its own DbContext and migrations folder (`src/Modules/<Module>/**/Migrations`).
Until the migration runner lands (roadmap phase 1.2), apply them per context:
```bash
dotnet ef database update --project src/Modules/Identity --startup-project src/Host/HardwareStore.Api
```
Add a migration:
```bash
dotnet ef migrations add <Name> --project src/Modules/<Module> --startup-project src/Host/HardwareStore.Api
```

## First owner account
One-time interactive command (see `src/Host/HardwareStore.Api/OwnerBootstrapCommand.cs`): run the API with `--bootstrap-owner` and `OwnerBootstrap:Enabled=true`.

## Tests
```bash
dotnet test HARDWARE_STORE.slnx            # needs Docker running (real PostgreSQL)
dotnet test --filter "FullyQualifiedName~Retail"   # one area only
```

## Configuration
- Secrets only in `.env` locally / the host's secret store in production. Never committed.
- SMTP variables are listed (commented) in `.env.example`; Development uses an in-memory email provider.
