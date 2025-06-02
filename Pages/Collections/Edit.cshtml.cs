using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Core.Domain;
using PromptStudio.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace PromptStudio.Pages.Collections
{
    public class EditModel(IPromptService promptService) : PageModel
    {
        [BindProperty]
        public CollectionViewModel Collection { get; set; } = default!;

        public Collection? OriginalCollection { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var collection = await promptService.GetCollectionByIdAsync(id);
            if (collection == null) 
                return NotFound();

            OriginalCollection = collection;

            Collection = new CollectionViewModel
            {
                Id = collection.Id,
                Name = collection.Name,
                Description = collection.Description,
                PromptTemplates = collection.PromptTemplates?.ToList() ?? []
            };
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                if (Collection?.Id > 0)
                {
                    OriginalCollection = await promptService.GetCollectionByIdAsync(Collection.Id);
                }
                return Page();
            }

            try
            {
                var updatedCollection = await promptService.UpdateCollectionAsync(
                    Collection.Id,
                    Collection.Name,
                    Collection.Description);

                if (updatedCollection == null)
                {
                    ModelState.AddModelError(string.Empty, "Could not update the collection. It may have been deleted or a concurrency issue occurred.");
                    if (Collection?.Id > 0)
                    {
                        OriginalCollection = await promptService.GetCollectionByIdAsync(Collection.Id);
                    }
                    return Page();
                }
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "The collection was modified by another user. Please reload and try again.");
                if (Collection?.Id > 0)
                {
                    OriginalCollection = await promptService.GetCollectionByIdAsync(Collection.Id);
                }
                return Page();
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"An unexpected error occurred: {ex.Message}");
                if (Collection?.Id > 0)
                {
                    OriginalCollection = await promptService.GetCollectionByIdAsync(Collection.Id);
                }
                return Page();
            }

            return RedirectToPage("/Index", new { message = $"Collection '{Collection.Name}' updated successfully." });
        }
    }

    public class CollectionViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public List<PromptTemplate> PromptTemplates { get; set; } = [];
    }
}
