using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Core.Interfaces;
using System.Text;
using PromptStudio.Core.Domain;

namespace PromptStudio.Pages.VariableCollections;

public class DownloadTemplateModel(IPromptService promptService) : PageModel
{
    public async Task<IActionResult> OnGetAsync(int promptId)
    {
        var template = await promptService.GetPromptTemplateByIdAsync(promptId);

        if (template == null)
        {
            return NotFound();
        }

        var csvContent = promptService.GenerateVariableCsvTemplate(template);
        var fileName = $"{template.Name.Replace(" ", "_")}_variables_template.csv";

        var bytes = Encoding.UTF8.GetBytes(csvContent);
        return File(bytes, "text/csv", fileName);
    }
}
