using System;
using System.Data;

namespace Data.Sqlite
{
    public class CommandException : DataException
    {
        public CommandException()
        {
        }

        public CommandException(IDbCommand command)
            : this(command, null, null)
        {
        }

        public CommandException(IDbCommand command, Exception innerException)
            : this(command, null, innerException)
        {
        }
        public CommandException(IDbCommand command, string message, Exception innerException)
            : base(message ?? "Execute DB Command Exception", innerException)
        {
            this.Command = command;
        }

        public IDbCommand Command
        {
            get; private set;
        }

        public override string Message
        {
            get
            {
                string message = base.Message;
                var cmd = Command;
                if (cmd != null)
                {
                    message += "CommandText: {0}".FormatArgs(cmd.CommandText);
                    if (cmd.Parameters != null)
                    {
                        foreach (IDbDataParameter p in cmd.Parameters)
                        {
                            message += "Parameter Name: {0}, Value: <{1}>".FormatArgs(p.ParameterName, p.Value);
                        }
                    }
                }
                return message;
            }
        }

    }

}
