# 三个 UI 修复实施计划

## 问题 1：添加股票时纯数字代码自动添加市场前缀

### 根因

`PositionEditViewModel.AddNewStock()` 直接将用户输入的 `NewCode` 传给 `IPositionService.AddStock()`，没有调用已有的 `DataMigrator.NormalizeCode()` 方法进行前缀补全。

### 修复

文件: `src/StockMonitor/UI/ViewModels/PositionEditViewModel.cs`

在 `AddNewStock()` 方法中，调用 `DataMigrator.NormalizeCode(NewCode.Trim())` 处理后再传给 `AddStock`。

```csharp
[RelayCommand]
private void AddNewStock()
{
    if (string.IsNullOrEmpty(NewCode)) return;
    var normalizedCode = DataMigrator.NormalizeCode(NewCode.Trim());
    _positionService.AddStock(normalizedCode);
    RefreshList();
    NewCode = string.Empty;
}
```

需要添加 `using StockMonitor.Data;`。

---

## 问题 2：操作列表未显示股票名称

### 根因

`PositionEditDialog.xaml` 中 ListBox 使用 `DisplayMemberPath="Code"`，只显示代码不显示名称。应改为显示 "名称(代码)" 格式。

### 修复

文件: `src/StockMonitor/UI/Views/PositionEditDialog.xaml`

将 `DisplayMemberPath="Code"` 替换为自定义 ItemTemplate，同时显示名称和代码：

```xml
<ListBox Grid.Row="0" ItemsSource="{Binding StockList}" SelectedItem="{Binding SelectedStock}"
         Height="150">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <TextBlock>
                <Run Text="{Binding Name, Mode=OneWay}"/>
                <Run Text="("/>
                <Run Text="{Binding Code, Mode=OneWay}"/>
                <Run Text=")"/>
            </TextBlock>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

---

## 问题 3：双击主面板隐藏功能没有了

### 根因

之前的修复中移除了 Window 的 `MouseDoubleClick` 事件，只保留了 TaskbarIcon 的 `DoubleClickCommand`。但用户期望的是**双击主面板（Border 区域）**也能切换隐藏。

问题在于窗口 `Background="Transparent"` + `AllowsTransparency=True`，透明区域不响应鼠标事件。解决方案：给 Border 添加 `MouseDoubleClick` 事件，因为 Border 有实际背景色（`#E6000000`），可以接收鼠标事件。

### 修复

文件: `src/StockMonitor/UI/Views/MainWindow.xaml`

在 Border 标签上添加 `MouseDoubleClick="Border_MouseDoubleClick"`。

文件: `src/StockMonitor/UI/Views/MainWindow.xaml.cs`

添加 `Border_MouseDoubleClick` 方法，调用 `ToggleVisibility()`：

```csharp
private void Border_MouseDoubleClick(object sender, MouseButtonEventArgs e)
{
    ToggleVisibility();
}
```

---

## 文件变更清单

| 文件 | 变更 |
|------|------|
| `src/StockMonitor/UI/ViewModels/PositionEditViewModel.cs` | AddNewStock 中调用 NormalizeCode |
| `src/StockMonitor/UI/Views/PositionEditDialog.xaml` | ListBox 用自定义模板显示名称+代码 |
| `src/StockMonitor/UI/Views/MainWindow.xaml` | Border 添加 MouseDoubleClick 事件 |
| `src/StockMonitor/UI/Views/MainWindow.xaml.cs` | 添加 Border_MouseDoubleClick 方法 |
