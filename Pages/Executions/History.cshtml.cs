using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;
using System.Text.Json;

namespace PromptStudio.Pages.Executions
{
    public class HistoryModel : PageModel
    {
        private readonly PromptStudioDbContext _context;
        private const int PageSize = 12;

        public HistoryModel(PromptStudioDbContext context)
        {
            _context = context;
        }

        public List<ExecutionViewModel> Executions { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        public async Task OnGetAsync(int page = 1)
        {
            CurrentPage = page;

            var totalExecutions = await _context.PromptExecutions.CountAsync();
            TotalPages = (int)Math.Ceiling(totalExecutions / (double)PageSize);

            var executions = await _context.PromptExecutions
                .Include(e => e.PromptTemplate)
                    .ThenInclude(p => p.Collection)
                .OrderByDescending(e => e.ExecutedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            Executions = executions.Select(e => new ExecutionViewModel
            {
                Id = e.Id,
                PromptTemplate = e.PromptTemplate,
                ResolvedPrompt = e.ResolvedPrompt,
                ExecutedAt = e.ExecutedAt,
                VariableValues = string.IsNullOrEmpty(e.VariableValues) 
                    ? new Dictionary<string, string>() 
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(e.VariableValues) ?? new Dictionary<string, string>()
            }).ToList();
        }        public class ExecutionViewModel
        {
            public int Id { get; set; }
            public PromptTemplate PromptTemplate { get; set; } = null!;
            public string ResolvedPrompt { get; set; } = string.Empty;
            public DateTime ExecutedAt { get; set; }
            public Dictionary<string, string> VariableValues { get; set; } = new();
            public int PromptTemplateId => PromptTemplate.Id;
        }
    }
}
