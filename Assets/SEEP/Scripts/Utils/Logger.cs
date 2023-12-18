using UnityEngine;

namespace SEEP.Utils
{
    public static class Logger
    {
        public static void Log<T>(T sender, string message)
        {
            Debug.Log($"(LOG) {sender.GetType()}: {message}");
        }
        
        public static void Log(string sender, string message)
        {
            Debug.Log($"(LOG) {sender}: {message}");
        }
        
        public static void Warning<T>(T sender, string message)
        {
            Debug.LogWarning($"(WRN) {sender.GetType()}: {message}");
        }

        public static void Warning(string sender, string message)
        {
            Debug.LogWarning($"(WRN) {sender}: {message}");
        }

        public static void Error<T>(T sender, string message)
        {
            Debug.LogError($"(ERR) {sender.GetType()}: {message}");
        }
        
        public static void Error(string sender, string message)
        {
            Debug.LogError($"(ERR) {sender}: {message}");
        }
    }
}