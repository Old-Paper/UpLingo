@echo off
chcp 65001 >nul
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -Command "$startup=[Environment]::GetFolderPath('Startup'); $old=Join-Path $startup 'Win11 Subscriber Widget.lnk'; if(Test-Path $old){Remove-Item $old -Force}; $exe=(Resolve-Path '.\UpLingo-1.10.0.exe').Path; Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'Win11SubscriberWidget' -ErrorAction SilentlyContinue; New-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'UpLingo' -Value ('\"' + $exe + '\"') -PropertyType String -Force | Out-Null; Write-Host '已加入开机启动:' $exe"
pause
