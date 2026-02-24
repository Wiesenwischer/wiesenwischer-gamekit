@echo off
REM Wrapper: Ruft build-server.ps1 mit allen uebergebenen Parametern auf
powershell -ExecutionPolicy Bypass -File "%~dp0build-server.ps1" %*
