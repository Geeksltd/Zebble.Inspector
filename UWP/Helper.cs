namespace Zebble.UWP
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    public static class Helper
    {
        public static string GetAppUIPath()
        {
            return Directory.GetParent(Environment.CurrentDirectory).GetAppUIFolder();
        }

        public static string GetSourCodeAttrbiut(Type type)
        {
            try
            {
                return $"{type.GetCustomAttributesData().FirstOrDefault(x => x.AttributeType.FullName == "Zebble.Services.Css.SourceCodeAttribute").ConstructorArguments[0].Value}";
            }
            catch (Exception ex)
            {
                var error = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }

            return "";
        }

        public static async Task LoadInVisualStudio(string filePath)
        {
            using var httpClient = new HttpClient();

            try
            {
                var response = await httpClient.GetStringAsync(new Uri($"http://localhost:19778/Zebble/VSIX/?type={filePath}"));
                httpClient.Dispose();
            }
            catch (Exception ex)
            {
                var error = "Error: " + ex.HResult.ToString("X") + " Message: " + ex.Message;
            }
        }
    }
}