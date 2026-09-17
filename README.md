# PanelKomplekt

Плагин для **AutoCAD 2021**: раскладка сэндвич-панелей по фасадам и спецификация.
После установки в AutoCAD появляется вкладка **PanelKomplekt** с кнопками команд.

Текущая версия и изменения — в [CHANGELOG.md](CHANGELOG.md). Незнакомые слова — в [словаре терминов](Docs/Glossary.md).

---

## Для пользователя AutoCAD

### Установка
1. Закройте AutoCAD.
2. Распакуйте архив `PanelKomplekt-<версия>.zip` целиком.
3. Дважды щёлкните `Install.cmd` и ответьте «Да» на вопрос Windows.
4. Запустите AutoCAD 2021 — на ленте появится вкладка «PanelKomplekt».

### Что умеет плагин
| Кнопка | Что делает |
|---|---|
| О плагине | Показывает окно со сведениями: версия, для кого и кем создан плагин, технологии. |
| ID элемента | Показывает ID выбранного элемента чертежа (тестовая). |
| Проверка панелей | Добавляет блок панели в чертёж и показывает данные выбранных панелей (служебная). |

Пошаговое описание каждой команды, обновление, удаление и решение проблем — в **[руководстве пользователя](Docs/UserGuide.md)**.

---

## Для программиста

Плагин написан на C# (.NET Framework 4.8, x64) в Visual Studio 2022.

| Документ | О чём |
|---|---|
| [Docs/DeveloperGuide.md](Docs/DeveloperGuide.md) | Настройка компьютера, сборка, отладка, новая команда, выпуск версии. **Начинать отсюда.** |
| [Docs/DevelopmentRules.md](Docs/DevelopmentRules.md) | Обязательные правила: коммиты, именование, ветки, слияние. |
| [Docs/ProjectStructure.md](Docs/ProjectStructure.md) | Структура папок, схема работы плагина, список команд. |
| [Docs/CommandTemplate.md](Docs/CommandTemplate.md) | Шаблон описания команды. |
| [Docs/Glossary.md](Docs/Glossary.md) | Словарь терминов. |
| [CHANGELOG.md](CHANGELOG.md) | Журнал изменений. |

Быстрый старт:
```
dotnet build PanelKomplekt.sln -c Debug                                   # сборка
powershell -ExecutionPolicy Bypass -File tools\New-Command.ps1 ...        # новая команда
powershell -ExecutionPolicy Bypass -File tools\Build-Bundle.ps1           # установочный архив в dist\
```

---

Закрытый проект, все права защищены — см. [LICENSE](LICENSE). Вопросы к заказчику — [Docs/CustomerQuestions.txt](Docs/CustomerQuestions.txt).
