using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PromptStudio.Migrations
{
    /// <inheritdoc />
    public partial class AddVariableCollection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Collections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromptTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CollectionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptTemplates_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromptVariables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PromptTemplateId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptVariables", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptVariables_PromptTemplates_PromptTemplateId",
                        column: x => x.PromptTemplateId,
                        principalTable: "PromptTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VariableCollections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PromptTemplateId = table.Column<int>(type: "int", nullable: false),
                    VariableSets = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VariableCollections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VariableCollections_PromptTemplates_PromptTemplateId",
                        column: x => x.PromptTemplateId,
                        principalTable: "PromptTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromptExecutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PromptTemplateId = table.Column<int>(type: "int", nullable: false),
                    ResolvedPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    VariableValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AiProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: true),
                    TokensUsed = table.Column<int>(type: "int", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(10,4)", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VariableCollectionId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromptExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromptExecutions_PromptTemplates_PromptTemplateId",
                        column: x => x.PromptTemplateId,
                        principalTable: "PromptTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromptExecutions_VariableCollections_VariableCollectionId",
                        column: x => x.VariableCollectionId,
                        principalTable: "VariableCollections",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "Collections",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2025, 5, 25, 16, 24, 21, 442, DateTimeKind.Utc).AddTicks(8848), "A sample collection to get you started", "Sample Collection", new DateTime(2025, 5, 25, 16, 24, 21, 442, DateTimeKind.Utc).AddTicks(9071) });

            migrationBuilder.InsertData(
                table: "PromptTemplates",
                columns: new[] { "Id", "CollectionId", "Content", "CreatedAt", "Description", "Name", "UpdatedAt" },
                values: new object[] { 1, 1, "Please review the following {{language}} code and provide feedback:\n\n```{{language}}\n{{code}}\n```\n\nFocus on:\n- Code quality\n- Performance\n- Security\n- Best practices", new DateTime(2025, 5, 25, 16, 24, 21, 443, DateTimeKind.Utc).AddTicks(5622), "Review code for best practices and improvements", "Code Review", new DateTime(2025, 5, 25, 16, 24, 21, 443, DateTimeKind.Utc).AddTicks(5809) });

            migrationBuilder.InsertData(
                table: "PromptVariables",
                columns: new[] { "Id", "CreatedAt", "DefaultValue", "Description", "Name", "PromptTemplateId", "Type" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 5, 25, 16, 24, 21, 443, DateTimeKind.Utc).AddTicks(7669), "javascript", "Programming language of the code", "language", 1, "Text" },
                    { 2, new DateTime(2025, 5, 25, 16, 24, 21, 443, DateTimeKind.Utc).AddTicks(7846), "// Paste your code here", "The code to review", "code", 1, "LargeText" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Collections_Name",
                table: "Collections",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PromptExecutions_ExecutedAt",
                table: "PromptExecutions",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PromptExecutions_PromptTemplateId",
                table: "PromptExecutions",
                column: "PromptTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptExecutions_VariableCollectionId",
                table: "PromptExecutions",
                column: "VariableCollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_PromptTemplates_CollectionId_Name",
                table: "PromptTemplates",
                columns: new[] { "CollectionId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PromptVariables_PromptTemplateId_Name",
                table: "PromptVariables",
                columns: new[] { "PromptTemplateId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VariableCollections_Name",
                table: "VariableCollections",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_VariableCollections_PromptTemplateId",
                table: "VariableCollections",
                column: "PromptTemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromptExecutions");

            migrationBuilder.DropTable(
                name: "PromptVariables");

            migrationBuilder.DropTable(
                name: "VariableCollections");

            migrationBuilder.DropTable(
                name: "PromptTemplates");

            migrationBuilder.DropTable(
                name: "Collections");
        }
    }
}
