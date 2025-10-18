using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeGenWebApp.Helpers
{
    public static class DtoGenerator
    {
        public static async Task GenerateAsync(string rootOutputDirPath, params JsonNode[] jsonDocuments)
        {
            var outputDirPath = Path.Combine(rootOutputDirPath, "api-models");

            Directory.CreateDirectory(outputDirPath);

            var lines = new List<string>();
            var indexedExports = new HashSet<string>();

            foreach (var json in jsonDocuments)
            {
                if (json?["components"]?["schemas"] is not JsonObject schemas) continue;

                foreach (var (schemaName, schemaNode) in schemas)
                {
                    if (schemaNode is null) continue;

                    var fileName = GetKebabCaseFileName(schemaName);
                    var modelPath = Path.Combine(outputDirPath, fileName);
                    var modelDefinition = new List<string>();

                    var (properties, imports) = GetPropertiesAndImports(schemaNode);

                    foreach (var import in imports)
                    {
                        var importFileName = GetKebabCaseFileName(import).Replace(".ts", "");
                        modelDefinition.Add($"import {{ {import} }} from './{importFileName}';");
                    }

                    if (imports.Any()) modelDefinition.Add("");

                    modelDefinition.Add($"export interface {schemaName} {{");

                    foreach (var prop in properties)
                        modelDefinition.Add($"  {prop}");

                    modelDefinition.Add("}");

                    await File.WriteAllTextAsync(modelPath, string.Join('\n', modelDefinition));
                    indexedExports.Add($"export {{ {schemaName} }} from './{fileName.Replace(".ts", "")}';");

                    Console.WriteLine($"{schemaName} -> {fileName}");
                }
            }

            await File.WriteAllTextAsync(Path.Combine(outputDirPath, "index.ts"), string.Join('\n', indexedExports));
        }

        private static (HashSet<string> Properties, HashSet<string> Imports) GetPropertiesAndImports(JsonNode schema)
        {
            var requiredProps = new HashSet<string>(schema["required"]?.AsArray().Select(n => n!.GetValue<string>()) ?? []);
            var imports = new HashSet<string>();
            var properties = new HashSet<string>();

            if (schema["properties"] is not JsonObject schemaProperties)
                return (properties, imports);

            foreach (var (propName, propertyNode) in schemaProperties)
            {
                if (propertyNode is null) continue;

                var (tsType, isImport) = MapToTypeScriptType(propertyNode);
                var isOptional = !requiredProps.Contains(propName);
                var optionalSymbol = isOptional ? "?" : "";

                if (isImport)
                {
                    // Add base type for arrays, e.g., "MyType" from "MyType[]"
                    imports.Add(tsType.Replace("[]", ""));
                }
                properties.Add($"{propName}{optionalSymbol}: {tsType} | undefined | null;");
            }

            return (properties, imports);
        }

        private static (string Type, bool IsImport) MapToTypeScriptType(JsonNode property)
        {
            if (property["$ref"] is JsonNode refNode)
            {
                return (refNode.GetValue<string>().Split('/').Last(), true);
            }

            var type = property["type"]?.GetValue<string>();
            return type switch
            {
                "string" => ("string", false),
                "integer" or "number" => ("number", false),
                "boolean" => ("boolean", false),
                "array" => (property["items"] != null ? $"{MapToTypeScriptType(property["items"]!).Type}[]" : "any[]",
                            property["items"]?["$ref"] != null),
                _ => ("any", false)
            };
        }

        private static string GetKebabCaseFileName(string className)
        {
            // 1. Remove common suffixes
            var strippedName = Regex.Replace(className, @"Vm|VM|Dto|Master$", "", RegexOptions.IgnoreCase);

            // 2. Convert to kebab-case
            var kebabCase = Regex.Replace(strippedName, @"([a-z0-9])([A-Z])", "$1-$2");
            kebabCase = Regex.Replace(kebabCase, @"([A-Z])([A-Z][a-z])", "$1-$2");
            return $"{kebabCase.ToLower()}.ts";
        }
    }
}
