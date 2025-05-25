using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;

namespace PromptStudio.Pages.Prompts
{
    public class DeleteModel : PageModel
    {
        private readonly PromptStudioDbContext _context;

        public DeleteModel(PromptStudioDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public PromptTemplate PromptTemplate { get; set; } = default!;

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
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var promptTemplate = await _context.PromptTemplates
                .Include(p => p.Variables)
                .Include(p => p.Executions)
                .FirstOrDefaultAsync(p => p.Id == PromptTemplate.Id);

            if (promptTemplate != null)
            {
                // Remove all related data (EF Core will handle cascading delete if configured)
                _context.PromptTemplates.Remove(promptTemplate);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Index");
        }
    }
}
