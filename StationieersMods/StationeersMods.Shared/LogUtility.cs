using System;
using UnityEngine;

namespace StationeersMods.Shared
{
    /// <summary>
    ///     A class for logging filtered messages.
    /// </summary>
    public class LogUtility
    {
        /// <summary>
        ///     Which level of messages to log.
        /// </summary>
        public static LogLevel logLevel = LogLevel.Info;

        /// <summary>
        ///     Log a debug message.
        /// </summary>
        /// <param name="message">The debug message.</param>
        public static void LogDebug(object message)
        {
            Debug.Log(message);
        }

        /// <summary>
        ///     Log a message.
        /// </summary>
        /// <param name="message">The message.</param>
        public static void LogInfo(object message)
        {
            Debug.Log(message);
        }

        /// <summary>
        ///     Log a warning.
        /// </summary>
        /// <param name="message">The warning message.</param>
        public static void LogWarning(object message)
        {
            Debug.Log(message);
        }

        /// <summary>
        ///     Log an error.
        /// </summary>
        /// <param name="message">The error message</param>
        public static void LogError(object message)
        {
            Debug.Log(message);
        }

        /// <summary>
        ///     Log an exception.
        /// </summary>
        /// <param name="exception">The exception</param>
        public static void LogException(Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}