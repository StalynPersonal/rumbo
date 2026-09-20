using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rumbo.Infraestructura.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConfiguracionDeCorreo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracionCorreoPlataforma",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UrlBase = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Host = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Puerto = table.Column<int>(type: "int", nullable: false),
                    UsarSslDirecto = table.Column<bool>(type: "bit", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClaveCifrada = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RemitenteCorreo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RemitenteNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    FechaUltimaPrueba = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UltimaPruebaCorrecta = table.Column<bool>(type: "bit", nullable: true),
                    UltimoErrorPrueba = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionCorreoPlataforma", x => x.Id);
                    table.CheckConstraint("CK_ConfiguracionCorreoPlataforma_Puerto", "[Puerto] BETWEEN 1 AND 65535");
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesCorreoEspacio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Host = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Puerto = table.Column<int>(type: "int", nullable: false),
                    UsarSslDirecto = table.Column<bool>(type: "bit", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClaveCifrada = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RemitenteCorreo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RemitenteNombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    FechaUltimaPrueba = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UltimaPruebaCorrecta = table.Column<bool>(type: "bit", nullable: true),
                    UltimoErrorPrueba = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesCorreoEspacio", x => x.Id);
                    table.CheckConstraint("CK_ConfiguracionesCorreoEspacio_Puerto", "[Puerto] BETWEEN 1 AND 65535");
                    table.ForeignKey(
                        name: "FK_ConfiguracionesCorreoEspacio_Espacios_EspacioId",
                        column: x => x.EspacioId,
                        principalTable: "Espacios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_ConfiguracionesCorreoEspacio_Espacio",
                table: "ConfiguracionesCorreoEspacio",
                column: "EspacioId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracionCorreoPlataforma");

            migrationBuilder.DropTable(
                name: "ConfiguracionesCorreoEspacio");
        }
    }
}
