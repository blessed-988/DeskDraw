@echo off
setlocal

echo ========================================
echo DeskDraw Release Build
echo ========================================

set SOLUTION=DeskDraw.sln
set CONFIG=Release
set RUNTIME=win-x64
set OUTPUT=DeskDraw\bin\%CONFIG%\net6.0-windows\fd

echo.
echo [1/2] Publishing single-file exe...
dotnet publish %SOLUTION% -c %CONFIG% -r %RUNTIME% --no-self-contained ^
  -p:PublishSingleFile=true ^
  -p:PublishReadyToRun=true ^
  -p:StripSymbols=true ^
  -o "%OUTPUT%"

if %errorlevel% neq 0 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo [2/2] Copying to project root...
copy /Y "%OUTPUT%\DeskDraw.exe" "DeskDraw.exe" >nul

echo.
echo ========================================
echo Done!
echo   DeskDraw.exe  -> %CD%\DeskDraw.exe
echo ========================================
echo.
pause
