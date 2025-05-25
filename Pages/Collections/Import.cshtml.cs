using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;
using PromptStudio.Services;
using System.Text.Json;

namespace PromptStudio.Pages.Collections
{    public class ImportModel : PageModel
    {
        private readonly PromptStudioDbContext _context;
        private readonly IPromptService _promptService;

        public ImportModel(PromptStudioDbContext context, IPromptService promptService)
        {
            _context = context;
            _promptService = promptService;
        }

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

        public async Task<IActionResult> OnPostAsync()
        {
            if (ImportFile == null || ImportFile.Length == 0)
            {
                ErrorMessage = "Please select a file to import.";
                return Page();
            }

            if (!ImportFile.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "Only JSON files are supported.";
                return Page();
            }

            try
            {
                using var stream = ImportFile.OpenReadStream();
                using var reader = new StreamReader(stream);
                var jsonContent = await reader.ReadToEndAsync();

                var importData = JsonSerializer.Deserialize<ImportData>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (importData?.Collection == null)
                {
                    ErrorMessage = "Invalid file format. Please ensure this is a valid PromptStudio export file.";
                    return Page();
                }

                // Check if collection already exists
                var existingCollection = await _context.Collections
                    .FirstOrDefaultAsync(c => c.Name == importData.Collection.Name);

                Collection targetCollection;

                if (existingCollection != null && OverwriteExisting)
                {
                    // Remove existing collection and its data
                    _context.Collections.Remove(existingCollection);
                    await _context.SaveChangesAsync();
                }

                // Create new collection
                var collectionName = importData.Collection.Name;
                if (existingCollection != null && !OverwriteExisting)
                {
                    // Find a unique name
                    var counter = 1;
                    while (await _context.Collections.AnyAsync(c => c.Name == $"{collectionName} ({counter})"))
                    {
                        counter++;
                    }
                    collectionName = $"{collectionName} ({counter})";
                }

                targetCollection = new Collection
                {
                    Name = collectionName,
                    Description = importData.Collection.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Collections.Add(targetCollection);
                await _context.SaveChangesAsync();

                // Import prompts
                foreach (var promptData in importData.Collection.Prompts ?? new List<PromptData>())
                {
                    var prompt = new PromptTemplate
                    {
                        Name = promptData.Name ?? "Untitled Prompt",
                        Description = promptData.Description,
                        Content = promptData.Content,
                        CollectionId = targetCollection.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.PromptTemplates.Add(prompt);
                    await _context.SaveChangesAsync();                    // Import variables (auto-detect from content to ensure consistency)
                    var variables = _promptService.ExtractVariableNames(prompt.Content ?? "");
                    foreach (var variableName in variables)
                    {                        var variable = new PromptVariable
                        {
                            Name = variableName,
                            PromptTemplateId = prompt.Id,
                            Description = promptData.Variables?.FirstOrDefault(v => v.Name == variableName)?.Description,
                            DefaultValue = promptData.Variables?.FirstOrDefault(v => v.Name == variableName)?.DefaultValue,
                            Type = Enum.TryParse<VariableType>(promptData.Variables?.FirstOrDefault(v => v.Name == variableName)?.Type ?? "Text", true, out var type) ? type : VariableType.Text
                        };
                        _context.PromptVariables.Add(variable);
                    }

                    // Import execution history if requested
                    if (ImportExecutionHistory && promptData.ExecutionHistory != null)
                    {
                        foreach (var executionData in promptData.ExecutionHistory)
                        {                            var execution = new PromptExecution
                            {
                                PromptTemplateId = prompt.Id,
                                VariableValues = executionData.VariableValues,
                                ResolvedPrompt = executionData.ResolvedPrompt,
                                ExecutedAt = executionData.ExecutedAt
                            };
                            _context.PromptExecutions.Add(execution);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                SuccessMessage = $"Successfully imported collection '{targetCollection.Name}' with {importData.Collection.Prompts?.Count ?? 0} prompts.";
                
                // Clear form
                ImportFile = null!;
                ImportExecutionHistory = false;
                OverwriteExisting = false;

                return Page();
            }
            catch (JsonException)
            {
                ErrorMessage = "Invalid JSON format. Please ensure this is a valid PromptStudio export file.";
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error importing collection: {ex.Message}";
                return Page();
            }
        }

        // Data classes for deserialization
        public class ImportData
        {
            public CollectionData? Collection { get; set; }
        }

        public class CollectionData
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public List<PromptData>? Prompts { get; set; }
        }

        public class PromptData
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? Content { get; set; }
            public List<VariableData>? Variables { get; set; }
            public List<ExecutionData>? ExecutionHistory { get; set; }
        }

        public class VariableData
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? DefaultValue { get; set; }
            public string? Type { get; set; }
        }        public class ExecutionData
        {
            public DateTime ExecutedAt { get; set; }
            public string? VariableValues { get; set; }
            public string? ResolvedPrompt { get; set; }
        }
    }
}
