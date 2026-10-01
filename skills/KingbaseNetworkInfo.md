# Kingbase 连接网络信息诊断

本技能文档说明如何通过 SQL 语句查看当前数据库连接的详细网络信息，以验证连接到了哪一个具体的数据库实例。

## 1. 诊断 SQL 语句

使用以下 SQL 语句可以获取服务器 IP、端口以及客户端连接信息：

```sql
SELECT 
    inet_server_addr() AS server_ip,           -- 服务器IP地址 
    inet_server_port() AS server_port,         -- 服务器端口 
    inet_client_addr() AS client_ip,           -- 客户端IP地址 
    inet_client_port() AS client_port,         -- 客户端端口 
    current_user AS current_user,              -- 当前用户 
    session_user AS session_user,              -- 会话用户 
    current_schema() AS current_schema,        -- 当前模式 
    version() AS version;                      -- KingbaseES版本
```

## 2. 地址映射关系

在本项目环境中，连接通过代理进行转发。代理地址与后端实际物理数据库地址的对应关系如下：

| 代理连接地址 (Frontend) | 实际数据库物理地址 (Backend) | 说明 |
| :--- | :--- | :--- |
| `192.0.2.10:14321` | `192.0.2.21:54321` | 节点 1 |
| `192.0.2.10:14322` | `192.0.2.22:54321` | 节点 2 |
| `192.0.2.10:14323` | `192.0.2.23:54321` | 节点 3 |

**注意：**
当执行上述 SQL 时，`inet_server_addr()` 返回的通常是 **实际数据库物理地址** (如 `192.0.2.21`)，而不是代理地址。这可以用来确切地判断当前连接落在了哪个物理节点上。
