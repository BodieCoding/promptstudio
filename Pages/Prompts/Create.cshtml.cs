using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PromptStudio.Core.Domain;
using PromptStudio.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace PromptStudio.Pages.Prompts;

public class CreateModel(IPromptService promptService, ILogger<CreateModel> logger) : PageModel
{
    [BindProperty]
    public PromptTemplate PromptTemplate { get; set; } = new();

    [BindProperty]
    [Required(ErrorMessage = "Please select a collection")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid collection")]
    public int CollectionId { get; set; }

    public SelectList? Collections { get; set; }

    public async Task<IActionResult> OnGetAsync(int? collectionId)
    {
        await LoadCollectionsAsync();

        if (collectionId.HasValue)
        {
            CollectionId = collectionId.Value;
        }

        PromptTemplate ??= new PromptTemplate();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCollectionsAsync();
            return Page();
        }

        try
        {
            var createdTemplate = await promptService.CreatePromptTemplateAsync(
                PromptTemplate.Name,
                PromptTemplate.Content,
                CollectionId,
                PromptTemplate.Description);

            if (createdTemplate == null)
            {
                ModelState.AddModelError(string.Empty, "Could not create the prompt template.");
                await LoadCollectionsAsync();
                return Page();
            }

            logger.LogInformation("Prompt template created with ID: {Id}", createdTemplate.Id);
            return RedirectToPage("./Index", new { message = $"Prompt '{createdTemplate.Name}' created successfully." });
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Argument error creating prompt template");
            ModelState.AddModelError(string.Empty, ex.Message);
            await LoadCollectionsAsync();
            return Page();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating prompt template");
            ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the prompt template.");
            await LoadCollectionsAsync();
            return Page();
        }
    }

    private async Task LoadCollectionsAsync()
    {
        var collectionsData = await promptService.GetCollectionsAsync();
        Collections = new SelectList(collectionsData ?? [], "Id", "Name", CollectionId);
    }
}
