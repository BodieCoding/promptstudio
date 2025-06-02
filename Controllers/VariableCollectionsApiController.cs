using Microsoft.AspNetCore.Mvc;
using PromptStudio.Core.Interfaces;
using PromptStudio.Pages.Collections;

namespace PromptStudio.Controllers;

/// <summary>
/// API controller for managing variable collections and CSV operations
/// </summary>
/// <remarks>
/// Initializes a new instance of the VariableCollectionsApiController
/// </remarks>
/// <param name="promptService">Service for managing prompts and variables</param>
[ApiController]
[Route("api/[controller]")]
public class VariableCollectionsApiController(IPromptService promptService) : ControllerBase
{
    /// <summary>
    /// Generates a CSV template for a prompt's variables
    /// </summary>
    /// <param name="promptId">ID of the prompt template</param>
    /// <returns>CSV file containing the variable template</returns>
    /// <response code="200">Returns the CSV template file</response>
    /// <response code="404">If the prompt template is not found</response>
    /// <response code="400">If there's an error generating the template</response>
    [HttpGet("template/{promptId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCsvTemplate(int promptId)
    {
        try
        {
            var template = await promptService.GetPromptTemplateByIdAsync(promptId);
            if (template == null)
            {
                return NotFound($"Prompt template with ID {promptId} not found");
            }

            var csvContent = promptService.GenerateVariableCsvTemplate(template);

            var fileName = $"{template.Name?.Replace(" ", "_").ReplaceInvalidFileNameChars()}_variables.csv";

            return File(
                System.Text.Encoding.UTF8.GetBytes(csvContent),
                "text/csv",
                fileName
            );
        }
        catch (Exception ex)
        {
            return BadRequest($"Error generating CSV template: {ex.Message}");
        }
    }

}

public static class StringExtensions
{
    public static string ReplaceInvalidFileNameChars(this string filename, string replacement = "_")
    {
        if (string.IsNullOrEmpty(filename)) return "untitled"; 
        return string.Join(replacement, filename.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
    }
}
