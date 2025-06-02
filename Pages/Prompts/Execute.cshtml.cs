using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Core.Domain;
using PromptStudio.Core.Interfaces;
using System.Text.Json;

namespace PromptStudio.Pages.Prompts;

public class ExecuteModel(IPromptService promptService) : PageModel
{
    public PromptTemplate PromptTemplate { get; set; } = null!;

    [BindProperty]
    public Dictionary<string, string> VariableValues { get; set; } = new();

    public string ResolvedPrompt { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        PromptTemplate? promptTemplate = await promptService.GetPromptTemplateByIdAsync(id);

        if (promptTemplate == null)
        {
            return NotFound();
        }

        PromptTemplate = promptTemplate;

        // Initialize variable values with defaults using LINQ and ToDictionary
        if (PromptTemplate.Variables != null)
        {
            VariableValues = PromptTemplate.Variables
                .ToDictionary(variable => variable.Name, variable => variable.DefaultValue ?? "");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        PromptTemplate? promptTemplate = await promptService.GetPromptTemplateByIdAsync(id);

        if (promptTemplate == null)
        {
            return NotFound();
        }

        PromptTemplate = promptTemplate;

        if (promptService.ValidateVariables(PromptTemplate, VariableValues))
        {
            ResolvedPrompt = promptService.ResolvePrompt(PromptTemplate, VariableValues);

            // Save execution history
            var execution = new PromptExecution
            {
                PromptTemplateId = PromptTemplate.Id,
                ResolvedPrompt = ResolvedPrompt,
                VariableValues = JsonSerializer.Serialize(VariableValues),
                ExecutedAt = DateTime.UtcNow
            };

            await promptService.SavePromptExecutionsAsync([execution]); // Use collection initializer for the list
        }
        else
        {
            ModelState.AddModelError("", "Please provide values for all required variables.");
        }

        return Page();
    }
}
