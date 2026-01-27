# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV BuildingInsideDocker=true
WORKDIR /src

COPY . .

RUN dotnet restore "BuscaYa.csproj" --disable-parallel
RUN dotnet publish "BuscaYa.csproj" -c Release -o /app/publish --no-restore 

# Server Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

ENV TZ=America/Managua
ENV GENERIC_TIMEZONE=America/Managua

# Instalar dependencias necesarias para SkiaSharp en Linux (Debian-based)
RUN apt-get update && apt-get install -y \
    libgdiplus \
    libc6-dev \
    libfontconfig1 \
    libfreetype6 \
    libharfbuzz0b \
    libjpeg62-turbo \
    libpng16-16 \
    libx11-6 \
    libx11-xcb1 \
    libxcb1 \
    libxcomposite1 \
    libxcursor1 \
    libxdamage1 \
    libxext6 \
    libxfixes3 \
    libxi6 \
    libxrandr2 \
    libxrender1 \
    libxss1 \
    libxtst6 \
    ca-certificates \
    fonts-liberation \
    libasound2 \
    libatk-bridge2.0-0 \
    libatk1.0-0 \
    libc6 \
    libcairo2 \
    libcups2 \
    libdbus-1-3 \
    libexpat1 \
    libgbm1 \
    libgcc1 \
    libglib2.0-0 \
    libgtk-3-0 \
    libnspr4 \
    libnss3 \
    libpango-1.0-0 \
    libpangocairo-1.0-0 \
    libstdc++6 \
    xdg-utils \
    && rm -rf /var/lib/apt/lists/*

# Persist ASP.NET Core DataProtection keys across restarts/deploys
#RUN mkdir -p /app/dp-keys
#VOLUME ["/app/dp-keys"]

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "BuscaYa.dll"]