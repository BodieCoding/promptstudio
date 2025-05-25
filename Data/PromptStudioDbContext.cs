using Microsoft.EntityFrameworkCore;
using PromptStudio.Domain;

namespace PromptStudio.Data;

public class PromptStudioDbContext : DbContext
{
    public PromptStudioDbContext(DbContextOptions<PromptStudioDbContext> options) : base(options)
    {
    }

    public DbSet<Collection> Collections { get; set; }
    public DbSet<PromptTemplate> PromptTemplates { get; set; }
    public DbSet<PromptVariable> PromptVariables { get; set; }
    public DbSet<PromptExecution> PromptExecutions { get; set; }

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

        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Seed a sample collection
        modelBuilder.Entity<Collection>().HasData(
            new Collection
            {
                Id = 1,
                Name = "Sample Collection",
                Description = "A sample collection to get you started",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
                CreatedAt = DateTime.UtcNow
            },
            new PromptVariable
            {
                Id = 2,
                Name = "code",
                Description = "The code to review",
                DefaultValue = "// Paste your code here",
                Type = VariableType.LargeText,
                PromptTemplateId = 1,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}
