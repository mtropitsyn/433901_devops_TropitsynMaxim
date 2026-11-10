using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CalculatorWeb.Migrations
{
    /// <inheritdoc />
    public partial class KafkaRequestId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RequestId",
                table: "DataInputVariants",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_DataInputVariants_RequestId",
                table: "DataInputVariants",
                column: "RequestId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DataInputVariants_RequestId",
                table: "DataInputVariants");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "DataInputVariants");
        }
    }
}
