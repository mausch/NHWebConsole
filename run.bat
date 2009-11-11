@echo off
call build.bat
start lib\cassini.exe "%cd%\SampleApp" 8112
start http://localhost:8112/nhconsole/index.aspx?q=from+System.Object