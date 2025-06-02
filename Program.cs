using Microsoft.EntityFrameworkCore;
using PromptStudio.Core.Data;
using PromptStudio.Core.Services;
using PromptStudio.Core.Interfaces;

// Create the web application builder
var builder = WebApplication.CreateBuilder(args);

// Configure services
{
    // Add MVC, API Controllers, and Razor Pages
    builder.Services.AddControllers();
    builder.Services.AddRazorPages();    // Configure Entity Framework with SQL Server
    builder.Services.AddDbContext<PromptStudioDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Register application services
    builder.Services.AddScoped<IPromptService, PromptStudio.Core.Services.PromptService>();
}

// Build the application
var app = builder.Build();

// Ensure database exists and is up to date
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PromptStudioDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP pipeline
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days.
        app.UseHsts();
    }

    // Enable HTTPS redirection and static files
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    
    // Enable routing and authorization
    app.UseRouting();
    app.UseAuthorization();

    // Map API Controllers and Razor Pages
    app.MapControllers();
    app.MapRazorPages();
}

// Start the application
app.Run();

// Make the implicit Program class accessible to test projects
public partial class Program { }
