# Multi-stage build for GalacticTrader API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["GalacticTrader.sln", "."]
COPY ["src/API/", "src/API/"]
COPY ["src/Services/", "src/Services/"]
COPY ["src/Data/", "src/Data/"]

# Restore dependencies
RUN dotnet restore "GalacticTrader.sln"

# Build the application
RUN dotnet build "GalacticTrader.sln" -c Release -o /app/build

# Publish the application
RUN dotnet publish "src/API/GalacticTrader.API.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "GalacticTrader.API.dll"]
