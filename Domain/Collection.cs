using System.ComponentModel.DataAnnotations;

namespace PromptStudio.Domain;

/// <summary>
/// A Collection is like a project folder - it groups related prompts together
/// </summary>
public class Collection
{
    public int Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<PromptTemplate> PromptTemplates { get; set; } = new List<PromptTemplate>();
}
