using UnityEngine;

namespace Estate2D.AntiCheat.Core
{
    public class AntiCheatResponse
    {
        public bool LogToConsole { get; set; }
        public string UserMessage { get; set; }

        public AntiCheatResponse()
        {
            LogToConsole = true;
            UserMessage = "Обнаружено подозрительное поведение. Система останавливается.";
        }
    }
}