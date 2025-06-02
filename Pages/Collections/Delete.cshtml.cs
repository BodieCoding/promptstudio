using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Core.Domain;
using PromptStudio.Core.Interfaces;

namespace PromptStudio.Pages.Collections
{
    public class DeleteModel(IPromptService promptService) : PageModel
    {
        [BindProperty]
        public Collection Collection { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var collection = await promptService.GetCollectionByIdAsync(id);

            if (collection == null)
            {
                return NotFound();
            }

            Collection = collection;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Collection == null || Collection.Id <= 0)
            {
                return Page();
            }

            var success = await promptService.DeleteCollectionAsync(Collection.Id);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Could not delete the collection. It might have already been deleted or an error occurred.");

                var currentCollection = await promptService.GetCollectionByIdAsync(Collection.Id);
                if (currentCollection != null)
                {
                    Collection = currentCollection;
                }

                return Page();
            }

            return RedirectToPage("/Index", new { message = $"Collection '{Collection.Name}' and all its contents were deleted successfully." });
        }
    }
}
