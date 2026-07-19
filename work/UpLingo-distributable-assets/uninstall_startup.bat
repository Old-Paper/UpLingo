@echo off
chcp 65001 >nul
powershell -NoProfile -ExecutionPolicy Bypass -Command "$startup=[Environment]::GetFolderPath('Startup'); $old=Join-Path $startup 'Win11 Subscriber Widget.lnk'; if(Test-Path $old){Remove-Item $old -Force}; Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'Win11SubscriberWidget' -ErrorAction SilentlyContinue; Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'UpLingo' -ErrorAction SilentlyContinue; Write-Host '已移除开机启动'"
pause
