using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Core.Domain;
using PromptStudio.Core.Interfaces;

namespace PromptStudio.Pages.Prompts
{    public class DuplicateModel : PageModel
    {
        private readonly PromptStudioDbContext _context;
        private readonly IPromptService _promptService;

        public DuplicateModel(PromptStudioDbContext context, IPromptService promptService)
        {
            _context = context;
            _promptService = promptService;
        }

        [BindProperty]
        public int OriginalPromptId { get; set; }

        [BindProperty]
        public PromptTemplate NewPrompt { get; set; } = new();

        public PromptTemplate OriginalPrompt { get; set; } = default!;
        public List<Collection> Collections { get; set; } = new();
        public List<string> DetectedVariables { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var originalPrompt = await _context.PromptTemplates
                .Include(p => p.Collection)
                .Include(p => p.Variables)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (originalPrompt == null)
            {
                return NotFound();
            }

            OriginalPrompt = originalPrompt;
            OriginalPromptId = id;

            // Populate form with original data (with modifications)
            NewPrompt.Name = $"{originalPrompt.Name} (Copy)";
            NewPrompt.Description = originalPrompt.Description;
            NewPrompt.Content = originalPrompt.Content;
            NewPrompt.CollectionId = originalPrompt.CollectionId;        // Load all collections for dropdown
            Collections = await _context.Collections
                .OrderBy(c => c.Name)
                .ToListAsync();

            DetectedVariables = _promptService.ExtractVariableNames(NewPrompt.Content ?? "");

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Reload necessary data
                OriginalPrompt = await _context.PromptTemplates
                    .Include(p => p.Collection)
                    .FirstOrDefaultAsync(p => p.Id == OriginalPromptId) ?? new();                Collections = await _context.Collections
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                DetectedVariables = _promptService.ExtractVariableNames(NewPrompt.Content ?? "");
                return Page();
            }

            // Create the new prompt
            var newPromptTemplate = new PromptTemplate
            {
                Name = NewPrompt.Name,
                Description = NewPrompt.Description,
                Content = NewPrompt.Content,
                CollectionId = NewPrompt.CollectionId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PromptTemplates.Add(newPromptTemplate);
            await _context.SaveChangesAsync();            // Add variables
            var variables = _promptService.ExtractVariableNames(newPromptTemplate.Content ?? "");
            foreach (var variableName in variables)
            {
                var variable = new PromptVariable
                {
                    Name = variableName,
                    PromptTemplateId = newPromptTemplate.Id
                };
                _context.PromptVariables.Add(variable);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("/Prompts/Execute", new { id = newPromptTemplate.Id });
        }
    }
}
