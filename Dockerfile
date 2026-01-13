# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV BuildingInsideDocker=true
WORKDIR /src

# Copiar archivos del proyecto
COPY . .

# Restaurar dependencias
RUN dotnet restore "BuscaYa.csproj" --disable-parallel

# Publicar aplicación
RUN dotnet publish "BuscaYa.csproj" -c Release -o /app/publish --no-restore 

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Configurar zona horaria (Nicaragua)
ENV TZ=America/Managua
ENV GENERIC_TIMEZONE=America/Managua
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Persist ASP.NET Core DataProtection keys across restarts/deploys
RUN mkdir -p /app/dp-keys
VOLUME ["/app/dp-keys"]

# Copiar archivos publicados
COPY --from=build /app/publish .

# Exponer puerto
EXPOSE 5000

# Health check (opcional - requiere curl o wget instalado)
# HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
#   CMD wget --no-verbose --tries=1 --spider http://localhost:5000/api/public/categorias || exit 1

ENTRYPOINT ["dotnet", "BuscaYa.dll"]