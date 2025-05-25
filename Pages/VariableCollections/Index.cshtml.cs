using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;
using PromptStudio.Services;
using System.Text.Json;

namespace PromptStudio.Pages.VariableCollections;

public class IndexModel : PageModel
{
    private readonly PromptStudioDbContext _context;
    private readonly IPromptService _promptService;

    public IndexModel(PromptStudioDbContext context, IPromptService promptService)
    {
        _context = context;
        _promptService = promptService;
    }

    public PromptTemplate PromptTemplate { get; set; } = default!;
    public List<VariableCollectionViewModel> VariableCollections { get; set; } = new();
    public List<string> VariableNames { get; set; } = new();
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int promptId, string? message = null, string? error = null)
    {
        var template = await _context.PromptTemplates
            .Include(pt => pt.Collection)
            .Include(pt => pt.Variables)
            .FirstOrDefaultAsync(pt => pt.Id == promptId);

        if (template == null)
        {
            return NotFound();
        }

        PromptTemplate = template;
        VariableNames = _promptService.ExtractVariableNames(template.Content);
        Message = message;
        ErrorMessage = error;

        // Load variable collections
        var collections = await _context.VariableCollections
            .Where(vc => vc.PromptTemplateId == promptId)
            .OrderByDescending(vc => vc.CreatedAt)
            .ToListAsync();

        VariableCollections = collections.Select(vc => new VariableCollectionViewModel
        {
            Id = vc.Id,
            Name = vc.Name,
            Description = vc.Description,
            PromptTemplateId = vc.PromptTemplateId,
            PromptTemplateName = template.Name,
            VariableSets = string.IsNullOrEmpty(vc.VariableSets)
                ? new List<VariableSetViewModel>()
                : JsonSerializer.Deserialize<List<Dictionary<string, string>>>(vc.VariableSets)?
                    .Select(dict => new VariableSetViewModel { Variables = dict })
                    .ToList() ?? new List<VariableSetViewModel>(),
            VariableNames = VariableNames,
            CreatedAt = vc.CreatedAt,
            UpdatedAt = vc.UpdatedAt
        }).ToList();

        return Page();
    }
}
