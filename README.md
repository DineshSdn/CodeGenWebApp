# Angular TypeScript Code Generator from Swagger/OpenAPI

This project is a **.NET 8 Web API** application that automatically generates Angular TypeScript code (DTOs and API services) from a provided Swagger/OpenAPI JSON file.

## Features

- **DTO Generation:** Converts Swagger schemas into TypeScript interfaces.
- **API Service Generation:** Creates Angular service classes for API endpoints.
- **Customizable Prefix:** Allows prefixing of generated API routes.
- **ZIP Packaging:** Bundles all generated code into a downloadable ZIP file.
- **Automatic Cleanup:** Cleans up temporary files after code generation.

## Technologies Used

- **.NET 8 (ASP.NET Core Web API)**
- **System.Text.Json.Nodes** for JSON parsing
- **Angular/TypeScript** code generation

## How It Works

1. **Upload** your Swagger/OpenAPI JSON file via the `/api/CodeGenerator/GetGeneratedCode` endpoint.
2. The backend parses the file and generates:
    - TypeScript interfaces for API models (`api-models/`)
    - Angular service classes for API endpoints (`api-services/`)
    - Barrel files (`index.ts`) for easy imports
3. The generated code is packaged into a ZIP file and returned for download.
4. Temporary files are automatically deleted after download.

## Output Structure
