using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;

namespace PromptStudio.Pages.Collections
{
    public class DeleteModel : PageModel
    {
        private readonly PromptStudioDbContext _context;

        public DeleteModel(PromptStudioDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Collection Collection { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var collection = await _context.Collections
                .Include(c => c.PromptTemplates)
                .FirstOrDefaultAsync(m => m.Id == id);

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

            if (collection != null)
            {
                // Remove all related data (EF Core will handle cascading delete if configured)
                _context.Collections.Remove(collection);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Index");
        }
    }
}
