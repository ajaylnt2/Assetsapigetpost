using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Logger
{
    public class Logger : ILogger
    {
        private static ILog log = null;
        static Logger()
        {
            log = LogManager.GetLogger(typeof(Logger));
            log4net.GlobalContext.Properties["host"] = Environment.MachineName;
        }

        /// <summary>
        /// Constructer with Parameter
        /// </summary>
        /// <param name="logClass">
        /// Class Type
        /// </param>
        /// <returns>
        /// NA
        /// </returns>
        /// <exception cref="<Exception type>">
        /// NA
        /// </exception>

        public Logger(Type logClass)
        {
            log = LogManager.GetLogger(logClass);
        }

        #region ILogger Members
        /// <summary>
        /// Function to Log Exception
        /// </summary>
        /// <param name="exception">
        /// Exception Information
        /// </param>
        /// <returns>
        /// NA
        /// </returns>
        /// <exception cref="<Exception type>">
        /// NA
        /// </exception>

        public void LogException(Exception exception)
        {
            if (log.IsErrorEnabled)
            {
                log.Error(string.Format(CultureInfo.InvariantCulture, "{0}", exception.Message), exception);
            }
        }

        /// <summary>
        /// Function to Log Error
        /// </summary>
        /// <param name="message">
        /// Error Message information
        /// </param>
        /// <returns>
        /// NA
        /// </returns>
        /// <exception cref="<Exception type>">
        /// NA
        /// </exception>

        public void LogError(string message)
        {
            if (log.IsErrorEnabled)
            {
                log.Error(string.Format(CultureInfo.InvariantCulture, "{0}", message));
            }
        }

        /// <summary>
        /// Function to Log Warning
        /// </summary>
        /// <param name="message">
        /// Warning Message Information
        /// </param>
        /// <returns>
        /// <Description of function return value>
        /// </returns>
        /// <exception cref="<Exception type>">
        /// <Exception that may be thrown by the function>
        /// </exception>

        public void LogWarningMessage(string message)
        {
            if (log.IsWarnEnabled)
            {
                log.Warn(string.Format(CultureInfo.InvariantCulture, "{0}", message));
            }
        }

        /// <summary>
        /// Function to Log Information
        /// </summary>
        /// <param name="message">
        /// Information Message
        /// </param>
        /// <returns>
        /// NA
        /// </returns>
        /// <exception cref="<Exception type>">
        /// NA
        /// </exception>

        public void LogInfoMessage(string message)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(string.Format(CultureInfo.InvariantCulture, "{0}", message));
            }
        }
        #endregion
    }
}
