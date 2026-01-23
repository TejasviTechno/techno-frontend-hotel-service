# -----------------------
# BUILD STAGE
# -----------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution & csproj first (layer caching)
COPY *.sln ./
COPY XConnect.API/*.csproj XConnect.API/
COPY XConnect.Auth/*.csproj XConnect.Auth/
COPY XConnect.BAL/*.csproj XConnect.BAL/
COPY XConnect.Common/*.csproj XConnect.Common/
COPY XConnect.ErrorLogHandler/*.csproj XConnect.ErrorLogHandler/
COPY XConnect.MarkupModule/*.csproj XConnect.MarkupModule/
COPY XConnect.Models/*.csproj XConnect.Models/
COPY XConnect.Supplier/*.csproj XConnect.Supplier/
COPY XConnection.DAL/*.csproj XConnection.DAL/

# Restore
RUN dotnet restore

# Copy full source
COPY . .

# Publish Release
RUN dotnet publish XConnect.API/XConnect.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# -----------------------
# RUNTIME STAGE
# -----------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Copy published output
COPY --from=build /app/publish .

# Fix permissions
RUN chown -R appuser:appgroup /app

# Run as non-root
USER appuser

# Environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

# Port
EXPOSE 8080

# Start
ENTRYPOINT ["dotnet", "XConnect.API.dll"]
