# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["GroupsMicroservice.csproj", "./"]
RUN dotnet restore "GroupsMicroservice.csproj"

# Copy all source files
COPY . .

# Build the application
RUN dotnet build "GroupsMicroservice.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "GroupsMicroservice.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Expose ports
EXPOSE 1000

# Copy published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "GroupsMicroservice.dll"]
