using CodeGenWebApp.Helpers;
using CodeGenWebApp.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace CodeGenWebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CodeGeneratorController : ControllerBase
    {
        private readonly ILogger<CodeGeneratorController> _logger;
        private readonly string _webRootPath;

        public CodeGeneratorController(ILogger<CodeGeneratorController> logger, IWebHostEnvironment webHostEnvironment)
        {
            _logger = logger;
            _webRootPath = webHostEnvironment.WebRootPath;
        }

        /// <summary>
        /// Upload your swagger.json file to extract the DTOs and APIs and get the Angular API call code generated
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost("[action]")]
        public async Task<IActionResult> GetGeneratedCode(CodeGenRequestDto model)
        {
            if (model.File == null)
                return BadRequest(ModelState);

            var uniqueDirName = Guid.NewGuid().ToString();
            var rootOutputDirPath = Path.Combine(_webRootPath, uniqueDirName);

            using (var sr = new StreamReader(model.File.OpenReadStream()))
            {
                var contents = await sr.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(contents))
                    return BadRequest(ModelState);

                var jsonNode = JsonNode.Parse(contents);

                await DtoGenerator.GenerateAsync(rootOutputDirPath, jsonNode);
                await ServiceGenerator.GenerateAsync(rootOutputDirPath, jsonNode, model.Prefix);

                ResourceHelper.CopyGenericAPIService(_webRootPath, rootOutputDirPath);
            }

            var zipFilePath = $"{rootOutputDirPath}.zip";

            ZipUtility.CreateZipFromDirectory(rootOutputDirPath, zipFilePath);

            PerformCleanup(rootOutputDirPath, zipFilePath);

            return File(new FileStream(zipFilePath, FileMode.Open), "application/octet-stream", Path.GetFileName(zipFilePath));
        }

        private static void PerformCleanup(string rootOutputDirPath, string zipFilePath)
        {
            // Delete file after download
            Task.Run(async () =>
            {
                await Task.Delay(10000);
                System.IO.File.Delete(zipFilePath);

                var files = Directory.GetFiles(rootOutputDirPath, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                    System.IO.File.Delete(file);

                Directory.Delete(rootOutputDirPath, true);
            });
        }
    }
}
