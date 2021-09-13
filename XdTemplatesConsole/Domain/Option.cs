
namespace XdTemplatesConsole
{
    public class Option
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string TemplateFolder { get; set; }
        public string OutputFolder { get; set; }

        public string VariableFile { get; set; }

        public string SearchPattern { get; set; }
    }
}