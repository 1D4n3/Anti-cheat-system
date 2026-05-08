# Быстрый старт системы античита

## За 5 минут до первого запуска

### 1. Создание конфигурации

```
Right-click в Project окне → Create → AntiCheat → Config
Переименуйте в "AntiCheatConfig"
Поместите в Assets/Resources/
```

### 2. Добавление менеджера на сцену

```
Create Empty GameObject → назовите "[AntiCheat Manager]"
Добавьте компонент AntiCheatManager (Add Component → AntiCheatManager)
Назначьте созданный конфиг в поле "Config"
```

### 3. Добавление модулей на игрока

На GameObject с движком персонажа добавьте:
- SpeedHackObserver
- RotationHackObserver

Оба компонента должны иметь включённое "Auto Register"

### 4. Добавление монитора (опционально)

```
Create Empty GameObject → назовите "[Debug Monitor]"
Добавьте компонент AntiCheatDebugMonitor
Включите опции: Show In Console и Show On Screen
```

### 5. Запуск

Нажмите Play - система готова!

## Первый тест

1. В консоли Runtime должны появиться логи типа:
   ```
   [AntiCheat] Система античита инициализирована
   [AntiCheat] Модуль Speed Hack Observer зарегистрирован
   [AntiCheat] Модуль Rotation Hack Observer зарегистрирован
   ```

2. Попробуйте вызвать читерство:
   - Для Speed Hack: ускорьте персонажа до 15+ м/с
   - Для Rotation Hack: быстро поворачивайте персонажа

3. При обнаружении будет логирована ошибка:
   ```
   [AntiCheat DETECTION] [SpeedHack] Speed Hack Observer: Обнаружено ускорение
   ```

## Минимальный код для интеграции

```csharp
using Estate2D.AntiCheat.Core;

public class MyGameScript : MonoBehaviour
{
    private void Start()
    {
        // Подписаться на события
        AntiCheatManager.Instance.OnCheatDetected += HandleCheatDetection;
    }

    private void HandleCheatDetection(AntiCheatReport report)
    {
        Debug.LogError($"Читерство обнаружено: {report.CheatType}");
        
        // Здесь можно:
        // - Отправить отчёт на сервер
        // - Показать Warning UI
        // - Запустить анти-читерский код
    }
}
```

## Проверка на других проектах

### Для демо-проекта:

1. **Скопируйте папку**
   ```
   Assets/Scripts/AntiCheat → Новый Проект/Assets/Scripts/
   ```

2. **Создайте конфигурацию** (см. выше)

3. **Создайте простую тестовую сцену:**
   ```csharp
   using UnityEngine;
   using Estate2D.AntiCheat.Core;
   
   public class TestScene : MonoBehaviour
   {
       private void Start()
       {
           Debug.Log("AntiCheat Modules: " + 
               AntiCheatManager.Instance.GetAllModules().Count);
       }
   }
   ```

4. **Запустите и проверьте логи**

### Для существующего проекта:

1. Скопируйте папку AntiCheat
2. Убедитесь, что есть папка Resources
3. Адаптируйте под вашу архитектуру движения
4. Добавьте обработчики событий в вашу систему

## Отключение модулей

```csharp
using Estate2D.AntiCheat.Utils;

// Отключить SpeedHack проверку
AntiCheatUtility.SetModuleEnabled("speed_hack_observer", false);

// Включить обратно
AntiCheatUtility.SetModuleEnabled("speed_hack_observer", true);

// Посмотреть статистику
AntiCheatUtility.PrintStatistics();
```

## Общие проблемы

| Проблема | Решение |
|----------|---------|
| "Config not assigned" | Создайте конфиг в Resources и назначьте |
| Модули не регистрируются | Проверьте Auto Register и наличие менеджера |
| Частые ложные срабатывания | Увеличьте толерантность в конфигурации |
| "DLL missing" ошибка | Убедитесь, что UniRx установлен (если используется) |
