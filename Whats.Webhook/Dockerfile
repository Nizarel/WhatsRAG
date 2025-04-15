# Use the official .NET 8.0 SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the solution and project files
COPY ../WhatsRAG.sln .
COPY ./Whats.Webhook/*.csproj ./Whats.Webhook/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the application files
COPY ./Whats.Webhook/. ./Whats.Webhook/

# Build the application
WORKDIR /app/Whats.Webhook
RUN dotnet publish -c Release -o out

# Use the official .NET 8.0 runtime image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy the published output from the build stage
COPY --from=build /app/Whats.Webhook/out ./

# Set the entry point for the container
ENTRYPOINT ["dotnet", "Whats.Webhook.dll"]