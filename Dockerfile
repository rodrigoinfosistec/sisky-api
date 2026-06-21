# Estágio 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

COPY src/*.csproj ./src/
RUN dotnet restore ./src/SiskyApi.csproj

COPY src/ ./src/
RUN dotnet publish ./src/SiskyApi.csproj -c Release -o /app/publish

# Estágio 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "SiskyApi.dll"]