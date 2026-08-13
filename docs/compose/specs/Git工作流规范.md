# Git 工作流规范

> 版本：v1.0  
> 适用：RailwayRenaissance 项目  
> 原则：改动一个单元 commit 一次，开发完一个功能 squash 标记大修改，merge 到 main

---

## 一、分支策略

```
main          ← 稳定版本，只接受 squash merge
  └─ feat/*   ← 功能开发分支
  └─ fix/*    ← 修复分支
  └─ docs/*   ← 文档分支
```

### 分支命名规则

| 类型 | 格式 | 示例 |
|------|------|------|
| 新功能 | `feat/功能名` | `feat/economic-system` |
| 修复 | `fix/问题描述` | `fix/vn-save-conflict` |
| 文档 | `docs/内容` | `docs/readme-update` |
| 资产 | `asset/内容` | `asset/character-sprites` |

---

## 二、Commit 规范

### 2.1 Commit 粒度

**一个 commit = 一个逻辑单元。**

| 场景 | 正确做法 | 错误做法 |
|------|---------|---------|
| 改了一个 bug | `git commit -m "fix: ..."` | 等攒了3个bug一起commit |
| 加了一个功能 | `git commit -m "feat: ..."` | 把整个功能拆成10个碎片commit |
| 改了UI和修了bug | 分两次commit | `git commit -m "fix: ..."` 包含UI改动 |

### 2.2 Commit Message 格式

```
<type>: <简短描述>

<可选详细说明>
```

**类型**：

| type | 用途 |
|------|------|
| `feat` | 新功能 |
| `fix` | 修复bug |
| `docs` | 文档变更 |
| `style` | UI/样式变更 |
| `refactor` | 重构（不改变功能） |
| `chore` | 杂项（meta文件、配置等） |

**示例**：
```
feat: 实现经济系统每日结算公式

- 新增客流/收入/燃料/工资/维护计算公式
- 新增剧情补贴系统 CompleteStoryGrant
- 初始资金从GameConfig读取
```

---

## 三、工作流

### 3.1 开发新功能

```bash
# 1. 从 main 创建分支
git checkout main
git pull
git checkout -b feat/feature-name

# 2. 逐个单元 commit
# 改完一个文件或一个逻辑单元就 commit
git add Assets/Scripts/xxx.cs
git commit -m "feat: 添加xxx功能"

# 3. 功能开发完成，squash 合并到 main
git checkout main
git merge --squash feat/feature-name
git commit -m "feat: 完整功能名"

# 4. 删除远程分支
git branch -D feat/feature-name
```

### 3.2 修复 bug

```bash
git checkout -b fix/bug-description
# 修复...
git commit -m "fix: 修复xxx问题"
git checkout main
git merge --squash fix/bug-description
git commit -m "fix: 修复xxx问题"
```

### 3.3 文档更新

```bash
git checkout -b docs/update-readme
# 修改...
git commit -m "docs: 更新README"
git checkout main
git merge --squash docs/update-readme
git commit -m "docs: 更新README"
```

---

## 四、何时 Push

| 场景 | 操作 |
|------|------|
| 本地开发中 | 只 commit，不 push |
| 功能分支完成，准备 squash | 可 push 分支备份（可选） |
| squash 合并到 main 后 | **立即 push** |
| 需要协同/备份 | 随时 push 分支 |

**核心原则**：main 分支只接受 squash merge 后的完整 commit，每个 commit 代表一个完整的功能/修复/文档更新。

---

## 五、Squash 规范

### 什么时候 squash

- 一个功能开发完成，所有子 commit 已经验证通过
- 准备合并到 main 之前

### squash 后 commit 的 message 格式

```
feat: 完整功能名称

- 子变更1
- 子变更2
- 子变更3
```

---

## 六、当前阶段建议

```
当前：直接在 main 上开发（小型项目阶段）
目标：过渡到 feat/* 分支工作流

过渡方案：
1. 继续在 main 上开发，但：
   - 每个逻辑单元 commit 一次
   - 不要频繁 push
   - 积累到完整功能后再 push
2. 当项目规模扩大时，切换到分支工作流
```