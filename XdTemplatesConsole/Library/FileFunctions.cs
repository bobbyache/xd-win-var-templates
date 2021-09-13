using System.IO;

namespace XdTemplatesConsole
{
    public interface IFileFunctions
    {
        bool FileExists(string filePath);
        bool DirectoryExists(string directoryPath);
        void CreateDirectory(string directoryPath);
        string GetFileDirectory(string filePath);
        string ReadTextFile(string filePath);
        void WriteTextFile(string path, string contents);
        void DeleteFile(string path);
         bool IsFolder(string path);
         string GetRootedFilePath(string projectFilePath, string filePath);
    }

    public class FileFunctions : IFileFunctions
    {
        public bool FileExists(string filePath) => File.Exists(filePath);
        public string GetFileDirectory(string filePath) => Path.GetDirectoryName(filePath);

        public string ReadTextFile(string filePath)
        {
            string contents = null;
            
            using (var file = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read))
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    contents = sr.ReadToEnd();
                }
            }
            return contents;
        }

        public void WriteTextFile(string path, string contents)
        {
            if (!DirectoryExists(GetFileDirectory(path)))
            {
                CreateDirectory(GetFileDirectory(path));
            }

            using (FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            using (StreamWriter streamWriter = new StreamWriter(fileStream))
            {
                streamWriter.Write(contents);
                streamWriter.Flush();
            }
        }

        public void DeleteFile(string path) => File.Delete(path);

        public bool DirectoryExists(string directoryPath) => Directory.Exists(directoryPath);

        public void CreateDirectory(string directoryPath) => Directory.CreateDirectory(directoryPath);

        public bool IsFolder(string path)
        {
            // TODO: To generate an error here you can pass a path with a trailing "\" in the --path prompt
            // You can use this to start some form of error handling in the app
            var fileSystemInfo = new DirectoryInfo(path);
            return fileSystemInfo.IsDirectory();
        }

        public string GetRootedFilePath(string projectFilePath, string filePath)
        {
            if (Path.IsPathRooted(filePath))
                return filePath;
            else
                return Path.Combine(Path.GetDirectoryName(projectFilePath), filePath);
        }
    }
}