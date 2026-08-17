FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/InventiCore.Mcp/InventiCore.Mcp.csproj", "src/InventiCore.Mcp/"]
COPY ["src/InventiCore.Application/InventiCore.Application.csproj", "src/InventiCore.Application/"]
COPY ["src/InventiCore.Domain/InventiCore.Domain.csproj", "src/InventiCore.Domain/"]
COPY ["src/InventiCore.Infrastructure/InventiCore.Infrastructure.csproj", "src/InventiCore.Infrastructure/"]
RUN dotnet restore "src/InventiCore.Mcp/InventiCore.Mcp.csproj"
COPY . .
WORKDIR "/src/src/InventiCore.Mcp"
RUN dotnet build "InventiCore.Mcp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InventiCore.Mcp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "InventiCore.Mcp.dll"]
