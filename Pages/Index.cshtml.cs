using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Core.Domain;
using PromptStudio.Core.Interfaces;

namespace PromptStudio.Pages;

public class IndexModel(IPromptService promptService) : PageModel
{
    private readonly IPromptService _promptService = promptService;

    public IList<Collection> Collections = [];

    public async Task OnGetAsync() => Collections = await _promptService.GetCollectionsAsync();
}
