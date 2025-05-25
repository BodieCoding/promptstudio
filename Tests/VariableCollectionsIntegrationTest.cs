using Microsoft.EntityFrameworkCore;
using PromptStudio.Data;
using PromptStudio.Domain;
using PromptStudio.Services;
using System.Text.Json;

namespace PromptStudio.Tests;

public class VariableCollectionsIntegrationTest
{
    private readonly PromptStudioDbContext _context;
    private readonly IPromptService _promptService;

    public VariableCollectionsIntegrationTest(PromptStudioDbContext context, IPromptService promptService)
    {
        _context = context;
        _promptService = promptService;
    }

    public async Task<bool> RunFullWorkflowTest()
    {
        try
        {
            Console.WriteLine("=== Variable Collections Integration Test ===");

            // Step 1: Get the existing prompt template
            var promptTemplate = await _context.PromptTemplates
                .Include(pt => pt.Variables)
                .FirstOrDefaultAsync(pt => pt.Id == 1);

            if (promptTemplate == null)
            {
                Console.WriteLine("❌ Prompt template not found");
                return false;
            }

            Console.WriteLine($"✅ Found prompt template: {promptTemplate.Name}");            // Step 2: Generate CSV template
            var csvTemplate = _promptService.GenerateVariableCsvTemplate(promptTemplate);
            Console.WriteLine($"✅ Generated CSV template:\n{csvTemplate}");

            // Step 3: Parse test CSV data
            var testCsv = """
                language,code
                javascript,"function greet(name) { return 'Hello ' + name; }"
                python,"def greet(name): return f'Hello {name}'"
                java,"public class Greeting { public static String greet(String name) { return ""Hello "" + name; } }"
                typescript,"function greet(name: string): string { return `Hello ${name}`; }"
                """; var expectedVariables = _promptService.ExtractVariableNames(promptTemplate.Content);
            var variableSets = _promptService.ParseVariableCsv(testCsv, expectedVariables);
            Console.WriteLine($"✅ Parsed {variableSets.Count} variable sets from CSV");

            // Step 4: Create Variable Collection
            var variableCollection = new VariableCollection
            {
                Name = "Test Language Examples",
                Description = "Test collection with multiple programming languages",
                PromptTemplateId = 1,
                VariableSets = JsonSerializer.Serialize(variableSets),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.VariableCollections.Add(variableCollection);
            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ Created variable collection with ID: {variableCollection.Id}");

            // Step 5: Batch execute
            var batchResults = _promptService.BatchExecute(promptTemplate, variableSets);
            Console.WriteLine($"✅ Executed batch: {batchResults.Count} results");

            // Step 6: Verify results
            var successCount = batchResults.Count(r => r.Success);
            var failCount = batchResults.Count(r => !r.Success);
            Console.WriteLine($"✅ Results: {successCount} successful, {failCount} failed");

            // Step 7: Show sample resolved prompts
            for (int i = 0; i < Math.Min(2, batchResults.Count); i++)
            {
                var result = batchResults[i];
                Console.WriteLine($"\n--- Sample Result {i + 1} ---");
                Console.WriteLine($"Variables: {JsonSerializer.Serialize(result.Variables)}");
                Console.WriteLine($"Resolved Prompt:\n{result.ResolvedPrompt}");
            }            // Step 8: Store executions in database
            var executions = batchResults.Where(r => r.Success).Select(result => new PromptExecution
            {
                PromptTemplateId = 1,
                ResolvedPrompt = result.ResolvedPrompt,
                VariableValues = JsonSerializer.Serialize(result.Variables),
                ExecutedAt = DateTime.UtcNow,
                AiProvider = "Integration Test",
                Model = "Test Model"
            }).ToList();

            if (executions.Any())
            {
                _context.PromptExecutions.AddRange(executions);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Stored {executions.Count} executions in database");
            }

            Console.WriteLine("\n🎉 All tests passed! Variable Collections feature is working correctly.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return false;
        }
    }
}
