@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "GAME_DIR="
set "PLUGINS_DIR="
set "TMP_DIR=%TEMP%\expies-curse-installer"

set "EXPIES_CURSE_LOCAL=%SCRIPT_DIR%Scav.ExpiesCurse.dll"
set "WORLD_SETTINGS_LOCAL=%SCRIPT_DIR%Scav.WorldSettingsHelper.dll"
set "SCAVLIB_LOCAL=%SCRIPT_DIR%ScavLib.API.dll"

set "EXPIES_CURSE_URL="
set "WORLD_SETTINGS_URL="
set "SCAVLIB_URL=https://github.com/Kanisuko/ScavLib-API-DLL-Repository/releases/download/v0.8.0/ScavLib.API.dll"

echo Scav.ExpiesCurse installer
echo.
echo This will install Scav.ExpiesCurse.dll and required dependencies into your BepInEx plugins folder.
echo.
choice /M "Continue"
if errorlevel 2 exit /b 0

call :resolve_game_dir
if errorlevel 1 goto :fail

call :assert_game_dir
if errorlevel 1 goto :fail

set "PLUGINS_DIR=%GAME_DIR%\BepInEx\plugins"
if not exist "%PLUGINS_DIR%" mkdir "%PLUGINS_DIR%"

echo [Expie's Curse Installer] Game directory: %GAME_DIR%
echo [Expie's Curse Installer] Plugin directory: %PLUGINS_DIR%

call :install_dll "Scav.ExpiesCurse.dll" "%EXPIES_CURSE_LOCAL%" "%EXPIES_CURSE_URL%" "%PLUGINS_DIR%\Scav.ExpiesCurse.dll" required
if errorlevel 1 goto :fail

call :install_dll "Scav.WorldSettingsHelper.dll" "%WORLD_SETTINGS_LOCAL%" "%WORLD_SETTINGS_URL%" "%PLUGINS_DIR%\Scav.WorldSettingsHelper.dll" required
if errorlevel 1 goto :fail

call :install_dll "ScavLib.API.dll" "%SCAVLIB_LOCAL%" "%SCAVLIB_URL%" "%PLUGINS_DIR%\ScavLib.API.dll" required
if errorlevel 1 goto :fail

set EXITCODE=0
goto :finish

:resolve_game_dir
for %%D in (
    "C:\Program Files (x86)\Steam\steamapps\common\Casualties Unknown Demo"
    "C:\Program Files\Steam\steamapps\common\Casualties Unknown Demo"
    "D:\SteamLibrary\steamapps\common\Casualties Unknown Demo"
    "E:\SteamLibrary\steamapps\common\Casualties Unknown Demo"
) do (
    if exist "%%~D\CasualtiesUnknown_Data\Managed\Assembly-CSharp.dll" (
        set "GAME_DIR=%%~D"
        exit /b 0
    )
)

echo [Expie's Curse Installer] Could not find the game automatically.
set /p GAME_DIR=Enter full game path: 
if not defined GAME_DIR exit /b 1
exit /b 0

:assert_game_dir
if not exist "%GAME_DIR%\CasualtiesUnknown_Data\Managed\Assembly-CSharp.dll" (
    echo [Expie's Curse Installer] Invalid game directory: missing Assembly-CSharp.dll
    exit /b 1
)
if not exist "%GAME_DIR%\BepInEx" (
    echo [Expie's Curse Installer] BepInEx folder not found. Install BepInEx 5 first.
    exit /b 1
)
exit /b 0

:install_dll
set "NAME=%~1"
set "LOCAL_PATH=%~2"
set "URL=%~3"
set "DEST=%~4"
set "REQUIRED=%~5"

if exist "%LOCAL_PATH%" (
    echo [Expie's Curse Installer] Installing %NAME% from local file.
    copy /y "%LOCAL_PATH%" "%DEST%" >nul
    exit /b 0
)

if defined URL (
    echo [Expie's Curse Installer] Downloading %NAME%...
    if not exist "%TMP_DIR%" mkdir "%TMP_DIR%"
    powershell -Command "Invoke-WebRequest -Uri '%URL%' -OutFile '%TMP_DIR%\download.tmp'"
    if errorlevel 1 (
        echo [Expie's Curse Installer] Failed to download %NAME%.
        exit /b 1
    )
    copy /y "%TMP_DIR%\download.tmp" "%DEST%" >nul
    del /q "%TMP_DIR%\download.tmp" >nul 2>nul
    exit /b 0
)

if /I "%REQUIRED%"=="required" (
    echo [Expie's Curse Installer] Missing %NAME% and no download URL is configured.
    echo [Expie's Curse Installer] Put %NAME% next to this installer or configure a release URL.
    exit /b 1
)
exit /b 0

:fail
set EXITCODE=1

:finish
if exist "%TMP_DIR%" rmdir /s /q "%TMP_DIR%" >nul 2>nul

echo.
if %EXITCODE% EQU 0 (
    echo Install finished.
) else (
    echo Install failed with exit code %EXITCODE%.
)

pause
exit /b %EXITCODE%
