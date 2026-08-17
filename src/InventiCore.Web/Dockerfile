FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/InventiCore.Web/InventiCore.Web.csproj", "src/InventiCore.Web/"]
RUN dotnet restore "src/InventiCore.Web/InventiCore.Web.csproj"
COPY . .
WORKDIR "/src/src/InventiCore.Web"
RUN dotnet build "InventiCore.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InventiCore.Web.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=publish /app/publish/wwwroot .
COPY src/InventiCore.Web/nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
