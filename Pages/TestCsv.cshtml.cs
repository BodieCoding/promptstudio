using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Core.Interfaces;

namespace PromptStudio.Pages;

public class TestCsvModel(IPromptService promptService) : PageModel
{
    public ParseResult? ParseResult { get; set; }

    public void OnGet()
    {
    }   

    public async Task<IActionResult> OnPostAsync(int promptId, string csvContent)
    {
        try
        {
            var template = await promptService.GetPromptTemplateByIdAsync(promptId);
            if (template == null)
                return NotFound();
                
            var expectedVariables = promptService.ExtractVariableNames(template.Content);
            var variableSets = promptService.ParseVariableCsv(csvContent, expectedVariables);
            ParseResult = new ParseResult
            {
                Success = true,
                VariableSets = variableSets
            };
        }
        catch (Exception ex)
        {
            ParseResult = new ParseResult
            {
                Success = false,
                Error = ex.Message
            };
        }

        return Page();
    }
}

public class ParseResult
{
    public bool Success { get; set; }
    public List<Dictionary<string, string>> VariableSets { get; set; } = [];
    public string? Error { get; set; }
}
