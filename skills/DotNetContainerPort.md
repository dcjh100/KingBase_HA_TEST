# .NET 8 容器端口配置说明

## 1. 现象
在 .NET 8 的 Docker 容器中，应用程序默认监听 **8080** 端口，而不是旧版本常见的 80 端口。
即使 `Dockerfile` 中仅写了 `EXPOSE 8080` 而未在代码中配置端口，应用依然能正确识别并运行在 8080 上。

## 2. 核心原理

### 2.1 基础镜像的默认配置
微软官方基础镜像 (`mcr.microsoft.com/dotnet/aspnet:8.0`) 预设了一个环境变量：
```bash
ASPNETCORE_HTTP_PORTS=8080
```
这是为了安全性考虑，允许容器以非 root 用户权限运行（非 root 用户通常无法绑定 1024 以下的端口）。

### 2.2 ASP.NET Core 的读取机制
ASP.NET Core 应用启动时，Kestrel 服务器会自动读取 `ASPNETCORE_HTTP_PORTS` 环境变量，并将应用绑定到该变量指定的端口。

### 2.3 EXPOSE 的作用
`Dockerfile` 中的 `EXPOSE 8080` 指令**不具备**实际配置端口的功能。它仅作为文档说明，告诉使用者和 Docker 守护进程该容器“打算”使用的端口。

## 3. 如何自定义端口

如果需要修改默认端口（例如改回 80 或使用其他端口），**不需要修改 C# 代码**，只需在 `Dockerfile` 中覆盖该环境变量即可。

**示例：将端口修改为 80**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# 覆盖默认环境变量，强制使用 80 端口
ENV ASPNETCORE_HTTP_PORTS=80

# 更新 EXPOSE 声明以保持一致
EXPOSE 80

ENTRYPOINT ["dotnet", "YourApp.dll"]
```

## 4. 参考文档
- [Microsoft Learn: .NET 8 容器默认端口更改](https://learn.microsoft.com/zh-cn/dotnet/core/compatibility/containers/8.0/aspnet-port)