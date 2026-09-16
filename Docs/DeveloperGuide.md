# Руководство разработчика PanelKomplekt

Как с нуля настроить компьютер, собрать, отладить плагин, добавить команду и выпустить версию.
Обязательные правила — в [DevelopmentRules.md](DevelopmentRules.md), устройство проекта — в [ProjectStructure.md](ProjectStructure.md).

## 1. Что нужно установить
| Программа | Для чего |
|---|---|
| Windows 10/11 x64 | AutoCAD 2021 работает только на 64-битной Windows. |
| Visual Studio 2022 (Community подходит) с рабочей нагрузкой «Разработка классических приложений .NET» и пакетом «.NET Framework 4.8 targeting pack» | Редактирование, сборка и отладка. |
| AutoCAD 2021 с действующей лицензией | Библиотеки API и проверка команд. |
| Git | История изменений и работа с GitHub. |

## 2. Получение проекта
```
git clone https://github.com/sanya-bogatyirev-04/PanelKomplekt.git
```
Откройте `PanelKomplekt.sln` в Visual Studio 2022.

Путь к AutoCAD задан в `PanelKomplekt/PanelKomplekt.csproj` в свойстве `AcadDir`
(по умолчанию `C:\Program Files\Autodesk\AutoCAD 2021`). Если AutoCAD установлен в другое место — поменяйте его там.
Если библиотек AutoCAD по этому пути нет (например, на сервере GitHub), проект автоматически берёт их из NuGet-пакета `AutoCAD.NET 24.0.0`.

## 3. Разовая настройка AutoCAD
В AutoCAD включена проверка надёжных расположений (`SECURELOAD`), поэтому отладочная DLL из папки проекта без настройки не загрузится.
1. AutoCAD → `ПАРАМЕТРЫ` → вкладка «Файлы» → «Надежные расположения».
2. Добавьте `<папка проекта>\PanelKomplekt\bin\Debug` и `<папка проекта>\tools`.
3. OK, перезапустите AutoCAD.

Если в `PanelKomplekt/start.scr` путь к DLL отличается от вашей папки проекта — поправьте его.

## 4. Сборка и отладка
1. Выберите конфигурацию **Debug** и профиль запуска **AutoCAD 2021**.
2. F5: Visual Studio соберёт проект, запустит AutoCAD, а `start.scr` загрузит DLL командой NETLOAD.
3. На ленте появится вкладка «PanelKomplekt»; точки останова в коде срабатывают.
4. Загруженную DLL нельзя выгрузить: после изменения кода закройте AutoCAD и снова нажмите F5.

Сборка без Visual Studio:
```
dotnet build PanelKomplekt.sln -c Debug
```

## 5. Новая команда
1. Ветка: `git switch master`, затем `git switch -c feature/C201_PanelLayout`.
2. Заготовка команды:
   ```
   powershell -ExecutionPolicy Bypass -File tools\New-Command.ps1 -Number C201 -Name PanelLayout -Title "Раскладка панелей" -Panel "Раскладка" -RibbonText "Раскладка"
   ```
   Скрипт создаст папку команды с `.cs` и `.md`, добавит команду в `CommandCatalog` и в таблицу команд.
3. Реализуйте алгоритм в `.cs`, заполните `.md`, добавьте папку в дерево в `ProjectStructure.md`, запишите изменение в `CHANGELOG.md`.
4. Проверьте в AutoCAD → коммиты по правилам → утверждение → слияние `git merge --no-ff` в master.

Номера команд: `C100` — служебные и тестовые, `C200` — раскладка, `C300` — спецификации.

### Иконки кнопки
- Файлы `<Key>_16.png` и `<Key>_32.png` в папке команды (Key — имя папки, например `C101_ShowElementId`). Прозрачный фон; 32 px — большая кнопка, 16 px — маленькая.
- Иконки встраиваются в DLL автоматически (правило `EmbeddedResource` в `PanelKomplekt.csproj`), загружает их `Core/IconLoader.cs`. Нет иконки — кнопка показывается только с текстом.
- Существующие иконки рисует `tools\Generate-Icons.ps1`: чтобы изменить цвета или добавить иконку новой команды, поправьте скрипт и запустите
  ```
  powershell -ExecutionPolicy Bypass -File tools\Generate-Icons.ps1 -PreviewPath %TEMP%\icons.png
  ```
  Параметр `-PreviewPath` создаёт картинку предпросмотра на светлом и тёмном фоне ленты.

## 6. Выпуск версии для заказчика
1. В `PanelKomplekt/PanelKomplekt.csproj` поднимите `<Version>` (например, `0.2.0`), допишите `CHANGELOG.md`.
2. Соберите архив локально:
   ```
   powershell -ExecutionPolicy Bypass -File tools\Build-Bundle.ps1
   ```
   Результат: `dist\PanelKomplekt-<версия>.zip` (содержимое описано в [ProjectStructure.md](ProjectStructure.md)).
3. Проверьте установку архива на своём компьютере: `Install.cmd` → запуск AutoCAD → вкладка на месте.
4. Выпуск на GitHub: после слияния в master поставьте тег и отправьте его:
   ```
   git tag v0.2.0
   git push origin v0.2.0
   ```
   GitHub Actions соберёт архив и создаст релиз на странице Releases.

## 7. Автосборка на GitHub
Файл `.github/workflows/build.yml`. При каждом push GitHub собирает проект и архив:
вкладка **Actions** → нужный запуск → раздел **Artifacts**. Красный крестик — сборка не прошла, подробности в журнале запуска.
Для приватного репозитория действует месячный лимит бесплатных минут GitHub Actions.

## 8. Частые проблемы
| Проблема | Причина и решение |
|---|---|
| Ошибка CS0104 «Application неоднозначно» | Подключён WPF: используйте псевдоним `AcApp` для `Autodesk.AutoCAD.ApplicationServices.Application`. |
| Команда не найдена в AutoCAD | В файле команды нет `[assembly: CommandClass(typeof(...))]`. |
| Нет кнопки на ленте | Команда не добавлена в `Core/CommandCatalog.cs`. |
| AutoCAD не загружает DLL при F5 | Папка `bin\Debug` не добавлена в надёжные расположения. |
| Сборка падает: файл занят | Закройте AutoCAD — он держит загруженную DLL. |
| При F5 изменения не видны (например, нет новых иконок) | На компьютере установлен плагин из архива (`C:\Program Files\Autodesk\ApplicationPlugins\PanelKomplekt.bundle`): AutoCAD загружает его при запуске, и отладочная DLL с тем же именем уже не загружается. Удалите установленную версию (`Uninstall.cmd`). Проверка: в командной строке при запуске строка «PanelKomplekt … загружен из <путь>» должна указывать на `bin\Debug`. |
