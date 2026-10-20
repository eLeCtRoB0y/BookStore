@echo off
rem Сборка исполняемых файлов в папку Release (не требует установленного .NET на целевом ПК)
chcp 65001 > nul
dotnet publish src\BookStore.App\BookStore.App.csproj -c Release -r win-x64 --self-contained true -o Release
if errorlevel 1 (
  echo Ошибка сборки.
  pause
  exit /b 1
)
echo Готово: Release\BookStore.exe
pause
