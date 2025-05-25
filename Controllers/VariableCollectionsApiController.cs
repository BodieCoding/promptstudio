using Microsoft.AspNetCore.Mvc;
using PromptStudio.Services;
using PromptStudio.Data;
using PromptStudio.Tests;

namespace PromptStudio.Controllers;

/// <summary>
/// API controller for managing variable collections and CSV operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class VariableCollectionsApiController : ControllerBase
{
    private readonly IPromptService _promptService;
    private readonly PromptStudioDbContext _context;

    /// <summary>
    /// Initializes a new instance of the VariableCollectionsApiController
    /// </summary>
    /// <param name="promptService">Service for managing prompts and variables</param>
    /// <param name="context">Database context</param>
    public VariableCollectionsApiController(IPromptService promptService, PromptStudioDbContext context)
    {
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

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
            var template = await _context.PromptTemplates.FindAsync(promptId);
            if (template == null)
                return NotFound($"Prompt template with ID {promptId} not found");

            var csvContent = _promptService.GenerateVariableCsvTemplate(template);

            return File(
                System.Text.Encoding.UTF8.GetBytes(csvContent),
                "text/csv",
                $"{template.Name.Replace(" ", "_")}_variables.csv"
            );
        }
        catch (Exception ex)
        {
            return BadRequest($"Error generating template: {ex.Message}");
        }
    }

    /// <summary>
    /// Tests parsing of CSV content for a prompt's variables
    /// </summary>
    /// <param name="request">Request containing prompt ID and CSV content</param>
    /// <returns>Parsed variable sets</returns>
    /// <response code="200">Returns the parsed variable sets</response>
    /// <response code="404">If the prompt template is not found</response>
    /// <response code="400">If there's an error parsing the CSV</response>
    [HttpPost("test-parse")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TestCsvParsing([FromBody] TestCsvRequest request)
    {
        try
        {
            var template = await _context.PromptTemplates.FindAsync(request.PromptId);
            if (template == null)
                return NotFound($"Prompt template with ID {request.PromptId} not found");

            var expectedVariables = _promptService.ExtractVariableNames(template.Content);
            var variableSets = _promptService.ParseVariableCsv(request.CsvContent, expectedVariables);
            return Ok(new { Success = true, VariableSets = variableSets, Count = variableSets.Count });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// Runs an integration test for the variable collections workflow
    /// </summary>
    /// <returns>Test execution result</returns>
    /// <response code="200">Returns the test execution result</response>
    [HttpGet("integration-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RunIntegrationTest()
    {
        var test = new VariableCollectionsIntegrationTest(_context, _promptService);
        var result = await test.RunFullWorkflowTest();

        return Ok(new { Success = result, Message = "Check console output for detailed results" });
    }
}

/// <summary>
/// Request model for testing CSV parsing
/// </summary>
public class TestCsvRequest
{
    /// <summary>
    /// The CSV content to parse
    /// </summary>
    public string CsvContent { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the prompt template
    /// </summary>
    public int PromptId { get; set; }
}
