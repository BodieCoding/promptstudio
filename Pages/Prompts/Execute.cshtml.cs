using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;
using PromptStudio.Services;
using System.Text.Json;

namespace PromptStudio.Pages.Prompts;

public class ExecuteModel : PageModel
{
    private readonly PromptStudioDbContext _context;
    private readonly IPromptService _promptService;

    public ExecuteModel(PromptStudioDbContext context, IPromptService promptService)
    {
        _context = context;
        _promptService = promptService;
    }

    public PromptTemplate PromptTemplate { get; set; } = null!;

    [BindProperty]
    public Dictionary<string, string> VariableValues { get; set; } = new();

    public string ResolvedPrompt { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        PromptTemplate = await _context.PromptTemplates
            .Include(pt => pt.Variables)
            .Include(pt => pt.Collection)
            .FirstOrDefaultAsync(pt => pt.Id == id);

        if (PromptTemplate == null)
        {
            return NotFound();
        }

        // Initialize variable values with defaults
        foreach (var variable in PromptTemplate.Variables)
        {
            VariableValues[variable.Name] = variable.DefaultValue ?? "";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        PromptTemplate = await _context.PromptTemplates
            .Include(pt => pt.Variables)
            .Include(pt => pt.Collection)
            .FirstOrDefaultAsync(pt => pt.Id == id);

        if (PromptTemplate == null)
        {
            return NotFound();
        }

        if (_promptService.ValidateVariables(PromptTemplate, VariableValues))
        {
            ResolvedPrompt = _promptService.ResolvePrompt(PromptTemplate, VariableValues);

            // Save execution history
            var execution = new PromptExecution
            {
                PromptTemplateId = PromptTemplate.Id,
                ResolvedPrompt = ResolvedPrompt,
                VariableValues = JsonSerializer.Serialize(VariableValues),
                ExecutedAt = DateTime.UtcNow
            };

            _context.PromptExecutions.Add(execution);
            await _context.SaveChangesAsync();
        }
        else
        {
            ModelState.AddModelError("", "Please provide values for all required variables.");
        }

        return Page();
    }
}
