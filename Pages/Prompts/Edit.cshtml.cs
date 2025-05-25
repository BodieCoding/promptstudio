using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;
using PromptStudio.Services;

namespace PromptStudio.Pages.Prompts
{    public class EditModel : PageModel
    {
        private readonly PromptStudioDbContext _context;
        private readonly IPromptService _promptService;

        public EditModel(PromptStudioDbContext context, IPromptService promptService)
        {
            _context = context;
            _promptService = promptService;
        }

        [BindProperty]
        public PromptTemplate PromptTemplate { get; set; } = default!;

        public List<string> DetectedVariables { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var promptTemplate = await _context.PromptTemplates
                .Include(p => p.Collection)
                .Include(p => p.Variables)
                .Include(p => p.Executions)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (promptTemplate == null)
            {
                return NotFound();
            }

            PromptTemplate = promptTemplate;
            DetectedVariables = _promptService.ExtractVariableNames(PromptTemplate.Content);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                DetectedVariables = _promptService.ExtractVariableNames(PromptTemplate.Content ?? "");
                return Page();
            }

            // Get the existing prompt template to update
            var existingPrompt = await _context.PromptTemplates
                .Include(p => p.Variables)
                .FirstOrDefaultAsync(p => p.Id == PromptTemplate.Id);

            if (existingPrompt == null)
            {
                return NotFound();
            }

            // Update the properties
            existingPrompt.Name = PromptTemplate.Name;
            existingPrompt.Description = PromptTemplate.Description;
            existingPrompt.Content = PromptTemplate.Content;
            existingPrompt.UpdatedAt = DateTime.UtcNow;

            // Update variables
            var newVariables = _promptService.ExtractVariableNames(PromptTemplate.Content ?? "");
            
            // Remove variables that no longer exist
            var variablesToRemove = existingPrompt.Variables
                .Where(v => !newVariables.Contains(v.Name))
                .ToList();
            
            foreach (var variable in variablesToRemove)
            {
                _context.PromptVariables.Remove(variable);
            }

            // Add new variables
            var existingVariableNames = existingPrompt.Variables.Select(v => v.Name);
            var variablesToAdd = newVariables
                .Where(name => !existingVariableNames.Contains(name))
                .Select(name => new PromptVariable
                {
                    Name = name,
                    PromptTemplateId = existingPrompt.Id
                });

            foreach (var variable in variablesToAdd)
            {
                existingPrompt.Variables.Add(variable);
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PromptTemplateExists(PromptTemplate.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("/Index");
        }

        private bool PromptTemplateExists(int id)
        {
            return _context.PromptTemplates.Any(e => e.Id == id);
        }
    }
}
