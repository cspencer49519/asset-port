@echo off
setlocal EnableDelayedExpansion

set "GAME_PATH="

:parse_args
if "%~1"=="" goto args_done
if /i "%~1"=="/GamePath" (
    set "GAME_PATH=%~2"
    shift
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
    echo ERROR: Could not find manifest.json.
    exit /b 1
)

call :resolve_python
if errorlevel 1 exit /b 1

call :resolve_game_path
if errorlevel 1 exit /b 1

set "FAIL_COUNT=0"
set "WARN_COUNT=0"

for /f "delims=" %%V in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" patchVersion') do set "PATCH_VERSION=%%V"

echo Verifying install at: %GAME_ROOT%
echo Expected patch version: %PATCH_VERSION%
echo.

for /f "delims=" %%P in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" paths.gameExe') do call :check_file "%%P" 1
call :check_file "BepInEx" 1
for /f "delims=" %%P in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" paths.patchDll') do call :check_file "%%P" 1
for /f "delims=" %%P in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" paths.expansionModDll') do call :check_file "%%P" 1
for /f "delims=" %%P in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" paths.newCardsModDll') do call :check_file "%%P" 1
for /f "delims=" %%P in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" paths.cardArtAssets') do call :check_file "%%P" 0
for /f "delims=" %%P in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" paths.sharedAssets') do call :check_file "%%P" 0

for /f "delims=" %%P in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" paths.sharedAssets') do set "SHARED_ASSETS_REL=%%P"
set "SHARED_PATH=%GAME_ROOT%\%SHARED_ASSETS_REL%"
if exist "%SHARED_PATH%" (
    for %%F in ("%SHARED_PATH%") do set "SHARED_SIZE=%%~zF"
    for /f "delims=" %%V in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" sharedAssets.vanillaBytes') do set "VANILLA_BYTES=%%V"
    for /f "delims=" %%V in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" sharedAssets.portedMinBytes') do set "PORTED_BYTES=%%V"
    if !SHARED_SIZE! LEQ !VANILLA_BYTES! (
        call :warn "sharedassets0.assets is vanilla size (!SHARED_SIZE! bytes) - re-run install without /SkipAssets"
    ) else if !SHARED_SIZE! GEQ !PORTED_BYTES! (
        call :pass "sharedassets0.assets looks ported (!SHARED_SIZE! bytes)"
    )
)

for /f "delims=" %%P in ('"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" paths.logFile') do set "LOG_FILE=%%P"
set "LOG_PATH=%GAME_ROOT%\%LOG_FILE%"

if exist "%LOG_PATH%" (
    call :pass "%LOG_FILE%"
    for /f "usebackq delims=" %%M in (`"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" logSuccessMarkers`) do (
        findstr /C:"%%M" "%LOG_PATH%" >nul 2>&1
        if errorlevel 1 (
            call :warn "Log missing (launch game once): %%M"
        ) else (
            call :pass "Log contains: %%M"
        )
    )
    for /f "usebackq delims=" %%M in (`"%PYTHON%" "%SCRIPT_DIR%\read_manifest.py" "%MANIFEST%" logFailureMarkers`) do (
        findstr /C:"%%M" "%LOG_PATH%" >nul 2>&1
        if not errorlevel 1 (
            call :fail "Log contains failure marker: %%M"
        )
    )
) else (
    call :warn "No log yet - launch the game once, then re-run this script."
)

echo.
if %FAIL_COUNT%==0 if %WARN_COUNT%==0 (
    echo All checks passed.
    exit /b 0
)
if %FAIL_COUNT%==0 (
    echo Passed with %WARN_COUNT% warning^(s^). See docs/TROUBLESHOOTING.md
    exit /b 0
)

echo %FAIL_COUNT% failure^(s^), %WARN_COUNT% warning^(s^). See docs/TROUBLESHOOTING.md
exit /b 1

:check_file
set "REL_PATH=%~1"
set "REQUIRED=%~2"
if exist "%GAME_ROOT%\%REL_PATH%" (
    call :pass "%REL_PATH%"
    goto :eof
)
if "%REQUIRED%"=="1" (
    call :fail "Missing: %REL_PATH%"
) else (
    call :warn "Missing (optional): %REL_PATH%"
)
goto :eof

:pass
echo [PASS] %~1
goto :eof

:fail
echo [FAIL] %~1
set /a FAIL_COUNT+=1
goto :eof

:warn
echo [WARN] %~1
set /a WARN_COUNT+=1
goto :eof

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
echo ERROR: Python not found. Install Python or use Verify-TCG071Install.ps1 instead.
exit /b 1

:resolve_python_done
exit /b 0

:resolve_game_path
if defined GAME_PATH (
    if exist "%GAME_PATH%\Card Shop Simulator.exe" (
        for %%I in ("%GAME_PATH%") do set "GAME_ROOT=%%~fI"
        exit /b 0
    )
    echo ERROR: Invalid game path: %GAME_PATH%
    exit /b 1
)

for %%I in ("%RELEASE_ROOT%\..\TCG Card Shop Simulator") do set "SIBLING=%%~fI"
if exist "%SIBLING%\Card Shop Simulator.exe" (
    set "GAME_ROOT=%SIBLING%"
    exit /b 0
)

echo ERROR: Pass /GamePath to the folder containing Card Shop Simulator.exe
exit /b 1
