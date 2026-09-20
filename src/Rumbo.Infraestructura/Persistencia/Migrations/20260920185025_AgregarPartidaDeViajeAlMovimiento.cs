using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rumbo.Infraestructura.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPartidaDeViajeAlMovimiento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoriaViaje",
                table: "Movimientos",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CategoriaViaje",
                table: "Movimientos");
        }
    }
}
