namespace Zebble.WinUI
{
    using System.IO;

    public static class PathExtensions
    {
        public static string AppUIFolder = "App.UI";
        public static string GetAppUIFolder(this DirectoryInfo currentDirectory)
        {
            while (currentDirectory.Parent.Name != "Run")
                currentDirectory = Directory.GetParent(currentDirectory.FullName);

            return Path.Combine(currentDirectory.Parent.Parent.FullName, AppUIFolder);
        }
    }
}