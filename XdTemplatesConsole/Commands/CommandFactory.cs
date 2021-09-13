using System;
using System.CommandLine;

namespace XdTemplatesConsole
{
    using static System.Console;

    public enum CommandType
    {
        GenerateSimple,
        GenerateMenu
    }

    public interface ICommandFactory
    {
        public Command Create(CommandType commandType);
    }

    public class CommandFactory : ICommandFactory
    {
        private readonly NLog.ILogger logger;
        private readonly IFileFunctions fileFunctions;

        public CommandFactory(IFileFunctions fileFunctions, NLog.ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException($"{nameof(logger)} cannot be null.");
            this.fileFunctions = fileFunctions ?? throw new ArgumentNullException($"{nameof(fileFunctions)} cannot be null.");
        }

        public Command Create(CommandType commandType)
        {
            switch (commandType)
            {
                case CommandType.GenerateSimple:
                    return new GenerateSimpleCommand(fileFunctions, logger).Configure();

                case CommandType.GenerateMenu:
                    return new GenerateMenuCommand(fileFunctions, logger).Configure();
                
                default:
                    throw new NotImplementedException();
            }
        }
    }
}