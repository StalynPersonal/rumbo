using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rumbo.Infraestructura.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTipoEspacioPropuestoAInvitacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TipoEspacioPropuesto",
                table: "Invitaciones",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoEspacioPropuesto",
                table: "Invitaciones");
        }
    }
}
