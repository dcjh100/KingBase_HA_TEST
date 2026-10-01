# Docker Desktop Windows 网络配置技能 (Netsh PortProxy)

## 1. 背景与问题
在 Windows 上运行 Docker Desktop 时，容器默认运行在 Hyper-V 虚拟机或 WSL2 子系统中。
- **问题**: Windows 下的 Docker 不支持 `network_mode: host`。这意味着容器无法直接共享宿主机的网络接口。
- **场景**: 当容器内的应用需要连接宿主机网络中可访问的外部数据库（如位于 VPN 或特定网段的数据库 IP），且该 IP 在容器网络内部无法直接路由时，通常需要借助宿主机作为跳板。
- **解决方案**: 使用 `host.docker.internal` DNS 名称让容器连接到宿主机，并在宿主机上配置 `netsh interface portproxy` 将流量转发到真实的目标 IP。

## 2. 解决方案架构

1. **容器端**:
   - 应用程序连接字符串指向 `host.docker.internal:PORT`。
   - `host.docker.internal` 解析为宿主机的虚拟网卡 IP。

2. **宿主机端 (Windows)**:
   - 使用 `netsh interface portproxy` 监听 `0.0.0.0:PORT` (或特定 IP)。
   - 将流量转发到 `TARGET_IP:PORT`。

## 3. 操作步骤

### 3.1 编写/运行配置脚本
我们提供了一个 PowerShell 脚本来自动配置这些转发规则。
脚本位置: `skills/scripts/Setup-KingbaseProxy.ps1`

**使用方法 (需管理员权限 PowerShell)**:
```powershell
# 开启转发 (默认转发 14321-14323 到 192.0.2.10)
.\skills\scripts\Setup-KingbaseProxy.ps1 -TargetIp "192.0.2.10" -Action "add"

# 查看当前规则
.\skills\scripts\Setup-KingbaseProxy.ps1 -Action "show"

# 删除规则
.\skills\scripts\Setup-KingbaseProxy.ps1 -Action "delete"
```

### 3.2 配置 Docker Compose
在 `docker-compose.yml` 中配置连接字符串：

```yaml
environment:
  - ConnectionStrings__KingBaseHAConnection=Server=host.docker.internal:14321,host.docker.internal:14322,host.docker.internal:14323;Database=test;UID=user;PWD=pass;...
```

### 3.3 验证连接
1. 在宿主机上测试端口是否通：`Test-NetConnection localhost -Port 14321`
2. 在容器内测试连接：`nc -zv host.docker.internal 14321`

## 4. 常见问题
- **防火墙**: 确保 Windows 防火墙允许入站连接到这些端口。
- **IP变动**: 如果目标数据库 IP 变动，需重新运行脚本更新规则。
- **IPv6**: `netsh interface portproxy` 仅支持 TCP，且需注意 `v4tov4` 配置。

## 5. 参考命令
手动添加规则示例：
```cmd
netsh interface portproxy add v4tov4 listenport=14321 listenaddress=0.0.0.0 connectport=14321 connectaddress=192.0.2.10
```
查看规则：
```cmd
netsh interface portproxy show all
```
