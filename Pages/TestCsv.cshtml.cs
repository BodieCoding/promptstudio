using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Services;
using PromptStudio.Data;

namespace PromptStudio.Pages;

public class TestCsvModel : PageModel
{
    private readonly IPromptService _promptService;
    private readonly PromptStudioDbContext _context;

    public TestCsvModel(IPromptService promptService, PromptStudioDbContext context)
    {
        _promptService = promptService;
        _context = context;
    }

    public ParseResult? ParseResult { get; set; }

    public void OnGet()
    {
    }    public async Task<IActionResult> OnPostAsync(int promptId, string csvContent)
    {
        try
        {
            var template = await _context.PromptTemplates.FindAsync(promptId);
            if (template == null)
                return NotFound();
                
            var expectedVariables = _promptService.ExtractVariableNames(template.Content);
            var variableSets = _promptService.ParseVariableCsv(csvContent, expectedVariables);
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
    public List<Dictionary<string, string>> VariableSets { get; set; } = new();
    public string? Error { get; set; }
}
