using Microsoft.EntityFrameworkCore;
using PromptStudio.Domain;

namespace PromptStudio.Data;

/// <summary>
/// Database context for the PromptStudio application
/// </summary>
public class PromptStudioDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the PromptStudioDbContext
    /// </summary>
    /// <param name="options">The options to be used by the context</param>
    public PromptStudioDbContext(DbContextOptions<PromptStudioDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the collections in the database
    /// </summary>
    public DbSet<Collection> Collections { get; set; }

    /// <summary>
    /// Gets or sets the prompt templates in the database
    /// </summary>
    public DbSet<PromptTemplate> PromptTemplates { get; set; }

    /// <summary>
    /// Gets or sets the prompt variables in the database
    /// </summary>
    public DbSet<PromptVariable> PromptVariables { get; set; }

    /// <summary>
    /// Gets or sets the prompt executions in the database
    /// </summary>
    public DbSet<PromptExecution> PromptExecutions { get; set; }

    /// <summary>
    /// Gets or sets the variable collections in the database
    /// </summary>
    public DbSet<VariableCollection> VariableCollections { get; set; }

    /// <summary>
    /// Configures the database model
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Collection configuration
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Name);
        });

        // PromptTemplate configuration
        modelBuilder.Entity<PromptTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired();

            entity.HasOne(e => e.Collection)
                  .WithMany(c => c.PromptTemplates)
                  .HasForeignKey(e => e.CollectionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.CollectionId, e.Name });
        });

        // PromptVariable configuration
        modelBuilder.Entity<PromptVariable>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.Property(e => e.Type).HasConversion<string>();

            entity.HasOne(e => e.PromptTemplate)
                  .WithMany(pt => pt.Variables)
                  .HasForeignKey(e => e.PromptTemplateId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.PromptTemplateId, e.Name }).IsUnique();
        });

        // PromptExecution configuration
        modelBuilder.Entity<PromptExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ResolvedPrompt).IsRequired();
            entity.Property(e => e.AiProvider).HasMaxLength(50);
            entity.Property(e => e.Model).HasMaxLength(50);
            entity.Property(e => e.Cost).HasColumnType("decimal(10,4)");

            entity.HasOne(e => e.PromptTemplate)
                  .WithMany(pt => pt.Executions)
                  .HasForeignKey(e => e.PromptTemplateId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ExecutedAt);
            entity.HasIndex(e => e.PromptTemplateId);
        });

        // VariableCollection configuration
        modelBuilder.Entity<VariableCollection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.VariableSets).IsRequired();

            entity.HasOne(e => e.PromptTemplate)
                  .WithMany(pt => pt.VariableCollections)
                  .HasForeignKey(e => e.PromptTemplateId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.PromptTemplateId);
            entity.HasIndex(e => e.Name);
        });

        // Seed initial data
        SeedData(modelBuilder);
    }

    /// <summary>
    /// Seeds the database with initial data
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model</param>
    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed a sample collection
        modelBuilder.Entity<Collection>().HasData(
            new Collection
            {
                Id = 1,
                Name = "Sample Collection",
                Description = "A sample collection to get you started",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed a sample prompt template
        modelBuilder.Entity<PromptTemplate>().HasData(
            new PromptTemplate
            {
                Id = 1,
                Name = "Code Review",
                Description = "Review code for best practices and improvements",
                Content = "Please review the following {{language}} code and provide feedback:\n\n```{{language}}\n{{code}}\n```\n\nFocus on:\n- Code quality\n- Performance\n- Security\n- Best practices",
                CollectionId = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed sample variables
        modelBuilder.Entity<PromptVariable>().HasData(
            new PromptVariable
            {
                Id = 1,
                Name = "language",
                Description = "Programming language of the code",
                DefaultValue = "javascript",
                Type = VariableType.Text,
                PromptTemplateId = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PromptVariable
            {
                Id = 2,
                Name = "code",
                Description = "The code to review",
                DefaultValue = "// Paste your code here",
                Type = VariableType.LargeText,
                PromptTemplateId = 1,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
