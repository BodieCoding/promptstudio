namespace PromptStudio.Pages.VariableCollections;

public class VariableSetViewModel
{
    public Dictionary<string, string> Variables { get; set; } = new();
}

public class VariableCollectionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PromptTemplateId { get; set; }
    public string PromptTemplateName { get; set; } = string.Empty;
    public List<VariableSetViewModel> VariableSets { get; set; } = new();
    public List<string> VariableNames { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class BatchExecutionResult
{
    public int VariableSetIndex { get; set; }
    public Dictionary<string, string> Variables { get; set; } = new();
    public string ResolvedPrompt { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ExecutionId { get; set; }
}
