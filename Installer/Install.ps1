<#
.SYNOPSIS
    Установка и удаление плагина PanelKomplekt для AutoCAD 2021.

.DESCRIPTION
    Копирует папку PanelKomplekt.bundle в C:\Program Files\Autodesk\ApplicationPlugins.
    Это надёжное расположение AutoCAD: оттуда плагин загружается автоматически при запуске,
    без предупреждений безопасности. Для записи в Program Files нужны права администратора —
    скрипт сам запросит их.

.PARAMETER Uninstall
    Удалить плагин вместо установки.
#>
param([switch]$Uninstall)

$ErrorActionPreference = 'Stop'

$BundleName = 'PanelKomplekt.bundle'
$TargetRoot = Join-Path $env:ProgramFiles 'Autodesk\ApplicationPlugins'
$TargetPath = Join-Path $TargetRoot $BundleName
$SourcePath = Join-Path $PSScriptRoot $BundleName

# Ожидание нажатия Enter перед закрытием окна, чтобы пользователь успел прочитать результат.
function Exit-WithPause([int]$Code) {
    Write-Host ''
    Read-Host 'Нажмите Enter, чтобы закрыть окно' | Out-Null
    exit $Code
}

# Без прав администратора перезапускаем этот же скрипт с запросом прав (окно UAC).
$principal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    $argList = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$PSCommandPath`"")
    if ($Uninstall) { $argList += '-Uninstall' }
    try {
        Start-Process powershell.exe -Verb RunAs -ArgumentList $argList
    }
    catch {
        Write-Host 'Операция отменена: права администратора не получены.' -ForegroundColor Red
        Exit-WithPause 1
    }
    exit 0
}

try {
    # Запущенный AutoCAD держит DLL плагина открытой — заменить или удалить её нельзя.
    if (Get-Process -Name acad -ErrorAction SilentlyContinue) {
        Write-Host 'AutoCAD запущен. Закройте AutoCAD и запустите установку ещё раз.' -ForegroundColor Yellow
        Exit-WithPause 1
    }

    if ($Uninstall) {
        if (Test-Path $TargetPath) {
            Remove-Item $TargetPath -Recurse -Force
            Write-Host "Плагин PanelKomplekt удалён из $TargetRoot" -ForegroundColor Green
        }
        else {
            Write-Host 'Плагин PanelKomplekt не установлен.'
        }
        Exit-WithPause 0
    }

    if (-not (Test-Path $SourcePath)) {
        Write-Host "Не найдена папка $BundleName рядом с установщиком. Распакуйте архив полностью и повторите." -ForegroundColor Red
        Exit-WithPause 1
    }

    # Старая версия удаляется целиком, чтобы не осталось лишних файлов.
    if (Test-Path $TargetPath) { Remove-Item $TargetPath -Recurse -Force }
    New-Item -ItemType Directory -Force $TargetRoot | Out-Null
    Copy-Item $SourcePath $TargetRoot -Recurse -Force

    # Файлы из скачанного архива помечены Windows как «из интернета» — .NET откажется загружать такую DLL.
    Get-ChildItem $TargetPath -Recurse -File | Unblock-File

    Write-Host "Плагин PanelKomplekt установлен в $TargetPath" -ForegroundColor Green
    Write-Host 'Запустите AutoCAD 2021: на ленте появится вкладка «PanelKomplekt».'
    Exit-WithPause 0
}
catch {
    Write-Host "Ошибка: $($_.Exception.Message)" -ForegroundColor Red
    Exit-WithPause 1
}
