using System.IO;

namespace CodeGenWebApp.Helpers
{
    public static class ResourceHelper
    {
        public static void CopyGenericAPIService(string webRootPath, string rootOutputDirPath)
        {
            var genericApiServicePath = Path.Combine(webRootPath, "resources", "generic-api-service.ts");

            if (!File.Exists(genericApiServicePath))
                return;

            var resourceDirPath = Path.Combine(rootOutputDirPath, "resources");

            Directory.CreateDirectory(resourceDirPath);

            var targetFilePath = Path.Combine(resourceDirPath, "generic-api-service.ts");

            File.Copy(genericApiServicePath, targetFilePath, true);
        }
    }
}
