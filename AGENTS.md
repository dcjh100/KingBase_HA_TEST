# AGENTS

面向 AI 编程助手（Claude Code / Copilot / Cursor 等）的项目上下文说明。

## 项目背景

本项目是一个技术验证项目（PoC），用于验证 **人大金仓 KingbaseES** 数据库的
高可用（HA）、读写分离与负载均衡能力。

- **开发语言**：C#
- **目标框架**：.NET 8
- **数据库驱动**：`Kdbndp_V9`（金仓官方 .NET 驱动，NuGet）
- **容器化**：Docker / docker compose

## 目录结构

| 目录 | 说明 |
| :--- | :--- |
| `src/KingBaseTest.Web` | ASP.NET Core 8 MVC 监控看板（主程序） |
| `test/KingBaseTest.Tests` | xUnit 集成测试（连接 / 读写分离 / 负载均衡验证） |
| `docs/` | 设计方案与开发计划 |
| `skills/` | 与金仓、Docker 网络相关的技术笔记与脚本 |
| `.trae/` | IDE 规则与 AI 助手上下文 |

## 构建与运行

```powershell
dotnet build KingBaseTest.slnx
dotnet run  --project src/KingBaseTest.Web
docker compose up --build      # 访问 http://localhost:18080
```

## 数据库连接配置

连接字符串位于 `src/KingBaseTest.Web/appsettings.json`，共四条：

| 名称 | 用途 |
| :--- | :--- |
| `KingBaseHAConnection` | 读写分离 - 主节点（`TargetSessionAttributes=Primary`） |
| `KingBaseHAReadConnection` | 读写分离 - 备节点（`TargetSessionAttributes=Standby`） |
| `KingBaseHAConnection1` | 官方驱动写法 - 主（`UseReadWriteSep=true`） |
| `KingBaseHAReadConnection1` | 官方驱动写法 - 备（`UseReadWriteSep=false`） |

> 仓库中的地址、账号、口令均为占位示例，请替换为实际环境值。
> 详细参数说明见 `skills/KingbaseHA_NpgsqlMode.md` 与 `skills/KingbaseDriver.md`。

## 编码规范

- 遵循 C# 标准编码规范。
- 文档文件名与内容使用中文。
- 单元测试按需生成、按需运行；编译时不自动运行单元测试。

## AI 技能库

`.claude/skills` 下的第三方技能包未纳入本仓库，需要时可通过
`npx openskills add anthropics/skills` 重新加载，详见 `skills/OpenSkillsIntegration.md`。
