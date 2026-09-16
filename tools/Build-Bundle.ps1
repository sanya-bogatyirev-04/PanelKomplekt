<#
.SYNOPSIS
    Сборка установочного архива плагина PanelKomplekt.

.DESCRIPTION
    1. Собирает решение в конфигурации Release.
    2. Складывает в dist\PanelKomplekt папку PanelKomplekt.bundle (PackageContents.xml + загрузчик + плагин),
       установщик Install.cmd / Uninstall.cmd / Install.ps1 и инструкцию ReadMe.txt.
    3. Упаковывает всё в dist\PanelKomplekt-<версия>.zip — этот архив отправляется заказчику.
    Версия берётся из <Version> в PanelKomplekt\PanelKomplekt.csproj.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\Build-Bundle.ps1
#>
$ErrorActionPreference = 'Stop'

$Root      = Split-Path $PSScriptRoot -Parent
$Csproj    = Join-Path $Root 'PanelKomplekt\PanelKomplekt.csproj'
$Installer = Join-Path $Root 'Installer'
$Dist      = Join-Path $Root 'dist'
$Package   = Join-Path $Dist 'PanelKomplekt'
$Bundle    = Join-Path $Package 'PanelKomplekt.bundle'

# Версия плагина — единственный источник: PanelKomplekt.csproj.
[xml]$project = Get-Content $Csproj -Raw -Encoding UTF8
$Version = ($project.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
if (-not $Version) { throw "В $Csproj не найден элемент <Version>." }
Write-Host "Версия плагина: $Version"

# 1. Сборка Release.
dotnet build (Join-Path $Root 'PanelKomplekt.sln') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Сборка завершилась с ошибкой.' }

# Основной плагин и загрузчик (AutoCAD загружает загрузчик, а он — плагин).
$Dlls = @(
    (Join-Path $Root 'PanelKomplekt\bin\Release\PanelKomplekt.dll'),
    (Join-Path $Root 'PanelKomplekt.Loader\bin\Release\PanelKomplekt.Loader.dll')
)
foreach ($dll in $Dlls) { if (-not (Test-Path $dll)) { throw "Не найдена собранная DLL: $dll" } }

# 2. Сборка папки пакета с нуля.
if (Test-Path $Dist) { Remove-Item $Dist -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $Bundle 'Contents') | Out-Null

$manifest = (Get-Content (Join-Path $Installer 'PackageContents.xml') -Raw -Encoding UTF8).Replace('{{Version}}', $Version)
[IO.File]::WriteAllText((Join-Path $Bundle 'PackageContents.xml'), $manifest, (New-Object Text.UTF8Encoding($false)))

Copy-Item $Dlls (Join-Path $Bundle 'Contents')
foreach ($file in 'Install.cmd', 'Uninstall.cmd', 'Install.ps1', 'ReadMe.txt') {
    Copy-Item (Join-Path $Installer $file) $Package
}

# 3. Архив для отправки.
$Zip = Join-Path $Dist "PanelKomplekt-$Version.zip"
Compress-Archive -Path (Join-Path $Package '*') -DestinationPath $Zip -Force

Write-Host "Готово: $Zip" -ForegroundColor Green
