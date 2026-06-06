@echo off
setlocal
set ROOT=%~dp0..
set MSBUILD="C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
set VSTEST="C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\TestWindow\vstest.console.exe"

if not exist %MSBUILD% (
  echo MSBuild not found. Install Visual Studio 2022 Build Tools.
  exit /b 1
)

%MSBUILD% "%ROOT%\LIS.Masters.Tests\LIS.Masters.Tests.csproj" /t:Build /p:Configuration=Release /v:minimal
if errorlevel 1 exit /b 1

%VSTEST% "%ROOT%\LIS.Masters.Tests\bin\Release\LIS.Masters.Tests.dll" /Logger:console
exit /b %ERRORLEVEL%
