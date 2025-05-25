using PromptStudio.Domain;
using System.Text;
using System.Text.RegularExpressions;

namespace PromptStudio.Services;

/// <summary>
/// Service for processing prompt templates and substituting variables
/// </summary>
public interface IPromptService
{
    /// <summary>
    /// Resolves a prompt template by substituting variables with provided values
    /// </summary>
    /// <param name="template">The prompt template to resolve</param>
    /// <param name="variableValues">Dictionary of variable names and their values</param>
    /// <returns>The resolved prompt with variables substituted</returns>
    string ResolvePrompt(PromptTemplate template, Dictionary<string, string> variableValues);

    /// <summary>
    /// Extracts variable names from a prompt template content
    /// </summary>
    /// <param name="promptContent">The content to extract variables from</param>
    /// <returns>List of variable names found in the content</returns>
    List<string> ExtractVariableNames(string promptContent);

    /// <summary>
    /// Validates that all required variables have values
    /// </summary>
    /// <param name="template">The prompt template to validate</param>
    /// <param name="variableValues">Dictionary of variable names and their values</param>
    /// <returns>True if all required variables have values, false otherwise</returns>
    bool ValidateVariables(PromptTemplate template, Dictionary<string, string> variableValues);

    /// <summary>
    /// Generates a sample CSV template for a prompt's variables
    /// </summary>
    /// <param name="template">The prompt template to generate CSV for</param>
    /// <returns>CSV content with headers for all variables</returns>
    string GenerateVariableCsvTemplate(PromptTemplate template);

    /// <summary>
    /// Parses CSV content into variable sets
    /// </summary>
    /// <param name="csvContent">The CSV content to parse</param>
    /// <param name="expectedVariables">List of expected variable names</param>
    /// <returns>List of dictionaries containing variable values for each row</returns>
    List<Dictionary<string, string>> ParseVariableCsv(string csvContent, List<string> expectedVariables);

    /// <summary>
    /// Batch executes a prompt template against multiple variable sets
    /// </summary>
    /// <param name="template">The prompt template to execute</param>
    /// <param name="variableSets">List of variable sets to use</param>
    /// <returns>List of execution results with variables, resolved prompts, and any errors</returns>
    List<(Dictionary<string, string> Variables, string ResolvedPrompt, bool Success, string? Error)> BatchExecute(
        PromptTemplate template, List<Dictionary<string, string>> variableSets);
}

/// <summary>
/// Implementation of the prompt service for managing prompts and variables
/// </summary>
public class PromptService : IPromptService
{
    private static readonly Regex VariablePattern = new(@"\{\{([^{}]+)\}\}", RegexOptions.Compiled);

    /// <summary>
    /// Resolves a prompt template by substituting variables with provided values
    /// </summary>
    /// <param name="template">The prompt template to resolve</param>
    /// <param name="variableValues">Dictionary of variable names and their values</param>
    /// <returns>The resolved prompt with variables substituted</returns>
    public string ResolvePrompt(PromptTemplate template, Dictionary<string, string> variableValues)
    {
        var resolvedContent = template.Content;

        foreach (var variable in template.Variables)
        {
            var placeholder = $"{{{{{variable.Name}}}}}";
            var value = variableValues.GetValueOrDefault(variable.Name, variable.DefaultValue ?? "");
            resolvedContent = resolvedContent.Replace(placeholder, value);
        }

        return resolvedContent;
    }

    /// <summary>
    /// Extracts variable names from a prompt template content
    /// </summary>
    /// <param name="promptContent">The content to extract variables from</param>
    /// <returns>List of variable names found in the content</returns>
    public List<string> ExtractVariableNames(string promptContent)
    {
        var variables = new List<string>();
        var startIndex = 0;

        while (true)
        {
            var start = promptContent.IndexOf("{{", startIndex);
            if (start == -1) break;

            var end = promptContent.IndexOf("}}", start + 2);
            if (end == -1) break;

            var variableName = promptContent.Substring(start + 2, end - start - 2).Trim();
            if (!string.IsNullOrEmpty(variableName) && !variables.Contains(variableName))
            {
                variables.Add(variableName);
            }

            startIndex = end + 2;
        }

        return variables;
    }

