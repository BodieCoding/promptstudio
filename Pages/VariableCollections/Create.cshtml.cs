using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Core.Domain;
using PromptStudio.Core.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace PromptStudio.Pages.VariableCollections;

public class CreateModel(IPromptService promptService) : PageModel
{
    [BindProperty]
    public int PromptId { get; set; }

    [BindProperty]
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500)]
    public string? Description { get; set; }

    [BindProperty]
    [Required]
    public IFormFile CsvFile { get; set; } = default!;

    public PromptTemplate PromptTemplate { get; set; } = default!;
    public List<string> VariableNames { get; set; } = [];
    public string SampleCsv { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int promptId)
    {
        var template = await promptService.GetPromptTemplateByIdAsync(promptId);

        if (template == null)
        {
            return NotFound();
        }

        PromptTemplate = template;
        PromptId = promptId;
        VariableNames = promptService.ExtractVariableNames(template.Content);
        SampleCsv = promptService.GenerateVariableCsvTemplate(template);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var template = await promptService.GetPromptTemplateByIdAsync(PromptId);
        if (template == null)        
            return NotFound();      

        PromptTemplate = template;
        VariableNames = promptService.ExtractVariableNames(template.Content);
        SampleCsv = promptService.GenerateVariableCsvTemplate(template);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        string csvContent;
        try
        {
            // Read CSV file
            using (var reader = new StreamReader(CsvFile.OpenReadStream(), Encoding.UTF8))
            {
                csvContent = await reader.ReadToEndAsync();
            }

            // Parse CSV locally to get the count for the message and for early validation
            // The service will also parse it, but this gives immediate feedback.
            var variableSetsForCount = promptService.ParseVariableCsv(csvContent, VariableNames);

            if (variableSetsForCount.Count == 0)
            {
                ModelState.AddModelError(nameof(CsvFile), "CSV file contains no data rows after the header.");
                return Page();
            }

            // Create variable collection using the service
            // The service method CreateVariableCollectionAsync takes the raw csvData.
            var createdCollection = await promptService.CreateVariableCollectionAsync(Name, PromptId, csvContent, Description);

            return RedirectToPage("./Index", new
            {
                promptId = PromptId,
                message = $"Variable collection '{createdCollection.Name}' created successfully with {variableSetsForCount.Count} variable sets."
            });
        }
        catch (ArgumentException ex) // Catch specific exceptions from ParseVariableCsv or CreateVariableCollectionAsync
        {
            // ArgumentException is thrown by ParseVariableCsv for header issues or row length mismatches.
            // It might also be thrown by CreateVariableCollectionAsync if promptId is invalid.
            ModelState.AddModelError(nameof(CsvFile), $"Error processing CSV file: {ex.Message}");
            return Page();
        }
        catch (Exception ex) // Catch any other unexpected errors
        {
            // Log the full exception ex for debugging
            ModelState.AddModelError(string.Empty, $"An unexpected error occurred: {ex.Message}");
            return Page();
        }
    }
}