using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LIS.Logger
{
    /// <summary>
    /// Ilogger inter face used to implement various logging methods in application.
    /// </summary>
    public interface ILogger
    {
        #region Trace
        /// <summary>
        /// Log the name of the method when execution starts.
        /// https://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.callermembernameattribute(v=vs.110).aspx
        /// </summary>
        /// <param name="message">Custom message</param>
        /// <param name="memberName">Method Name</param>
        /// <param name="sourceFilePath">File Pahth</param>
        /// <param name="sourceLineNumber">Line Number</param>
        void LogTrace(string message);

        #endregion

        #region Fatal
        /// <summary>
        /// Log system exception.
        /// </summary>
        /// <param name="exception">Exception object.</param>
        void LogException(Exception exception);

        /// <summary>
        /// Logs system exception with custom error message.
        /// </summary>
        /// <param name="message">Custom error message.</param>
        /// <param name="exception">Exception object.</param>
        void LogException(string message, Exception exception);

        /// <summary>
        /// Logs system exception with formatted custom error message.
        /// </summary>
        /// <param name="exception">Exception </param>
        /// <param name="format">Message Format</param>
        /// <param name="args">Arguments</param>
        void LogException(string format, Exception exception, params object[] args);


        /// <summary>
        ///  Log system exception..
        /// </summary>
        /// <param name="message">Custom error message.</param>
        void LogFatal(string message);

        /// <summary>
        /// Log formatted system exception.
        /// </summary>
        /// <param name="format">Format of Message</param>
        /// <param name="args">arguments for format</param>
        void LogFatal(string format, params object[] args);
        #endregion

        #region Error
        /// <summary>
        /// Log custom error message based on application logic and validations.
        /// </summary>
        /// <param name="message">Custom error message.</param>
        void LogError(string message);

        /// <summary>
        /// Log formatted custom error message based on application logic and validations.
        /// </summary>
        /// <param name="format">Format of Message</param>
        /// <param name="args">arguments for format</param>
        void LogError(string format, params object[] args);
        #endregion

        #region Warning
        /// <summary>
        /// Log custom warning message based application logic and validations.
        /// </summary>
        /// <param name="message">Custom warning message.</param>
        void LogWarning(string message);

        /// <summary>
        /// Log formatted custom warning message based application logic and validations.
        /// </summary>
        /// <param name="format">Format of Message</param>
        /// <param name="args">arguments for format</param>
        void LogWarning(string format, params object[] args);
        #endregion

        #region Info
        /// <summary>
        /// Logs custom information and provides status.
        /// </summary>
        /// <param name="message">Custom information message.</param>
        void LogInfo(string message);

        /// <summary>
        /// Log formatted custom information and provides status.
        /// </summary>
        /// <param name="format">Format of Message</param>
        /// <param name="args">arguments for format</param>
        void LogInfo(string format, params object[] args);
        #endregion

        #region Debug
        /// <summary>
        /// Logs debug information and provides status.
        /// </summary>
        /// <param name="message">Custom information message.</param>
        void LogDebug(string message);

        /// <summary>
        /// Log formatted debug information and provides status.
        /// </summary>
        /// <param name="format">Format of Message</param>
        /// <param name="args">arguments for format</param>
        void LogDebug(string format, params object[] args);
        #endregion
    }
}
