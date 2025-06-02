using Microsoft.AspNetCore.Mvc;
using PromptStudio.Core.Interfaces;
using System.Text.Json;

namespace PromptStudio.Controllers;

/// <summary>
/// API controller for prompt management and execution
/// </summary>
[ApiController]
[Route("api/prompts")]
public class PromptApiController(IPromptService promptService) : ControllerBase
{
    /// <summary>
    /// Get all collections
    /// </summary>
    /// <returns>List of collections with basic information</returns>
    [HttpGet("collections")]
    public async Task<IActionResult> GetCollections()
    {
        try
        {
            var collections = await promptService.GetCollectionsAsync();

            var response = collections.Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.CreatedAt,
                c.UpdatedAt,
                PromptCount = c.PromptTemplates?.Count ?? 0
            }).OrderBy(c => c.Name).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving collections: {ex.Message}");
        }
    }

    /// <summary>
    /// Get a specific collection with its prompts
    /// </summary>
    /// <param name="id">Collection ID</param>
    /// <returns>Collection details with prompts</returns>
    [HttpGet("collections/{id}")]
    public async Task<IActionResult> GetCollection(int id)
    {
        try
        {
            var collection = await promptService.GetCollectionByIdAsync(id);

            if (collection == null)
                return NotFound($"Collection with ID {id} not found");

            var response = new
            {
                collection.Id,
                collection.Name,
                collection.Description,
                collection.CreatedAt,
                collection.UpdatedAt,
                Prompts = collection.PromptTemplates?.Select(pt => new
                {
                    pt.Id,
                    pt.Name,
                    pt.Description,
                    pt.CreatedAt,
                    pt.UpdatedAt,
                    VariableCount = pt.Variables?.Count ?? 0,
                    Variables = pt.Variables?.Select(v => new
                    {
                        v.Name,
                        Type = v.Type.ToString(),
                        v.Description,
                        v.DefaultValue
                    }).ToList() ?? []
                }).ToList() ?? []
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving collection: {ex.Message}");
        }
    }

    /// <summary>
    /// Get all prompt templates, optionally filtered by collection
    /// </summary>
    /// <param name="collectionId">Optional collection ID to filter by</param>
    /// <returns>List of prompt templates</returns>
    [HttpGet("prompts")]
    public async Task<IActionResult> GetPromptTemplates(int? collectionId = null)
    {
        try
        {
            var templates = await promptService.GetPromptTemplatesAsync(collectionId);

            var response = templates.Select(pt => new
            {
                pt.Id,
                pt.Name,
                pt.Description,
                pt.Content,
                pt.CollectionId,
                CollectionName = pt.Collection?.Name,
                pt.CreatedAt,
                pt.UpdatedAt,
                Variables = pt.Variables?.Select(v => new
                {
                    v.Name,
                    Type = v.Type.ToString(),
                    v.Description,
                    v.DefaultValue
                }).ToList() ?? [],
                VariableCount = pt.Variables?.Count ?? 0
            }).OrderBy(pt => pt.CollectionName).ThenBy(pt => pt.Name).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving prompt templates: {ex.Message}");
        }
    }

    /// <summary>
    /// Get a specific prompt template
    /// </summary>
    /// <param name="id">Prompt template ID</param>
    /// <returns>Prompt template details</returns>
    [HttpGet("prompts/{id}")]
    public async Task<IActionResult> GetPromptTemplate(int id)
    {
        try
        {
            var template = await promptService.GetPromptTemplateByIdAsync(id);

            if (template == null)
                return NotFound($"Prompt template with ID {id} not found");

            var response = new
            {
                template.Id,
                template.Name,
                template.Description,
                template.Content,
                template.CollectionId,
                CollectionName = template.Collection?.Name,
                template.CreatedAt,
                template.UpdatedAt,
                Variables = template.Variables?.Select(v => new
                {
                    v.Name,
                    Type = v.Type.ToString(),
                    v.Description,
                    v.DefaultValue
                }).ToList() ?? [],
                ExtractedVariables = promptService.ExtractVariableNames(template.Content)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving prompt template: {ex.Message}");
        }
    }

    /// <summary>
    /// Execute a prompt template with provided variables
    /// </summary>
    /// <param name="id">Prompt template ID</param>
    /// <param name="request">Execution request with variables</param>
    /// <returns>Resolved prompt and execution details</returns>
    [HttpPost("prompts/{id}/execute")]
    public async Task<IActionResult> ExecutePrompt(int id, [FromBody] ExecutePromptRequest request)
    {
        try
        {
            var variablesJson = JsonSerializer.Serialize(request.Variables ?? []);
            var result = await promptService.ExecutePromptTemplateAsync(id, variablesJson);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest($"Invalid request for executing prompt: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error executing prompt: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new prompt template
    /// </summary>
    /// <param name="request">Prompt template creation request</param>
    /// <returns>Created prompt template</returns>
    [HttpPost("prompts")]
    public async Task<IActionResult> CreatePromptTemplate([FromBody] CreatePromptTemplateRequest request)
    {
        try
        {
            var template = await promptService.CreatePromptTemplateAsync(
                request.Name,
                request.Content,
                request.CollectionId,
                request.Description);

            var response = new
            {
                template.Id,
                template.Name,
                template.Description,
                template.Content,
                template.CollectionId,
                template.CreatedAt,
                template.UpdatedAt,
                ExtractedVariables = promptService.ExtractVariableNames(template.Content),
                Variables = template.Variables?.Select(v => new
                {
                    v.Id,
                    v.Name,
                    Type = v.Type.ToString(),
                    v.Description,
                    v.DefaultValue
                }).ToList() ?? []
            };

            return CreatedAtAction(nameof(GetPromptTemplate), new { id = template.Id }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest($"Invalid request for creating prompt template: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error creating prompt template: {ex.Message}");
        }
    }

    /// <summary>
    /// Get execution history, optionally filtered by prompt template
    /// </summary>
    /// <param name="promptId">Optional prompt template ID to filter by</param>
    /// <param name="limit">Maximum number of executions to return</param>
    /// <returns>List of executions</returns>
    [HttpGet("executions")]
    public async Task<IActionResult> GetExecutionHistory(int? promptId = null, int limit = 50)
    {
        try
        {
            var executions = await promptService.GetExecutionHistoryAsync(promptId, limit);

            var response = executions.Select(pe => new
            {
                pe.Id,
                pe.ExecutedAt,
                pe.ResolvedPrompt,
                pe.AiProvider,
                pe.Model,
                pe.Cost,
                PromptTemplate = pe.PromptTemplate == null ? null : new
                {
                    pe.PromptTemplate.Id,
                    pe.PromptTemplate.Name,
                    pe.PromptTemplate.CollectionId,
                    CollectionName = pe.PromptTemplate.Collection?.Name
                },
                Variables = string.IsNullOrEmpty(pe.VariableValues)
                    ? []
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(pe.VariableValues) ?? []
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving execution history: {ex.Message}");
        }
    }

    /// <summary>
    /// Get variable collections for a prompt template
    /// </summary>
    /// <param name="promptId">Prompt template ID</param>
    /// <returns>List of variable collections</returns>
    [HttpGet("variable-collections")]
    public async Task<IActionResult> GetVariableCollections(int promptId)
    {
        try
        {
            var collections = await promptService.GetVariableCollectionsAsync(promptId);

            var response = collections.Select(vc => new
            {
                vc.Id,
                vc.Name,
                vc.Description,
                vc.PromptTemplateId,
                vc.CreatedAt,
                vc.UpdatedAt,
                VariableSetCount = string.IsNullOrEmpty(vc.VariableSets)
                    ? 0
                    : JsonSerializer.Deserialize<List<Dictionary<string, string>>>(vc.VariableSets)?.Count ?? 0
            }).OrderBy(vc => vc.Name).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving variable collections: {ex.Message}");
        }
    }

    /// <summary>
    /// Execute a prompt template with a variable collection (batch execution)
    /// </summary>
    /// <param name="collectionId">Variable collection ID</param>
    /// <param name="request">Batch execution request</param>
    /// <returns>Batch execution results</returns>
    [HttpPost("variable-collections/{collectionId}/execute")]
    public async Task<IActionResult> ExecuteBatch(int collectionId, [FromBody] BatchExecuteRequest request)
    {
        try
        {
            var result = await promptService.ExecuteBatchAsync(collectionId, request.PromptId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest($"Invalid request for batch execution: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error executing batch: {ex.Message}");
        }
    }    /// <summary>
    /// Generate a CSV template for a prompt template
    /// </summary>
    /// <param name="templateId">Prompt template ID</param>
    /// <returns>CSV file containing the variable template</returns>
    /// <response code="200">Returns the CSV template file</response>
    /// <response code="404">If the prompt template is not found</response>
    /// <response code="400">If there's an error generating the template</response>
    [HttpGet("prompt-templates/{templateId}/csv-template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateCsvTemplate(int templateId)
    {
        try
        {
            var template = await promptService.GetPromptTemplateByIdAsync(templateId);
            if (template == null)
            {
                return NotFound($"Prompt template with ID {templateId} not found");
            }

            var csvContent = promptService.GenerateVariableCsvTemplate(template);

            var cleanName = template.Name?.Replace(" ", "_") ?? "untitled";
            foreach (var invalidChar in Path.GetInvalidFileNameChars())
            {
                cleanName = cleanName.Replace(invalidChar, '_');
            }
            var fileName = $"{cleanName}_variables.csv";

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

    /// <summary>
    /// Create a variable collection from CSV data
    /// </summary>
    /// <param name="request">Variable collection creation request</param>
    /// <returns>Created variable collection</returns>
    [HttpPost("variable-collections")]
    public async Task<IActionResult> CreateVariableCollectionFromCsv([FromBody] CreateVariableCollectionRequest request)
    {
        try
        {
            var variableCollection = await promptService.CreateVariableCollectionAsync(
                request.Name,
                request.PromptId,
                request.CsvData,
                request.Description);

            var variableSets = string.IsNullOrEmpty(variableCollection.VariableSets)
                ? []
                : JsonSerializer.Deserialize<List<Dictionary<string, string>>>(variableCollection.VariableSets) ?? [];

            var response = new
            {
                variableCollection.Id,
                variableCollection.Name,
                variableCollection.Description,
                variableCollection.PromptTemplateId,
                VariableSetCount = variableSets.Count,
                variableCollection.CreatedAt,
                variableCollection.UpdatedAt
            };

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest($"CSV parsing or creation error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Create a new collection
    /// </summary>
    /// <param name="request">Collection creation request</param>
    /// <returns>Created collection details</returns>
    [HttpPost("collections")]
    public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required.");

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Collection name is required.");

            var collection = await promptService.CreateCollectionAsync(request.Name.Trim(), request.Description?.Trim());

            var response = new
            {
                collection.Id,
                collection.Name,
                collection.Description,
                collection.CreatedAt,
                collection.UpdatedAt,
                PromptCount = 0
            };

            return CreatedAtAction(nameof(GetCollection), new { id = collection.Id }, response);
        }
        catch (ArgumentException ex)
        {
            if (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                return Conflict(new { message = ex.Message });
            }
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Error creating collection: {ex.Message}");
        }
    }
}

/// <summary>
/// Request model for executing a prompt
/// </summary>
public class ExecutePromptRequest
{
    /// <summary>
    /// Variable values for the prompt
    /// </summary>
    public Dictionary<string, string> Variables { get; set; } = [];
}

/// <summary>
/// Request model for creating a prompt template
/// </summary>
public class CreatePromptTemplateRequest
{
    /// <summary>
    /// Name of the prompt template
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Prompt content with variables
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Collection ID to add the template to
    /// </summary>
    public int CollectionId { get; set; }
}

/// <summary>
/// Request model for creating a variable collection from CSV
/// </summary>
public class CreateVariableCollectionRequest
{
    /// <summary>
    /// Prompt template ID
    /// </summary>
    public int PromptId { get; set; }

    /// <summary>
    /// Name for the variable collection
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// CSV data with headers matching prompt variables
    /// </summary>
    public string CsvData { get; set; } = string.Empty;
}

/// <summary>
/// Request model for batch execution
/// </summary>
public class BatchExecuteRequest
{
    /// <summary>
    /// Prompt template ID to execute
    /// </summary>
    public int PromptId { get; set; }
}

/// <summary>
/// Request model for creating a collection
/// </summary>
public class CreateCollectionRequest
{
    /// <summary>
    /// Collection name
    /// </summary>
    public string Name { get; set; } = string.Empty;    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; set; }
}
