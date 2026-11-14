FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Host/HardwareStore.Api/HardwareStore.Api.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# pg_dump / pg_restore 17 from the PGDG repository, so the client matches the PostgreSQL 17 server (backups, restore check).
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && install -d /usr/share/postgresql-common/pgdg \
    && curl -fsSL -o /usr/share/postgresql-common/pgdg/apt.postgresql.org.asc https://www.postgresql.org/media/keys/ACCC4CF8.asc \
    && . /etc/os-release \
    && echo "deb [signed-by=/usr/share/postgresql-common/pgdg/apt.postgresql.org.asc] https://apt.postgresql.org/pub/repos/apt $VERSION_CODENAME-pgdg main" > /etc/apt/sources.list.d/pgdg.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends postgresql-client-17 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
RUN mkdir -p /home/app/.local/share/HardwareStore/DataProtection/Development \
      /home/app/.local/share/HardwareStore/DataProtection/Preview \
    && chown -R app:app /home/app/.local/share/HardwareStore
COPY --from=build /out .
RUN mkdir -p /app/App_Data/catalog-media \
    && chown -R app:app /app/App_Data
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=3s --start-period=20s --retries=5 CMD curl --fail --silent http://localhost:8080/health/ready || exit 1
USER app
ENTRYPOINT ["dotnet", "HardwareStore.Api.dll"]
