@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\configure.ps1" %*
