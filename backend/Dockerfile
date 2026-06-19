# EventosVivos API — imagen multi-stage (.NET 10).
# Contexto de build: ./backend
# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore en capa propia (mejor caché): se copian los .csproj/.slnx primero.
COPY EventosVivos.slnx Directory.Build.props ./
COPY src/EventosVivos.Domain/EventosVivos.Domain.csproj             src/EventosVivos.Domain/
COPY src/EventosVivos.Application/EventosVivos.Application.csproj    src/EventosVivos.Application/
COPY src/EventosVivos.Infrastructure/EventosVivos.Infrastructure.csproj src/EventosVivos.Infrastructure/
COPY src/EventosVivos.Api/EventosVivos.Api.csproj                    src/EventosVivos.Api/
RUN dotnet restore src/EventosVivos.Api/EventosVivos.Api.csproj

# Resto del código y publish.
COPY . .
RUN dotnet publish src/EventosVivos.Api/EventosVivos.Api.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Las imágenes .NET 8+ ya incluyen un usuario no-root "app" (UID 1654).
USER app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

COPY --from=build --chown=app:app /app/publish .
ENTRYPOINT ["dotnet", "EventosVivos.Api.dll"]
