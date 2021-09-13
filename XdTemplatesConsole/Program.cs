using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.CommandLine;
using NLog;
using NLog.Extensions.Logging;

namespace XdTemplatesConsole
{
    class Program
    {
        static int Main(string[] args)
        {
            ICommandFactory commandFactory = null;
            ServiceProvider serviceProvider = null;

            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json").Build();

            LogManager.Configuration = new NLogLoggingConfiguration(config.GetSection("NLog"));
            
            var services = new ServiceCollection()
                .AddSingleton<ILogger>(logger => LogManager.Setup().LoadConfigurationFromSection(config).GetCurrentClassLogger())
                .AddSingleton<IFileFunctions>(ah => new FileFunctions())
                .AddSingleton<ICommandFactory, CommandFactory>()
            ;

            serviceProvider = services.BuildServiceProvider();
            commandFactory = serviceProvider.GetService<ICommandFactory>();

            var rootCommand = new RootCommand("XD Templates");
            var generateCommand = new Command("gen", "Generate.");

            rootCommand.Add(generateCommand);

            generateCommand.Add(commandFactory.Create(CommandType.GenerateSimple));
            generateCommand.Add(commandFactory.Create(CommandType.GenerateMenu));

            //
            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;
        }
    }
}
