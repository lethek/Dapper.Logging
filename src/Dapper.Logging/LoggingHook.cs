using System;
using System.Data.Common;
using Dapper.Logging.Configuration;
using Dapper.Logging.Hooks;
using Microsoft.Extensions.Logging;

namespace Dapper.Logging
{
    internal class LoggingHook<T> : ISqlHooks<T>
    {
        private readonly ILogger _logger;
        private readonly DbLoggingConfiguration _config;
        private readonly Func<DbConnection, object> _connectionProjector;

        public LoggingHook(ILogger logger, DbLoggingConfiguration config)
        {
            _logger = logger;
            _config = config;
            _connectionProjector = _config.ConnectionProjector ?? (_ => Empty.Object);
        }

        public void ConnectionOpened(DbConnection connection, T context, TimeSpan elapsed) => 
            _logger.Log(
                _config.LogLevel, 
                _config.OpenConnectionMessage,
                elapsed.TotalMilliseconds,
                context,
                _connectionProjector(connection));

        public void ConnectionClosed(DbConnection connection, T context, TimeSpan elapsed) => 
            _logger.Log(
                _config.LogLevel, 
                _config.CloseConnectionMessage,
                elapsed.TotalMilliseconds,
                context,
                _connectionProjector(connection));

        public void CommandExecuted(DbCommand command, T context, TimeSpan elapsed) =>
            _logger.Log(
                _config.LogLevel, 
                _config.ExecuteQueryMessage, 
                command.CommandText,
                command.GetParameters(hideValues: !_config.LogSensitiveData),
                elapsed.TotalMilliseconds,
                context,
                _connectionProjector(command.Connection));
    }
}