# Multi-stage build: restore/publish on the SDK image, run on the slim ASP.NET runtime.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (cached layer) — only the csproj changes bust this layer.
COPY src/ContentForge.Api/ContentForge.Api.csproj src/ContentForge.Api/
RUN dotnet restore src/ContentForge.Api/ContentForge.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/ContentForge.Api/ContentForge.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ContentForge.Api.dll"]
