@echo off
REM Run all AVILIS seed scripts against local SQLEXPRESS / AVSLIS
setlocal
set SERVER=.\SQLEXPRESS
set DATABASE=AVSLIS
set SCRIPTDIR=%~dp0

echo ============================================
echo AVILIS CRUD Test Data Seed
echo Server: %SERVER%  Database: %DATABASE%
echo ============================================
echo.

where sqlcmd >nul 2>&1
if errorlevel 1 (
    echo ERROR: sqlcmd not found. Install SQL Server command-line tools.
    exit /b 1
)

echo [1/3] SeedSampleData.sql ...
sqlcmd -S %SERVER% -d %DATABASE% -i "%SCRIPTDIR%SeedSampleData.sql" -b
if errorlevel 1 goto :failed

echo.
echo [2/4] SeedFullDatabaseInsert.sql ...
sqlcmd -S %SERVER% -d %DATABASE% -i "%SCRIPTDIR%SeedFullDatabaseInsert.sql" -b
if errorlevel 1 goto :failed

echo.
echo [3/4] SeedAllCrudTestData.sql ...
sqlcmd -S %SERVER% -d %DATABASE% -i "%SCRIPTDIR%SeedAllCrudTestData.sql" -b
if errorlevel 1 goto :failed

echo.
echo [4/4] VerifyCrudSeedData.sql ...
sqlcmd -S %SERVER% -d %DATABASE% -i "%SCRIPTDIR%VerifyCrudSeedData.sql" -b
if errorlevel 1 goto :failed

echo.
echo ============================================
echo Seed completed successfully.
echo ============================================
exit /b 0

:failed
echo.
echo SEED FAILED. Check SQL errors above.
exit /b 1
