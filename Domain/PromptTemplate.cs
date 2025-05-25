using System.ComponentModel.DataAnnotations;

namespace PromptStudio.Domain;

/// <summary>
/// A PromptTemplate is like an API request template - it's a reusable prompt with variables
/// </summary>
public class PromptTemplate
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    [Required]
    public string Content { get; set; } = string.Empty;
    
    // Foreign key
    public int CollectionId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
      // Navigation properties
    public virtual Collection Collection { get; set; } = null!;
    public virtual ICollection<PromptVariable> Variables { get; set; } = new List<PromptVariable>();
    public virtual ICollection<PromptExecution> Executions { get; set; } = new List<PromptExecution>();
    public virtual ICollection<VariableCollection> VariableCollections { get; set; } = new List<VariableCollection>();
}
