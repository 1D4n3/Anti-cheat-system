# Инструкция по тестированию системы на других проектах

## Структура для экспорта

Минимальный набор файлов для использования на других проектах:

```
AntiCheat/
├── Core/
│   ├── IAntiCheatModule.cs
│   ├── AntiCheatManager.cs
│   ├── AntiCheatConfig.cs
│   ├── AntiCheatReport.cs
│   └── AntiCheatResponse.cs
├── SpeedHackObserver.cs
├── RotateHackObserver.cs
├── RotateHackObserver.cs
├── SecureInt.cs
├── SecureFloat.cs
└── Utils/
    ├── AntiCheatUtility.cs
    ├── AntiCheatDebugMonitor.cs
    └── MemoryModificationObserver.cs
```

## Тестовый проект #1: Простой 2D платформер

### Цель
Проверить работу SpeedHackObserver на базовом движении персонажа.

### Шаги

1. **Создайте сцену**
   ```
   File → New Scene
   ```

2. **Добавьте простой персонаж**
   ```
   Create → 2D Object → Sprite → Square
   Назовите Player
   Add Component → Rigidbody2D (Body Type: Dynamic)
   Add Component → Collider 2D
   ```

3. **Добавьте скрипт движения**
   ```csharp
   using UnityEngine;
   
   public class SimplePlayer : MonoBehaviour
   {
       private Rigidbody2D rb;
       
       private void Start() => rb = GetComponent<Rigidbody2D>();
       
       private void FixedUpdate()
       {
           float input = Input.GetAxis("Horizontal");
           rb.linearVelocity = new Vector2(input * 5f, rb.linearVelocity.y);
       }
   }
   ```

4. **Добавьте AntiCheat систему** (см. Quick Start)

5. **Проверьте**
   - Нормальное движение = без срабатываний
   - Попробуйте вручную установить высокую скорость через консоль

### Ожидаемый результат
```
Normal speed (5 m/s) → No detections ✓
High speed (20 m/s) → Detection after 3 frames ✓
```

## Тестовый проект #2: 3D First-Person

### Цель
Проверить работу RotationHackObserver на поворотах камеры.

### Шаги

1. **Создайте сцену с камерой**
   ```
   Create → 3D Object → Capsule (это игрок)
   Создайте камеру как child объекта
   ```

2. **Скрипт для поворота**
   ```csharp
   using UnityEngine;
   
   public class CameraLook : MonoBehaviour
   {
       public float sensitivity = 2f;
       private float rotX = 0;
       
       private void Update()
       {
           float mouseX = Input.GetAxis("Mouse X");
           rotX -= Input.GetAxis("Mouse Y") * sensitivity;
           rotX = Mathf.Clamp(rotX, -90, 90);
           
           transform.localRotation = Quaternion.Euler(rotX, 0, 0);
           transform.parent.Rotate(0, mouseX * sensitivity, 0);
       }
   }
   ```

3. **Добавьте AntiCheat** и RotationHackObserver на Capsule

4. **Проверьте**
   - Нормальные повороты = нет срабатываний
   - Экстремальная угловая скорость = обнаружение

### Ожидаемый результат
```
Normal camera movement → No detections ✓
Rapid rotation (>200°/sec) → Detection after 3 frames ✓
```

## Тестовый проект #3: Интеграция с существующей системой

### Цель
Проверить адаптацию к произвольному движку движения.

### Пример кода

```csharp
using UnityEngine;
using Estate2D.AntiCheat.Core;

public class CustomMovementEngine : MonoBehaviour, IAntiCheatModule
{
    public Rigidbody2D rb;
    private AntiCheatConfig _config;

    public string ModuleId => "custom_movement_check";
    public string ModuleName => "Custom Movement Validator";
    public bool IsEnabled { get; set; } = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        AntiCheatManager.Instance.RegisterModule(this);
    }

    public void Initialize(AntiCheatConfig config) => _config = config;

    public void OnCheatDetected(AntiCheatReport report)
    {
        // Синхронизировать состояние при обнаружении
        if (report.CheatType == CheatType.SpeedHack)
        {
            Debug.Log("Detected speed hack - resetting player");
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void Shutdown() { }

    private void FixedUpdate()
    {
        // Ваша система движения
        float input = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(input * 10f, rb.linearVelocity.y);
    }
}
```

## Процедура полного тестирования

### Чек-лист для каждого проекта

