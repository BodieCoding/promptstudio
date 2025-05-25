using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;
using System.ComponentModel.DataAnnotations;

namespace PromptStudio.Pages.Collections
{
    public class EditModel : PageModel
    {
        private readonly PromptStudioDbContext _context;

        public EditModel(PromptStudioDbContext context)
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
            if (!ModelState.IsValid)
            {
                // Reload the prompts for display
                var existingCollection = await _context.Collections
                    .Include(c => c.PromptTemplates)
                    .FirstOrDefaultAsync(c => c.Id == Collection.Id);
                
                if (existingCollection != null)
                {
                    Collection.PromptTemplates = existingCollection.PromptTemplates;
                }
                
                return Page();
            }

            _context.Attach(Collection).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CollectionExists(Collection.Id))
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

        private bool CollectionExists(int id)
        {
            return _context.Collections.Any(e => e.Id == id);
        }
    }
}
