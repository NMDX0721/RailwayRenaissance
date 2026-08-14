# AGENTS.md — 铁路复兴：沙能冲击

Unity 2D 像素风模拟经营游戏。中文优先。

## 快速 Facts

- **引擎**: Unity 6000.4.6f1 (NOT 2021/2022/6000.0)
- **语言**: C#
- **美术风格**: 16-bit 像素画，暖色调，类似 Stardew Valley
- **平台**: Windows + Android

## 项目结构

```
Assets/
├── Scripts/           # C# 脚本（主要代码）
│   ├── VN/            # 视觉小说系统（核心子系统）
│   ├── LoginManager.cs, TitleScreen.cs, UIManager.cs, GameData.cs ...
├── Scenes/            # Unity 场景
│   ├── Login.unity          # 登录界面（UGUI）
│   ├── TitleScreen.unity    # 标题界面（UI Toolkit）
│   ├── VN_Test.unity        # VN 系统测试场景
│   └── StationSlice_V1.unity # 车站运营主场景
├── Resources/         # 运行时加载资源
│   ├── characters/    # 角色立绘（按角色ID子目录）
│   ├── bg/            # 背景图 1920×1080
│   ├── bgm/           # 背景音乐 .ogg
│   ├── sfx/           # 音效 .ogg
│   ├── Scripts/       # VN 剧本 JSON
│   ├── UI/            # UI 样式资源
│   └── Fonts/         # 字体（zpix 像素字体）
├── Documentation/     # 项目文档
参考资料/               # 设计文档（经济系统、角色设定、世界观等）
```

## 编辑器外验证

项目没有独立的 build/test CLI。验证代码变更的唯一可靠方式：

1. **检查编译错误**: 读 Unity Editor 日志
   ```powershell
   Get-Content "C:\Users\Oe_Lee\AppData\Local\Unity\Editor\Editor.log" -Tail 200 | Select-String "error CS"
   ```
2. **重启游戏**: `taskkill /F /IM "Unity.exe"` 后在 Unity 中重新打开场景
3. **批处理编译验证**: 关闭 Unity 后运行
   `"D:\Unity Hub\Unity 6000.4.6f1\Editor\Unity.exe" -quit -batchmode -projectPath "D:\Unity Project\RailwayRenaissance" -logFile compile_check.log`

## Git 工作流

详见 `docs/compose/specs/Git工作流规范.md`，核心原则：

- **分支策略**: `main` 只接受 squash merge，开发在 `feat/*` 分支
- **Commit 粒度**: 一个 commit = 一个逻辑单元（改一个bug、加一个功能）
- **Squash**: 功能完成后 squash 合并到 main，每个 main commit 代表一个完整功能
- **Push 时机**: 开发中只 commit 不 push，squash 合并到 main 后立即 push

## 关键约定

- **不要直接编辑 `.unity` YAML 文件** — 场景结构损坏难以修复，优先在 Unity Editor 内操作或用编辑器脚本生成
- **中文 UI 优先** — 按钮、文本、标签用中文
- **资源命名规范**:
  - 角色立绘: `characters/{角色ID}/{表情ID}.png` — 如 `characters/lin_biaohan/smile.png`
  - 背景: `bg/{场景ID}.png`
  - 音乐: `bgm/{音乐ID}.ogg`
  - 音效: `sfx/{音效ID}.ogg`
  - VN 剧本: `Scripts/prologue_XX_{名称}.json`

## VN 系统要点

- 入口: `VNManager.cs` — 单例模式，管理所有 VN 流程
- 对话框: `DialogueBox.cs` — 使用 UI Toolkit (UIDocument)
- 打字机效果: `TypewriterEffect.cs`
- 音频: `VNAudioManager.cs` — BGM 淡入淡出，独立于全局 AudioManager
- 存档: `VNSaveSystem.cs` + `VNSaveLoadUI.cs`
- 剧本格式: JSON，存放在 `Assets/Resources/Scripts/`
- 字体: `zpix` 像素字体，路径 `Resources/Fonts/zpix`

## 现有协作规则

`.trae/rules/rail-revival-collaboration.md` 定义了团队协作规范，核心要点：
- 先看任务再做事，不自行扩大范围
- 修改必须可说明（改了哪些文件、对象、脚本）
- 不把外部拼改 `.unity` YAML 当主修复路径
- 聚焦第一个可玩切片，不发散

## 设计文档

`参考资料/` 目录包含所有设计文档，按重要性排序：
1. `游戏开发文档.md` — 结构化GDD主索引，链接所有子文档
2. `经济系统.md` — 基于真实数据的经济模型
3. `角色设定.md` — 角色档案
4. `世界观与车辆设定.md` — 世界观和车辆
5. `世界观扩展设定.md` — 企业史/沙本位/雾峰村/私营化/岁月悬案/文化传承
6. `序章剧本_归乡.md` — 序章完整剧本

新增设计文档位于 `docs/compose/specs/`：
- 核心玩法循环.md、科技树设计.md、沙能竞争系统设计.md
- 跨系统联动公式.md、区域解锁与政治系统.md
- VN与模拟经营对接文档.md、序章后续剧情设计.md 等

阅读顺序: 主文档 → 系统文档 → 剧本文档
