# UpLingo maintenance map

Use this file to route work before reading source. Avoid opening all of `WidgetForm.cs` unless the task crosses several areas.

## Fast task routing

| Task | Start here | Usually also needed |
|---|---|---|
| Professional-app detection, active time, daily software streak | `WidgetForm.UsageTracking.cs` | `ProfessionalAppCatalog.cs`, `UsageStatsService.cs`, `ProfessionalCheckinService.cs` |
| YouTube upload card, monthly grid, creator messages | `WidgetForm.Creator.cs` | `CreatorFeed.cs`, `CreatorCheckinService.cs`, `WidgetRules.cs` |
| Subscriber/API refresh | `RefreshService.cs` | `SubscriberFetcher.cs`, `CreatorFeed.cs`, `ChannelIdentity.cs` |
| Subscriber rows, header, window movement | `WidgetForm.cs` | `WindowStateButton.cs`, `ChannelEditForm.cs`, `CardPanel.cs`, `CountDisplay.cs`, `MilestoneBar.cs` |
| Weekly report and history | `WeeklyReportStore.cs` | Search `BuildWeeklyReportAndRollBaselines` in `WidgetForm.cs` |
| Achievements and fireworks | `MilestoneTracker.cs` | `AchievementsForm.cs`, `FireworksForm.cs` |
| Settings/config migration | `SettingsForm.cs` | `WidgetConfig.cs`, `ConfigStore.cs`, config model files |
| Application icon | `Resources/UpLingoIcon.svg` | `Resources/Generate-AppIcon.ps1`, `AppIcon.cs`, `.csproj` |
| Packaging/deployment | root `BuildRelease.ps1` | `Properties/AssemblyInfo.cs`, `.csproj`, distributable assets |

## Invariants to preserve

- Opening a known professional app completes that day; reward time requires the app in the foreground plus user input within five minutes.
- A full-history retry may calibrate monthly counts but must never award retroactive creator make-up cards.
- Current-month upload counts prefer the YouTube uploads playlist; RSS is fallback only.
- Cached startup data may render the UI but may not finalize a weekly report.
- Channel identity changes must migrate or reset histories deliberately.
- Existing deployments keep `config.json`, logs, reports, and personal `motivational_slogans.txt`.
- Channel cards use delayed single-click editing so a double-click can open the channel without opening the editor. Preserve the drag threshold when changing these events.
- Window state is one three-state value: `free`, `topmost`, or `locked_topmost`. Legacy booleans remain serialized only for backward compatibility.
- Window resizing is custom because a native `WS_THICKFRAME` produces a white frame on some Windows themes. Keep the form borderless and exercise a real edge drag after changing `CreateParams`, `WndProc`, or the message filter.
- Only open configured YouTube URLs after `ChannelInputValidator` accepts the exact `youtube.com` host or one of its subdomains.

## Verification

Run `RunChecks.ps1` from this directory. It performs a Release build, logic test, and startup self-test. Use root `BuildRelease.ps1 -DeployPath <folder>` only after checks pass.

## Version updates

The executable name comes from `<AssemblyName>` in `Win11SubscriberWidget.csproj`. Runtime display version comes from `AssemblyVersion`. Update `AssemblyInfo.cs`, the project assembly name, manifest, README, and helper BAT files together.
