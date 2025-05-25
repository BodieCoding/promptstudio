using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PromptStudio.Data;
using PromptStudio.Domain;
using PromptStudio.Services;

namespace PromptStudio.Pages.Prompts;

public class CreateModel : PageModel
{
    private readonly PromptStudioDbContext _context;
    private readonly IPromptService _promptService;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(PromptStudioDbContext context,
                       IPromptService promptService,
                       ILogger<CreateModel> logger)
    {
        _context = context;
        _promptService = promptService;
        _logger = logger;
    }

    [BindProperty]
    public PromptTemplate PromptTemplate { get; set; } = new();

    [BindProperty]
    [Required(ErrorMessage = "Please select a collection")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid collection")]
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
        PromptTemplate.CollectionId = CollectionId;
        PromptTemplate.CreatedAt = DateTime.UtcNow;
        PromptTemplate.UpdatedAt = DateTime.UtcNow;

        try
        {
            _context.PromptTemplates.Add(PromptTemplate);
            _logger.LogInformation("Adding prompt template with ID: {Id}", PromptTemplate.Id);
            _logger.LogInformation("Prompt template content: {Content}", PromptTemplate.Content);
            _logger.LogInformation("Prompt template collection ID: {CollectionId}", PromptTemplate.CollectionId);
            _logger.LogInformation("Prompt template created at: {CreatedAt}", PromptTemplate.CreatedAt);
            _logger.LogInformation("Prompt template updated at: {UpdatedAt}", PromptTemplate.UpdatedAt);
            _logger.LogInformation("Saving prompt template...");
            var result = await _context.SaveChangesAsync();
            _logger.LogInformation("SaveChangesAsync completed with result: {Result}", result);
            _logger.LogInformation("Prompt template saved with ID: {Id}", PromptTemplate.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving prompt template");

            if (ex.InnerException != null)
            {
                _logger.LogError(ex.InnerException, "Inner exception when saving prompt template");
            }

            ModelState.AddModelError(string.Empty, "An error occurred while saving the prompt template.");
            await LoadCollections();
            return Page();
        }        // Extract and create variables
        try
        {
            var variableNames = _promptService.ExtractVariableNames(PromptTemplate.Content);
            _logger.LogInformation("Extracted {Count} variables: {Variables}", variableNames.Count, string.Join(", ", variableNames));

            foreach (var variableName in variableNames)
            {
                var variable = new PromptVariable
                {
                    Name = variableName,
                    Type = VariableType.Text,
                    PromptTemplateId = PromptTemplate.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _logger.LogDebug("Adding variable: {VariableName} for prompt template {TemplateId}", variableName, PromptTemplate.Id);
                _context.PromptVariables.Add(variable);
            }

            var varSaveResult = await _context.SaveChangesAsync();
            _logger.LogInformation("Variables saved successfully, SaveChangesAsync result: {Result}", varSaveResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving variables");
            // Continue execution even if variable extraction fails
        }

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
