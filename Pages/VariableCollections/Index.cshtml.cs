using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Core.Domain;
using PromptStudio.Core.Interfaces;
using System.Text.Json;

namespace PromptStudio.Pages.VariableCollections;

public class IndexModel(IPromptService promptService) : PageModel
{
    public PromptTemplate PromptTemplate { get; set; } = default!;
    public List<VariableCollectionViewModel> VariableCollections { get; set; } = [];
    public List<string> VariableNames { get; set; } = [];
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int promptId, string? message = null, string? error = null)
    {
        var template = await promptService.GetPromptTemplateByIdAsync(promptId);
        if (template == null)
            return NotFound("No prompt template found.");

        PromptTemplate = template;
        VariableNames = promptService.ExtractVariableNames(template.Content);
        Message = message;
        ErrorMessage = error;

        // Load variable collections
        var collections = await promptService.GetVariableCollectionsAsync(promptId);
        if (collections == null)
        {
            ErrorMessage = "No variable collections found for this prompt template.";
            return Page();
        }

        VariableCollections = [.. collections.Select(vc => new VariableCollectionViewModel
        {
            Id = vc.Id,
            Name = vc.Name,
            Description = vc.Description,
            PromptTemplateId = vc.PromptTemplateId,
            PromptTemplateName = template.Name,
            VariableSets = string.IsNullOrEmpty(vc.VariableSets)
                ? []
                : JsonSerializer.Deserialize<List<Dictionary<string, string>>>(vc.VariableSets)?
                    .Select(dict => new VariableSetViewModel { Variables = dict })
                    .ToList() ?? [],
            VariableNames = VariableNames,
            CreatedAt = vc.CreatedAt,
            UpdatedAt = vc.UpdatedAt
        })];

        return Page();
    }
}
