using System.ComponentModel.DataAnnotations;

namespace PromptStudio.Domain;

/// <summary>
/// Variables that can be substituted in prompt templates (like {{variable_name}})
/// </summary>
public class PromptVariable
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(200)]
    public string? Description { get; set; }
    
    public string? DefaultValue { get; set; }
    
    [Required]
    public VariableType Type { get; set; } = VariableType.Text;
    
    // Foreign key
    public int PromptTemplateId { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual PromptTemplate PromptTemplate { get; set; } = null!;
}

public enum VariableType
{
    Text,
    Number,
    Boolean,
    File,
    LargeText
}
