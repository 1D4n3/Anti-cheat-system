# Система Централизованного Античита для Unity

## Описание архитектуры

Система состоит из следующих компонентов:

### Ядро (Core)
- **AntiCheatManager** - центральный синглтон, управляет всеми модулями
- **IAntiCheatModule** - интерфейс для реализации модулей обнаружения
- **AntiCheatConfig** - конфигурация системы (ScriptableObject)
- **AntiCheatReport** - структура для отчётов об обнаружениях
- **AntiCheatResponse** - определяет ответ на обнаружение

### Модули обнаружения
- **SpeedHackObserver** - обнаружение аномальной скорости движения
- **RotationHackObserver** - обнаружение аномальной скорости вращения
- **MemoryModificationObserver** - пример модуля для обнаружения изменений памяти

### Утилиты
- **AntiCheatUtility** - статические вспомогательные методы
- **AntiCheatDebugMonitor** - визуализация статуса системы

## Установка и настройка

### Шаг 1: Создание конфигурации

1. В Project окне нажмите правой кнопкой мыши
2. Create → AntiCheat → Config
3. Назовите файл "AntiCheatConfig"
4. Поместите его в папку Resources/

```
Assets/Resources/AntiCheatConfig.asset
```

### Шаг 2: Добавление AntiCheatManager на сцену

1. Создайте пустой GameObject
2. Назовите его "[AntiCheat Manager]"
3. Добавьте компонент AntiCheatManager
4. Назначьте созданный конфиг в поле Config

### Шаг 3: Добавление модулей обнаружения

На объект с персонажем или другой контролируемый объект добавьте компоненты:

- **SpeedHackObserver** (для обнаружения ускорения)
- **RotationHackObserver** (для обнаружения аномального вращения)

Убедитесь, что включено "Auto Register" в обоих компонентах.

## Использование системы

### Базовые операции

```csharp
using Estate2D.AntiCheat.Core;
using Estate2D.AntiCheat.Utils;

// Получить менеджер
var manager = AntiCheatManager.Instance;

// Проверить статус
bool isEnabled = manager.Config.Enabled;

// Получить модуль
var module = manager.GetModule("speed_hack_observer");

// Включить/отключить модуль
AntiCheatUtility.SetModuleEnabled("speed_hack_observer", false);

// Получить историю обнаружений
var history = manager.DetectionHistory;
Debug.Log($"Total detections: {history.Count}");

// Получить последнее обнаружение
var lastDetection = AntiCheatUtility.GetLastDetection();
if (lastDetection != null)
{
    Debug.Log($"Type: {lastDetection.CheatType}");
    Debug.Log($"Message: {lastDetection.Message}");
}
```

### Подписка на события

```csharp
var manager = AntiCheatManager.Instance;

// Событие при обнаружении
manager.OnCheatDetected += (report) =>
{
    Debug.LogError($"CHEAT: {report.CheatType} - {report.Message}");
    // Отправить на сервер, показать UI и т.д.
};

// Событие логирования
manager.OnSystemLog += (message) =>
{
    Debug.Log($"[AntiCheat] {message}");
};
```

### Защита переменных

```csharp
// Использовать SecureInt и SecureFloat вместо обычных
public SecureInt score = new SecureInt(0);
public SecureFloat health = new SecureFloat(100f);

// Получить значение
int currentScore = score.Value;
float currentHealth = health.Value;

// Установить значение
score.SetValue(100);
health.SetValue(50f);

// Проверить целостность
if (!score.IsValid)
{
    Debug.LogError("Score was modified!");
}
```

## Тестирование системы

### Метод 1: Тестирование на текущей сцене

1. Откройте сцену с вашим персонажем
2. Добавьте AntiCheatManager на сцену (как описано выше)
3. Добавьте компоненты обнаружения на объект персонажа
4. Добавьте AntiCheatDebugMonitor на пустой GameObject для визуализации

#### Тест SpeedHack Observer:
- Запустите сцену
- Используйте консоль для изменения скорости персонажа
- Попробуйте увеличить скорость сверх лимита (по умолчанию 10 м/с)
- Система должна обнаружить нарушение после 3 последовательных превышений

#### Тест RotationHack Observer:
- Быстро поворачивайте персонажа мышкой
- Старайтесь превысить лимит вращения (по умолчанию 200°/сек)
- Система должна обнаружить аномалию

### Метод 2: Тестирование через консоль

```csharp
// В любом скрипте или через Developer Console:

using Estate2D.AntiCheat.Core;
using Estate2D.AntiCheat.Utils;

// Включить режим отладки для более подробных логов
var manager = AntiCheatManager.Instance;

// Посмотреть статистику
AntiCheatUtility.PrintStatistics();

// Манипулировать модулями
AntiCheatUtility.SetModuleEnabled("speed_hack_observer", true);
AntiCheatUtility.SetModuleEnabled("rotation_hack_observer", false);

// Очистить историю
manager.ClearDetectionHistory();

// Получить информацию о последнем обнаружении
var last = AntiCheatUtility.GetLastDetection();
if (last != null)
{
    Debug.Log($"Последнее обнаружение: {last.CheatType} в {last.DetectionTime}");
}
```

