---
name: "dotnet-publish"
description: "构建并发布 .NET 项目到指定输出目录。Invoke when user says 发布/publish/dotnet publish，或需要把编译产物拷贝到发布目录。"
---

# .NET 项目发布 Skill

## 功能说明

在 Windows 环境下，使用 `dotnet build` 编译项目，再用 `dotnet publish` 把发布产物拷贝到用户指定的目录。发布前会先尝试终止同名进程（避免文件被锁定）。

## 触发时机

- 用户说"发布"、"publish"、"打发布包"、"发布到 xxx 目录"
- 用户要求把编译产物拷贝到特定路径（如 `H:\xxx`、`D:\Publish\xxx`）
- WPF / WinForms / 控制台等 .NET 项目均可

## 前置条件

1. 工作区包含 `.csproj` 项目文件（通常在 `src/` 下）
2. 目标框架已确定（如 `net10.0-windows`）
3. 用户已给出发布目录（例如 `H:\Stock小工具v2`）

若用户未给出发布目录，先向用户询问。

## 操作步骤

### Step 1：获取项目信息

在工作区根目录执行以下命令，确认项目位置和目标框架：

```
Get-ChildItem -Recurse -Filter "*.csproj" | Select-Object FullName
```

通常项目文件位于：
- `src/<ProjectName>/<ProjectName>.csproj`（示例项目中：`src/StockMonitor/StockMonitor.csproj`）

目标框架通过读取 `.csproj` 中的 `<TargetFramework>` 元素获取。

### Step 2：构建（Release 配置）

```powershell
cd <工作区根目录>
dotnet build src/<ProjectName>/<ProjectName>.csproj -c Release 2>&1 | Select-Object -Last 20
```

确保出现 `0 个警告，0 个错误`。如果有错误，需要先解决再继续。

### Step 3：终止正在运行的程序（避免文件锁定）

发布前需要先杀掉同名进程，否则 `dll/exe` 会被锁定导致发布失败：

```powershell
Get-Process -Name "<ProjectName>" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
```

进程名通常与项目文件名一致（例如 `StockMonitor` 对应 `StockMonitor.exe`）。

### Step 4：发布到目标目录

```powershell
cd <工作区根目录>
dotnet publish src/<ProjectName>/<ProjectName>.csproj `
  -c Release `
  -f <TargetFramework> `
  --self-contained false `
  -o "<发布目录>" `
  2>&1 | Select-Object -Last 10
```

参数说明：

| 参数 | 说明 |
|------|------|
| `-c Release` | Release 配置（优化后的代码，体积更小） |
| `-f net10.0-windows` | 目标框架（从 .csproj 读取） |
| `--self-contained false` | 依赖框架部署（不包含 .NET 运行时）。如果用户的目标机器未装 .NET 运行时，改为 `--self-contained true -r win-x64` |
| `-o "路径"` | 发布输出目录 |

### Step 5：验证发布结果

确认发布目录下有产物：

```powershell
Get-ChildItem "<发布目录>" | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize
```

应包含以下核心文件：
- `<项目名>.exe` - 主程序入口
- `<项目名>.dll` - 程序集
- `*.deps.json`、`*.runtimeconfig.json` - .NET 运行时配置
- 其他依赖 dll（如 `CommunityToolkit.Mvvm.dll`、`Hardcodet.NotifyIcon.Wpf.dll` 等）
- 项目配置文件（如 `config.json`、`stocks.json` 等）

## 本次会话中的实际执行示例（供参考）

```powershell
# 发布目录：H:\Stock小工具v2
# 项目文件：src/StockMonitor/StockMonitor.csproj
# 目标框架：net10.0-windows
# 部署模式：依赖框架（--self-contained false）

Get-Process -Name "StockMonitor" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 1
cd "g:\实时股票\StockMonitor"
dotnet publish src/StockMonitor/StockMonitor.csproj -c Release -f net10.0-windows --self-contained false -o "H:\Stock小工具v2" 2>&1 | Select-Object -Last 10
```

## 常见问题

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| 发布失败，提示文件被 `xxx (pid)` 锁定 | 程序正在运行 | 先 `Stop-Process -Name xxx -Force` 杀掉进程，等 1-2 秒再发布 |
| 目标机器无法运行，提示需要安装 .NET 运行时 | `--self-contained false` 依赖框架部署 | 改为 `--self-contained true -r win-x64` 重新发布（文件体积显著增加，但无需装 .NET） |
| 构建时有中文乱码 | PowerShell 5 默认编码问题 | 不影响编译结果；若需中文输出正常，可设 `[Console]::OutputEncoding = [System.Text.Encoding]::UTF8` |

## 注意事项

- 发布目录如果已有文件，会被覆盖。重要数据建议用户先备份。
- 发布前务必确认 `dotnet build` 已经成功（0 警告 0 错误），再执行 publish。
- 第一次在新机器上运行发布版程序时，需要目标机器安装对应版本的 .NET 运行时（除非用 `--self-contained true`）。
