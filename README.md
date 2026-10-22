# Scenario Constructor

[English](#english) | [Русский](#русский)

## English

A Unity tool for assembling training scenarios from reusable steps in the Inspector. Execution modes, event sources, logging, and scoring are configured independently without duplicating steps.

**Requirements:** Unity **6000.4.4f1**, URP, and the standard packages included in the project. No third-party runtime frameworks are used.

### Getting started

1. Open the project in Unity and wait for import and compilation.
2. Select **Scenario Constructor > Open Demo Scene** and enter Play Mode.
3. Choose **Training** or **Exam**. Use **Restart** to reset the current run.
4. Tasks appear on the left, actions in the center, and results and the journal on the right.
5. After completion, use **Switch analyzer**, then **Re-evaluate** to score the same journal without replaying the scenario.

A 1600 × 900 or 1920 × 1080 Game view is recommended. The result panel retains the last completed score during a new run; check its run ID.

### Execution rules

- **Training:** one group, unlimited retries. A successful step earns 10 points on attempt 1, 5 on attempt 2, and 0 on later attempts. Other active steps retain their progress.
- **Exam:** three sequential groups, S01–S04, S05–S08, and S09–S12. A violation resolves its step as **Failed**, without retry. At the group's error limit (default: 2), remaining unfinished steps become **Skipped**. Score: `max(0, 100 − 10 × Failed − 5 × Skipped)`.
- **Activation:** S01–S04 together → S05 → S06 → S07 → S08 → S09–S10 together → S11–S12 together. Grouping and activation are separate settings.
- Pending-step actions count as violations by default; a profile can instead ignore them. An early violation for a future exam group belongs to that group, but its threshold is applied only when the group becomes current.

The demo uses Button, Trigger, Collision, and UnityEvent sources. S02 requires entering A before B; S04 requires power to be on.

### Editing scenarios

Open **Training** or **Exam** under `Assets/ScenarioConstructor/Examples/Inspection`.

- Edit **Groups** for membership and group order. Drag **Activation Batches** to change execution order; members of one batch activate together. Every step must occur once in each list, and batches must follow group order.
- Edit a step's **Rule** for ordered action IDs or a required boolean state. Change the error limit in **Exam Execution**.
- Select **Execution**, **Logger**, **Analyzer**, and **Pending Actions** independently in the profile.
- Configure **ScenarioActionRelay** with a session, step, exact action ID, and optional state flags. Forward events through ButtonActionSource, TriggerActionSource, CollisionActionSource, or a UnityEvent calling `Raise()`.

Create assets through **Assets > Create > Scenario Constructor**. Update scene bindings when replacing demo step assets. Start the mode again to apply configuration changes.

### Architecture and extension

**Runtime** contains Unity-independent conditions, execution, journals, and analysis. **Unity** contains configuration assets, event adapters, and presentation. **Editor** contains authoring tools and validation; **Tests** covers domain and integration behavior.

Add a condition with `IStepRule` / `StepRuleAsset`, an executor with `IScenarioExecution` / `ScenarioExecutionAsset`, a logger with `IScenarioLogger` / `ScenarioLoggerAsset`, or an analyzer with `IScenarioAnalyzer` / `ScenarioAnalyzerAsset`. Implement the contract, expose its factory as an asset, and select it in the configuration. New event sources can call `ScenarioActionRelay.Raise()`.

### Validation and further improvements

**Scenario Constructor > Test Runner** provides **73 EditMode** and **14 PlayMode** tests, including the 15-point training and 70-point exam examples. PlayMode tests require graphics. A Windows x64 Development/Mono build was validated.

Journals are currently kept in memory and are lost when the application closes. Scene bindings validate action IDs against the selected step rule. Further improvements include clearer group/error indicators and more predictable threshold handling for future groups. Journal export/import and overlapping groups are deferred. Other platforms and performance budgets have not been validated.

### Time spent

Approximately 5 hours of actual work were spent on completing the task.

---

## Русский

Инструмент Unity для сборки учебных сценариев из переиспользуемых шагов через Inspector. Режим исполнения, источники событий, логирование и оценивание настраиваются независимо, без дублирования шагов.

**Требования:** Unity **6000.4.4f1**, URP и стандартные пакеты, включённые в проект. Сторонние runtime-фреймворки не используются.

### Запуск

1. Откройте проект в Unity и дождитесь импорта и компиляции.
2. Выберите **Scenario Constructor > Open Demo Scene** и включите Play Mode.
3. Выберите **Training** или **Exam**. Кнопка **Restart** сбрасывает текущее прохождение.
4. Слева расположены задания, в центре — действия, справа — результаты и журнал.
5. После завершения нажмите **Switch analyzer**, затем **Re-evaluate**, чтобы оценить тот же журнал без повторного прохождения.

Рекомендуемый размер Game view — 1600 × 900 или 1920 × 1080. Во время нового прохождения панель результатов сохраняет последнюю завершённую оценку; её принадлежность можно проверить по идентификатору запуска.

### Правила прохождения

- **Тренировка:** одна группа, неограниченные повторные попытки. Успешный шаг приносит 10 баллов с первой попытки, 5 со второй и 0 с последующих. Остальные активные шаги сохраняют прогресс.
- **Экзамен:** три последовательные группы: S01–S04, S05–S08 и S09–S12. Нарушение завершает соответствующий шаг со статусом **Failed**, без повторной попытки. При достижении лимита ошибок группы (по умолчанию 2) оставшиеся незавершённые шаги получают **Skipped**. Оценка: `max(0, 100 − 10 × Failed − 5 × Skipped)`.
- **Активация:** S01–S04 одновременно → S05 → S06 → S07 → S08 → S09–S10 одновременно → S11–S12 одновременно. Группы и активация настраиваются отдельно.
- Действия недоступных шагов по умолчанию считаются нарушениями; в профиле можно выбрать их игнорирование. Досрочная ошибка будущей экзаменационной группы относится к ней, но её лимит применяется только при переходе к этой группе.

Демонстрация использует Button, Trigger, Collision и UnityEvent. S02 требует сначала войти в A, затем в B; S04 — включить питание перед подтверждением.

### Настройка сценариев

Откройте **Training** или **Exam** в `Assets/ScenarioConstructor/Examples/Inspection`.

- Изменяйте **Groups** для настройки состава и порядка групп. Перетаскивайте **Activation Batches** для изменения порядка исполнения; шаги одного пакета активируются одновременно. Каждый шаг должен встречаться один раз в каждом списке, а пакеты должны следовать порядку групп.
- Изменяйте **Rule** шага для настройки порядка действий или требуемого логического состояния. Лимит ошибок задаётся в **Exam Execution**.
- Независимо выбирайте **Execution**, **Logger**, **Analyzer** и **Pending Actions** в профиле.
- В **ScenarioActionRelay** назначьте сессию, шаг, точный идентификатор действия и необязательные флаги состояния. Передавайте события через ButtonActionSource, TriggerActionSource, CollisionActionSource или UnityEvent, вызывающий `Raise()`.

Создавайте ассеты через **Assets > Create > Scenario Constructor**. При замене шагов демонстрации обновляйте привязки в сцене. Чтобы применить изменения конфигурации, запустите режим заново.

### Архитектура и расширение

**Runtime** содержит независимые от Unity условия, исполнение, журналы и анализ. **Unity** — конфигурационные ассеты, адаптеры событий и интерфейс. **Editor** — инструменты настройки и валидацию; **Tests** — проверки ядра и интеграции.

Для нового условия реализуйте `IStepRule` / `StepRuleAsset`, исполнителя — `IScenarioExecution` / `ScenarioExecutionAsset`, логгера — `IScenarioLogger` / `ScenarioLoggerAsset`, анализатора — `IScenarioAnalyzer` / `ScenarioAnalyzerAsset`. Реализуйте контракт, предоставьте фабрику в виде ассета и выберите его в конфигурации. Новые источники событий могут вызывать `ScenarioActionRelay.Raise()`.

### Проверки и дальнейшие улучшения

В **Scenario Constructor > Test Runner** доступны **73 EditMode** и **14 PlayMode** тестов, включая примеры на 15 баллов в тренировке и 70 в экзамене. PlayMode-тестам требуется графика. Проверена Windows x64 Development/Mono сборка.

Журналы пока хранятся в памяти и теряются при закрытии приложения. В привязках сцены проверяется соответствие идентификаторов действий правилу выбранного шага. Дальнейшие улучшения — более понятное отображение групп и ошибок, предсказуемая обработка лимита будущих групп. Экспорт/импорт журналов и перекрывающиеся группы отложены. Другие платформы и ограничения производительности не проверялись.

### Затраченное время

На выполнение задание было затраченно около 5 часов фактической работы. 
