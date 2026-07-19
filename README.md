# UpLingo

UpLingo 是一款面向内容创作者的 Windows 桌面数据小组件。它把 B 站与 YouTube 的粉丝数据、成长目标、投稿节奏和专业软件使用记录放在同一个轻量界面中。

## 主要功能

- B 站 / YouTube 粉丝数、托盘轮播与缓存回退。
- 里程碑进度、对标频道、差距百分比、成就记录和全屏烟花。
- 周报、粉丝趋势与自定义励志标语。
- YouTube 投稿打卡、连续投稿与补签卡。
- OBS、Premiere Pro、After Effects、Photoshop、达芬奇、剪映、Audition、FL Studio、Blender 等专业软件的本地使用统计与连续打开打卡。

## 下载与运行

在本仓库的 **Releases** 页面下载最新分发包，解压后运行 `UpLingo-1.10.0.exe`。

系统要求：Windows 10 / Windows 11，以及 .NET Framework 4.8。

首次运行后，右键小组件或托盘图标进入“设置”，填写 B 站 UID、YouTube 频道和 YouTube Data API Key。也可以先将 `work/UpLingo-distributable-assets/config.example.json` 复制为 `config.json` 后再填写。

## 从源码构建

本项目使用 .NET Framework 4.8。准备好对应开发环境后，在仓库根目录运行：

```powershell
.\BuildRelease.ps1
```

脚本会编译程序、执行检查，并在 `outputs` 目录生成可分发压缩包。

## 隐私说明

账号配置、YouTube API Key、粉丝缓存、打卡记录、使用时长、日志和周报均只保存在本地，已被 Git 忽略，不会上传到本仓库。
