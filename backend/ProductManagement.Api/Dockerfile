FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY backend/ProductManagement.Api/ProductManagement.Api.csproj backend/ProductManagement.Api/
RUN dotnet restore backend/ProductManagement.Api/ProductManagement.Api.csproj

COPY backend/ProductManagement.Api/ backend/ProductManagement.Api/
WORKDIR /src/backend/ProductManagement.Api
RUN dotnet publish ProductManagement.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ProductManagement.Api.dll"]
