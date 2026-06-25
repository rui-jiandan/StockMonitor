---
name: "git-commit-push"
description: "查看 git 变更状态，执行 commit 并 push 到远程仓库。Invoke when user says 提交/推送/push/commit/提交推送/推送代码。"
---

# Git 提交与推送 Skill

## 功能说明

对当前工作区的代码变更执行一次完整的 Git 提交流程：查看变更 → 暂存所有修改 → 提交 → 推送到远程仓库。适用于标准的日常代码提交场景。

## 触发时机

- 用户说"提交"、"推送"、"提交推送"、"commit"、"push"、"提交代码"、"推送代码"
- 用户完成功能开发或问题修复后需要保存变更
- 用户在会话末尾要求保存本次修改

## 前置条件

1. 当前目录是一个 Git 仓库（有 `.git` 目录）
2. 有已配置的远程仓库（通常是 `origin`）
3. 用户已完成代码变更

## 操作步骤

### Step 1：查看当前状态

先确认当前状态，以便向用户说明发生了哪些变更：

```powershell
cd <工作区根目录>
git status
```

典型输出：

```
On branch v2.0.0
Changes not staged for commit:
  modified:   src/StockMonitor/App.xaml.cs
  modified:   src/StockMonitor/UI/Views/MainWindow.xaml.cs
```

同时查看变更统计（可选）：

```powershell
git diff --stat
```

输出格式：

```
src/StockMonitor/App.xaml.cs                 |  38 ++++++++++
src/StockMonitor/UI/Views/MainWindow.xaml.cs | 101 +++++++++++++++++++++++---- 
2 files changed, 127 insertions(+), 12 deletions(-)
```

> **提示**：如果有未跟踪文件（`Untracked files`），也需要一并告诉用户。

### Step 2：暂存所有变更

暂存所有跟踪文件和新文件

```powershell
git add -A
```

`-A` 会同时：
- 跟踪新增文件
- 暂存修改的文件
- 标记已删除文件

### Step 3：提交（要求用户提供 commit message

提交消息应当描述本次修改内容：

```powershell
git commit -m "描述本次修改的标题"
```

提交消息建议：

| 场景 | 推荐格式 | 示例 |
|------|--------|------|
| 修复 Bug | 修复 xxx 问题 | 修复托盘菜单打开操作弹窗时程序异常退出 |
| 新增功能 | 添加 xxx 功能 | 添加预警通知窗口 |
| 重构/优化 | 优化 xxx 逻辑 | 优化弹窗定位逻辑 |
| 配置变更 | 修改 xxx 配置 | 增加 xxx 配置项 |

> **注意**：提交消息使用中文（用户是中文用户），简洁清晰地表达修改内容。

如果用户没有明确提交消息，可以直接用"修复 xxx"或"添加 xxx"。

### Step 4：推送到远程仓库

```powershell
git push
```

正常情况下，会输出类似：

```
Enumerating objects: 15, done.
Counting objects: 100% (15/15), done.
...
Total 8 (delta 6), reused 0 (delta 0)
...
  18b56b7..565a109  v2.0.0 -> v2.0.0
```

最后一行的 `18b56b7..565a109 v2.0.0 -> v2.0.0`代表远程分支更新成功。

> **重要**：如果出现冲突或 `git push` 失败，需要先查看错误信息：
> - `rejected`：远程分支落后，需 `git pull` 拉取最新代码，解决冲突后再 push
> - `fatal: unable to access`：网络或权限问题

## 本次会话中的实际执行示例（供参考）

```powershell
# 1. 查看状态
cd "g:\实时股票\StockMonitor"
git status
git diff --stat

# 2. 暂存所有变更
git add -A

# 3. 提交
git commit -m "修复托盘菜单打开操作弹窗时程序异常退出及弹窗定位问题"

# 4. 推送
git push
```

## 常见问题

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| `nothing to commit` | 工作区没有任何文件变更 | 告知用户没有需要提交的变更 |
| `Your branch is up to date with 'origin/xxx'. 'origin/xxx'.` 但 push 被拒绝 | 本地分支领先远程分支领先远程 | `git pull` 后再 push |
| `nothing to commit` 实际有变更 | 远程分支领先本地 | 先 `git pull` 拉取后再 push |
| `git push` 报权限错误 | 未授权或 SSH key 问题 | 检查 Git 凭证配置 |
| 提交消息中文乱码 | 控制台编码 | 在提交消息包含中文字符 | 可改用英文或确保 UTF-8 编码正常 |
| 有未跟踪文件未被提交 | `git add -A` 后还有未跟踪 | 确认所有文件都已被 add 到暂存区 |

## 注意事项

1. **提交前建议先构建项目：`dotnet build`（如果是 .NET 项目，确保能编译通过再提交。
2. **不要把编译产物目录（如 `bin/`、`obj/`、发布目录）不要提交到仓库，通常这些目录已经加到 `.gitignore`
3. 提交消息建议用中文描述问题场景使用中文用户习惯中文的约定，例如"修复 xxx"
4. **推送到远程失败时不要直接报错给用户，先看错误原因再处理
5. 如果用户未给出提交消息，可以先向用户询问，或直接用一条简短的中文提交消息描述（如"修复 xxx"）
