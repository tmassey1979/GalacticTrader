# Multi-stage build for GalacticTrader API
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only backend projects needed for the API image
COPY ["src/API/", "src/API/"]
COPY ["src/Services/", "src/Services/"]
COPY ["src/Data/", "src/Data/"]

# Restore and build API dependencies without pulling solution-only projects
RUN dotnet restore "src/API/GalacticTrader.API.csproj"
RUN dotnet build "src/API/GalacticTrader.API.csproj" -c Release -o /app/build

# Publish the application
RUN dotnet publish "src/API/GalacticTrader.API.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
EXPOSE 8443

ENTRYPOINT ["dotnet", "GalacticTrader.API.dll"]
