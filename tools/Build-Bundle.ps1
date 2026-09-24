<#
.SYNOPSIS
    Сборка установочного архива плагина PanelKomplekt.

.DESCRIPTION
    1. Собирает решение в конфигурации Release: две сборки из одного кода.
    2. Складывает в dist\PanelKomplekt папку PanelKomplekt.bundle:
         PackageContents.xml
         Contents\R23 — сборка net47 для AutoCAD 2020;
         Contents\R24 — сборка net48 для AutoCAD 2021–2024.
       В каждой папке — загрузчик, плагин, библиотеки для Excel и файл блока.
       Какую папку загружать, AutoCAD выбирает сам по PackageContents.xml.
       Рядом кладутся установщик Install.cmd / Uninstall.cmd / Install.ps1 и инструкция ReadMe.txt.
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

# Сборка → папка в пакете. Должно совпадать с TargetFrameworks в .csproj и с путями в PackageContents.xml.
$Targets = [ordered]@{
    'net47' = 'R23'   # AutoCAD 2020
    'net48' = 'R24'   # AutoCAD 2021–2024
}

# Версия плагина — единственный источник: PanelKomplekt.csproj.
[xml]$project = Get-Content $Csproj -Raw -Encoding UTF8
$Version = ($project.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version
if (-not $Version) { throw "В $Csproj не найден элемент <Version>." }
Write-Host "Версия плагина: $Version"

# 1. Сборка Release (обе сборки сразу).
dotnet build (Join-Path $Root 'PanelKomplekt.sln') -c Release
if ($LASTEXITCODE -ne 0) { throw 'Сборка завершилась с ошибкой.' }

# 2. Сборка папки пакета с нуля.
if (Test-Path $Dist) { Remove-Item $Dist -Recurse -Force }
New-Item -ItemType Directory -Force (Join-Path $Bundle 'Contents') | Out-Null

$manifest = (Get-Content (Join-Path $Installer 'PackageContents.xml') -Raw -Encoding UTF8).Replace('{{Version}}', $Version)
[IO.File]::WriteAllText((Join-Path $Bundle 'PackageContents.xml'), $manifest, (New-Object Text.UTF8Encoding($false)))

foreach ($framework in $Targets.Keys) {
    $series  = $Targets[$framework]
    $target  = Join-Path $Bundle "Contents\$series"
    $plugin  = Join-Path $Root "PanelKomplekt\bin\Release\$framework"
    $loader  = Join-Path $Root "PanelKomplekt.Loader\bin\Release\$framework\PanelKomplekt.Loader.dll"
    New-Item -ItemType Directory -Force $target | Out-Null

    if (-not (Test-Path (Join-Path $plugin 'PanelKomplekt.dll'))) { throw "Не найдена сборка плагина: $plugin\PanelKomplekt.dll" }
    if (-not (Test-Path $loader)) { throw "Не найден загрузчик: $loader" }

    # Загрузчик (его загружает AutoCAD), плагин и все библиотеки, от которых он зависит (ClosedXML и зависимости для Excel).
    Copy-Item $loader $target
    Get-ChildItem $plugin -Filter '*.dll' | ForEach-Object { Copy-Item $_.FullName $target }

    # Файл-шаблон блока панели: плагин ищет его в папке Blocks рядом с PanelKomplekt.dll.
    $blocks = Join-Path $plugin 'Blocks'
    if (-not (Test-Path (Join-Path $blocks 'PK_Panel.dwg'))) { throw "Не найден файл блока: $blocks\PK_Panel.dwg" }
    Copy-Item $blocks $target -Recurse

    Write-Host "Сборка $framework → Contents\$series"
}

foreach ($file in 'Install.cmd', 'Uninstall.cmd', 'Install.ps1', 'ReadMe.txt') {
    Copy-Item (Join-Path $Installer $file) $Package
}

# 3. Архив для отправки.
$Zip = Join-Path $Dist "PanelKomplekt-$Version.zip"
Compress-Archive -Path (Join-Path $Package '*') -DestinationPath $Zip -Force

Write-Host "Готово: $Zip" -ForegroundColor Green