    /// <summary>
    /// Validates that all required variables have values
    /// </summary>
    /// <param name="template">The prompt template to validate</param>
    /// <param name="variableValues">Dictionary of variable names and their values</param>
    /// <returns>True if all required variables have values, false otherwise</returns>
    public bool ValidateVariables(PromptTemplate template, Dictionary<string, string> variableValues)
    {
        foreach (var variable in template.Variables)
        {
            if (!variableValues.ContainsKey(variable.Name) && string.IsNullOrEmpty(variable.DefaultValue))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Generates a sample CSV template for a prompt's variables
    /// </summary>
    /// <param name="template">The prompt template to generate CSV for</param>
    /// <returns>CSV content with headers for all variables</returns>
    public string GenerateVariableCsvTemplate(PromptTemplate template)
    {
        var variableNames = ExtractVariableNames(template.Content);
        if (!variableNames.Any())
        {
            return "No variables found in this prompt template.";
        }

        // Create CSV header
        var csv = string.Join(",", variableNames) + "\n";

        // Add sample rows with default values or examples
        var sampleRow1 = string.Join(",", variableNames.Select(name =>
        {
            var variable = template.Variables.FirstOrDefault(v => v.Name == name);
            return $"\"{variable?.DefaultValue ?? "sample_" + name}\"";
        }));

        var sampleRow2 = string.Join(",", variableNames.Select(name =>
        {
            var variable = template.Variables.FirstOrDefault(v => v.Name == name);
            return $"\"{variable?.DefaultValue ?? "example_" + name}\"";
        }));

        csv += sampleRow1 + "\n";
        csv += sampleRow2 + "\n";

        return csv;
    }

    /// <summary>
    /// Parses CSV content into variable sets
    /// </summary>
    /// <param name="csvContent">The CSV content to parse</param>
    /// <param name="expectedVariables">List of expected variable names</param>
    /// <returns>List of dictionaries containing variable values for each row</returns>
    public List<Dictionary<string, string>> ParseVariableCsv(string csvContent, List<string> expectedVariables)
    {
        var result = new List<Dictionary<string, string>>();
        var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2) // Need at least header + 1 data row
        {
            throw new ArgumentException("CSV must contain at least a header row and one data row.");
        }

        // Parse header
        var headers = ParseCsvLine(lines[0]);

        // Validate headers contain expected variables
        var missingVariables = expectedVariables.Except(headers, StringComparer.OrdinalIgnoreCase).ToList();
        if (missingVariables.Any())
        {
            throw new ArgumentException($"CSV is missing required columns: {string.Join(", ", missingVariables)}");
        }

        // Parse data rows
        for (int i = 1; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i]);
            if (values.Length != headers.Length)
            {
                throw new ArgumentException($"Row {i + 1} has {values.Length} values but expected {headers.Length}");
            }

            var variableSet = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length; j++)
            {
                variableSet[headers[j]] = values[j];
            }
            result.Add(variableSet);
        }

        return result;
    }

    /// <summary>
    /// Batch executes a prompt template against multiple variable sets
    /// </summary>
    /// <param name="template">The prompt template to execute</param>
    /// <param name="variableSets">List of variable sets to use</param>
    /// <returns>List of execution results with variables, resolved prompts, and any errors</returns>
    public List<(Dictionary<string, string> Variables, string ResolvedPrompt, bool Success, string? Error)> BatchExecute(
        PromptTemplate template, List<Dictionary<string, string>> variableSets)
    {
        var results = new List<(Dictionary<string, string>, string, bool, string?)>();

        foreach (var variableSet in variableSets)
        {
            try
            {
                if (!ValidateVariables(template, variableSet))
                {
                    results.Add((variableSet, string.Empty, false, "Missing required variables"));
                    continue;
                }

                var resolvedPrompt = ResolvePrompt(template, variableSet);
                results.Add((variableSet, resolvedPrompt, true, null));
            }
            catch (Exception ex)
            {
                results.Add((variableSet, string.Empty, false, ex.Message));
            }
        }

        return results;
    }

    private string[] ParseCsvLine(string line)
    {
        // Simple CSV parser - handles quoted fields
        var result = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    current.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }
}
