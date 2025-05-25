using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;
using PromptStudio.Services;
using System.Text.Json;
using System.Text;

namespace PromptStudio.Pages.VariableCollections;

public class ExecuteModel : PageModel
{
    private readonly PromptStudioDbContext _context;
    private readonly IPromptService _promptService;

    public ExecuteModel(PromptStudioDbContext context, IPromptService promptService)
    {
        _context = context;
        _promptService = promptService;
    }

    public PromptTemplate PromptTemplate { get; set; } = default!;
    public VariableCollectionViewModel VariableCollection { get; set; } = default!;
    public List<string> VariableNames { get; set; } = new();
    public List<BatchExecutionResult> Results { get; set; } = new();
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int promptId, int collectionId, string? message = null, string? error = null)
    {
        await LoadDataAsync(promptId, collectionId);
        Message = message;
        ErrorMessage = error;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int promptId, int collectionId)
    {
        await LoadDataAsync(promptId, collectionId);

        try
        {
            // Get the actual domain entity
            var variableCollectionEntity = await _context.VariableCollections
                .FirstOrDefaultAsync(vc => vc.Id == collectionId);

            if (variableCollectionEntity == null)
            {
                return NotFound("Variable collection not found.");
            }

            // Parse variable sets from JSON
            var variableSets = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(variableCollectionEntity.VariableSets)
                ?? new List<Dictionary<string, string>>();

            // Execute batch
            var batchResults = _promptService.BatchExecute(PromptTemplate, variableSets);

            // Convert to view model results
            Results = batchResults.Select((result, index) => new BatchExecutionResult
            {
                VariableSetIndex = index,
                Variables = result.Variables,
                ResolvedPrompt = result.ResolvedPrompt,
                Success = result.Success,
                ErrorMessage = result.Error
            }).ToList();

            // Store successful executions in database
            var executions = Results.Where(r => r.Success).Select(result => new PromptExecution
            {
                PromptTemplateId = promptId,
                ResolvedPrompt = result.ResolvedPrompt,
                VariableValues = JsonSerializer.Serialize(result.Variables),
                ExecutedAt = DateTime.UtcNow,
                AiProvider = "Manual Batch",
                Model = "N/A"
            }).ToList();

            if (executions.Any())
            {
                _context.PromptExecutions.AddRange(executions);
                await _context.SaveChangesAsync();

                // Update execution IDs in results
                for (int i = 0, j = 0; i < Results.Count; i++)
                {
                    if (Results[i].Success)
                    {
                        Results[i].ExecutionId = executions[j].Id;
                        j++;
                    }
                }
            }

            Message = $"Batch execution completed! {Results.Count(r => r.Success)} successful, {Results.Count(r => !r.Success)} failed.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error during batch execution: {ex.Message}";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostExportResultsAsync(int promptId, int collectionId)
    {
        await LoadDataAsync(promptId, collectionId);

        // Re-execute to get fresh results (or we could store them in session/tempdata)
        try
        {
            var variableCollectionEntity = await _context.VariableCollections
                .FirstOrDefaultAsync(vc => vc.Id == collectionId);

            if (variableCollectionEntity == null)
            {
                return NotFound("Variable collection not found.");
            }

            var variableSets = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(variableCollectionEntity.VariableSets)
                ?? new List<Dictionary<string, string>>();

            var batchResults = _promptService.BatchExecute(PromptTemplate, variableSets);

            // Generate CSV content
            var csv = new StringBuilder();

            // Header
            var headers = new List<string> { "Set_Index", "Success", "Error" };
            headers.AddRange(VariableNames);
            headers.Add("Resolved_Prompt");
            csv.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

            // Data rows
            for (int i = 0; i < batchResults.Count; i++)
            {
                var result = batchResults[i];
                var row = new List<string>
                {
                    (i + 1).ToString(),
                    result.Success.ToString(),
                    result.Error ?? ""
                };

                // Variable values
                foreach (var varName in VariableNames)
                {
                    var value = result.Variables.TryGetValue(varName, out var val) ? val : "";
                    row.Add($"\"{value.Replace("\"", "\"\"")}\""); // Escape quotes
                }

                // Resolved prompt
                row.Add($"\"{result.ResolvedPrompt.Replace("\"", "\"\"")}\"");

                csv.AppendLine(string.Join(",", row));
            }

            var fileName = $"{PromptTemplate.Name.Replace(" ", "_")}_{VariableCollection.Name.Replace(" ", "_")}_results_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(
                Encoding.UTF8.GetBytes(csv.ToString()),
                "text/csv",
                fileName
            );
        }
        catch (Exception ex)
        {
            return RedirectToPage("./Execute", new { promptId, collectionId, error = $"Export failed: {ex.Message}" });
        }
    }

    private async Task LoadDataAsync(int promptId, int collectionId)
    {
        PromptTemplate = await _context.PromptTemplates
            .Include(pt => pt.Variables)
            .FirstOrDefaultAsync(pt => pt.Id == promptId) ?? throw new InvalidOperationException("Prompt template not found.");

        var collectionEntity = await _context.VariableCollections
            .FirstOrDefaultAsync(vc => vc.Id == collectionId) ?? throw new InvalidOperationException("Variable collection not found.");

        VariableNames = _promptService.ExtractVariableNames(PromptTemplate.Content);

        // Convert to view model
        VariableCollection = new VariableCollectionViewModel
        {
            Id = collectionEntity.Id,
            Name = collectionEntity.Name,
            Description = collectionEntity.Description,
            PromptTemplateId = collectionEntity.PromptTemplateId,
            PromptTemplateName = PromptTemplate.Name,
            VariableSets = string.IsNullOrEmpty(collectionEntity.VariableSets)
                ? new List<VariableSetViewModel>()
                : JsonSerializer.Deserialize<List<Dictionary<string, string>>>(collectionEntity.VariableSets)?
                    .Select(dict => new VariableSetViewModel { Variables = dict })
                    .ToList() ?? new List<VariableSetViewModel>(),
            VariableNames = VariableNames,
            CreatedAt = collectionEntity.CreatedAt,
            UpdatedAt = collectionEntity.UpdatedAt
        };
    }
}
