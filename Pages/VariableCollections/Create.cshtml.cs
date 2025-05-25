using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;
using PromptStudio.Services;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace PromptStudio.Pages.VariableCollections;

public class CreateModel : PageModel
{
    private readonly PromptStudioDbContext _context;
    private readonly IPromptService _promptService;

    public CreateModel(PromptStudioDbContext context, IPromptService promptService)
    {
        _context = context;
        _promptService = promptService;
    }

    [BindProperty]
    public int PromptId { get; set; }

    [BindProperty]
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    [StringLength(500)]
    public string? Description { get; set; }

    [BindProperty]
    [Required]
    public IFormFile CsvFile { get; set; } = default!;

    public PromptTemplate PromptTemplate { get; set; } = default!;
    public List<string> VariableNames { get; set; } = new();
    public string SampleCsv { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int promptId)
    {
        var template = await _context.PromptTemplates
            .Include(pt => pt.Variables)
            .FirstOrDefaultAsync(pt => pt.Id == promptId);

        if (template == null)
        {
            return NotFound();
        }

        PromptTemplate = template;
        PromptId = promptId;
        VariableNames = _promptService.ExtractVariableNames(template.Content);
        SampleCsv = _promptService.GenerateVariableCsvTemplate(template);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Reload template for validation
        var template = await _context.PromptTemplates
            .Include(pt => pt.Variables)
            .FirstOrDefaultAsync(pt => pt.Id == PromptId);

        if (template == null)
        {
            return NotFound();
        }

        PromptTemplate = template;
        VariableNames = _promptService.ExtractVariableNames(template.Content);
        SampleCsv = _promptService.GenerateVariableCsvTemplate(template);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            // Read CSV file
            string csvContent;
            using (var reader = new StreamReader(CsvFile.OpenReadStream(), Encoding.UTF8))
            {
                csvContent = await reader.ReadToEndAsync();
            }

            // Parse CSV
            var variableSets = _promptService.ParseVariableCsv(csvContent, VariableNames);

            if (!variableSets.Any())
            {
                ModelState.AddModelError("CsvData", "CSV file contains no data rows.");
                return Page();
            }

            // Create variable collection
            var variableCollection = new VariableCollection
            {
                Name = Name,
                Description = Description,
                PromptTemplateId = PromptId,
                VariableSets = JsonSerializer.Serialize(variableSets),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VariableCollections.Add(variableCollection);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index", new
            {
                promptId = PromptId,
                message = $"Variable collection '{Name}' created successfully with {variableSets.Count} variable sets."
            });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("CsvData", $"Error processing CSV file: {ex.Message}");
            return Page();
        }
    }
}
