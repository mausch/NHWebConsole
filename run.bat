@echo off
msbuild NHWebConsole.sln
start lib\cassini.exe /path:SampleApp /pm:Specific /p:8112
start http://localhost:8112/nhconsole/index.aspx?q=from+System.Object