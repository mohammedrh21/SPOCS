# ============================================================
# Stage 1: Build & Publish
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files for optimized layer caching
COPY ["SPOCS.Domain/SPOCS.Domain.csproj", "SPOCS.Domain/"]
COPY ["SPOCS.Application/SPOCS.Application.csproj", "SPOCS.Application/"]
COPY ["SPOCS.Infrastructure/SPOCS.Infrastructure.csproj", "SPOCS.Infrastructure/"]
COPY ["SPOCS.API/SPOCS.API.csproj", "SPOCS.API/"]

# Restore dependencies
RUN dotnet restore "SPOCS.API/SPOCS.API.csproj"

# Copy all source files
COPY . .

# Build and publish release output
WORKDIR "/src/SPOCS.API"
RUN dotnet publish "SPOCS.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============================================================
# Stage 2: Runtime
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Expose default HTTP port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SPOCS.API.dll"]
