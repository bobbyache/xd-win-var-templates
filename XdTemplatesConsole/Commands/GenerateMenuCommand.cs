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

    public class GenerateMenuCommand : BaseCommand
    {
        private static string prefix = "";
        private static string postfix = "";
        private static string urlEncodedPrefix = "";
        private static string urlEncodedPostfix = "";

        public GenerateMenuCommand(IFileFunctions fileFunctions, NLog.ILogger logger): base (fileFunctions, logger){}

        public override Command Configure()
        {
            var cmd = new Command("menu", "Opens the interactive menu.");
            cmd.Handler = CommandHandler.Create(() => OpenOptionsMenu());


            return cmd;
        }

        private static string GetOptionInput(Option[] options, string lastOption = null)
        {
            if (lastOption != null)
            {
                Console.WriteLine("");
                Console.WriteLine("That option isn't valid...");
                Console.WriteLine("");
            }

            Console.WriteLine("");
            Console.WriteLine("What action would you like to take?");

            foreach (Option option in options)
            {
                Console.WriteLine($"\t[{option.Id}]. {option.Name}");
            }

            Console.WriteLine("\t[C]. Cancel");
            Console.WriteLine("");
            Console.Write("Make your choice: ");

            var result = Console.ReadLine();

            if (result.ToLower() == "c")
                return "C";
            else if (options.Any((opt) => opt.Id == result))
                return result;
            else
                return GetOptionInput(options, result);
        }

        private void OpenOptionsMenu()
        {
            XDocument document = XDocument.Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Options.xml"));
            FetchPlaceFixers(document);
            var options = GetConfiguredOptions(document);
            
            var result = GetOptionInput(options).ToString();
            if (result == "C")
                return;

            var option = options.Where(opt => opt.Id == result).SingleOrDefault();
            if (option != null)
            {
                ProcessOption(option);
            }
        }

        private void FetchPlaceFixers(XDocument document)
        {
            var normalPrefixElement = document.Element("Settings").Element("Prefixes").Elements("Prefix")
                .Where(p => p.Attribute("Id").Value == "NORMAL").SingleOrDefault();
            prefix = normalPrefixElement.Attribute("Prefix").Value;
            postfix = normalPrefixElement.Attribute("Postfix").Value;

            var urlEncodedPrefixElement = document.Element("Settings").Element("Prefixes").Elements("Prefix")
                .Where(p => p.Attribute("Id").Value == "HTMLENC").SingleOrDefault();
            urlEncodedPrefix = urlEncodedPrefixElement.Attribute("Prefix").Value;
            urlEncodedPostfix = urlEncodedPrefixElement.Attribute("Postfix").Value;
        }

        private Option[] GetConfiguredOptions(XDocument document)
        {
            var options = document.Element("Settings").Element("Options").Elements("Option").Select((opt) => 
                new Option
                {
                    Id = opt.Attribute("Id").Value,
                    Name = opt.Attribute("Name").Value,
                    TemplateFolder = opt.Attribute("TemplateFolder").Value,
                    OutputFolder = opt.Attribute("OutputFolder").Value,
                    VariableFile = opt.Attribute("VariableFile").Value,
                    SearchPattern = opt.Attribute("SearchPattern").Value
                }
            );
            return options.ToArray();
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
                Console.ReadKey();

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAILED TO WRITE TO EXECUTE");
                Console.WriteLine(ex.StackTrace);
                Console.ForegroundColor = ConsoleColor.White;
            }
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