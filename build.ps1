Write-Host "========================================"
Write-Host "DeskDraw Release Build"
Write-Host "========================================"

Write-Host ""
Write-Host "[1/1] Publishing single-file exe to project root..."
dotnet publish DeskDraw.sln -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=false `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:PublishReadyToRun=true `
  -p:StripSymbols=true `
  -o $PWD

Write-Host ""
Write-Host "========================================"
Write-Host "Done!"
Write-Host "  DeskDraw.exe  -> $PWD\DeskDraw.exe"
Write-Host "========================================"
Write-Host ""
pause
