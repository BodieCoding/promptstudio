using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Core.Domain;
using PromptStudio.Core.Interfaces;

namespace PromptStudio.Pages.Collections;

public class CreateModel(IPromptService promptService) : PageModel
{
    [BindProperty]
    public Collection Collection { get; set; } = new();

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }        
        
        await promptService.CreateCollectionAsync(Collection.Name, Collection.Description);

        return RedirectToPage("/Index");
    }
}
