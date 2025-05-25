using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;
using PromptStudio.Services;

namespace PromptStudio.Pages.Prompts;

public class CreateModel : PageModel
{
    private readonly PromptStudioDbContext _context;
    private readonly IPromptService _promptService;

    public CreateModel(PromptStudioDbContext context, IPromptService promptService)
    {
        _context = context;
        _promptService = promptService;
    }

    [BindProperty]
    public PromptTemplate PromptTemplate { get; set; } = new();

    [BindProperty]
    public int CollectionId { get; set; }

    public SelectList? Collections { get; set; }

    public async Task<IActionResult> OnGetAsync(int? collectionId)
    {
        await LoadCollections();

        if (collectionId.HasValue)
        {
            CollectionId = collectionId.Value;
            PromptTemplate.CollectionId = collectionId.Value;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCollections();
            return Page();
        }

        PromptTemplate.CollectionId = CollectionId;
        PromptTemplate.CreatedAt = DateTime.UtcNow;
        PromptTemplate.UpdatedAt = DateTime.UtcNow;

        _context.PromptTemplates.Add(PromptTemplate);
        await _context.SaveChangesAsync();

        // Extract and create variables
        var variableNames = _promptService.ExtractVariableNames(PromptTemplate.Content);
        foreach (var variableName in variableNames)
        {
            var variable = new PromptVariable
            {
                Name = variableName,
                Type = VariableType.Text,
                PromptTemplateId = PromptTemplate.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.PromptVariables.Add(variable);
        }

        await _context.SaveChangesAsync();

        return RedirectToPage("/Index");
    }

    private async Task LoadCollections()
    {
        var collections = await _context.Collections
            .OrderBy(c => c.Name)
            .ToListAsync();

        Collections = new SelectList(collections, "Id", "Name", CollectionId);
    }
}
