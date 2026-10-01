# KingBase_HA_TEST

> 人大金仓 KingbaseES **高可用（HA） / 读写分离 / 负载均衡** 的验证工具与实时监控看板

一个技术验证项目（PoC），用于直观、可重复地验证 KingbaseES 集群的以下行为：

- 多主机连接串（`Host=a,b,c`）能否正确完成**故障转移**；
- `TargetSessionAttributes=Primary/Standby` 能否稳定落在**可写主节点 / 只读备节点**；
- 关闭连接池后，多节点列表能否实现**随机负载均衡**。

## 功能

### 1. 三通道实时监控看板

首页即看板（`/Monitor/Index`），三列并排、互不干扰地高频探测数据库：

| 通道 | 连接字符串 | 预期行为 |
| :--- | :--- | :--- |
| **Primary**（读写） | `KingBaseHAConnection`<br>`TargetSessionAttributes=Primary` | 始终落在主节点，`transaction_read_only = off`；同时执行一次真实写入验证（建表 → 插入 → 删除） |
| **Standby**（只读） | `KingBaseHAReadConnection`<br>`TargetSessionAttributes=Standby` | 始终落在备节点，`transaction_read_only = on` |
| **Load Balance**（无连接池） | 基于只读连接串动态追加 `Pooling=false` | 每次新建连接，`inet_server_addr()` 在多个节点间跳变 |

每个通道独立展示 **Server IP / Port**、**读写状态**、**响应耗时**、**成功失败徽标**与**实时日志**（保留最近 50 条）；
Load Balance 通道额外用进度条展示各节点的**命中次数与占比**。

- 刷新频率可调（默认 1000 ms）
- 支持「启动监控 / 停止监控」全局开关，以及每通道「单步」执行

### 2. xUnit 集成测试

见 [`test/KingBaseTest.Tests`](test/KingBaseTest.Tests)：

| 用例 | 断言 |
| :--- | :--- |
| `CanConnectToKingBaseAndSelectOne` | 主连接串可连通，`select 1 from dual` 返回 `1`，`transaction_read_only = off` |
| `CanConnectToKingBaseReadOnlyAndSelectOne` | 只读连接串可连通，`transaction_read_only = on` |
| `VerifyLoadBalancingBehavior` | 禁用连接池后连续 10 次连接，命中的不同 Server IP 数量 **> 1** |

每个用例都会打印一次完整的网络信息（`inet_server_addr` / `inet_server_port` / `inet_client_addr` / `inet_client_port` / `current_user` / `session_user` / `current_schema` / `version`），便于确认「当前到底连到了哪个物理节点」。

## 技术栈

| 项 | 选型 |
| :--- | :--- |
| 运行时 | .NET 8 |
| Web | ASP.NET Core 8 MVC + Razor |
| 数据库驱动 | `Kdbndp_V9` 8.0.3.125（金仓官方 .NET 驱动，NuGet） |
| 前端 | Bootstrap 5 + jQuery + bootstrap-icons（AJAX 轮询） |
| 测试 | xUnit |
| 部署 | Docker / docker compose |

> 关于 Npgsql 兼容模式的连接串写法，见 [`skills/KingbaseHA_NpgsqlMode.md`](skills/KingbaseHA_NpgsqlMode.md)；
> 关于官方 Kdbndp 驱动的参数与读写分离配置，见 [`skills/KingbaseDriver.md`](skills/KingbaseDriver.md)。

## 目录结构

```
├── src/KingBaseTest.Web/           ASP.NET Core 8 MVC 监控看板（主程序）
│   ├── Controllers/MonitorController.cs   探测 API：CheckHealth
│   ├── Views/Monitor/Index.cshtml         三列式监控看板
│   └── appsettings.json                  连接字符串配置
├── test/KingBaseTest.Tests/        xUnit 集成测试
├── docs/                           设计方案与开发计划
├── skills/                         金仓驱动 / Docker 网络等技术笔记与脚本
├── Dockerfile                      根目录构建文件
├── docker-compose.yml              本地一键编排
└── KingBaseTest.slnx               .NET 解决方案
```

## 快速开始

### 本地运行

```powershell
dotnet build KingBaseTest.slnx
dotnet run --project src/KingBaseTest.Web
# 打开 http://localhost:5242
```

### Docker

```bash
docker compose up --build
# 打开 http://localhost:18080
```

## 连接配置

四条连接字符串位于 [`src/KingBaseTest.Web/appsettings.json`](src/KingBaseTest.Web/appsettings.json)：

