FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/InventiCore.Api/InventiCore.Api.csproj", "src/InventiCore.Api/"]
COPY ["src/InventiCore.Application/InventiCore.Application.csproj", "src/InventiCore.Application/"]
COPY ["src/InventiCore.Domain/InventiCore.Domain.csproj", "src/InventiCore.Domain/"]
COPY ["src/InventiCore.Infrastructure/InventiCore.Infrastructure.csproj", "src/InventiCore.Infrastructure/"]
RUN dotnet restore "src/InventiCore.Api/InventiCore.Api.csproj"
COPY . .
WORKDIR "/src/src/InventiCore.Api"
RUN dotnet build "InventiCore.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InventiCore.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventiCore.Api.dll"]
