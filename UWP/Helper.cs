namespace Zebble.UWP
{
    using System;
    using System.Reflection;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Olive;

    public static class Helper
    {
        static HttpClient Http = new HttpClient();

        public static string GetAppUIPath()
        {
            return Directory.GetParent(Environment.CurrentDirectory).GetAppUIFolder();
        }

        public static string GetSourCodeAttrbiut(Type type)
        {
            try
            {
                type?.GetCustomAttributes<Services.Css.SourceCodeAttribute>()
                    .OrEmpty()
                    .Select(v => v.FilePath)
                    .Trim()
                    .FirstOrDefault();
            }
            catch (Exception ex)
            {
                var error = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }

            return "";
        }

        public static async Task LoadInVisualStudio(string filePath)
        {
            try
            {
                await Http.GetStringAsync(new Uri($"http://localhost:19778/Zebble/VSIX/?type={filePath}"));
            }
            catch (Exception ex)
            {
                Log.For<Inspector>().Error("Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message);
            }
        }
    }
}