using System;
using System.Data.Common;

namespace Dapper.Logging.Hooks
{
    public interface ISqlHooks<in T>
    {
        void ConnectionOpened(DbConnection connection, T context, TimeSpan elapsed);
        void ConnectionClosed(DbConnection connection, T context, TimeSpan elapsed);
        void CommandExecuted(DbCommand command, T context, TimeSpan elapsed);
    }
}