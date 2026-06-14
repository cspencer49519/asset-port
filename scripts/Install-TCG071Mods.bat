@echo off
setlocal EnableDelayedExpansion

set "GAME_PATH="
set "SKIP_ASSETS=0"
set "FORCE=0"
set "WHATIF=0"

:parse_args
if "%~1"=="" goto args_done
if /i "%~1"=="/GamePath" (
    set "GAME_PATH=%~2"
    shift
    shift
    goto parse_args
)
if /i "%~1"=="/SkipAssets" (
    set "SKIP_ASSETS=1"
    shift
    goto parse_args
)
if /i "%~1"=="/Force" (
    set "FORCE=1"
    shift
    goto parse_args
)
if /i "%~1"=="/WhatIf" (
    set "WHATIF=1"
    shift
    goto parse_args
)
if not defined GAME_PATH (
    set "GAME_PATH=%~1"
    shift
    goto parse_args
)
shift
goto parse_args

:args_done

set "SCRIPT_DIR=%~dp0"
if "%SCRIPT_DIR:~-1%"=="\" set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"
for %%I in ("%SCRIPT_DIR%\..") do set "RELEASE_ROOT=%%~fI"
set "MANIFEST=%RELEASE_ROOT%\manifest.json"

if not exist "%MANIFEST%" (
    echo ERROR: Could not find manifest.json at "%MANIFEST%"
    exit /b 1
)

call :resolve_python
if errorlevel 1 exit /b 1

for /f "delims=" %%V in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" patchVersion') do set "PATCH_VERSION=%%V"
for /f "delims=" %%V in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" paths.gameExe') do set "GAME_EXE=%%V"
for /f "delims=" %%V in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" paths.dataFolder') do set "DATA_FOLDER=%%V"
for /f "delims=" %%V in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" paths.sharedAssets') do set "SHARED_ASSETS=%%V"
for /f "delims=" %%V in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" paths.sharedAssetsResS') do set "SHARED_ASSETS_RESS=%%V"
for /f "delims=" %%V in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" paths.sharedAssetsResource') do set "SHARED_ASSETS_RESOURCE=%%V"

call :resolve_game_path
if errorlevel 1 exit /b 1

call :resolve_patch_dll
if errorlevel 1 exit /b 1

echo ==^> Release root: %RELEASE_ROOT%
echo ==^> Game root:    %GAME_ROOT%
echo ==^> Patch DLL:    %PATCH_DLL%

if not exist "%GAME_ROOT%\%GAME_EXE%" (
    echo ERROR: Invalid game folder ^(missing %GAME_EXE%^): %GAME_ROOT%
    exit /b 1
)

if not exist "%GAME_ROOT%\BepInEx" (
    echo WARN BepInEx folder not found. Install BepInEx ^(Nexus mod 27^) before playing.
)

set "PLUGIN_DIR=%GAME_ROOT%\BepInEx\plugins\TCGShopExpansionMod071Patch"
set "PLUGIN_DLL=%PLUGIN_DIR%\TCGShopExpansionMod071Patch.dll"

if not exist "%PLUGIN_DIR%" (
    if "%WHATIF%"=="0" mkdir "%PLUGIN_DIR%"
    if "%WHATIF%"=="1" echo [WhatIf] Create directory "%PLUGIN_DIR%"
)

if exist "%PLUGIN_DLL%" if "%FORCE%"=="0" (
    for %%A in ("%PLUGIN_DLL%") do set "EXISTING_SIZE=%%~zA"
    for %%A in ("%PATCH_DLL%") do set "INCOMING_SIZE=%%~zA"
    if "!EXISTING_SIZE!"=="!INCOMING_SIZE!" (
        echo  OK  Patch DLL already installed ^(same size^).
        goto install_assets
    )
    echo WARN Patch DLL exists and differs. Re-run with /Force to overwrite.
    goto install_assets
)

if "%WHATIF%"=="1" (
    echo [WhatIf] Copy "%PATCH_DLL%" to "%PLUGIN_DLL%"
) else (
    copy /Y "%PATCH_DLL%" "%PLUGIN_DLL%" >nul
    echo  OK  Installed patch DLL v%PATCH_VERSION%
)

:install_assets
if "%SKIP_ASSETS%"=="1" (
    echo WARN Skipped sharedassets install ^(/SkipAssets^).
    goto manual_steps
)

echo ==^> Ported sharedassets trio ^(Genobear card frames^)
set "ASSETS_SOURCE=%RELEASE_ROOT%\assets"
if not exist "%ASSETS_SOURCE%" set "ASSETS_SOURCE=%RELEASE_ROOT%\output"

if not exist "%ASSETS_SOURCE%" (
    echo WARN No assets/ or output/ folder in release - skipping sharedassets install.
    echo WARN Card frames will stay vanilla until you get a full release zip with assets/.
    goto manual_steps
)

