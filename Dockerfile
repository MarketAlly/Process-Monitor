# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["MarketAlly.ProcessMonitor.csproj", "."]
RUN dotnet restore

# Copy all source files
COPY . .

# Build the application
RUN dotnet build -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS final
WORKDIR /app

# Install required packages for process monitoring
RUN apt-get update && apt-get install -y \
    procps \
    && rm -rf /var/lib/apt/lists/*

# Create logs directory
RUN mkdir -p /app/logs

# Copy published application
COPY --from=publish /app/publish .

# Create non-root user
RUN groupadd -r processmonitor && useradd -r -g processmonitor processmonitor
RUN chown -R processmonitor:processmonitor /app

# Switch to non-root user
USER processmonitor

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD test -f /app/logs/processmonitor-*.log || exit 1

# Entry point
ENTRYPOINT ["dotnet", "MarketAlly.ProcessMonitor.dll"]