# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ARG AWS_ACCESS_KEY_ID
ARG AWS_SECRET_ACCESS_KEY
ARG AWS_REGION

ENV AWS_ACCESS_KEY_ID=${AWS_ACCESS_KEY_ID}
ENV AWS_SECRET_ACCESS_KEY=${AWS_SECRET_ACCESS_KEY}
ENV AWS_REGION=${AWS_REGION}

# Copy only what we need (NO tests, NO solution restore)
COPY app/Core/Domain/Domain.csproj app/Core/Domain/
COPY app/Core/Infrastructure/Infrastructure.csproj app/Core/Infrastructure/
COPY app/Core/Modules/Modules.csproj app/Core/Modules/
COPY app/Core/Shared/Shared.csproj app/Core/Shared/
COPY app/Host/API/Host.csproj app/Host/API/

# Restore ONLY API project (fixes broken .sln test references)
RUN dotnet restore app/Host/API/Host.csproj

# Copy full source
COPY . .

# Publish API
WORKDIR /src/app/Host/API
RUN dotnet publish Host.csproj -c Release -o /app/publish /p:UseAppHost=false

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

ARG AWS_ACCESS_KEY_ID
ARG AWS_SECRET_ACCESS_KEY
ARG AWS_REGION

ENV AWS_ACCESS_KEY_ID=${AWS_ACCESS_KEY_ID}
ENV AWS_SECRET_ACCESS_KEY=${AWS_SECRET_ACCESS_KEY}
ENV AWS_REGION=${AWS_REGION}

# Install curl for health check
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published output
COPY --from=build /app/publish .

# Expose API port
EXPOSE 5000

# ENV (DEV MODE ENABLED)
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Development

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s \
  CMD curl -f http://localhost:5000/swagger || exit 1

# Run app
ENTRYPOINT ["dotnet", "Host.dll"]