# UpLingo quick map

This directory is the only source of truth. Do not decompile the deployed EXE for future edits. The `src` folder beside a deployed EXE is only a release copy and must be regenerated from here.

Read `MAINTENANCE.md` first and open only the files routed for the current task. `WidgetForm.cs` is intentionally split with partial files for creator and usage-tracking work.

## Project

- C# WinForms, .NET Framework 4.8, SDK-style project.
- Main project: `Win11SubscriberWidget.csproj`.
- Main UI: `Win11SubscriberWidget/WidgetForm.cs`.
- Creator card partial: `Win11SubscriberWidget/WidgetForm.Creator.cs`.
- Professional usage partial: `Win11SubscriberWidget/WidgetForm.UsageTracking.cs`.
- Build: `..\dotnet-sdk\dotnet.exe build .\Win11SubscriberWidget.csproj -c Release --no-restore` from this directory.
- Output executable: `bin\Release\net48\UpLingo-1.10.1.exe`.
- Logic check: run `bin\Release\net48\UpLingo-1.10.1.exe --logic-test`, then confirm `logic-test.log` says `PASS`.
- Interface check: run the EXE with `--fetch-test`; it writes `fetch-test.log` and does not save fetched data.
- Standard local check: run `./RunChecks.ps1`.

## File map

- `RefreshService.cs`: parallel channel refresh and YouTube creator-history orchestration.
- `SubscriberFetcher.cs`: Bilibili/YouTube subscriber API parsing. It intentionally does not fetch play/view counts.
- `CreatorFeed.cs`: current-month uploads-playlist refresh, first-run current-year scan, and RSS fallback.
- `WidgetRules.cs`: pure monthly-check, milestone, benchmark-percent, and weekly-report gating rules.
- `ConfigStore.cs`: atomic/versioned config writes, deferred periodic saves, backup, and corrupt-config recovery.
- `ChannelIdentity.cs`: stable platform/channel keys and history migration after YouTube resolves to a UC id.
- `MilestoneTracker.cs`, `AchievementsForm.cs`, `FireworksForm.cs`: achievements, logs, and fireworks.
- `WeeklyReportStore.cs`: one text report block per week.
- `CardPanel.cs`: sparkline, 12-month 投稿 check-in grid, and paired red/yellow + blue/purple flame streak drawing.
- `SettingsForm.cs`: settings UI and benchmark-channel editing.
- `AppLogger.cs`: redacted, rotating `widget_debug.log`.
- `ProfessionalAppCatalog.cs`, `UsageStatsService.cs`, `ProfessionalCheckinService.cs`: local professional-software tracking, daily check-ins, and time-based make-up-card rewards.
- `CreatorCheckinService.cs`, `MakeupPickerForm.cs`, `UsageStatsForm.cs`: 投稿/专业软件 check-in sections, make-up-card selection, and the statistics window.

## Behavior that must stay intact

- 投稿打卡: green=actual upload, purple=make-up card, red=confirmed past month without upload, yellow=current unfinished month, gray=future or history not yet confirmed. A real extra upload in a month grants one 投稿补签卡 per upload after the first. The first refresh after this feature is introduced only establishes a video-count baseline and must never retroactively issue cards for old videos.
- 专业软件打卡: an observed known professional app counts for the day; green=actual open, purple=make-up card, red=missed day, yellow=today pending. Every completed 2 hours of observed active time in a day grants one 专业补签卡.
- `monthly_updates` is persistent history. Never recompute old months only from the limited RSS feed.
- Failed full-year history scans are retried with a persisted delay (one day for failure, seven days for truncation). Full-history retries calibrate counts only and must never mint retroactive 投稿补签卡.
- A weekly report is generated only after every configured non-benchmark channel has fresh data. Cached startup data must not finalize it.
- Refresh results carry an in-memory generation id; stale results after settings changes must be discarded.
- Closing hides to tray. Normal launch is single-instance. `--self-test`, `--logic-test`, `--fetch-test`, and `--achievements` may run separately.
- If tray carousel is off, show static YouTube subscriber count.
- Do not restore any play/view-count request unless the UI feature is explicitly requested again.
- Opening any known professional app completes the daily software check-in. Usage/reward time counts only while a known app owns the foreground window and Windows has seen user input within 5 minutes. Keep the aggregation local; never inspect files, projects, window titles, or send usage data anywhere. Cards may only repair historical days/months and never turn current pending time into a completed check-in.

## Release safety

- A release may include EXE, EXE config, README, helper BAT files, slogan template, config example, and a fresh `src` copy.
- Never package `config.json`, `config.json.bak`, API keys, `subscriber_events.log`, `weekly_report.txt`, `widget_debug.log`, or test logs.
- When deploying to an existing local folder, preserve its `motivational_slogans.txt` along with all user data; only the distributable package uses the default slogan template.
- Bump `Properties/AssemblyInfo.cs`, rebuild, run both tests, then synchronize deployed `src` from this directory.
- The legacy namespace and source folder remain `Win11SubscriberWidget`; changing them has no user-facing benefit. The public product name is `UpLingo`.
