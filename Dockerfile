# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/HouseHeroes.ApiService/HouseHeroes.ApiService.csproj", "src/HouseHeroes.ApiService/"]
COPY ["src/HouseHeroes.ServiceDefaults/HouseHeroes.ServiceDefaults.csproj", "src/HouseHeroes.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore "src/HouseHeroes.ApiService/HouseHeroes.ApiService.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/src/HouseHeroes.ApiService"
RUN dotnet build "HouseHeroes.ApiService.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "HouseHeroes.ApiService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "HouseHeroes.ApiService.dll"]

# Migration stage (includes SDK for EF tools)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS migration
WORKDIR /app

# Copy the published app
COPY --from=publish /app/publish .

# Install Entity Framework CLI
RUN dotnet tool install --global dotnet-ef --version 9.0.0
ENV PATH="$PATH:/root/.dotnet/tools"

# Default command for migrations
ENTRYPOINT ["dotnet", "ef", "database", "update"]