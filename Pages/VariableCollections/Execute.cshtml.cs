using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Core.Domain;
using PromptStudio.Core.Interfaces;
using System.Text.Json;
using System.Text;

namespace PromptStudio.Pages.VariableCollections;

public class ExecuteModel(IPromptService promptService) : PageModel
{
    public PromptTemplate PromptTemplate { get; set; } = default!;
    public VariableCollectionViewModel VariableCollection { get; set; } = default!;
    public List<string> VariableNames { get; set; } = [];
    public List<BatchExecutionResult> Results { get; set; } = [];
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
            if (PromptTemplate == null || VariableCollection == null)
            {
                ErrorMessage = "Prompt Template or Variable Collection not loaded correctly.";
                return Page();
            }

            var variableSets = VariableCollection.VariableSets
                                .Select(vs => vs.Variables)
                                .ToList();

            var batchLogicResults = promptService.BatchExecute(PromptTemplate, variableSets);

            Results = [..batchLogicResults.Select((result, index) => new BatchExecutionResult
            {
                VariableSetIndex = index,
                Variables = result.Variables,
                ResolvedPrompt = result.ResolvedPrompt,
                Success = result.Success,
                ErrorMessage = result.Error
            })];

            var executionsToSave = Results
                .Where(r => r.Success)
                .Select(result => new PromptExecution
                {
                    PromptTemplateId = promptId,
                    ResolvedPrompt = result.ResolvedPrompt,
                    VariableValues = JsonSerializer.Serialize(result.Variables),
                    ExecutedAt = DateTime.UtcNow,
                    AiProvider = "Manual Batch",
                    Model = "N/A"
                }).ToList();

            if (executionsToSave.Count != 0)
            {
                var savedExecutions = await promptService.SavePromptExecutionsAsync(executionsToSave);

                int savedIndex = 0;
                for (int i = 0; i < Results.Count; i++)
                {
                    if (Results[i].Success && savedIndex < savedExecutions.Count)
                    {
                        Results[i].ExecutionId = savedExecutions[savedIndex].Id;
                        savedIndex++;
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

        if (PromptTemplate == null || VariableCollection == null)
        {
            return RedirectToPage("./Execute", new { promptId, collectionId, error = "Could not load data for export." });
        }

        try
        {
            var variableSets = VariableCollection.VariableSets
                                .Select(vs => vs.Variables)
                                .ToList();

            var batchLogicResults = promptService.BatchExecute(PromptTemplate, variableSets);

            var csv = new StringBuilder();

            List<string> headers = ["Set_Index", "Success", "Error"];
            headers.AddRange(VariableNames);
            headers.Add("Resolved_Prompt");
            csv.AppendLine(string.Join(",", headers.Select(h => $"\"{h.Replace("\"", "\"\"")}\"")));

            for (int i = 0; i < batchLogicResults.Count; i++)
            {
                var (Variables, ResolvedPrompt, Success, Error) = batchLogicResults[i];
                List<string> row =
                [
                    (i + 1).ToString(),
                    Success.ToString(),
                    $"\"{(Error ?? "").Replace("\"", "\"\"")}\""
                ];

                foreach (var varName in VariableNames)
                {
                    var value = Variables.TryGetValue(varName, out var val) ? val : "";
                    row.Add($"\"{value.Replace("\"", "\"\"")}\"");
                }

                row.Add($"\"{ResolvedPrompt.Replace("\"", "\"\"")}\"");

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
        PromptTemplate = await promptService.GetPromptTemplateByIdAsync(promptId)
           ?? throw new InvalidOperationException($"Prompt template with ID {promptId} not found.");

        var allVariableCollectionsForPrompt = await promptService.GetVariableCollectionsAsync(promptId)
            ?? throw new InvalidOperationException($"Variable collections not found for prompt ID {promptId}.");

        VariableNames = promptService.ExtractVariableNames(PromptTemplate.Content);

        var specificVariableCollection = allVariableCollectionsForPrompt
            .FirstOrDefault(vc => vc.Id == collectionId) ?? throw new InvalidOperationException($"Variable collection with ID {collectionId} not found for prompt ID {promptId}.");

        VariableCollection = new VariableCollectionViewModel
        {
            Id = specificVariableCollection.Id,
            Name = specificVariableCollection.Name,
            Description = specificVariableCollection.Description,
            PromptTemplateId = specificVariableCollection.PromptTemplateId,
            PromptTemplateName = PromptTemplate.Name,
            VariableSets = string.IsNullOrEmpty(specificVariableCollection.VariableSets)
                ? []
                : JsonSerializer.Deserialize<List<Dictionary<string, string>>>(specificVariableCollection.VariableSets)?
                    .Select(dict => new VariableSetViewModel { Variables = dict })
                    .ToList() ?? [],
            VariableNames = VariableNames,
            CreatedAt = specificVariableCollection.CreatedAt,
            UpdatedAt = specificVariableCollection.UpdatedAt
        };
    }
}
