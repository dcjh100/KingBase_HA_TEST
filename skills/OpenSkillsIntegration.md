# OpenSkills 技能集成说明

本项目已利用 **openskills** 工具加载并集成了一系列外部技能，旨在提升 AI 辅助开发的效率与能力。

## 1. 技能来源
集成的技能主要来源于以下高质量渠道：

*   **Anthropic 官方技能库**: `anthropics/skills`
    *   包含由 Anthropic 提供的标准、经过验证的 AI 技能，用于增强代码理解、文档生成及通用任务处理能力。
*   **ComposioHQ 精选技能**: `ComposioHQ/awesome-claude-skills`
    *   筛选并集成了社区中广受好评的开源项目提供的技能。这些技能通常包含特定领域的最佳实践和实用工具。

### 1.1 引入命令
若需在其他环境重新加载这些技能，请运行以下命令：

```bash
npx openskills add anthropics/skills
npx openskills add ComposioHQ/awesome-claude-skills
```

## 2. 功能与优势
通过引入这些技能，项目环境具备了更广泛的能力支持：
*   **扩展性**: 能够快速接入新的工具和工作流。
*   **最佳实践**: 直接复用社区验证过的优秀解决方案。
*   **自动化**: 增强了自动化处理复杂任务的能力。

## 3. 使用方式
这些技能已加载到当前的 AI 辅助环境中，在交互过程中 AI 可根据需求自动调用相关能力来协助完成任务。无需开发者进行额外的手动配置。
