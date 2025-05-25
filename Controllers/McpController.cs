using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;
using PromptStudio.Services;
using System.Text.Json;

namespace PromptStudio.Controllers;

/// <summary>
/// API controller for MCP server integration
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class McpController : ControllerBase
{
    private readonly PromptStudioDbContext _context;
    private readonly IPromptService _promptService;

    /// <summary>
    /// Initializes a new instance of the McpController
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="promptService">Prompt service</param>
    public McpController(PromptStudioDbContext context, IPromptService promptService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
    }

    /// <summary>
    /// Get all collections
    /// </summary>
    /// <returns>List of collections with basic information</returns>
    [HttpGet("collections")]
    public async Task<IActionResult> GetCollections()
    {
        var collections = await _context.Collections
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.CreatedAt,
                c.UpdatedAt,
                PromptCount = c.PromptTemplates.Count()
            })
            .OrderBy(c => c.Name)
            .ToListAsync();

        return Ok(collections);
    }

    /// <summary>
    /// Get a specific collection with its prompts
    /// </summary>
    /// <param name="id">Collection ID</param>
    /// <returns>Collection details with prompts</returns>
    [HttpGet("collections/{id}")]
    public async Task<IActionResult> GetCollection(int id)
    {
        var collection = await _context.Collections
            .Include(c => c.PromptTemplates)
            .ThenInclude(pt => pt.Variables)
            .Where(c => c.Id == id)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.CreatedAt,
                c.UpdatedAt,
                Prompts = c.PromptTemplates.Select(pt => new
                {
                    pt.Id,
                    pt.Name,
                    pt.Description,
                    pt.CreatedAt,
                    pt.UpdatedAt,
                    VariableCount = pt.Variables.Count(),
                    Variables = pt.Variables.Select(v => new
                    {
                        v.Name,
                        v.Type,
                        v.Description,
                        v.DefaultValue
                    })
                })
            })
            .FirstOrDefaultAsync();

        if (collection == null)
            return NotFound($"Collection with ID {id} not found");

        return Ok(collection);
    }

    /// <summary>
    /// Get all prompt templates, optionally filtered by collection
    /// </summary>
    /// <param name="collectionId">Optional collection ID to filter by</param>
    /// <returns>List of prompt templates</returns>
    [HttpGet("prompts")]
    public async Task<IActionResult> GetPromptTemplates(int? collectionId = null)
    {
        var query = _context.PromptTemplates
            .Include(pt => pt.Collection)
            .Include(pt => pt.Variables)
            .AsQueryable();

        if (collectionId.HasValue)
        {
            query = query.Where(pt => pt.CollectionId == collectionId.Value);
        }

        var templates = await query
            .Select(pt => new
            {
                pt.Id,
                pt.Name,
                pt.Description,
                pt.Content,
                pt.CollectionId,
                CollectionName = pt.Collection.Name,
                pt.CreatedAt,
                pt.UpdatedAt,
                Variables = pt.Variables.Select(v => new
                {
                    v.Name,
                    v.Type,
                    v.Description,
                    v.DefaultValue
                }),
                VariableCount = pt.Variables.Count()
            })
            .OrderBy(pt => pt.CollectionName)
            .ThenBy(pt => pt.Name)
            .ToListAsync();

        return Ok(templates);
    }

    /// <summary>
    /// Get a specific prompt template
    /// </summary>
    /// <param name="id">Prompt template ID</param>
    /// <returns>Prompt template details</returns>
    [HttpGet("prompts/{id}")]
    public async Task<IActionResult> GetPromptTemplate(int id)
    {
        var template = await _context.PromptTemplates
            .Include(pt => pt.Collection)
            .Include(pt => pt.Variables)
            .Where(pt => pt.Id == id)
            .Select(pt => new
            {
                pt.Id,
                pt.Name,
                pt.Description,
                pt.Content,
                pt.CollectionId,
                CollectionName = pt.Collection.Name,
                pt.CreatedAt,
                pt.UpdatedAt,
                Variables = pt.Variables.Select(v => new
                {
                    v.Name,
                    v.Type,
                    v.Description,
                    v.DefaultValue
                }),
                ExtractedVariables = _promptService.ExtractVariableNames(pt.Content)
            })
            .FirstOrDefaultAsync();

        if (template == null)
            return NotFound($"Prompt template with ID {id} not found");

        return Ok(template);
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
        var template = await _context.PromptTemplates
            .Include(pt => pt.Variables)
            .FirstOrDefaultAsync(pt => pt.Id == id);

        if (template == null)
            return NotFound($"Prompt template with ID {id} not found");

        try
        {
            // Validate variables
            if (!_promptService.ValidateVariables(template, request.Variables))
            {
                return BadRequest("Missing required variables");
            }

            // Resolve the prompt
            var resolvedPrompt = _promptService.ResolvePrompt(template, request.Variables);

            // Save execution history
            var execution = new PromptExecution
            {
                PromptTemplateId = template.Id,
                ResolvedPrompt = resolvedPrompt,
                VariableValues = JsonSerializer.Serialize(request.Variables),
                ExecutedAt = DateTime.UtcNow,
                AiProvider = "MCP",
                Model = "N/A"
            };

            _context.PromptExecutions.Add(execution);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                ExecutionId = execution.Id,
                ResolvedPrompt = resolvedPrompt,
                Variables = request.Variables,
                ExecutedAt = execution.ExecutedAt,
                TemplateId = template.Id,
                TemplateName = template.Name
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error executing prompt: {ex.Message}");
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
        // Validate collection exists
        var collectionExists = await _context.Collections.AnyAsync(c => c.Id == request.CollectionId);
        if (!collectionExists)
        {
            return BadRequest($"Collection with ID {request.CollectionId} does not exist");
        }

        var template = new PromptTemplate
        {
            Name = request.Name,
            Description = request.Description,
            Content = request.Content,
            CollectionId = request.CollectionId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.PromptTemplates.Add(template);
        await _context.SaveChangesAsync();

        // Extract and create variables
        var variableNames = _promptService.ExtractVariableNames(template.Content);
        foreach (var variableName in variableNames)
        {
            var variable = new PromptVariable
            {
                Name = variableName,
                Type = VariableType.Text,
                PromptTemplateId = template.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.PromptVariables.Add(variable);
        }

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPromptTemplate), new { id = template.Id }, new
        {
            template.Id,
            template.Name,
            template.Description,
            template.Content,
            template.CollectionId,
            template.CreatedAt,
            template.UpdatedAt,
            ExtractedVariables = variableNames
        });
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
        var query = _context.PromptExecutions
            .Include(pe => pe.PromptTemplate)
            .ThenInclude(pt => pt.Collection)
            .AsQueryable();

        if (promptId.HasValue)
        {
            query = query.Where(pe => pe.PromptTemplateId == promptId.Value);
        }
        var executionsQuery = await query
            .OrderByDescending(pe => pe.ExecutedAt)
            .Take(limit)
            .Select(pe => new
            {
                pe.Id,
                pe.ExecutedAt,
                pe.ResolvedPrompt,
                pe.AiProvider,
                pe.Model,
                pe.Cost,
                pe.VariableValues,
                PromptTemplate = new
                {
                    pe.PromptTemplate.Id,
                    pe.PromptTemplate.Name,
                    pe.PromptTemplate.CollectionId,
                    CollectionName = pe.PromptTemplate.Collection.Name
                }
            })
            .ToListAsync();

        var executions = executionsQuery.Select(pe => new
        {
            pe.Id,
            pe.ExecutedAt,
            pe.ResolvedPrompt,
            pe.AiProvider,
            pe.Model,
            pe.Cost,
            pe.PromptTemplate,
            Variables = string.IsNullOrEmpty(pe.VariableValues)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(pe.VariableValues)
        }).ToList();

        return Ok(executions);
    }

    /// <summary>
    /// Get variable collections for a prompt template
    /// </summary>
    /// <param name="promptId">Prompt template ID</param>
    /// <returns>List of variable collections</returns>    [HttpGet("variable-collections")]
    public async Task<IActionResult> GetVariableCollections(int promptId)
    {
        var collectionsQuery = await _context.VariableCollections
            .Where(vc => vc.PromptTemplateId == promptId)
            .Select(vc => new
            {
                vc.Id,
                vc.Name,
                vc.Description,
                vc.PromptTemplateId,
                vc.CreatedAt,
                vc.UpdatedAt,
                vc.VariableSets
            })
            .OrderBy(vc => vc.Name)
            .ToListAsync();

        var collections = collectionsQuery.Select(vc => new
        {
            vc.Id,
            vc.Name,
            vc.Description,
            vc.PromptTemplateId,
            vc.CreatedAt,
            vc.UpdatedAt,
            VariableSetCount = string.IsNullOrEmpty(vc.VariableSets)
                ? 0
                : JsonSerializer.Deserialize<List<Dictionary<string, string>>>(vc.VariableSets)!.Count
        }).ToList();

        return Ok(collections);
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
        var variableCollection = await _context.VariableCollections
            .FirstOrDefaultAsync(vc => vc.Id == collectionId);

        if (variableCollection == null)
            return NotFound($"Variable collection with ID {collectionId} not found");

        var template = await _context.PromptTemplates
            .Include(pt => pt.Variables)
            .FirstOrDefaultAsync(pt => pt.Id == request.PromptId);

        if (template == null)
            return NotFound($"Prompt template with ID {request.PromptId} not found");

        try
        {
            // Parse variable sets
            var variableSets = string.IsNullOrEmpty(variableCollection.VariableSets)
                ? new List<Dictionary<string, string>>()
                : JsonSerializer.Deserialize<List<Dictionary<string, string>>>(variableCollection.VariableSets)!;

            // Execute batch
            var results = _promptService.BatchExecute(template, variableSets);

            // Save successful executions
            var executions = results
                .Where(r => r.Success)
                .Select(r => new PromptExecution
                {
                    PromptTemplateId = template.Id,
                    ResolvedPrompt = r.ResolvedPrompt,
                    VariableValues = JsonSerializer.Serialize(r.Variables),
                    ExecutedAt = DateTime.UtcNow,
                    AiProvider = "MCP Batch",
                    Model = "N/A"
                })
                .ToList();

            if (executions.Any())
            {
                _context.PromptExecutions.AddRange(executions);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                CollectionId = collectionId,
                CollectionName = variableCollection.Name,
                PromptId = template.Id,
                PromptName = template.Name,
                TotalSets = results.Count,
                SuccessfulExecutions = results.Count(r => r.Success),
                FailedExecutions = results.Count(r => !r.Success),
                Results = results.Select((r, index) => new
                {
                    SetIndex = index,
                    Variables = r.Variables,
                    ResolvedPrompt = r.ResolvedPrompt,
                    Success = r.Success,
                    Error = r.Error
                })
            });
        }
        catch (Exception ex)
        {
            return BadRequest($"Error executing batch: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate a CSV template for a prompt template
    /// </summary>
    /// <param name="templateId">Prompt template ID</param>
    /// <returns>CSV template content</returns>
    [HttpGet("prompt-templates/{templateId}/csv-template")]
    public async Task<IActionResult> GenerateCsvTemplate(int templateId)
    {
        var template = await _context.PromptTemplates
            .Include(pt => pt.Variables)
            .FirstOrDefaultAsync(pt => pt.Id == templateId);

        if (template == null)
        {
            return NotFound($"Prompt template with ID {templateId} not found");
        }

        var csvTemplate = _promptService.GenerateVariableCsvTemplate(template);
        return Ok(csvTemplate);
    }

    /// <summary>
    /// Create a variable collection from CSV data
    /// </summary>
    /// <param name="request">Variable collection creation request</param>
    /// <returns>Created variable collection</returns>
    [HttpPost("variable-collections")]
    public async Task<IActionResult> CreateVariableCollectionFromCsv([FromBody] CreateVariableCollectionRequest request)
    {
        // Validate prompt exists
        var template = await _context.PromptTemplates
            .Include(pt => pt.Variables)
            .FirstOrDefaultAsync(pt => pt.Id == request.PromptId);

        if (template == null)
        {
            return NotFound($"Prompt template with ID {request.PromptId} not found");
        }

        try
        {
            // Extract variable names from template
            var variableNames = _promptService.ExtractVariableNames(template.Content);

            // Parse CSV data
            var variableSets = _promptService.ParseVariableCsv(request.CsvData, variableNames);

            if (!variableSets.Any())
            {
                return BadRequest("CSV data contains no valid rows");
            }

            // Create variable collection
            var variableCollection = new VariableCollection
            {
                Name = request.Name,
                Description = request.Description,
                PromptTemplateId = request.PromptId,
                VariableSets = JsonSerializer.Serialize(variableSets),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VariableCollections.Add(variableCollection);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                variableCollection.Id,
                variableCollection.Name,
                variableCollection.Description,
                variableCollection.PromptTemplateId,
                VariableSetCount = variableSets.Count,
                variableCollection.CreatedAt,
                variableCollection.UpdatedAt
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest($"CSV parsing error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
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
    public Dictionary<string, string> Variables { get; set; } = new();
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
