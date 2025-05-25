using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;

namespace PromptStudio.Pages;

public class IndexModel : PageModel
{
    private readonly PromptStudioDbContext _context;

    public IndexModel(PromptStudioDbContext context)
    {
        _context = context;
    }

    public IList<Collection> Collections { get; set; } = new List<Collection>();

    public async Task OnGetAsync()
    {
        Collections = await _context.Collections
            .Include(c => c.PromptTemplates)
            .ThenInclude(pt => pt.Variables)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}
