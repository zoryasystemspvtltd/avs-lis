@echo off
setlocal
set SERVER=.\SQLEXPRESS
set DB=AVSLIS
set SCRIPT=%~dp0SeedDiagnosticReportDemo.sql

echo Running diagnostic report demo seed on %SERVER% / %DB% ...
sqlcmd -S %SERVER% -d %DB% -E -i "%SCRIPT%"
if errorlevel 1 (
  echo Seed FAILED.
  exit /b 1
)
echo Seed completed.
exit /b 0
