# 龙女仆睡眠钟

基于 2026 年 11 月 9 日的《龙女仆睡眠钟》项目备忘制作的 Unity 四页离线原型。使用 Unity 6000.6.8f1。

![Windows 实机中的睡前记录页](Docs/Bedtime.png)

## 运行

- **Windows 运行包**　下载并解压 [DragonMaidSleepClock-v0.1.1-Windows-x64.zip](Downloads/DragonMaidSleepClock-v0.1.1-Windows-x64.zip)，打开其中的 `DragonMaidSleepClock.exe`。在开发目录里也可直接打开 `Builds/Windows/DragonMaidSleepClock.exe`。
- **Unity 编辑器**　在 Unity Hub 中添加本工程，打开 `Assets/Scenes/NightWatch.unity`，按 Play。可从编辑器菜单 `Build → Build Windows Prototype` 重新生成 Windows 版本。

## 体验一晚

1. 在「值夜选择」点「请缇露露值夜」，再点「确认并返回睡前」。也可以选择自己记录。
2. 点「开始睡眠记录」。从这一步起，本晚值夜人已确定。可切换页面或关闭程序，重新打开后继续计时。
3. 醒来后点「结束记录，查看报告」，保存实际起止时间。想快速预览晨间流程，可点「以模拟一夜结束本次记录」，这会用一条标明为模拟的整夜样例完成本次记录。
4. 在报告中查看本晚时长、起止时间和值夜便笺。点「查看 7 天趋势」，选择有记录的日期，再点「查看这晚的报告」。

## 替换 PNG 与预留 AN 地址

**Windows 运行包**里编辑 `DragonMaidSleepClock_Data/StreamingAssets/artwork-urls.json`。**Unity 工程**里编辑 [artwork-urls.json](Assets/StreamingAssets/artwork-urls.json)。保存后重启程序，或在编辑器中重新进入 Play。

| 配置项 | 显示位置 |
| --- | --- |
| `nightWatch.pngUrl` / `nightWatch.anUrl` | 睡前记录页和值夜选择页共用的缇露露插画 |
| `morning.pngUrl` / `morning.anUrl` | 早晨报告里的早餐插画 |
| `icons.pngUrl` | 全局图标，使用一行 16 个等大正方形的透明 PNG |

例如，在配置文件旁新建 `art` 文件夹，放入自己的 `night.png`，按下面的示例填写。联网素材也可以直接填自己的 HTTPS 文件地址。留空的 PNG 项使用内置图；替换失败时同样会显示内置图。

```json
{
  "nightWatch": { "pngUrl": "art/night.png", "anUrl": "art/night.an" },
  "morning": { "pngUrl": "", "anUrl": "" },
  "icons": { "pngUrl": "" }
}
```

`.an` 使用自定义格式，目前预留播放器接口。接入时继承 [AnArtworkPlayer](Assets/Scripts/AnArtworkPlayer.cs)，把组件挂在主场景的 `Dragon Sleep App` 上。`Configure(slot, url)` 接收夜间或晨间插画的地址，`GetFrame(slot)` 返回当前帧纹理。没有播放器，或播放器尚未返回帧时，页面显示该位置的 PNG 或内置图。

图标从左到右依次为 `moon, cup, sun, chart, clock, arrow, leaf, check, book, star, play, stop, drop, chevron, calendar, person`。可参考[内置图标排列](Assets/Resources/Art/JournalIcons.png)。

## 本地数据

首次启动会在过去六天中填入五条模拟记录，今天和其中一天留空。新记录会更新早晨报告和趋势。趋势按本地起床日期显示最近七个自然日，每天取结束时间最晚的一条，平均时长只计算有记录的日期。输入来自手动计时或标记为模拟的数据。

Windows 默认保存位置为 `%USERPROFILE%\AppData\LocalLow\DragonMaidStudio\DragonMaidSleepClock\sleep-journal.json`。在命令行附加 `-journalPath "完整 JSON 文件路径"` 可使用独立演示存档。
