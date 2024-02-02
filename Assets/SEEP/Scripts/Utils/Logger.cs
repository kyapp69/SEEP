#define UNITY_DIALOGS // Comment out to disable dialogs for fatal errors
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR && UNITY_DIALOGS
using UnityEditor;
#endif

namespace SEEP.Utils
{
///////////////////////////
// Types
///////////////////////////

    [System.Flags]
    public enum LoggerChannel : uint
    {
        Common = 1 << 0,
        NetworkManager = 1 << 1,
        GameManager = 1 << 2,
        CameraManager = 1 << 3,
        InteractableSystem = 1 << 4,
        UI = 1 << 5,
        Input = 1 << 6,
        Assert = 1 << 7,
        ClientController = 1 << 8,
        DroneController = 1 << 9,
        HackerController = 1 << 10,
        LobbyManager = 1 << 11
    }

    /// <summary>
    /// The priority of the log
    /// </summary>
    public enum Priority
    {
        // Default, simple output about game
        Info,

        // Warnings that things might not be as expected
        Warning,

        // Things have already failed, alert the dev
        Error,

        // Things will not recover, bring up pop up dialog
        FatalError,
    }

    public class Logger
    {
        public const LoggerChannel kAllChannels = (LoggerChannel)~0u;

        ///////////////////////////
        // Singleton set up 
        ///////////////////////////

        private static Logger _instance;

        private static Logger Instance
        {
            get { return _instance ??= new Logger(); }
        }

        private Logger()
        {
            _loggerChannels = kAllChannels;
        }

        ///////////////////////////
        // Members
        ///////////////////////////
        private LoggerChannel _loggerChannels;

        public delegate void OnLogFunc(LoggerChannel loggerChannel, Priority priority, string message);

        public static event OnLogFunc OnLog;

        ///////////////////////////
        // LoggerChannel Control
        ///////////////////////////

        public static void ResetChannels()
        {
            Instance._loggerChannels = kAllChannels;
        }

        public static void AddChannel(LoggerChannel loggerChannelToAdd)
        {
            Instance._loggerChannels |= loggerChannelToAdd;
        }

        public static void RemoveChannel(LoggerChannel loggerChannelToRemove)
        {
            Instance._loggerChannels &= ~loggerChannelToRemove;
        }

        public static void ToggleChannel(LoggerChannel loggerChannelToToggle)
        {
            Instance._loggerChannels ^= loggerChannelToToggle;
        }

        public static bool IsChannelActive(LoggerChannel loggerChannelToCheck)
        {
            return (Instance._loggerChannels & loggerChannelToCheck) == loggerChannelToCheck;
        }

        public static void SetChannels(LoggerChannel loggerChannelsToSet)
        {
            Instance._loggerChannels = loggerChannelsToSet;
        }

        ///////////////////////////

        ///////////////////////////
        // Logging functions
        ///////////////////////////

        /// <summary>
        /// Standard logging function, priority will default to info level
        /// </summary>
        /// <param name="logLoggerChannel"></param>
        /// <param name="message"></param>
        public static void Log(LoggerChannel logLoggerChannel, string message)
        {
            FinalLog(logLoggerChannel, Priority.Info, message);
        }

        /// <summary>
        /// Standard logging function with specified priority
        /// </summary>
        /// <param name="logLoggerChannel"></param>
        /// <param name="priority"></param>
        /// <param name="message"></param>
        public static void Log(LoggerChannel logLoggerChannel, Priority priority, string message)
        {
            FinalLog(logLoggerChannel, priority, message);
        }

        /// <summary>
        /// Log with format args, priority will default to info level
        /// </summary>
        /// <param name="logLoggerChannel"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Log(LoggerChannel logLoggerChannel, string message, params object[] args)
        {
            FinalLog(logLoggerChannel, Priority.Info, string.Format(message, args));
        }

        /// <summary>
        /// Log with format args and specified priority
        /// </summary>
        /// <param name="logLoggerChannel"></param>
        /// <param name="priority"></param>
        /// <param name="message"></param>
        /// <param name="args"></param>
        public static void Log(LoggerChannel logLoggerChannel, Priority priority, string message, params object[] args)
        {
            FinalLog(logLoggerChannel, priority, string.Format(message, args));
        }

