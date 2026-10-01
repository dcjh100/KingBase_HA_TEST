---
name: "dotnet-custom-base"
description: "Provides a custom .NET 8 ASP.NET Core base image configuration with busybox and remote debugging tools. Invoke when setting up Dockerfiles for .NET projects requiring debug capabilities."
---

# Dotnet Custom Base Image

This skill provides a standard Docker base image configuration for .NET 8 applications that includes debugging tools.

## Base Image Configuration

When creating a Dockerfile, use the following `base` stage:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base 
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
```

## Usage

Use this configuration when:
1. The user needs a .NET 8 base image.
2. Debugging tools (`busybox`, `vsdbg`) are required in the container.
3. The application is hosted on Linux (Debian/Ubuntu based).
