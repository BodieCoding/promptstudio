# 🚀 Prompt Studio

> **Postman for AI Prompts** - Organize, test, and manage your AI prompts with ease.

Prompt Studio is a clean, modern web application that helps you organize your AI prompts into collections, define variables for dynamic content, and test prompts with different values - just like Postman does for APIs.

## ✨ Features

- **📁 Collections**: Organize prompts into logical groups
- **⚡ Dynamic Variables**: Use `{{variable}}` syntax for reusable prompts
- **🧪 Test & Execute**: Run prompts with different variable values
- **📊 Execution History**: Track when and how prompts were used
- **🎨 Modern UI**: Clean, Postman-inspired interface
- **🔗 Variable Auto-Detection**: Automatically extracts variables from prompt content

## 🏗️ Architecture

Built with **Clean Architecture** principles:

```
PromptStudio/
├── Domain/              # Core business entities
│   ├── Collection.cs
│   ├── PromptTemplate.cs
│   ├── PromptVariable.cs
│   └── PromptExecution.cs
├── Services/            # Business logic
│   └── PromptService.cs
├── Data/                # Entity Framework
│   └── PromptStudioDbContext.cs
└── Pages/               # Razor Pages UI
    ├── Collections/
    └── Prompts/
```

## 🚀 Quick Start

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (Docker or local)

### Setup with Docker SQL Server

1. **Start SQL Server in Docker**:
   ```bash
   docker pull mcr.microsoft.com/mssql/server:2019-latest
   docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=YourPassword123!' -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2019-latest
   ```

2. **Clone and Run**:
   ```bash
   git clone <your-repo>
   cd PromptStudio
   dotnet run
   ```

3. **Open Browser**: Navigate to `http://localhost:5131`

### First Steps

1. **Create a Collection** - Like a Postman collection for related prompts
2. **Add Prompts** - Write prompts with `{{variables}}` for dynamic content
3. **Execute & Test** - Run prompts with different variable values
4. **Copy Results** - Use resolved prompts with your favorite AI provider

## 📝 Example Prompts

### Code Review
```
Please review the following {{language}} code for {{focus_area}}:

```{{language}}
{{code}}
```

Focus on:
- Code quality
- Performance  
- Security
- Best practices
```

### Content Writing
```
Write a {{tone}} {{content_type}} about {{topic}} for {{audience}}.

Requirements:
- Length: {{length}}
- Include: {{key_points}}
- Avoid: {{avoid_topics}}
```

## 🎯 Use Cases

- **Development Teams**: Standardize code review prompts
- **Content Creators**: Template-driven content generation
- **Support Teams**: Consistent customer response templates
- **Researchers**: Structured data analysis prompts
- **Educators**: Reusable teaching and assessment prompts

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core 8, Entity Framework Core
- **Frontend**: Razor Pages, Bootstrap 5, Bootstrap Icons
- **Database**: SQL Server
- **Architecture**: Clean Architecture, Repository Pattern

## 🔧 Configuration

Update `appsettings.json` with your database connection:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=PromptStudio;User Id=sa;Password=YourPassword123!;TrustServerCertificate=true;"
  }
}
```

## 📊 Database Schema

```sql
Collections (id, name, description, created_at, updated_at)
├── PromptTemplates (id, collection_id, name, description, content, created_at, updated_at)
    ├── PromptVariables (id, template_id, name, description, default_value, type, created_at)
    └── PromptExecutions (id, template_id, resolved_prompt, variable_values, executed_at, ...)
```

## 🚧 Roadmap

- [ ] **AI Provider Integration** - Direct integration with OpenAI, Claude, etc.
- [ ] **Prompt Sharing** - Import/export collections
- [ ] **Team Collaboration** - Share collections with team members
- [ ] **Advanced Variables** - File uploads, dropdowns, validation
- [ ] **Analytics** - Usage statistics and prompt performance
- [ ] **API Support** - REST API for automation

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/amazing-feature`
3. Commit changes: `git commit -m 'Add amazing feature'`
4. Push to branch: `git push origin feature/amazing-feature`
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by [Postman](https://www.postman.com/) - the amazing API testing tool
- Built with modern web development best practices
- Designed for the AI-powered development era

---

**Made with ❤️ for the AI community**

*Simplifying prompt management, one variable at a time.*
