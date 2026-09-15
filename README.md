# PanelKomplekt

Плагин для AutoCAD 2021 (C#, .NET Framework 4.8, x64): раскладка сэндвич-панелей по фасадам и спецификация.

## Структура
- `PanelKomplekt.sln` — решение, открывать в Visual Studio 2022.
- `PanelKomplekt/` — проект плагина.
  - `PluginApp.cs` — точка входа (`IExtensionApplication`).
  - `Commands/` — команды AutoCAD (`[CommandMethod]`).
  - `start.scr` — скрипт, загружающий собранную DLL при отладке.
  - `Properties/launchSettings.json` — запуск AutoCAD 2021 по F5.
- `tools/` — вспомогательные скрипты (выгрузка содержимого DWG в текст для анализа).
- `Вопросы_заказчику.txt` — вопросы по функционалу.

## Разовая настройка AutoCAD (доверенные папки)
В AutoCAD включена защита загрузки (`SECURELOAD = 1`), поэтому DLL и скрипты из чужих папок блокируются.
1. AutoCAD → `ПАРАМЕТРЫ` (OPTIONS) → вкладка «Файлы» → «Надежные расположения».
2. «Добавить» → `C:\Users\bogatyrev\source\repos\PanelKomplekt\PanelKomplekt\bin\Debug`.
3. «Добавить» → `C:\Users\bogatyrev\source\repos\PanelKomplekt\tools`.
4. OK.

## Отладка
1. Открыть `PanelKomplekt.sln` в VS 2022, профиль запуска «AutoCAD 2021».
2. F5: сборка → запуск AutoCAD → `start.scr` выполняет NETLOAD.
3. В AutoCAD ввести `PK_INFO`.
4. Загруженную DLL выгрузить нельзя: после изменения кода закрыть AutoCAD и снова нажать F5.

## Сборка из командной строки
```
dotnet build PanelKomplekt.sln -c Debug
```
