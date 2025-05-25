using System.ComponentModel.DataAnnotations;

namespace PromptStudio.Domain;

public class VariableCollection
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public int PromptTemplateId { get; set; }
    public PromptTemplate PromptTemplate { get; set; } = null!;
    
    /// <summary>
    /// JSON array of variable sets, each containing key-value pairs for variables
    /// Example: [{"name": "John", "role": "Developer"}, {"name": "Jane", "role": "Designer"}]
    /// </summary>
    public string VariableSets { get; set; } = "[]";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property for executions generated from this collection
    public List<PromptExecution> Executions { get; set; } = new();
}
