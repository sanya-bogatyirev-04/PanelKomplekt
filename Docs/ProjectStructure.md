# Структура проекта PanelKomplekt

Плагин AutoCAD 2021 (C#, .NET Framework 4.8, x64). Обновляется при каждом изменении структуры.

## Папки и файлы
```
PanelKomplekt/                         корень репозитория
├── PanelKomplekt.sln                  решение VS 2022
├── README.md                          главная страница: для пользователя и для программиста
├── CHANGELOG.md                       журнал изменений простым языком
├── LICENSE                            закрытый проект, все права защищены
├── CLAUDE.md                          инструкции для Claude (ссылаются на Docs)
├── .editorconfig                      кодировка, отступы, окончания строк
├── .gitignore / .gitattributes        настройки git (dist/ не хранится, *.dwg — двоичные)
├── .github/workflows/build.yml        автосборка на GitHub и релиз по тегу v*
├── Docs/                              документация
│   ├── UserGuide.md                   руководство пользователя AutoCAD
│   ├── DeveloperGuide.md              руководство разработчика
│   ├── DevelopmentRules.md            обязательные правила разработки
│   ├── ProjectStructure.md            этот файл
│   ├── CommandTemplate.md             шаблон описания команды (используется New-Command.ps1)
│   ├── Glossary.md                    словарь терминов
│   └── CustomerQuestions.txt          вопросы заказчику
├── Installer/                         исходники установщика
│   ├── PackageContents.xml            описание пакета для автозагрузчика AutoCAD
│   ├── Install.cmd / Uninstall.cmd    запуск установки / удаления двойным щелчком
│   ├── Install.ps1                    логика установки и удаления
│   └── ReadMe.txt                     краткая инструкция внутри архива
├── tools/                             вспомогательные скрипты
│   ├── New-Command.ps1                заготовка новой команды
│   ├── Generate-Icons.ps1             рисование иконок кнопок ленты (16 и 32 px)
│   ├── Build-Bundle.ps1               сборка установочного архива в dist/
│   ├── dump.lsp                       выгрузка содержимого DWG в текст
│   └── dump.scr                       скрипт запуска dump.lsp
└── PanelKomplekt/                     проект плагина
    ├── PanelKomplekt.csproj           версия, ссылки на AutoCAD (локально или из NuGet)
    ├── start.scr                      NETLOAD собранной DLL при отладке
    ├── Properties/launchSettings.json запуск AutoCAD 2021 по F5
    ├── App.cs                         точка входа (IExtensionApplication), создание ленты
    ├── Core/                          общий код для всех команд
    │   ├── CommandInfo.cs             описание команды (номер, имя, кнопка)
    │   ├── CommandCatalog.cs          список всех команд — источник кнопок ленты
    │   ├── CommandRunner.cs           запуск тела команды с обработкой ошибок
    │   ├── IconLoader.cs              загрузка иконок кнопок из ресурсов DLL
    │   ├── PluginInfo.cs              сведения о плагине: версия, заказчик, разработчик, технологии
    │   └── AcadWindow.cs              главное окно AutoCAD как владелец диалогов (MessageBox, формы)
    ├── Ribbon/                        лента
    │   ├── RibbonBuilder.cs           построение вкладки по CommandCatalog
    │   └── RibbonCommandHandler.cs    запуск команды AutoCAD по нажатию кнопки
    └── Commands/                      команды, каждая в своей папке
        ├── C100_About/                PK_C100_ABOUT — окно «О плагине»
        │   ├── C100_About.cs
        │   ├── C100_About.md
        │   └── C100_About_16.png / _32.png    иконки кнопки
        └── C101_ShowElementId/        PK_C101_SHOWELEMENTID — ID выбранного элемента
            ├── C101_ShowElementId.cs
            ├── C101_ShowElementId.md
            └── C101_ShowElementId_16.png / _32.png
```

## Установочный архив
`tools/Build-Bundle.ps1` создаёт `dist/PanelKomplekt-<версия>.zip`:
```
PanelKomplekt-<версия>.zip
├── PanelKomplekt.bundle/
│   ├── PackageContents.xml
│   └── Contents/PanelKomplekt.dll
├── Install.cmd
├── Uninstall.cmd
├── Install.ps1
└── ReadMe.txt
```
Установка копирует `PanelKomplekt.bundle` в `C:\Program Files\Autodesk\ApplicationPlugins`.

## Как работает плагин
```mermaid
flowchart TD
    A[Запуск AutoCAD] --> B["Автозагрузчик читает PackageContents.xml<br/>и загружает PanelKomplekt.dll"]
    B --> C["App.Initialize()"]
    C --> D["Первый простой AutoCAD:<br/>RibbonBuilder.Create()"]
    D --> E["CommandCatalog — список команд"]
    E --> F["Вкладка PanelKomplekt:<br/>по кнопке на команду"]
    F -->|нажатие кнопки| G["RibbonCommandHandler<br/>отправляет имя команды в AutoCAD"]
    G --> H["Метод команды с [CommandMethod]"]
    H --> I["CommandRunner.Run:<br/>тело команды + перехват ошибок"]
```

1. AutoCAD загружает `PanelKomplekt.dll` (из `.bundle` или через NETLOAD при отладке) и вызывает `App.Initialize()`.
2. `App` при первом простое вызывает `RibbonBuilder.Create()`; при смене рабочего пространства — повторно.
3. `RibbonBuilder` берёт список из `CommandCatalog` и создаёт кнопки, сгруппированные по панелям; иконки берёт `IconLoader` по ключу команды (`CommandInfo.Key`).
4. Нажатие кнопки → `RibbonCommandHandler` отправляет в AutoCAD имя команды (`GlobalName`).
5. AutoCAD вызывает метод с `[CommandMethod]` → тело команды выполняется через `CommandRunner.Run`.

## Команды
| Номер | Имя в AutoCAD | Панель | Назначение | Статус |
|---|---|---|---|---|
| C100 | PK_C100_ABOUT | Сервис | Окно «О плагине»: версия, заказчик, разработчик, технологии | служебная |
| C101 | PK_C101_SHOWELEMENTID | Сервис | ID выбранного элемента | тестовая |
