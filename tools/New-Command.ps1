<#
.SYNOPSIS
    Создание заготовки новой команды PanelKomplekt по правилам проекта.

.DESCRIPTION
    1. Создаёт папку PanelKomplekt\Commands\<Номер>_<Название>.
    2. Создаёт в ней код команды (.cs) и документацию (.md) по шаблону Docs\CommandTemplate.md.
    3. Добавляет команду в PanelKomplekt\Core\CommandCatalog.cs — кнопка появится на ленте.
    4. Добавляет строку в таблицу команд в Docs\ProjectStructure.md.
    Дерево файлов в Docs\ProjectStructure.md, описание в .md и иконки кнопки нужно дописать вручную.

.PARAMETER Number
    Номер команды: буква C и три цифры, например C201 (сотни — см. Docs\DevelopmentRules.md).

.PARAMETER Name
    Название на английском в стиле PascalCase, например PanelLayout.

.PARAMETER Title
    Краткое название по-русски, например "Раскладка панелей".

.PARAMETER Panel
    Панель на вкладке ленты, например "Раскладка".

.PARAMETER RibbonText
    Подпись кнопки на ленте, например "Раскладка".

.PARAMETER RepoRoot
    Корень репозитория. По умолчанию — родительская папка tools.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\New-Command.ps1 -Number C201 -Name PanelLayout -Title "Раскладка панелей" -Panel "Раскладка" -RibbonText "Раскладка"
