FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# 显式声明暴露 8080 端口 (.NET 8 默认)
# 外部映射通过 docker-compose ports (18080:8080) 处理
EXPOSE 8080
# EXPOSE 8081

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    busybox \
    curl \
    unzip \
    ca-certificates \
    && rm -rf /var/lib/apt/lists/* \
    && curl -sSL https://aka.ms/getvsdbgsh | bash /dev/stdin -v latest -l /remote_debugger \
    && busybox --install -s

RUN which busybox && busybox --help

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/KingBaseTest.Web/KingBaseTest.Web.csproj", "src/KingBaseTest.Web/"]
RUN dotnet restore "./src/KingBaseTest.Web/KingBaseTest.Web.csproj"
COPY . .
WORKDIR "/src/src/KingBaseTest.Web"
RUN dotnet build "./KingBaseTest.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./KingBaseTest.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KingBaseTest.Web.dll"]