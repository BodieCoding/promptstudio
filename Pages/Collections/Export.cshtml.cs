using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;
using System.Text.Json;

namespace PromptStudio.Pages.Collections
{
    public class ExportModel : PageModel
    {
        private readonly PromptStudioDbContext _context;

        public ExportModel(PromptStudioDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Collection Collection { get; set; } = default!;

        [BindProperty]
        public bool IncludeExecutionHistory { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var collection = await _context.Collections
                .Include(c => c.PromptTemplates)
                    .ThenInclude(p => p.Variables)
                .Include(c => c.PromptTemplates)
                    .ThenInclude(p => p.Executions)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (collection == null)
            {
                return NotFound();
            }

            Collection = collection;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var collection = await _context.Collections
                .Include(c => c.PromptTemplates)
                    .ThenInclude(p => p.Variables)
                .Include(c => c.PromptTemplates)
                    .ThenInclude(p => p.Executions)
                .FirstOrDefaultAsync(c => c.Id == Collection.Id);

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
                    CreatedAt = collection.CreatedAt,
                    Prompts = collection.PromptTemplates.Select(p => new
                    {
                        p.Name,
                        p.Description,
                        p.Content,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt,
                        Variables = p.Variables.Select(v => new
                        {
                            v.Name,
                            v.Description,
                            v.DefaultValue,
                            v.Type
                        }),
                        ExecutionHistory = IncludeExecutionHistory ? p.Executions                        .Select(e => new
                        {
                            ExecutedAt = e.ExecutedAt,
                            VariableValues = e.VariableValues,
                            ResolvedPrompt = e.ResolvedPrompt
                        }) : null
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
