using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Data;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace XdTemplatesConsole
{
    using static System.Console;

    public class GenerateSimpleCommand : BaseCommand
    {
        private string prefix = "{{";
        private string postfix = "}}";
        private string urlEncodedPrefix = "{{||";
        private string urlEncodedPostfix = "||}}";

        public GenerateSimpleCommand(IFileFunctions fileFunctions, NLog.ILogger logger): base (fileFunctions, logger){}

        public override Command Configure()
        {
            var templateFolderOption = new Option<string>( new[] { "--template-folder", "-t" }, "The Template Folder.");
            templateFolderOption.IsRequired = true;
            templateFolderOption.Argument.Arity = ArgumentArity.ExactlyOne;

            var outputFolderOption = new Option<string>( new[] { "--output-folder", "-o" }, "The Output Folder.");
            outputFolderOption.IsRequired = true;
            outputFolderOption.Argument.Arity = ArgumentArity.ExactlyOne;

            var variablesFileOption = new Option<string>( new[] { "--vars", "-v" }, "The Variables file.");
            variablesFileOption.IsRequired = true;
            variablesFileOption.Argument.Arity = ArgumentArity.ExactlyOne;

            var searchPatternOption = new Option<string>( new[] { "--pattern", "-p" }, "The Qik Template Folder.");
            searchPatternOption.IsRequired = true;
            searchPatternOption.Argument.Arity = ArgumentArity.ExactlyOne;

            var cmd = new Command("direct", "Immediately generates the generate operation with the parameters given.")
            {
                templateFolderOption,
                outputFolderOption,
                variablesFileOption,
                searchPatternOption
            };

            cmd.Handler = CommandHandler.Create<string, string, string, string>((Action<string, string, string, string>)((templateFolder, outputFolder, vars, pattern) =>
            {
                var option = new Option()
                {
                    TemplateFolder = templateFolder,
                    OutputFolder = outputFolder,
                    VariableFile = vars,
                    SearchPattern = pattern
                };

                ProcessOption(option);
            }));

            return cmd;
        }

        private void ProcessOption(Option option)
        {
            try
            {
                List<KeyValuePair<string, string>> variables = GetContents(option.VariableFile);
                IEnumerable<string> templateFiles = Directory.EnumerateFiles(option.TemplateFolder, option.SearchPattern);

                Console.WriteLine($"Data from {option.VariableFile}");
                Console.WriteLine($"Processing from {option.TemplateFolder}");
                Console.WriteLine($"Output to {option.OutputFolder}");

                if (!Directory.Exists(option.OutputFolder))
                {
                    Directory.CreateDirectory(option.OutputFolder);
                }

                foreach (string templateFile in templateFiles)
                {
                    ProcessTemplateFile(templateFile, variables, option.OutputFolder);
                }

                foreach(string templateFile in templateFiles)
                {
                    CheckTemplateFile(templateFile, option.OutputFolder);
                }

                Console.WriteLine("DONE PROCESSING !!! HAVE A NICE DAY !!!");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAILED TO WRITE TO EXECUTE");
                Console.WriteLine(ex.StackTrace);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }

        private void ProcessTemplateFile(string templateFile, List<KeyValuePair<string, string>> variables, string outputFolder)
        {
            string resultText = fileFunctions.ReadTextFile(templateFile);

            foreach (var variable in variables)
            {
                resultText = resultText.Replace(urlEncodedPrefix + variable.Key + urlEncodedPostfix, HttpUtility.HtmlEncode(variable.Value));
                resultText = resultText.Replace(prefix + variable.Key + postfix, variable.Value);
            }
            fileFunctions.WriteTextFile(Path.Combine(outputFolder, Path.GetFileName(templateFile)), resultText);
        }

        private void CheckTemplateFile(string templateFile, string outputFolder)
        {
            var resultFile = Path.Combine(outputFolder, Path.GetFileName(templateFile));
            if (File.Exists(resultFile))
            {
                var resultText = fileFunctions.ReadTextFile(resultFile);
                var list = UnknownPlaceholders(resultText, prefix + "[0-9a-zA-z-]+" + postfix);
                var list2 = UnknownPlaceholders(resultText, urlEncodedPrefix + "[0-9a-zA-z-]+" + urlEncodedPostfix);

                var results = list.Distinct().Union(list2.Distinct());
                
                if (results.Count() > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Found the following unknown placeholders - these were not replaced:");
                    foreach (var item in results)
                    {
                        Console.WriteLine(item);
                    }
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
        }

        private IEnumerable<string> UnknownPlaceholders(string source, string pattern)
        {
            string search = "{{[0-9a-zA-z-]+}}";
            MatchCollection matches = Regex.Matches(source, search);

            return matches.Select(m => m.ToString());
        }

        private List<KeyValuePair<string, string>> GetContents(string filePath)
        {
            List<KeyValuePair<string, string>> idList = new List<KeyValuePair<string, string>>();

            if (File.Exists(filePath))
            {
                using (StreamReader streamReader = File.OpenText(filePath))
                {
                    string input = null;
                    while ((input = streamReader.ReadLine()) != null)
                    {

                        if (!input.Trim().StartsWith("#") && input.Trim() != String.Empty)
                        {
                            var keyVal = input.Trim().Split('=', StringSplitOptions.RemoveEmptyEntries);
                            idList.Add(new KeyValuePair<string, string>(keyVal[0], keyVal[1]));
                        }

                    }
                }
            }
            return idList;
        }
    }
}