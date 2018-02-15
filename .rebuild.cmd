@echo off
PowerShell.exe -ExecutionPolicy ByPass -Command "& { %~dp0build.ps1 -Script %~dp0build.cake -Target %1 -Configuration %2; exit $LASTEXITCODE }"
pause