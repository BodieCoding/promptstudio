using System.ComponentModel.DataAnnotations;

namespace PromptStudio.Domain;

/// <summary>
/// Represents an execution of a prompt template with specific variable values
/// Like a test execution record in professional testing tools
/// </summary>
public class PromptExecution
{
    public int Id { get; set; }
    
    // Foreign key
    public int PromptTemplateId { get; set; }
    
    /// <summary>
    /// The final prompt text after variable substitution
    /// </summary>
    [Required]
    public string ResolvedPrompt { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON string containing the variable values used in this execution
    /// </summary>
    public string? VariableValues { get; set; }
    
    /// <summary>
    /// The AI provider used (OpenAI, Claude, etc.)
    /// </summary>
    [StringLength(50)]
    public string? AiProvider { get; set; }
    
    /// <summary>
    /// The model used (gpt-4, claude-3, etc.)
    /// </summary>
    [StringLength(50)]
    public string? Model { get; set; }
    
    /// <summary>
    /// The response from the AI provider
    /// </summary>
    public string? Response { get; set; }
    
    /// <summary>
    /// Time taken for the AI request in milliseconds
    /// </summary>
    public int? ResponseTimeMs { get; set; }
    
    /// <summary>
    /// Tokens used in the request (if available)
    /// </summary>
    public int? TokensUsed { get; set; }
    
    /// <summary>
    /// Cost of the request (if available)
    /// </summary>
    public decimal? Cost { get; set; }
    
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual PromptTemplate PromptTemplate { get; set; } = null!;
}
