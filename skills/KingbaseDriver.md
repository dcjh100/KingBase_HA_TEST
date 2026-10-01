# Kingbase 官方驱动说明

## 1. 当前状态
本项目目前使用 **Npgsql** 驱动 (兼容模式) 连接 KingbaseES 数据库。
由于 Npgsql 社区活跃度高且生态完善，优先使用 Npgsql 进行开发。

### 1.1 获取较新版 Kdbndp (替代方案)
若必须使用官方驱动，可通过引用 **SqlSugar** 间接获取较新版本的 `Kdbndp`。
- **SqlSugar 版本**: `5.1.4.211` (及以上)
- **间接引用**: `Kdbndp` 版本 `9.3.7.1219`
- **说明**: 相比旧版 `Kdbndp_V9`，此版本可能修复了部分已知问题。

## 2. Kdbndp 驱动配置指南 (基于官方文档)
**注意**: 若需切换回官方 `Kdbndp` 驱动，请参考以下信息（来源：`docs\金仓驱动替换方法.docx`）。

### 2.1 读写分离与高可用配置
在 Kdbndp 驱动中，实现读写分离和多节点负载均衡的方法与 JDBC 不同。

- **开启读写分离**: 在连接字符串中添加 `UseReadWriteSep=true`。
- **多节点配置**: `Server` 参数指定多个 IP，用逗号分隔。
  - 示例: `Server=198.51.100.11,198.51.100.12,198.51.100.13`
- **关键参数**:
  - `UseReadWriteSep`: `true`/`false` (默认 `false`)。开启后支持读写分离。
    - **注意**: 文档提及“目前读写分离需要单独申请驱动”，可能需要特定版本的 DLL。
    - **潜在行为 (待验证)**: 设置为 `true` 时，连接可能强制变为只读模式 (ReadOnly)，从而无法执行写入操作。
  - `PrimaryCanRead`: `true`/`false` (默认 `false`)。读写分离模式下，是否允许主节点承担读流量。
  - `CheckClusterState`: 检查主备状态的时间间隔，单位秒 (默认 `10`)。
  - `WhiteList`: 强制发往主机的 SQL 白名单 (格式 `{sql1:sql2}`)，用冒号分隔。
  - `BlackList`: 禁止发往备机的 SQL 黑名单。

### 2.2 连接字符串最佳实践
适用于纯 .NET 开发 (net4.5-net10.0) 的读写分离配置示例：

```text
Server=198.51.100.11,198.51.100.12,198.51.100.13;
Port=54321;
UID=system;
PWD=your_password;
database=test;
UseReadWriteSep=true;
Connection Idle Lifetime=30;
DbVersion=Oracle;
```

### 2.3 常用参数说明
| 参数 | 说明 | 建议值 |
| :--- | :--- | :--- |
| `Timeout` | 连接超时 (默认 15s) | `15 + 2 * 节点数` |
| `CommandTimeout` | SQL 执行超时 (默认 30s) | 根据业务调整 (超慢 SQL 可单独设置) |
| `DbVersion` | 数据库模式 (pg/oracle/sqlserver/mysql) | .NET 8 必须设置 (如 `oracle`) |
| `Pooling` | 连接池 (默认 true) | `true` (读写分离必须开启) |
| `Minimum Pool Size` | 连接池最小连接数 | `2` (维持长连接) |
| `Maximum Pool Size` | 连接池最大连接数 | `1000` (默认 100) |
| `Connection Idle Lifetime` | 空闲连接保活时间 (默认 300s) | `30` (高并发场景) |
| `KeepAlive` | TCP KeepAlive (默认 0) | `10` (高并发建议) |

### 2.4 代码替换指南 (从其他数据库迁移)
如果从 MySQL 或 SQL Server 迁移到 Kingbase，需进行以下替换：

- **命名空间**: `using Kdbndp;`
- **核心对象**:
  - `SqlConnection` / `MySqlConnection` -> `KdbndpConnection`
  - `SqlCommand` / `MySqlCommand` -> `KdbndpCommand`
  - `SqlDataReader` -> `KdbndpDataReader`
- **类型映射 (DbType)**:
  - `Int32` -> `KdbndpDbType.Integer` (MySQL) / `KdbndpDbType.SqlServer_Int` (SQL Server)
  - `Varchar` / `NVarchar` -> `KdbndpDbType.Varchar`

## 3. 历史问题解答 (FAQ)
- **Q: 如何实现 JDBC 的 `USEDISPATCH`, `HOSTLOADRATE` 效果?**
  - **A**: Kdbndp 不直接支持 JDBC 的 `USEDISPATCH` 和 `HOSTLOADRATE` 参数。
  - **负载均衡**: 通过 `Server` 多 IP 列表 + `UseReadWriteSep=true` 实现。
  - **负载比例**: Kdbndp 似乎不支持精细的比例调整 (如 `HOSTLOADRATE=50`)。主要通过 `PrimaryCanRead` 控制主节点是否参与读负载。若需要更复杂的负载均衡，建议使用中间件 (如 KingbaseClusterWare) 或 Npgsql 的负载均衡功能。

## 4. 相关资料
- 原始文档: `docs\金仓驱动替换方法.docx`
- 官方文档: https://docs.kingbase.com.cn/cn/KES-V9R2C13/application/client_interface/ADO.NET/Kdbndp/ado-net-4