### Метод 3: Тестирование на других проектах

#### Для нового проекта:
1. Скопируйте папку `Assets/Scripts/AntiCheat/` в новый проект
2. Убедитесь, что существует папка `Assets/Resources/`
3. Создайте конфигурацию: Assets → Resources → Create → AntiCheat → Config
4. Создайте сцену тестирования
5. Добавьте AntiCheatManager
6. Добавьте тестовый объект с компонентами обнаружения

#### Для существующего проекта с движком:
1. Добавьте папку AntiCheat из этого проекта
2. Интегрируйте с существующей системой движения
3. Убедитесь, что объект с Rigidbody2D или движком имеет компоненты обнаружения
4. Запустите и проверьте логи

## Конфигурирование системы

Отредактируйте AntiCheatConfig.asset для настройки параметров:

### Глобальные настройки
- **Enabled** - включить/отключить систему целиком
- **Debug Mode** - подробное логирование
- **Log Detections** - логировать все обнаружения

### Настройки обнаружения скорости
- **Max Allowed Speed** - максимальная скорость (м/с)
- **Speed Check Interval** - интервал проверки (сек)
- **Speed Tolerance Multiplier** - допуск (1.1 = +10%)
- **Speed Suspicion Threshold** - количество нарушений для срабатывания

### Настройки обнаружения вращения
- **Max Rotation Speed** - максимальная скорость вращения (°/сек)
- **Rotation Check Interval** - интервал проверки (сек)
- **Rotation Suspicion Threshold** - количество нарушений для срабатывания

### Настройки ответа
- **Default Response Type** - тип ответа (Log, Warning, Soft, Hard, Ban)
- **Quit Game On Detection** - выход из игры при обнаружении
- **Send Reports To Server** - отправка отчётов на сервер

## Создание собственного модуля

```csharp
using UnityEngine;
using Estate2D.AntiCheat.Core;

public class CustomHackObserver : MonoBehaviour, IAntiCheatModule
{
    private AntiCheatConfig _config;
    private bool _isEnabled = true;

    public string ModuleId => "custom_observer";
    public string ModuleName => "Custom Observer";

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    private void Start()
    {
        AntiCheatManager.Instance.RegisterModule(this);
    }

    public void Initialize(AntiCheatConfig config)
    {
        _config = config;
    }

    public void OnCheatDetected(AntiCheatReport report)
    {
        // Реагировать на обнаружения других модулей
    }

    public void Shutdown()
    {
        // Очистка ресурсов
    }

    private void FixedUpdate()
    {
        if (!_isEnabled || _config == null)
            return;

        // Ваша логика обнаружения
        bool cheatDetected = CheckForCheat();

        if (cheatDetected)
        {
            var report = new AntiCheatReport
            {
                ModuleId = ModuleId,
                ModuleName = ModuleName,
                CheatType = CheatType.Unknown,
                SeverityLevel = 5,
                TargetObject = gameObject,
                Message = "Обнаружено подозрительное поведение"
            };

            AntiCheatManager.Instance.ReportCheatDetection(report);
        }
    }

    private bool CheckForCheat()
    {
        // Ваша логика проверки
        return false;
    }
}
```

## Отладка и мониторинг

### Использование Debug Monitor

Добавьте AntiCheatDebugMonitor на сцену для визуализации:

1. Создайте пустой GameObject
2. Добавьте компонент AntiCheatDebugMonitor
3. Включите опции визуализации

В правом верхнем углу экрана появится панель с информацией о системе.

### Логирование

Все события логируются в консоль Unity при включённом Debug Mode в конфигурации.

## Производительность

- Система использует минимальные ресурсы
- Проверки выполняются с указанными интервалами
- История сохраняется в памяти (можно очистить при необходимости)
- Рекомендуется отключать неиспользуемые модули для оптимизации

## Лучшие практики

1. **Не полагайтесь только на клиентскую проверку** - используйте серверную валидацию для критичных данных
2. **Регулярно обновляйте пороги** - адаптируйте лимиты под вашу игру
3. **Комбинируйте модули** - используйте несколько проверок одновременно
4. **Логируйте и анализируйте** - ведите статистику обнаружений для улучшения
5. **Тестируйте на разных устройствах** - производительность и поведение могут отличаться
6. **Используйте SecureInt и SecureFloat** - защищайте критичные переменные

## Возможные проблемы и решения

### Проблема: Модули не регистрируются
**Решение:** Убедитесь, что:
- AntiCheatManager существует на сцене
- Auto Register включен в модулях
- Конфигурация загружена

### Проблема: Частые ложные обнаружения
**Решение:**
- Увеличьте толерантность (SpeedToleranceMultiplier)
- Увеличьте интервал проверки
- Увеличьте порог подозрений (Suspicion Threshold)

### Проблема: Система не работает на других проектах
**Решение:**
- Проверьте пути namespaces
- Убедитесь, что все зависимости скопированы
- Создайте новую конфигурацию для проекта
