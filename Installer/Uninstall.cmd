@echo off
rem PanelKomplekt: uninstall plugin for AutoCAD 2021 (logic is in Install.ps1)
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1" -Uninstall
