using System.ComponentModel.DataAnnotations;

namespace PromptStudio.Models;

/// <summary>
/// Request model for creating a new collection
/// </summary>
public class CreateCollectionRequest
{
    /// <summary>
    /// Name of the collection
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the collection
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }
}
