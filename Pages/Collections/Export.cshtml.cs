using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Core.Domain;
using PromptStudio.Core.Interfaces;
using System.Text.Json;

namespace PromptStudio.Pages.Collections
{
    public class ExportModel(IPromptService promptService) : PageModel
    {
        [BindProperty]
        public Collection Collection { get; set; } = default!;

        [BindProperty]
        public bool IncludeExecutionHistory { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var collection = await promptService.GetCollectionByIdAsync(id);

            if (collection == null)
            {
                return NotFound();
            }

            Collection = collection;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var collection = await promptService.GetCollectionByIdAsync(Collection.Id);

            if (collection == null)
            {
                return NotFound();
            }

            var exportData = new
            {
                ExportedAt = DateTime.UtcNow,
                ExportVersion = "1.0",
                Collection = new
                {
                    collection.Name,
                    collection.Description,
                    collection.CreatedAt,
                    Prompts = collection.PromptTemplates.Select(p => new
                    {
                        p.Name,
                        p.Description,
                        p.Content,
                        p.CreatedAt,
                        p.UpdatedAt,
                        Variables = p.Variables.Select(v => new
                        {
                            v.Name,
                            v.Description,
                            v.DefaultValue,
                            Type = v.Type.ToString()
                        }),
                        ExecutionHistory = IncludeExecutionHistory && p.Executions != null
                            ? p.Executions.Select(e => new
                            {
                                e.ExecutedAt,
                                e.VariableValues,
                                e.ResolvedPrompt
                            })
                            : null
                    })
                }
            };

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonString = JsonSerializer.Serialize(exportData, jsonOptions);
            var fileName = $"{collection.Name.Replace(" ", "_")}_export_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            var contentType = "application/json";

            return File(System.Text.Encoding.UTF8.GetBytes(jsonString), contentType, fileName);
        }
    }
}
