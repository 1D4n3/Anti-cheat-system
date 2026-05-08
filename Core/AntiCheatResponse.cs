using UnityEngine;

namespace Estate2D.AntiCheat.Core
{
    /// <summary>
    /// Определяет действие, которое система должна выполнить при обнаружении читерства.
    /// </summary>
    public class AntiCheatResponse
    {
        /// <summary>
        /// Тип ответа на обнаружение.
        /// </summary>
        public ResponseType Type { get; set; }

        /// <summary>
        /// Логировать ли событие в консоль.
        /// </summary>
        public bool LogToConsole { get; set; }

        /// <summary>
        /// Отправлять ли отчёт на сервер.
        /// </summary>
        public bool SendToServer { get; set; }

        /// <summary>
        /// Заблокировать ли игрока.
        /// </summary>
        public bool BanPlayer { get; set; }

        /// <summary>
        /// Остановить ли игру.
        /// </summary>
        public bool PauseGame { get; set; }

        /// <summary>
        /// Отключить ли компонент, который читерил.
        /// </summary>
        public bool DisableComponent { get; set; }

        /// <summary>
        /// Вывести ли диалог предупреждения.
        /// </summary>
        public bool ShowWarning { get; set; }

        /// <summary>
        /// Сообщение для пользователя.
        /// </summary>
        public string UserMessage { get; set; }

        public AntiCheatResponse()
        {
            Type = ResponseType.Warning;
            LogToConsole = true;
            SendToServer = false;
            BanPlayer = false;
            PauseGame = false;
            DisableComponent = false;
            ShowWarning = true;
            UserMessage = "Обнаружено подозрительное поведение. Это может привести к блокировке аккаунта.";
        }
    }

    /// <summary>
    /// Типы ответов на обнаруженное читерство.
    /// </summary>
    public enum ResponseType
    {
        /// <summary>
        /// Только логирование, никакого воздействия.
        /// </summary>
        Log,

        /// <summary>
        /// Предупредить игрока.
        /// </summary>
        Warning,

        /// <summary>
        /// Мягкое наказание (отключение компонента).
        /// </summary>
        Soft,

        /// <summary>
        /// Жёсткое наказание (остановка игры).
        /// </summary>
        Hard,

        /// <summary>
        /// Полный бан игрока.
        /// </summary>
        Ban
    }
}
