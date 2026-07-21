@echo off
chcp 65001 >nul
cd /d "%~dp0"
if exist fetch-test.log del fetch-test.log
start /wait "" "%~dp0UpLingo-1.10.2.exe" --fetch-test
if exist fetch-test.log (
  type fetch-test.log
) else (
  echo 没有生成测试日志
)
pause