        /// <summary>
        /// Assert that the passed in condition is true, otherwise log a fatal error
        /// </summary>
        /// <param name="condition">The condition to test</param>
        /// <param name="message">A user provided message that will be logged</param>
        public static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                FinalLog(LoggerChannel.Assert, Priority.FatalError, string.Format("Assert Failed: {0}", message));
            }
        }

        /// <summary>
        /// This function controls where the final string goes
        /// </summary>
        /// <param name="logLoggerChannel"></param>
        /// <param name="priority"></param>
        /// <param name="message"></param>
        private static void FinalLog(LoggerChannel logLoggerChannel, Priority priority, string message)
        {
            if (!IsChannelActive(logLoggerChannel)) return;
            // Dialog boxes can't support rich text mark up, do we won't colour the final string 
            var finalMessage = ContructFinalString(logLoggerChannel, priority, message, priority != Priority.FatalError);

#if UNITY_EDITOR && UNITY_DIALOGS
            // Fatal errors will create a pop up when in the editor
            if (priority == Priority.FatalError)
            {
                var ignore = EditorUtility.DisplayDialog("Fatal error", finalMessage, "Ignore", "Break");
                if (!ignore)
                {
                    Debug.Break();
                }
            }
#endif
            // Call the correct unity logging function depending on the type of error 
            switch (priority)
            {
                case Priority.FatalError:
                case Priority.Error:
                    Debug.LogError(finalMessage);
                    break;

                case Priority.Warning:
                    Debug.LogWarning(finalMessage);
                    break;

                case Priority.Info:
                    Debug.Log(finalMessage);
                    break;
            }

            OnLog?.Invoke(logLoggerChannel, priority, finalMessage);
        }

        /// <summary>
        /// Creates the final string with colouration based on loggerChannel and priority 
        /// </summary>
        /// <param name="logLoggerChannel"></param>
        /// <param name="priority"></param>
        /// <param name="message"></param>
        /// <param name="shouldColour"></param>
        /// <returns></returns>
        private static string ContructFinalString(LoggerChannel logLoggerChannel, Priority priority, string message, bool shouldColour)
        {
            var priortiyColour = priorityToColour[priority];

            if (!channelToColour.TryGetValue(logLoggerChannel, out var channelColour))
            {
                channelColour = "black";
                Debug.LogErrorFormat("Please add colour for loggerChannel {0}", logLoggerChannel);
            }

            if (shouldColour)
            {
                return string.Format("<b><color={0}>[{1}] </color></b> <color={2}>{3}</color>", channelColour,
                    logLoggerChannel, priortiyColour, message);
            }

            return $"[{logLoggerChannel}] {message}";
        }

        /// <summary>
        /// Map a loggerChannel to a colour, using Unity's rich text system
        /// </summary>
        private static readonly Dictionary<LoggerChannel, string> channelToColour = new Dictionary<LoggerChannel, string>
        {
            { LoggerChannel.Common, "cyan" },
            { LoggerChannel.Assert, "lightblue" },
            { LoggerChannel.Input, "blue" },
            { LoggerChannel.GameManager, "green" },
            { LoggerChannel.NetworkManager, "yellow" },
            { LoggerChannel.UI, "purple" },
            { LoggerChannel.LobbyManager, "orange" },
            { LoggerChannel.CameraManager, "teal" },
            { LoggerChannel.ClientController, "olive" },
            { LoggerChannel.DroneController, "navy" },
            { LoggerChannel.HackerController, "red" },
            { LoggerChannel.InteractableSystem, "darkgreen" }
            /*{ LoggerChannel.Build, "navy" },
            { LoggerChannel.Analytics, "maroon" },
            / LoggerChannel.Common, "cyan"},
            { LoggerChannel.GameManager, "green"},
            { LoggerChannel.NetworkManager, "navy"},
            { LoggerChannel.CameraManager, "maroon"},
            { LoggerChannel.InteractableSystem, "orange"},
            { LoggerChannel.UI, "purple"}*/
        };

        /// <summary>
        /// Map a priority to a colour, using Unity's rich text system
        /// </summary>
        private static readonly Dictionary<Priority, string> priorityToColour = new Dictionary<Priority, string>
        {
#if UNITY_PRO_LICENSE
        { Priority.Info,        "white" },
#else
            { Priority.Info, "black" },
#endif
            { Priority.Warning, "orange" },
            { Priority.Error, "red" },
            { Priority.FatalError, "red" },
        };
    }
}