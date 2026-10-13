@echo off
REM ==========================================================
REM  Winterdeveloper - SON KIS  EXE olusturucu
REM  Cift tikla, dist klasorunde tek bir exe olusur.
REM  Gereken: .NET 8 SDK  (https://dotnet.microsoft.com/download/dotnet/8.0)
REM ==========================================================

set GAME_EXE=SonKis

where dotnet >nul 2>nul
if errorlevel 1 (
  echo [HATA] dotnet bulunamadi. .NET 8 SDK kurman gerekiyor.
  pause
  exit /b 1
)

echo Exe olusturuluyor...
dotnet publish SonKis.csproj -c Release -r win-x64 --self-contained true ^
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:AssemblyName=%GAME_EXE% -o dist

if errorlevel 1 (
  echo [HATA] Derleme basarisiz. Yukaridaki hata mesajini bana gonder.
  pause
  exit /b 1
)

echo.
echo TAMAM! Oyun burada: dist\%GAME_EXE%.exe
pause