for /f "tokens=2 delims==" %%I in ('wmic os get localdatetime /value 2^>nul') do set "DATETIME=%%I"
if not defined DATETIME set "DATETIME=00000000-000000"
set "TIMESTAMP=%DATETIME:~0,8%-%DATETIME:~8,6%"
set "DATA_DIR=%GAME_ROOT%\%DATA_FOLDER%"
set "BACKUP_DIR=%DATA_DIR%\_backup_sharedassets_%TIMESTAMP%"
set "INSTALLED_ANY=0"

call :install_asset "%SHARED_ASSETS%"
call :install_asset "%SHARED_ASSETS_RESS%"
call :install_asset "%SHARED_ASSETS_RESOURCE%"

if "%INSTALLED_ANY%"=="1" echo  OK  Sharedassets backup folder: %BACKUP_DIR%

:manual_steps
echo.
echo ==^> Manual steps still required
echo   1. Install Nexus mods + Genobear ^(phases 1-3 in docs/INSTALL-071.md^) if not done yet
echo   2. Run: scripts\Verify-TCG071Install.bat "%GAME_ROOT%"
echo   3. Launch game, press F1, configure ExpansionMod ^(see docs/VERSION_MATRIX.md^)
echo   4. Do not use /SkipAssets on a normal install - card frames need the ported trio
echo.
echo  OK  Install complete.
exit /b 0

:resolve_python
set "PYTHON="
where python >nul 2>&1
if not errorlevel 1 (
    set "PYTHON=python"
    goto resolve_python_done
)
where py >nul 2>&1
if not errorlevel 1 (
    set "PYTHON=py -3"
    goto resolve_python_done
)
echo ERROR: Python not found. Install Python or use Install-TCG071Mods.ps1 instead.
exit /b 1

:resolve_python_done
exit /b 0

:resolve_game_path
if defined GAME_PATH (
    if exist "%GAME_PATH%\%GAME_EXE%" (
        for %%I in ("%GAME_PATH%") do set "GAME_ROOT=%%~fI"
        exit /b 0
    )
    echo ERROR: Game folder not found at "%GAME_PATH%"
    exit /b 1
)

for %%I in ("%RELEASE_ROOT%\..\TCG Card Shop Simulator") do set "SIBLING=%%~fI"
if exist "%SIBLING%\%GAME_EXE%" (
    echo WARN Using sibling game folder: %SIBLING%
    set "GAME_ROOT=%SIBLING%"
    exit /b 0
)

set "STEAM1=%ProgramFiles(x86)%\Steam\steamapps\common\TCG Card Shop Simulator"
set "STEAM2=%ProgramFiles%\Steam\steamapps\common\TCG Card Shop Simulator"
if exist "%STEAM1%\%GAME_EXE%" (
    echo WARN Using Steam default path: %STEAM1%
    set "GAME_ROOT=%STEAM1%"
    exit /b 0
)
if exist "%STEAM2%\%GAME_EXE%" (
    echo WARN Using Steam default path: %STEAM2%
    set "GAME_ROOT=%STEAM2%"
    exit /b 0
)

echo ERROR: Game folder not found. Pass /GamePath to the folder containing %GAME_EXE%
exit /b 1

:resolve_patch_dll
set "PATCH_DLL=%RELEASE_ROOT%\patches\TCGShopExpansionMod071Patch.dll"
if exist "%PATCH_DLL%" exit /b 0
set "PATCH_DLL=%RELEASE_ROOT%\TCGShopExpansionMod071Patch\bin\Release\netstandard2.1\TCGShopExpansionMod071Patch.dll"
if exist "%PATCH_DLL%" (
    echo WARN Using dev build output for patch DLL.
    exit /b 0
)
echo ERROR: Patch DLL not found. Run scripts\Build-Release.ps1 or dotnet build first.
exit /b 1

:install_asset
set "REL_PATH=%~1"
for %%N in ("%REL_PATH%") do set "ASSET_NAME=%%~nxN"
set "SRC=%ASSETS_SOURCE%\%ASSET_NAME%"
set "DST=%DATA_DIR%\%ASSET_NAME%"
if not exist "%SRC%" (
    echo WARN Missing source asset: %SRC%
    exit /b 0
)
if exist "%DST%" (
    if not exist "%BACKUP_DIR%" (
        if "%WHATIF%"=="0" mkdir "%BACKUP_DIR%"
        if "%WHATIF%"=="1" echo [WhatIf] Create directory "%BACKUP_DIR%"
    )
    if "%WHATIF%"=="1" (
        echo [WhatIf] Backup "%DST%" to "%BACKUP_DIR%\%ASSET_NAME%"
    ) else (
        copy /Y "%DST%" "%BACKUP_DIR%\%ASSET_NAME%" >nul
        echo  OK  Backed up %ASSET_NAME%
    )
)
if "%WHATIF%"=="1" (
    echo [WhatIf] Copy "%SRC%" to "%DST%"
) else (
    copy /Y "%SRC%" "%DST%" >nul
    echo  OK  Installed %ASSET_NAME%
)
set "INSTALLED_ANY=1"
exit /b 0
