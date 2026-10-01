# Kingbase (人大金仓) 高可用连接字符串配置 (Npgsql 兼容模式)

> **重要提示**: 若使用 Npgsql 兼容模式，请务必使用 **Npgsql 10.0.1** 或更高版本，以确保最佳的兼容性和高可用特性支持。

KingbaseES 数据库支持在连接字符串中配置多个主机地址，以实现故障转移（Failover）和负载均衡（Load Balance）。

本文档描述的是 **Npgsql 驱动兼容模式** 下的配置方式。在使用 `Kdbndp` 驱动时，支持这种标准格式。

推荐使用 `Server=host:port,host:port` 的格式。

## 1. 连接字符串格式

```
Server=IP1:Port1,IP2:Port2,IP3:Port3;Database=DBName;User Id=User;Password=Pwd;...
```

**示例配置：**

```json
"ConnectionStrings": {
  "KingBaseHAConnection": "Server=192.0.2.10:14321,192.0.2.10:14322,192.0.2.10:14323;Database=test;User Id=kbuser;Password=your_password;TargetSessionAttributes=Primary;LoadBalanceHosts=true;",
  "KingBaseHAReadConnection": "Server=192.0.2.10:14321,192.0.2.10:14322,192.0.2.10:14323;Database=test;User Id=kbuser;Password=your_password;TargetSessionAttributes=Standby;LoadBalanceHosts=true;"
}
```

## 2. 关键参数说明

*   **Server**: 指定多个主机和端口，格式为 `IP:Port`，多个地址之间用逗号分隔。
*   **TargetSessionAttributes**: 
    *   `Primary` (推荐): 驱动程序将只连接到主节点（可写节点）。
    *   `Standby`: 只连接到备节点（只读节点）。用于读写分离场景。
    *   `Any`: 连接到任何可用的节点（主或备）。
    *   注意：在新版 Npgsql 中，`PreferStandby` 等旧参数已被移除或不再推荐。
*   **LoadBalanceHosts**:
    *   `true`: 随机选择主机列表中的一个进行连接，实现简单的负载均衡。
    *   `false` (默认): 按顺序从左到右尝试连接。

## 3. 实现原理

当应用程序发起连接时，驱动程序会解析主机列表：
1.  驱动程序会解析 `Server` 字段中的所有地址。
2.  如果 `LoadBalanceHosts=true`，则随机选择一个地址进行尝试；否则按顺序尝试。
3.  如果连接成功但节点状态不符合 `TargetSessionAttributes` 要求（例如要求 `Primary` 但连到了只读备库），驱动会断开并尝试下一个地址。

## 4. 读写分离策略

*   **写操作**：使用配置了 `TargetSessionAttributes=Primary` 的连接字符串。
*   **读操作**：使用配置了 `TargetSessionAttributes=Standby` 的连接字符串。

## 5. 常见问题：为什么 LoadBalanceHosts=true 但总是连接到同一个 IP？

Npgsql 驱动默认启用了 **连接池 (Connection Pooling)**。

*   **现象**：当应用程序第一次成功连接到某个节点后，该连接会被放入池中。后续的代码如果请求建立连接，Npgsql 会直接从池中复用这个现成的连接，而**不会**重新执行主机选择逻辑。
*   **结果**：在短时间内重复执行连接测试代码，看到的往往是同一个 IP/端口。
*   **解决方法（仅限测试）**：
    *   在连接字符串中添加 `Pooling=false` 禁用连接池。
    *   或者在每次连接后调用 `NpgsqlConnection.ClearPool(conn)`。
*   **生产建议**：生产环境中应**保持**连接池开启（默认），以获得最佳性能。负载均衡会在连接池创建新连接（如并发增加或旧连接过期重连）时自然生效。
