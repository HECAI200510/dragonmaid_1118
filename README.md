# 龙女仆睡眠钟

基于 2026 年 11 月 9 日的《龙女仆睡眠钟》项目备忘制作的 Unity 四页离线原型。使用 Unity 6000.6.8f1。

![Windows 实机中的睡前记录页](Docs/Bedtime.png)

## 运行

- **Windows 运行包**　解压 `DragonMaidSleepClock-v0.1.0-Windows-x64.zip`，打开其中的 `DragonMaidSleepClock.exe`。在开发目录里也可直接打开 `Builds/Windows/DragonMaidSleepClock.exe`。
- **Unity 编辑器**　在 Unity Hub 中添加本工程，打开 `Assets/Scenes/NightWatch.unity`，按 Play。可从编辑器菜单 `Build → Build Windows Prototype` 重新生成 Windows 版本。

## 体验一晚

1. 在「值夜选择」点「请缇露露值夜」，再点「确认并返回睡前」。也可以选择自己记录。
2. 点「开始睡眠记录」。从这一步起，本晚值夜人已确定。可切换页面或关闭程序，重新打开后继续计时。
3. 醒来后点「结束记录，查看报告」，保存实际起止时间。想快速预览晨间流程，可点「以模拟一夜结束本次记录」，这会用一条标明为模拟的整夜样例完成本次记录。
4. 在报告中查看本晚时长、起止时间和值夜便笺。点「查看 7 天趋势」，选择有记录的日期，再点「查看这晚的报告」。

## 本地数据

首次启动会在过去六天中填入五条模拟记录，今天和其中一天留空。新记录会更新早晨报告和趋势。趋势按本地起床日期显示最近七个自然日，每天取结束时间最晚的一条，平均时长只计算有记录的日期。输入来自手动计时或标记为模拟的数据。

Windows 默认保存位置为 `%USERPROFILE%\AppData\LocalLow\DragonMaidStudio\DragonMaidSleepClock\sleep-journal.json`。在命令行附加 `-journalPath "完整 JSON 文件路径"` 可使用独立演示存档。
