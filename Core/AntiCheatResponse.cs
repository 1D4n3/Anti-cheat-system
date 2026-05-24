using UnityEngine;

namespace Estate2D.AntiCheat.Core
{
    /// <summary>
    /// Определяет действие, которое система должна выполнить при обнаружении читерства.
    /// </summary>
    public class AntiCheatResponse
    {
        /// <summary>
        /// Логировать ли событие в консоль.
        /// </summary>
        public bool LogToConsole { get; set; }

        /// <summary>
        /// Сообщение для пользователя.
        /// </summary>
        public string UserMessage { get; set; }

        public AntiCheatResponse()
        {
            LogToConsole = true;
            UserMessage = "Обнаружено подозрительное поведение. Система останавливается.";
        }
    }
}
