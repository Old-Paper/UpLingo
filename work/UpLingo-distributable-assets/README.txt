UpLingo v1.10.0
===============

系统要求
--------
- Windows 10 / Windows 11
- .NET Framework 4.8

快速开始
--------
1. 解压整个文件夹，不要只单独复制 EXE。
2. 双击 UpLingo-1.10.0.exe 或 run_widget.bat。
3. 首次运行会生成 config.json。
4. 右键小组件或托盘图标打开“设置”，填写 B站 UID、YouTube 频道和 YouTube Data API Key。
5. 也可以把 config.example.json 复制为 config.json 后手动编辑。

主要功能
--------
- B站 / YouTube 粉丝数与托盘轮播。
- 对标频道、差距百分比和追赶进度。
- 粉丝里程碑、成就记录和全屏烟花。
- 每日趋势、里程碑进度和每周周报。
- YouTube 视频动态、12个月投稿打卡和红黄投稿连胜。
- 专业软件打卡：打开 OBS、PR、AE、PS、达芬奇、剪映、AU、FL Studio、Blender 等任一软件即可完成当天打卡；蓝紫火焰显示连续天数。补签卡时长仅在专业软件位于前台且最近 5 分钟有人操作时累计。
- 使用统计内分别显示投稿与专业软件打卡。专业软件当天累计每满 2 小时获得 1 张专业补签卡；一个月额外投稿 1 期获得 1 张投稿补签卡（首次启用时只记录历史基线，不会为旧视频补发卡）。补签仅能用于已经过去的漏签日/月。
- 缓存回退：网络失败时继续显示上次数据。
- 配置自动备份与损坏恢复，重复启动时自动拦截。
- 专业软件使用统计：OBS、PR、AE、PS、达芬奇、剪映、AU、FL Studio、Blender 等仅在实际使用后显示本地时长。

自定义励志标语
--------------
编辑 motivational_slogans.txt，每行一句。
程序每次刷新时随机切换；标题栏最多显示 16 个字符，悬停可查看全文。

常用脚本
--------
- check_config.bat：测试所有频道配置。
- install_startup.bat：加入当前用户开机启动。
- uninstall_startup.bat：取消开机启动。

运行后生成的数据
----------------
- config.json：配置、缓存和成就领取状态，可能包含 YouTube API Key，请勿公开分享。
- subscriber_events.log：成就、预警和周报日志。
- weekly_report.txt：独立周报历史。
- widget_debug.log：自动轮换的故障诊断日志，不记录 API Key。

分发说明
--------
本压缩包不包含任何个人账号配置、API Key、缓存、日志或周报。
