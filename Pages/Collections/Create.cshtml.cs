using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Data;
using PromptStudio.Domain;

namespace PromptStudio.Pages.Collections;

public class CreateModel : PageModel
{
    private readonly PromptStudioDbContext _context;

    public CreateModel(PromptStudioDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Collection Collection { get; set; } = new();

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Collection.CreatedAt = DateTime.UtcNow;
        Collection.UpdatedAt = DateTime.UtcNow;

        _context.Collections.Add(Collection);
        await _context.SaveChangesAsync();

        return RedirectToPage("/Index");
    }
}
