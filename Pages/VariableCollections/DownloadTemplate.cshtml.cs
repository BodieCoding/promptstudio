using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Services;
using System.Text;

namespace PromptStudio.Pages.VariableCollections;

public class DownloadTemplateModel : PageModel
{
    private readonly PromptStudioDbContext _context;
    private readonly IPromptService _promptService;

    public DownloadTemplateModel(PromptStudioDbContext context, IPromptService promptService)
    {
        _context = context;
        _promptService = promptService;
    }

    public async Task<IActionResult> OnGetAsync(int promptId)
    {
        var template = await _context.PromptTemplates
            .Include(pt => pt.Variables)
            .FirstOrDefaultAsync(pt => pt.Id == promptId);

        if (template == null)
        {
            return NotFound();
        }

        var csvContent = _promptService.GenerateVariableCsvTemplate(template);
        var fileName = $"{template.Name.Replace(" ", "_")}_variables_template.csv";

        var bytes = Encoding.UTF8.GetBytes(csvContent);
        return File(bytes, "text/csv", fileName);
    }
}
