using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Core.Domain;
using PromptStudio.Core.Interfaces;

namespace PromptStudio.Pages.Prompts
{
    public class DeleteModel(IPromptService promptService) : PageModel
    {
        [BindProperty]
        public PromptTemplate PromptTemplate { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var promptTemplate = await promptService.GetPromptTemplateByIdAsync(id);

            if (promptTemplate == null)
            {
                return NotFound();
            }

            PromptTemplate = promptTemplate;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (PromptTemplate == null || PromptTemplate.Id <= 0)
            {
                return Page();
            }

            var success = await promptService.DeletePromptTemplateAsync(PromptTemplate.Id);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Could not delete the prompt template. It might have already been deleted or an error occurred.");
                var currentTemplate = await promptService.GetPromptTemplateByIdAsync(PromptTemplate.Id);
                if (currentTemplate != null) PromptTemplate = currentTemplate; else return NotFound();
                return Page();
            }

            return RedirectToPage("/Index", new { message = $"Prompt '{PromptTemplate.Name}' deleted successfully." });
        }
    }
}
