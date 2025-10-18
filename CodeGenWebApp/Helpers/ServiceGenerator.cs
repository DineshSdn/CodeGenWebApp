using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeGenWebApp.Helpers
{
    public static class ServiceGenerator
    {
        private const string GenericApiServicePath = "\"@core-services\"";

        public static async Task GenerateAsync(string rootOutputDirPath, JsonNode json, string prefix)
        {
            var outputDirPath = Path.Combine(rootOutputDirPath, "api-services");

            Directory.CreateDirectory(outputDirPath);

            var indexedExports = new HashSet<string>();

            if (json?["paths"] is not JsonObject paths)
                return;

            var serviceMap = new Dictionary<string, List<ApiDetail>>();

            foreach (var (originalPath, pathNode) in paths)
            {
                if (pathNode is null) continue;

                // Add prefix to the path
                var tokens = originalPath.Split('/').Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

                if (!string.IsNullOrWhiteSpace(prefix))
                    tokens[0] = $"{prefix}-{tokens[0]}";

                var updatedPath = $"/{string.Join('/', tokens)}";

                // Group endpoints into services
                var pathTokens = updatedPath.Split('/').Where(t => !string.IsNullOrWhiteSpace(t) && !t.Contains('{')).ToList();
                var methodName = pathTokens.LastOrDefault("unknown");
                var serviceName = pathTokens.ElementAtOrDefault(pathTokens.Count - 2) ?? "unknown";
                var actionName = char.ToLower(methodName[0]) + methodName[1..];

                if (!serviceMap.ContainsKey(serviceName))
                    serviceMap[serviceName] = [];

                serviceMap[serviceName].Add(new ApiDetail(originalPath, updatedPath, actionName, pathNode));

                Console.WriteLine($"{serviceName} -> {actionName} -> {updatedPath}");
            }

            // Generate a .service.ts file for each group
            foreach (var (serviceName, details) in serviceMap)
            {
                var fileName = GetKebabCaseServiceFileName(serviceName);
                var servicePath = Path.Combine(outputDirPath, fileName);
                var serviceDefinition = new StringBuilder();
                var imports = new HashSet<string>();

                serviceDefinition.AppendLine("import { Injectable } from \"@angular/core\";");
                serviceDefinition.AppendLine($"import {{ GenericApiService }} from {GenericApiServicePath};");
                serviceDefinition.AppendLine("import { Observable } from \"rxjs\";");

                // Collect DTO imports
                foreach (var detail in details)
                {
                    var modelType = GetRequestModelType(detail.Details);
                    if (modelType != "any") imports.Add(modelType);
                }

                if (imports.Any())
                {
                    serviceDefinition.AppendLine();
                    serviceDefinition.AppendLine("import {");
                    serviceDefinition.AppendLine($"    {string.Join(",\n    ", imports)}");
                    serviceDefinition.AppendLine("} from \"../api-models\";");
                }

                serviceDefinition.AppendLine();
                serviceDefinition.AppendLine("@Injectable({");
                serviceDefinition.AppendLine("    providedIn: 'root',");
                serviceDefinition.AppendLine("})");
                serviceDefinition.AppendLine($"export class {serviceName}Service {{");
                serviceDefinition.AppendLine("    constructor(private readonly _genericApiService: GenericApiService) { }");

                foreach (var detail in details)
                {
                    serviceDefinition.AppendLine();
                    serviceDefinition.Append(GenerateMethod(detail));
                }
                serviceDefinition.AppendLine("}");

                await File.WriteAllTextAsync(servicePath, serviceDefinition.ToString());
                indexedExports.Add($"export {{ {serviceName}Service }} from './{fileName.Replace(".service.ts", ".service")}';");
            }

            await File.WriteAllTextAsync(Path.Combine(outputDirPath, "index.ts"), string.Join('\n', indexedExports));
        }

        private static string GenerateMethod(ApiDetail detail)
        {
            var sb = new StringBuilder();
            var apiPath = detail.UpdatedPath.Replace("{", "${");
            var routeKeys = string.Join(", ", detail.UpdatedPath.Split('/')
                .Where(x => x.Contains('{'))
                .Select(x => $"{x.Replace("{", "").Replace("}", "")} : any"));

            var httpMethodNode = detail.Details["get"] ?? detail.Details["post"];
            var isGetRequest = detail.Details["get"] != null;

            var methodParams = new List<string>();
            if (!string.IsNullOrEmpty(routeKeys)) methodParams.Add(routeKeys);

            var (modelType, isModelDefined) = GetMethodModel(httpMethodNode, isGetRequest);

            if (isModelDefined)
            {
                var modelParam = $"model: {modelType}";
                if (isGetRequest) modelParam += " = null";
                methodParams.Add(modelParam);
            }

            methodParams.Add("isShowLoader: boolean = true");

            sb.AppendLine($"    {ToCamelCase(detail.ActionName)}<T>({string.Join(", ", methodParams)}): Observable<T> {{");

            if (isGetRequest)
                sb.AppendLine($"        return this._genericApiService.get<T>(`{apiPath}`, {(isModelDefined ? "model" : "null")}, isShowLoader);");
            else
                sb.AppendLine($"        return this._genericApiService.post<T>(`{apiPath}`, model, isShowLoader);");

            sb.AppendLine("    }");
            return sb.ToString();
        }

        private static (string ModelType, bool IsDefined) GetMethodModel(JsonNode? httpMethodNode, bool isGetRequest)
        {
            if (httpMethodNode is null) return ("any", false);

            if (!isGetRequest) // POST request
            {
                return (GetRequestModelType(httpMethodNode), true);
            }

            // GET request with query parameters
            if (httpMethodNode["parameters"] is not JsonArray parameters) return ("any", false);

            var queryParams = parameters
                .Where(p => p?["in"]?.GetValue<string>() == "query")
                .Select(p =>
                {
                    var name = p?["name"]?.GetValue<string>() ?? "unknown";
                    var type = MapToTypeScriptType(p?["schema"]).Type;
                    return $"{name}?: {type} | null | undefined";
                })
                .ToList();

            if (!queryParams.Any()) return ("any", false);
            return ($"{{ {string.Join(", ", queryParams)} }} | null | undefined", true);
        }

        private static string GetRequestModelType(JsonNode? detailsNode)
        {
            var refPath = detailsNode?["post"]?["requestBody"]?["content"]?["application/json"]?["schema"]?["$ref"]?.GetValue<string>();
            return refPath?.Split('/').Last() ?? "any";
        }

        private static (string Type, bool IsImport) MapToTypeScriptType(JsonNode? property)
        {
            if (property?["$ref"] is JsonNode refNode)
                return (refNode.GetValue<string>().Split('/').Last(), true);

            return property?["type"]?.GetValue<string>() switch
            {
                "string" => ("string", false),
                "integer" or "number" => ("number", false),
                "boolean" => ("boolean", false),
                _ => ("any", false)
            };
        }

        private static string ToCamelCase(string str) =>
            Regex.Replace(str, @"[-_\s]([a-z])", m => m.Groups[1].Value.ToUpper());

        private static string GetKebabCaseServiceFileName(string className)
        {
            var kebabCase = Regex.Replace(className, @"([a-z0-9])([A-Z])", "$1-$2");
            kebabCase = Regex.Replace(kebabCase, @"([A-Z])([A-Z][a-z])", "$1-$2");
            return $"{kebabCase.ToLower()}.service.ts";
        }

        private record ApiDetail(string OriginalPath, string UpdatedPath, string ActionName, JsonNode Details);
    }
}
