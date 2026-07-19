# UpLingo maintenance map

Use this file to route work before reading source. Avoid opening all of `WidgetForm.cs` unless the task crosses several areas.

## Fast task routing

| Task | Start here | Usually also needed |
|---|---|---|
| Professional-app detection, active time, daily software streak | `WidgetForm.UsageTracking.cs` | `ProfessionalAppCatalog.cs`, `UsageStatsService.cs`, `ProfessionalCheckinService.cs` |
| YouTube upload card, monthly grid, creator messages | `WidgetForm.Creator.cs` | `CreatorFeed.cs`, `CreatorCheckinService.cs`, `WidgetRules.cs` |
| Subscriber/API refresh | `RefreshService.cs` | `SubscriberFetcher.cs`, `CreatorFeed.cs`, `ChannelIdentity.cs` |
| Subscriber rows, header, window movement | `WidgetForm.cs` | `CardPanel.cs`, `CountDisplay.cs`, `MilestoneBar.cs` |
| Weekly report and history | `WeeklyReportStore.cs` | Search `BuildWeeklyReportAndRollBaselines` in `WidgetForm.cs` |
| Achievements and fireworks | `MilestoneTracker.cs` | `AchievementsForm.cs`, `FireworksForm.cs` |
| Settings/config migration | `SettingsForm.cs` | `WidgetConfig.cs`, `ConfigStore.cs`, config model files |
| Packaging/deployment | root `BuildRelease.ps1` | `Properties/AssemblyInfo.cs`, `.csproj`, distributable assets |

## Invariants to preserve

- Opening a known professional app completes that day; reward time requires the app in the foreground plus user input within five minutes.
- A full-history retry may calibrate monthly counts but must never award retroactive creator make-up cards.
- Current-month upload counts prefer the YouTube uploads playlist; RSS is fallback only.
- Cached startup data may render the UI but may not finalize a weekly report.
- Channel identity changes must migrate or reset histories deliberately.
- Existing deployments keep `config.json`, logs, reports, and personal `motivational_slogans.txt`.

## Verification

Run `RunChecks.ps1` from this directory. It performs a Release build, logic test, and startup self-test. Use root `BuildRelease.ps1 -DeployPath <folder>` only after checks pass.

## Version updates

The executable name comes from `<AssemblyName>` in `Win11SubscriberWidget.csproj`. Runtime display version comes from `AssemblyVersion`. Update `AssemblyInfo.cs`, the project assembly name, manifest, README, and helper BAT files together.
