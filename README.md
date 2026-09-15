# PanelKomplekt

Плагин для AutoCAD 2021 (C#, .NET Framework 4.8, x64): раскладка сэндвич-панелей по фасадам и спецификация.

## Документация
- [Docs/DevelopmentRules.md](Docs/DevelopmentRules.md) — правила разработки (коммиты, именование, ветки).
- [Docs/ProjectStructure.md](Docs/ProjectStructure.md) — структура проекта и список команд.
- [Docs/CommandTemplate.txt](Docs/CommandTemplate.txt) — шаблон документации команды.
- [Docs/CustomerQuestions.txt](Docs/CustomerQuestions.txt) — вопросы заказчику.

## Разовая настройка AutoCAD (доверенные папки)
В AutoCAD включена защита загрузки (`SECURELOAD = 1`), поэтому DLL и скрипты из чужих папок блокируются.
1. AutoCAD → `ПАРАМЕТРЫ` (OPTIONS) → вкладка «Файлы» → «Надежные расположения».
2. «Добавить» → `C:\Users\bogatyrev\source\repos\PanelKomplekt\PanelKomplekt\bin\Debug`.
3. «Добавить» → `C:\Users\bogatyrev\source\repos\PanelKomplekt\tools`.
4. OK.

## Отладка
1. Открыть `PanelKomplekt.sln` в VS 2022, профиль запуска «AutoCAD 2021».
2. F5: сборка → запуск AutoCAD → `start.scr` выполняет NETLOAD.
3. На ленте появится вкладка «PanelKomplekt».
4. Загруженную DLL выгрузить нельзя: после изменения кода закрыть AutoCAD и снова нажать F5.

## Сборка из командной строки
```
dotnet build PanelKomplekt.sln -c Debug
```