- [ ] Конфигурация создана и назначена
- [ ] AntiCheatManager на сцене
- [ ] Все модули зарегистрированы
- [ ] Debug Monitor показывает статус
- [ ] Логи выводятся в консоль
- [ ] Обнаружение работает при спуфинге данных
- [ ] Система восстанавливается после обнаружения
- [ ] Нет ложных срабатываний при нормальной игре
- [ ] Производительность приемлема

### Тестирование производительности

```csharp
using System.Diagnostics;
using UnityEngine;

public class PerformanceTest : MonoBehaviour
{
    private void Start()
    {
        Stopwatch sw = Stopwatch.StartNew();
        
        for (int i = 0; i < 10000; i++)
        {
            var manager = AntiCheatManager.Instance;
            var module = manager.GetModule("speed_hack_observer");
        }
        
        sw.Stop();
        Debug.Log($"10000 iterations: {sw.ElapsedMilliseconds}ms");
    }
}
```

## Отчёт о тестировании

Создайте файл TEST_REPORT.md:

```markdown
# Отчёт тестирования AntiCheat системы

## Проект: [Название]
## Дата: [Дата]
## Версия Unity: [Версия]

### Результаты

#### SpeedHackObserver
- Status: ✓ Работает / ✗ Не работает
- False positives: [кол-во]
- Performance: [ms/frame]

#### RotationHackObserver
- Status: ✓ Работает / ✗ Не работает
- False positives: [кол-во]
- Performance: [ms/frame]

#### Проблемы
- [Описание]
- [Решение]

#### Примечания
- [Особенности интеграции]
- [Рекомендации]
```

## Экспорт и распространение

### Создание пакета для распространения

1. **Подготовка**
   ```
   Удалите пути, специфичные для вашего проекта
   Обновите namespace если нужно
   Создайте чистую версию всех файлов
   ```

2. **Архивирование**
   ```
   Assets/Scripts/AntiCheat → AntiCheat_v1.0.zip
   ```

3. **Документация**
   - README.md (основная документация)
   - QUICK_START.md (быстрый старт)
   - TEST_PROCEDURES.md (процедуры тестирования)

## Типичные проблемы при тестировании

### Проблема: "AntiCheatConfig is null"
```
Решение:
1. Создайте конфиг: Assets → Resources → Create → AntiCheat → Config
2. Назовите его в точности "AntiCheatConfig"
3. Убедитесь, что он в папке Resources
```

### Проблема: Модули не регистрируются
```
Решение:
1. Проверьте наличие AntiCheatManager на сцене
2. Включите Auto Register в каждом модуле
3. Проверьте консоль на ошибки регистрации
```

### Проблема: Много ложных обнаружений
```
Решение:
1. Увеличьте SpeedToleranceMultiplier до 1.3-1.5
2. Увеличьте SpeedSuspicionThreshold до 5
3. Адаптируйте MaxAllowedSpeed под вашу игру
```

### Проблема: Система не реагирует на попытки читерства
```
Решение:
1. Включите Debug Mode в конфигурации
2. Проверьте, что IsEnabled = true для модулей
3. Попробуйте установить экстремальные значения для теста
```

## Примеры интеграции с популярными фреймворками

### С PlayFab

```csharp
using PlayFab;
using Estate2D.AntiCheat.Core;

public class PlayFabIntegration : MonoBehaviour
{
    private void Start()
    {
        AntiCheatManager.Instance.OnCheatDetected += SendReportToPlayFab;
    }

    private void SendReportToPlayFab(AntiCheatReport report)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate { StatisticName = "CheatDetections", Value = 1 }
            }
        };
        
        PlayFabClientAPI.UpdatePlayerStatistics(request, null, null);
    }
}
```

### С Photon

```csharp
using Photon.Pun;
using Estate2D.AntiCheat.Core;

public class PhotonIntegration : MonoBehaviourPun
{
    private void Start()
    {
        AntiCheatManager.Instance.OnCheatDetected += HandleDetection;
    }

    private void HandleDetection(AntiCheatReport report)
    {
        photonView.RPC("BanPlayer", RpcTarget.AllBuffered, report.ModuleId);
    }

    [PunRPC]
    void BanPlayer(string moduleId)
    {
        Debug.LogError($"Player banned for: {moduleId}");
        // Реализовать бан
    }
}
```

## Заключение

Система централизованного античита готова к использованию на разных проектах. 
Она легко адаптируется под различные типы игр и архитектуры, обеспечивая гибкость 
при сохранении централизованного управления и мониторинга.
