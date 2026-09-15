# Структура проекта PanelKomplekt

Плагин AutoCAD 2021 (C#, .NET Framework 4.8, x64). Обновляется при каждом изменении структуры.

```
PanelKomplekt/                         корень репозитория
├── PanelKomplekt.sln                  решение VS 2022
├── CLAUDE.md                          инструкции для Claude (ссылаются на Docs)
├── README.md                          краткое описание, настройка AutoCAD, отладка
├── .editorconfig                      кодировка, отступы, окончания строк
├── .gitignore / .gitattributes        настройки git
├── Docs/                              документация проекта
│   ├── DevelopmentRules.md            обязательные правила разработки
│   ├── ProjectStructure.md            этот файл
│   ├── CommandTemplate.txt            шаблон документации команды
│   └── CustomerQuestions.txt          вопросы заказчику
├── tools/                             вспомогательные скрипты
│   ├── dump.lsp                       выгрузка содержимого DWG в текст (слои, блоки, тексты)
│   └── dump.scr                       скрипт запуска dump.lsp
└── PanelKomplekt/                     проект плагина
    ├── PanelKomplekt.csproj           настройки сборки, ссылки на библиотеки AutoCAD 2021
    ├── start.scr                      NETLOAD собранной DLL при отладке
    ├── Properties/launchSettings.json запуск AutoCAD 2021 по F5
    ├── App.cs                         точка входа (IExtensionApplication), создание ленты
    ├── Core/                          общий код для всех команд
    │   ├── CommandInfo.cs             описание команды (номер, имя, кнопка)
    │   ├── CommandCatalog.cs          список всех команд — источник кнопок ленты
    │   └── CommandRunner.cs           запуск тела команды с обработкой ошибок
    ├── Ribbon/                        лента
    │   ├── RibbonBuilder.cs           построение вкладки по CommandCatalog
    │   └── RibbonCommandHandler.cs    запуск команды AutoCAD по нажатию кнопки
    └── Commands/                      команды, каждая в своей папке
        ├── C100_About/                PK_C100_ABOUT — версия плагина
        │   ├── C100_About.cs
        │   └── C100_About.txt
        └── C101_ShowElementId/        PK_C101_SHOWELEMENTID — ID выбранного элемента (тестовая)
            ├── C101_ShowElementId.cs
            └── C101_ShowElementId.txt
```

## Как связаны части
1. AutoCAD загружает `PanelKomplekt.dll` → вызывает `App.Initialize()`.
2. `App` при первом простое вызывает `RibbonBuilder.Create()`.
3. `RibbonBuilder` берёт список из `CommandCatalog` и создаёт по кнопке на каждую команду.
4. Нажатие кнопки → `RibbonCommandHandler` отправляет в AutoCAD имя команды (`GlobalName`).
5. AutoCAD вызывает метод с `[CommandMethod]` → тело команды выполняется через `CommandRunner.Run`.

## Команды
| Номер | Имя в AutoCAD | Панель | Назначение | Статус |
|---|---|---|---|---|
| C100 | PK_C100_ABOUT | Сервис | Версия плагина | служебная |
| C101 | PK_C101_SHOWELEMENTID | Сервис | ID выбранного элемента | тестовая |
