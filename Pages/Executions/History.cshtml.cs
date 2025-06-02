using Microsoft.AspNetCore.Mvc.RazorPages;
using PromptStudio.Core.Domain;
using PromptStudio.Core.Interfaces;
using System.Text.Json;

namespace PromptStudio.Pages.Executions
{
    public class HistoryModel(IPromptService promptService) : PageModel
    {
        private const int PageSize = 12;

        public List<ExecutionViewModel> Executions { get; set; } = [];
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        public async Task OnGetAsync(int page = 1)
        {
            CurrentPage = page;

            var totalExecutions = await promptService.GetTotalExecutionsCountAsync();
            TotalPages = (int)Math.Ceiling(totalExecutions / (double)PageSize);
            if (TotalPages == 0) TotalPages = 1;
            if (CurrentPage > TotalPages) CurrentPage = TotalPages;
            if (CurrentPage < 1) CurrentPage = 1;

            var executionsData = await promptService.GetExecutionHistoryAsync(CurrentPage, PageSize);

            Executions = [..executionsData.Select(e => new ExecutionViewModel
            {
                Id = e.Id,
                PromptTemplate = e.PromptTemplate,
                ResolvedPrompt = e.ResolvedPrompt,
                ExecutedAt = e.ExecutedAt,
                VariableValues = string.IsNullOrEmpty(e.VariableValues)
                    ? []
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(e.VariableValues) ?? []
            })];
        }

        public class ExecutionViewModel
        {
            public int Id { get; set; }
            public PromptTemplate PromptTemplate { get; set; } = default!;
            public string ResolvedPrompt { get; set; } = string.Empty;
            public DateTime ExecutedAt { get; set; }
            public Dictionary<string, string> VariableValues { get; set; } = [];
            public int PromptTemplateId => PromptTemplate?.Id ?? 0;
        }
    }
}
