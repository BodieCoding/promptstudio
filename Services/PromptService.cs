using PromptStudio.Domain;

namespace PromptStudio.Services;

/// <summary>
/// Service for processing prompt templates and substituting variables
/// </summary>
public interface IPromptService
{
    /// <summary>
    /// Resolves a prompt template by substituting variables with provided values
    /// </summary>
    string ResolvePrompt(PromptTemplate template, Dictionary<string, string> variableValues);
    
    /// <summary>
    /// Extracts variable names from a prompt template content
    /// </summary>
    List<string> ExtractVariableNames(string promptContent);
    
    /// <summary>
    /// Validates that all required variables have values
    /// </summary>
    bool ValidateVariables(PromptTemplate template, Dictionary<string, string> variableValues);
}

public class PromptService : IPromptService
{
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
}
