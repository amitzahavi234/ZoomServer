using System;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace ZoomServer
{
    public class UserLogger
    {
        /// <summary>
        /// The logging factory used to create loggers and manage logging configurations.
        /// </summary>
        private static readonly LogFactory logFactory = LogManager.LogFactory;

        /// <summary>
        /// Static constructor that safely loads the NLog configuration or creates a default one if missing.
        /// </summary>
        static UserLogger()
        {
            try
            {
                // Load NLog config from file if exists
                LogManager.Setup().LoadConfigurationFromFile("nlog.config", optional: true);

                // If no config was loaded, create a default configuration to log to console
                if (LogManager.Configuration == null)
                {
                    var config = new LoggingConfiguration();

                    var consoleTarget = new ColoredConsoleTarget("console")
                    {
                        Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message}"
                    };

                    config.AddTarget(consoleTarget);
                    config.AddRule(LogLevel.Info, LogLevel.Fatal, consoleTarget);

                    LogManager.Configuration = config;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserLogger] Failed to initialize logging: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves an ILogger instance for a specific user, associating the username with the logger to create a unique log context.
        /// </summary>
        /// <param name="username">The username to associate with the logger.</param>
        public static ILogger GetLoggerForUser(string username)
        {
            // Set the "username" variable to create a unique log file per user (if configured)
            MappedDiagnosticsLogicalContext.Set("username", username);
            return logFactory.GetLogger("UserLogger");
        }
    }
}
