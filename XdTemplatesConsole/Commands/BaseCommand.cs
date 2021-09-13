using System;
using System.CommandLine;

namespace XdTemplatesConsole
{
    using static System.Console;

    public abstract class BaseCommand
    {
        protected readonly NLog.ILogger logger;
        protected readonly IFileFunctions fileFunctions;

        public BaseCommand(IFileFunctions fileFunctions, NLog.ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException($"{nameof(logger)} cannot be null.");
            this.fileFunctions = fileFunctions ?? throw new ArgumentNullException($"{nameof(fileFunctions)} cannot be null.");
        }

        public abstract Command Configure();
    }
}