| 名称 | 驱动写法 | 说明 |
| :--- | :--- | :--- |
| `KingBaseHAConnection` | Npgsql 兼容 | 主节点，`TargetSessionAttributes=Primary`，`LoadBalanceHosts=true` |
| `KingBaseHAReadConnection` | Npgsql 兼容 | 备节点，`TargetSessionAttributes=Standby` |
| `KingBaseHAConnection1` | Kdbndp 官方 | 主节点，`UseReadWriteSep=true` |
| `KingBaseHAReadConnection1` | Kdbndp 官方 | 备节点，`UseReadWriteSep=false` |

> ⚠️ **本仓库中的地址、账号、口令均为占位示例**（地址使用 RFC 5737 文档网段 `192.0.2.0/24` / `198.51.100.0/24`），
> 部署前请替换为实际环境值，或改用环境变量覆盖：
>
> ```bash
> ConnectionStrings__KingBaseHAConnection="Host=...;Database=...;Username=...;Password=..."
> ```

## 监控 API

```
GET /Monitor/CheckHealth?mode={primary|standby|loadbalance}
```

成功：

```json
{
  "success": true,
  "timestamp": "2026-01-01T12:00:00",
  "data": {
    "serverIp": "192.0.2.21",
    "serverPort": "54321",
    "clientIp": "192.0.2.100",
    "isReadOnly": "off",
    "version": "KingbaseES V009R...",
    "elapsedMs": 12
  }
}
```

失败：`{ "success": false, "error": "<完整异常链>", "elapsedMs": 1 }`

服务端执行的探测 SQL：

```sql
SELECT
    inet_server_addr()                             AS server_ip,
    inet_server_port()                             AS server_port,
    inet_client_addr()                             AS client_ip,
    current_setting('transaction_read_only')       AS is_read_only,
    version()                                      AS version;
```

`mode=primary` 时还会额外执行一次真实写入验证：
`CREATE TABLE IF NOT EXISTS HA_WRITE_TEST` → `INSERT` → `DELETE`。

## 容器网络（Windows + Docker Desktop）

Windows 下 Docker 不支持 `network_mode: host`，容器内无法直连宿主机所在网段的数据库 IP。
本项目采用 `host.docker.internal` + 宿主机 `netsh interface portproxy` 的方案：

```powershell
# 需管理员权限：把本机 14321-14323 转发到真实数据库地址
.\skills\scripts\Setup-KingbaseProxy.ps1 -TargetIp "192.0.2.10" -Action add

.\skills\scripts\Setup-KingbaseProxy.ps1 -Action show
.\skills\scripts\Setup-KingbaseProxy.ps1 -Action delete
```

`docker-compose.yml` 中已配置 `extra_hosts: host.docker.internal:host-gateway`，
并将容器网段固定为 `10.100.0.0/16` 以避免与数据库网段路由冲突。

详见 [`skills/DockerNetshProxy.md`](skills/DockerNetshProxy.md)。

## 文档

| 文档 | 内容 |
| :--- | :--- |
| [`docs/数据库监控功能设计方案.md`](docs/数据库监控功能设计方案.md) | 看板系统架构、功能设计、API 与界面原型 |
| [`docs/plans/数据库监控功能开发计划.md`](docs/plans/数据库监控功能开发计划.md) | 分阶段任务分解 |
| [`skills/KingbaseNetworkInfo.md`](skills/KingbaseNetworkInfo.md) | 网络信息诊断 SQL 与代理地址 ↔ 物理节点映射 |
| [`skills/KingbaseHA_NpgsqlMode.md`](skills/KingbaseHA_NpgsqlMode.md) | Npgsql 兼容模式连接串与负载均衡原理 |
| [`skills/KingbaseDriver.md`](skills/KingbaseDriver.md) | Kdbndp 官方驱动参数、读写分离与迁移替换对照 |
| [`skills/DotNetContainerPort.md`](skills/DotNetContainerPort.md) | .NET 容器端口约定 |
| [`AGENTS.md`](AGENTS.md) | 面向 AI 编程助手的项目上下文 |

## 说明

- 本仓库为内网验证环境代码的**脱敏副本**：真实 IP、账号口令、镜像仓库与内部发布配置均已移除或替换为占位值，Git 历史亦重建为单条提交。
- `.claude/skills` 下的第三方技能包未纳入本仓库，需要时执行 `npx openskills add anthropics/skills` 重新加载，见 [`skills/OpenSkillsIntegration.md`](skills/OpenSkillsIntegration.md)。
