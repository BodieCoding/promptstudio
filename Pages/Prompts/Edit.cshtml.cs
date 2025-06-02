using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PromptStudio.Core.Domain;
using PromptStudio.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace PromptStudio.Pages.Prompts
{
    public class EditModel(IPromptService promptService) : PageModel
    {
        [BindProperty]
        public PromptTemplateViewModel PromptTemplate { get; set; } = default!;

        public List<string> DetectedVariables { get; set; } = [];
        public SelectList? Collections { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var promptTemplate = await promptService.GetPromptTemplateByIdAsync(id);

            if (promptTemplate == null)
            {
                return NotFound();
            }

            PromptTemplate = new PromptTemplateViewModel
            {
                Id = promptTemplate.Id,
                Name = promptTemplate.Name,
                Description = promptTemplate.Description,
                Content = promptTemplate.Content,
                CollectionId = promptTemplate.CollectionId,
                Executions = [.. promptTemplate.Executions]
            };
            await LoadCollectionsAsync(promptTemplate.CollectionId);
            DetectedVariables = promptService.ExtractVariableNames(PromptTemplate.Content ?? "");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                DetectedVariables = promptService.ExtractVariableNames(PromptTemplate.Content ?? "");
                await LoadCollectionsAsync(PromptTemplate.CollectionId);
                return Page();
            }

            try
            {
                var updatedTemplate = await promptService.UpdatePromptTemplateAsync(
                    PromptTemplate.Id,
                    PromptTemplate.Name,
                    PromptTemplate.Content,
                    PromptTemplate.CollectionId,
                    PromptTemplate.Description);

                if (updatedTemplate == null)
                {
                    // This could happen if the template was deleted between GET and POST,
                    // or if UpdatePromptTemplateAsync returns null on some other failure.
                    ModelState.AddModelError(string.Empty, "Could not update the prompt template. It may have been deleted.");
                    await LoadCollectionsAsync(PromptTemplate.CollectionId);
                    DetectedVariables = promptService.ExtractVariableNames(PromptTemplate.Content ?? "");
                    return Page();
                }
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadCollectionsAsync(PromptTemplate.CollectionId);
                DetectedVariables = promptService.ExtractVariableNames(PromptTemplate.Content ?? "");
                return Page();
            }
            catch (Exception ex) 
            {   
                ModelState.AddModelError(string.Empty, "An error occurred while updating the prompt template. " + ex.Message);
                await LoadCollectionsAsync(PromptTemplate.CollectionId);
                DetectedVariables = promptService.ExtractVariableNames(PromptTemplate.Content ?? "");
                return Page();
            }

            return RedirectToPage("/Index", new { message = $"Prompt '{PromptTemplate.Name}' updated successfully." });
        }

        private async Task LoadCollectionsAsync(int? selectedCollectionId)
        {
            var collectionsData = await promptService.GetCollectionsAsync();
            Collections = new SelectList(collectionsData ?? [], "Id", "Name", selectedCollectionId);
        }
    }

    public class PromptTemplateViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a collection")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid collection")]
        public int CollectionId { get; set; }

        public List<PromptExecution> Executions { get; set; } = [];
    }
}