#>
param(
    [Parameter(Mandatory)] [ValidatePattern('^C\d{3}$')] [string]$Number,
    [Parameter(Mandatory)] [ValidatePattern('^[A-Z][A-Za-z0-9]+$')] [string]$Name,
    [Parameter(Mandatory)] [string]$Title,
    [Parameter(Mandatory)] [string]$Panel,
    [Parameter(Mandatory)] [string]$RibbonText,
    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

# В Windows PowerShell 5.1 $PSScriptRoot пуст в значениях параметров по умолчанию, поэтому корень вычисляется здесь.
if (-not $RepoRoot) { $RepoRoot = Split-Path $PSScriptRoot -Parent }

$FullName   = "${Number}_$Name"
$GlobalName = "PK_${Number}_$($Name.ToUpperInvariant())"
$Project    = Join-Path $RepoRoot 'PanelKomplekt'
$CommandDir = Join-Path $Project "Commands\$FullName"
$Catalog    = Join-Path $Project 'Core\CommandCatalog.cs'
$Structure  = Join-Path $RepoRoot 'Docs\ProjectStructure.md'
$Template   = Join-Path $RepoRoot 'Docs\CommandTemplate.md'

$Utf8Bom   = New-Object Text.UTF8Encoding($true)   # .cs — по .editorconfig
$Utf8NoBom = New-Object Text.UTF8Encoding($false)  # .md

# Проверки до создания файлов, чтобы не оставить проект в полусозданном состоянии.
if (Test-Path $CommandDir) { throw "Папка команды уже существует: $CommandDir" }
$existing = Get-ChildItem (Join-Path $Project 'Commands') -Directory | Where-Object { $_.Name -like "${Number}_*" }
if ($existing) { throw "Номер $Number уже занят командой $($existing.Name)." }
foreach ($path in $Catalog, $Structure, $Template) {
    if (-not (Test-Path $path)) { throw "Не найден файл: $path" }
}

# Подстановка значений в шаблон.
function Expand-Tokens([string]$Text) {
    return $Text.Replace('{{Name}}', $FullName).Replace('{{Number}}', $Number).
        Replace('{{GlobalName}}', $GlobalName).Replace('{{Title}}', $Title).
        Replace('{{Panel}}', $Panel).Replace('{{RibbonText}}', $RibbonText)
}

$code = @'
using Autodesk.AutoCAD.Runtime;
using PanelKomplekt.Core;

// Регистрация класса команд: при наличии ExtensionApplication AutoCAD ищет команды только в перечисленных классах.
[assembly: CommandClass(typeof(PanelKomplekt.Commands.{{Name}}))]

namespace PanelKomplekt.Commands
{
    /// <summary>
    /// {{Number}}. {{Title}}.
    /// Документация: {{Name}}.md в папке команды.
    /// </summary>
    public class {{Name}}
    {
        /// <summary>Имя команды в AutoCAD.</summary>
        public const string GlobalName = "{{GlobalName}}";

        /// <summary>Описание команды для ленты и каталога.</summary>
        public static readonly CommandInfo Info = new CommandInfo
        {
            Key = nameof({{Name}}),
            Number = "{{Number}}",
            GlobalName = GlobalName,
            RibbonText = "{{RibbonText}}",
            RibbonPanel = "{{Panel}}",
            Description = "{{Title}}"
        };

        /// <summary>
        /// Точка входа команды.
        /// </summary>
        [CommandMethod(GlobalName)]
        public void Execute()
        {
            CommandRunner.Run(Info, doc =>
            {
                // TODO: реализовать алгоритм команды.
                doc.Editor.WriteMessage("\nКоманда {{Number}} ещё не реализована.\n");
            });
        }
    }
}
'@

# 1–2. Папка, код и документация.
New-Item -ItemType Directory -Force $CommandDir | Out-Null
[IO.File]::WriteAllText((Join-Path $CommandDir "$FullName.cs"), (Expand-Tokens $code).Replace("`r`n", "`n").Replace("`n", "`r`n"), $Utf8Bom)
$doc = Expand-Tokens ([IO.File]::ReadAllText($Template, $Utf8NoBom))
[IO.File]::WriteAllText((Join-Path $CommandDir "$FullName.md"), $doc, $Utf8NoBom)

# 3. Строка в каталоге команд — перед закрывающей скобкой списка.
$lines = [Collections.Generic.List[string]]([IO.File]::ReadAllLines($Catalog, $Utf8NoBom))
$closeIndex = -1
for ($i = $lines.Count - 1; $i -ge 0; $i--) { if ($lines[$i].Trim() -eq '};') { $closeIndex = $i; break } }
if ($closeIndex -lt 0) { throw "В $Catalog не найден конец списка команд ('};')." }
$lines.Insert($closeIndex, "            $FullName.Info,")
[IO.File]::WriteAllLines($Catalog, $lines, $Utf8Bom)

# 4. Строка в таблице команд: сразу после последней строки таблицы в разделе «## Команды»
#    (после таблицы в файле могут идти другие разделы, поэтому дописывать в конец файла нельзя).
$row = "| $Number | $GlobalName | $Panel | $RibbonText | $Title | в разработке |"
$structureLines = [Collections.Generic.List[string]]([IO.File]::ReadAllLines($Structure, $Utf8NoBom))
$sectionIndex = -1
for ($i = 0; $i -lt $structureLines.Count; $i++) { if ($structureLines[$i].Trim() -eq '## Команды') { $sectionIndex = $i; break } }
if ($sectionIndex -lt 0) { throw "В $Structure не найден раздел «## Команды»." }
$insertIndex = -1
for ($i = $sectionIndex + 1; $i -lt $structureLines.Count; $i++) {
    if ($structureLines[$i].StartsWith('|')) { $insertIndex = $i + 1 }
    elseif ($insertIndex -ge 0) { break }
}
if ($insertIndex -lt 0) { throw "В разделе «## Команды» файла $Structure не найдена таблица." }
$structureLines.Insert($insertIndex, $row)
[IO.File]::WriteAllLines($Structure, $structureLines, $Utf8NoBom)

Write-Host "Создана команда $FullName ($GlobalName)" -ForegroundColor Green
Write-Host "  $CommandDir"
Write-Host 'Дальше:'
Write-Host "  1. Дописать алгоритм в $FullName.cs и описание в $FullName.md."
Write-Host '  2. Добавить папку команды в дерево в Docs\ProjectStructure.md.'
Write-Host "  3. Добавить иконки $($FullName)_16.png и $($FullName)_32.png (tools\Generate-Icons.ps1)."
Write-Host '  4. Собрать проект и проверить в AutoCAD.'
