@echo off
setlocal
set ROOT=%~dp0..

set MSBUILD=
set VSTEST=

if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" (
  set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
  set "VSTEST=C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
)
if not defined MSBUILD if exist "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
  set "MSBUILD=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
  set "VSTEST=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
)
if not defined MSBUILD if exist "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" (
  set "MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
  set "VSTEST=C:\Program Files\Microsoft Visual Studio\2022\BuildTools\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"
)

if not defined MSBUILD (
  echo MSBuild not found. Install Visual Studio 2022 Build Tools with MSBuild + Test Tools.
  exit /b 1
)
if not exist "%VSTEST%" (
  echo VSTest not found at "%VSTEST%".
  exit /b 1
)

echo Using MSBuild: %MSBUILD%
echo Using VSTest:  %VSTEST%

"%MSBUILD%" "%ROOT%\LIS.Masters.Tests\LIS.Masters.Tests.csproj" /t:Build /p:Configuration=Release /v:minimal
if errorlevel 1 exit /b 1

"%VSTEST%" "%ROOT%\LIS.Masters.Tests\bin\Release\LIS.Masters.Tests.dll" /Logger:console
exit /b %ERRORLEVEL%
