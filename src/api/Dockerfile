# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Directory.Build.props and project files
COPY Directory.Build.props ./
COPY src/api/MujDomecek.API/MujDomecek.API.csproj MujDomecek.API/
COPY src/api/MujDomecek.Application/MujDomecek.Application.csproj MujDomecek.Application/
COPY src/api/MujDomecek.Domain/MujDomecek.Domain.csproj MujDomecek.Domain/
COPY src/api/MujDomecek.Infrastructure/MujDomecek.Infrastructure.csproj MujDomecek.Infrastructure/

# Restore dependencies for API project only
RUN dotnet restore MujDomecek.API/MujDomecek.API.csproj

# Copy everything else
COPY src/api/ .

# Build and publish
RUN dotnet publish MujDomecek.API/MujDomecek.API.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

COPY --from=build /app/publish .

# Set ownership
RUN chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "MujDomecek.API.dll"]
