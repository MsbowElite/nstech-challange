using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertStatusToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // First, add a temporary column to store the string value
            migrationBuilder.AddColumn<string>(
                name: "StatusTemp",
                table: "Orders",
                type: "text",
                nullable: true);

            // Convert existing int values to strings
            // 0 = Draft, 1 = Placed, 2 = Confirmed, 3 = Canceled
            migrationBuilder.Sql(@"
                UPDATE ""Orders""
                SET ""StatusTemp"" = 
                    CASE ""Status""
                        WHEN 0 THEN 'Draft'
                        WHEN 1 THEN 'Placed'
                        WHEN 2 THEN 'Confirmed'
                        WHEN 3 THEN 'Canceled'
                        ELSE 'Draft'
                    END
            ");

            // Drop the old Status column
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Orders");

            // Rename the temp column to Status
            migrationBuilder.RenameColumn(
                name: "StatusTemp",
                table: "Orders",
                newName: "Status");

            // Make it non-nullable
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Orders",
                type: "text",
                nullable: false,
                defaultValue: "Draft");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add temporary column for integer values
            migrationBuilder.AddColumn<int>(
                name: "StatusTemp",
                table: "Orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Convert strings back to integers
            migrationBuilder.Sql(@"
                UPDATE ""Orders""
                SET ""StatusTemp"" = 
                    CASE ""Status""
                        WHEN 'Draft' THEN 0
                        WHEN 'Placed' THEN 1
                        WHEN 'Confirmed' THEN 2
                        WHEN 'Canceled' THEN 3
                        ELSE 0
                    END
            ");

            // Drop the string column
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Orders");

            // Rename temp back to Status
            migrationBuilder.RenameColumn(
                name: "StatusTemp",
                table: "Orders",
                newName: "Status");
        }
    }
}
