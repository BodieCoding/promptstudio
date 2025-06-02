using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Core.Interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace PromptStudio.Pages.Collections
{
    public class ImportModel(IPromptService promptService) : PageModel
    {
        [BindProperty]
        public IFormFile ImportFile { get; set; } = default!;

        [BindProperty]
        public bool ImportExecutionHistory { get; set; }

        [BindProperty]
        public bool OverwriteExisting { get; set; }

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
        }

        // This method pre-processes the JSON to handle potential legacy formats for VariableType
        private static string SanitizeVariableTypes(string jsonContent)
        {
            try
            {
                var root = JsonNode.Parse(jsonContent);
                if (root == null) return jsonContent;

                // Try both possible locations for prompts
                JsonArray? prompts = root["collection"]?["prompts"] as JsonArray
                                     ?? root["prompts"] as JsonArray;

                if (prompts != null)
                {
                    foreach (var promptNode in prompts)
                    {
                        if (promptNode?["variables"] is JsonArray variables)
                        {
                            foreach (var variableNode in variables)
                            {
                                var typeNode = variableNode?["type"];
                                if (typeNode != null && typeNode.GetValueKind() == JsonValueKind.Number)
                                {
                                    // Ensure variableNode is not null before assignment
                                    variableNode!["type"] = JsonValue.Create("Text")
                                                            ?? throw new InvalidOperationException("Failed to create JSON value for variable type.");
                                }
                            }
                        }
                    }
                }
                return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
            }
            catch (JsonException)
            {
                // If JSON parsing fails during sanitization, return original content
                return jsonContent;
            }
            catch (Exception)
            {
                // For other errors during sanitization, return original (fail gracefully)
                return jsonContent;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ImportFile == null || ImportFile.Length == 0)
            {
                ErrorMessage = "Please select a file to import.";
                return Page();
            }

            if (!ImportFile.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Only JSON files are supported for import.";
                return Page();
            }

            try
            {
                string jsonContent;
                using (var stream = ImportFile.OpenReadStream())
                using (var reader = new StreamReader(stream))
                {
                    jsonContent = await reader.ReadToEndAsync();
                }

                // Sanitize variable type fields before passing to the service
                var sanitizedJsonContent = SanitizeVariableTypes(jsonContent);

                var importedCollection = await promptService.ImportCollectionFromJsonAsync(
                    sanitizedJsonContent,
                    ImportExecutionHistory,
                    OverwriteExisting);

                if (importedCollection != null)
                {
                    SuccessMessage = $"Successfully imported collection '{importedCollection.Name}'.";
                    // Clear form fields on success
                    ImportFile = default!;
                    ImportExecutionHistory = false;
                    OverwriteExisting = false;
                }
                else
                {
                    ErrorMessage = "Failed to import collection. The file format might be invalid or an unexpected error occurred.";
                }

                return Page();
            }
            catch (JsonException jsonEx)
            {
                ErrorMessage = $"Invalid JSON format: {jsonEx.Message}. Please ensure this is a valid PromptStudio export file.";
                return Page();
            }
            catch (ArgumentException argEx)
            {
                ErrorMessage = $"Import error: {argEx.Message}";
                return Page();
            }
            catch (Exception ex)
            {
                // Catching DbUpdateException or other specific exceptions if thrown by the service
                ErrorMessage = $"An unexpected error occurred during import: {ex.Message}";
                return Page();
            }
        }

        // Data classes for deserialization are no longer needed here if the service handles parsing.
        // If SanitizeVariableTypes needed them, they would remain, but it uses JsonNode.
    }
}
