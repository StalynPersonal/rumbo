using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Rumbo.Infraestructura.Persistencia.Migrations
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Espacios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MonedaBase = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    ZonaHoraria = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Espacios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Monedas",
                columns: table => new
                {
                    Codigo = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Simbolo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Decimales = table.Column<int>(type: "int", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Monedas", x => x.Codigo);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Cuerpo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UsuarioDestinoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Datos = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: true),
                    ProgramadaPara = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaEnvio = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FechaLectura = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Presupuestos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TipoPeriodo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InicioPeriodo = table.Column<DateOnly>(type: "date", nullable: false),
                    FinPeriodo = table.Column<DateOnly>(type: "date", nullable: false),
                    Moneda = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Presupuestos", x => x.Id);
                    table.CheckConstraint("CK_Presupuestos_Periodo", "[FinPeriodo] >= [InicioPeriodo]");
                });

            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CorreoUsuario = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TipoEntidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntidadId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Cambios = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: true),
                    FechaHora = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DireccionIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    IdCorrelacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Exitosa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CulturaPreferida = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UltimoAcceso = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Categorias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CategoriaPadreId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Icono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: true),
                    EsDelSistema = table.Column<bool>(type: "bit", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Categorias_Categorias_CategoriaPadreId",
                        column: x => x.CategoriaPadreId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Categorias_Espacios_EspacioId",
                        column: x => x.EspacioId,
                        principalTable: "Espacios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesEspacio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiaInicioMes = table.Column<int>(type: "int", nullable: false),
                    UmbralAvisoPresupuesto = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    UmbralCriticoPresupuesto = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    UmbralExcedidoPresupuesto = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MesesHistorialParaAnalisis = table.Column<int>(type: "int", nullable: false),
                    RecomendacionesActivas = table.Column<bool>(type: "bit", nullable: false),
                    DiasAvisoPagoRecurrente = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesEspacio", x => x.Id);
                    table.CheckConstraint("CK_ConfiguracionesEspacio_DiaInicioMes", "[DiaInicioMes] BETWEEN 1 AND 28");
                    table.CheckConstraint("CK_ConfiguracionesEspacio_Umbrales", "[UmbralAvisoPresupuesto] > 0 AND [UmbralAvisoPresupuesto] <= [UmbralCriticoPresupuesto] AND [UmbralCriticoPresupuesto] <= [UmbralExcedidoPresupuesto]");
                    table.ForeignKey(
                        name: "FK_ConfiguracionesEspacio_Espacios_EspacioId",
                        column: x => x.EspacioId,
                        principalTable: "Espacios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TasasCambio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonedaOrigen = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    MonedaDestino = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Tasa = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    Origen = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TasasCambio", x => x.Id);
                    table.CheckConstraint("CK_TasasCambio_TasaPositiva", "[Tasa] > 0");
                    table.ForeignKey(
                        name: "FK_TasasCambio_Monedas_MonedaDestino",
                        column: x => x.MonedaDestino,
                        principalTable: "Monedas",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TasasCambio_Monedas_MonedaOrigen",
                        column: x => x.MonedaOrigen,
                        principalTable: "Monedas",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolesReclamaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolesReclamaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolesReclamaciones_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cuentas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Moneda = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    SaldoInicial = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    SaldoActual = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    PropietarioUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EsCompartida = table.Column<bool>(type: "bit", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    Institucion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UltimosDigitos = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LimiteCredito = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    DiaCorte = table.Column<int>(type: "int", nullable: true),
                    DiaPago = table.Column<int>(type: "int", nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuentas", x => x.Id);
                    table.CheckConstraint("CK_Cuentas_DiaCorte", "[DiaCorte] IS NULL OR [DiaCorte] BETWEEN 1 AND 28");
                    table.CheckConstraint("CK_Cuentas_DiaPago", "[DiaPago] IS NULL OR [DiaPago] BETWEEN 1 AND 28");
                    table.CheckConstraint("CK_Cuentas_LimiteCredito", "[LimiteCredito] IS NULL OR [LimiteCredito] >= 0");
                    table.ForeignKey(
                        name: "FK_Cuentas_Espacios_EspacioId",
                        column: x => x.EspacioId,
                        principalTable: "Espacios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cuentas_Monedas_Moneda",
                        column: x => x.Moneda,
                        principalTable: "Monedas",
                        principalColumn: "Codigo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cuentas_Usuarios_PropietarioUsuarioId",
                        column: x => x.PropietarioUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Invitaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Correo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    HashCodigo = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NombreEspacioPropuesto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RolAsignado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EmitidaPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaExpiracion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaUso = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AceptadaPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invitaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invitaciones_Espacios_EspacioId",
                        column: x => x.EspacioId,
                        principalTable: "Espacios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invitaciones_Usuarios_EmitidaPorUsuarioId",
                        column: x => x.EmitidaPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MembresiasEspacio",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaIngreso = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MembresiasEspacio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MembresiasEspacio_Espacios_EspacioId",
                        column: x => x.EspacioId,
                        principalTable: "Espacios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MembresiasEspacio_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TokensRenovacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Hash = table.Column<string>(type: "nchar(64)", fixedLength: true, maxLength: 64, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaExpiracion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaRevocacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReemplazadoPorTokenId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DireccionIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Dispositivo = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokensRenovacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokensRenovacion_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosInicioSesion",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosInicioSesion", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UsuariosInicioSesion_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosReclamaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosReclamaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuariosReclamaciones_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UsuariosRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuariosRoles_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UsuariosTokens_Usuarios_UserId",
                        column: x => x.UserId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LineasPresupuesto",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PresupuestoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MontoAsignado = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    UmbralAviso = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    UmbralCritico = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    UmbralExcedido = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasPresupuesto", x => x.Id);
                    table.CheckConstraint("CK_LineasPresupuesto_MontoNoNegativo", "[MontoAsignado] >= 0");
                    table.ForeignKey(
                        name: "FK_LineasPresupuesto_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LineasPresupuesto_Presupuestos_PresupuestoId",
                        column: x => x.PresupuestoId,
                        principalTable: "Presupuestos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Deudas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Acreedor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MontoOriginal = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    SaldoActual = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Moneda = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    TasaInteres = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: true),
                    PagoMinimo = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    PagoMensual = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    DiaVencimiento = table.Column<int>(type: "int", nullable: true),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaEstimadaLiquidacion = table.Column<DateOnly>(type: "date", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CuentaVinculadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsableUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deudas", x => x.Id);
                    table.CheckConstraint("CK_Deudas_MontoOriginalPositivo", "[MontoOriginal] > 0");
                    table.CheckConstraint("CK_Deudas_SaldoNoNegativo", "[SaldoActual] >= 0");
                    table.CheckConstraint("CK_Deudas_Vencimiento", "[DiaVencimiento] IS NULL OR [DiaVencimiento] BETWEEN 1 AND 28");
                    table.ForeignKey(
                        name: "FK_Deudas_Cuentas_CuentaVinculadaId",
                        column: x => x.CuentaVinculadaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GastosRecurrentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MontoEstimado = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Moneda = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    EsMontoFijo = table.Column<bool>(type: "bit", nullable: false),
                    Frecuencia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProximaFechaPago = table.Column<DateOnly>(type: "date", nullable: false),
                    UltimaFechaPago = table.Column<DateOnly>(type: "date", nullable: true),
                    DiasAvisoPrevio = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reparto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GastosRecurrentes", x => x.Id);
                    table.CheckConstraint("CK_GastosRecurrentes_MontoNoNegativo", "[MontoEstimado] >= 0");
                    table.ForeignKey(
                        name: "FK_GastosRecurrentes_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GastosRecurrentes_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IngresosRecurrentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MontoEstimado = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Moneda = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    EsMontoFijo = table.Column<bool>(type: "bit", nullable: false),
                    Frecuencia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProximaFechaCobro = table.Column<DateOnly>(type: "date", nullable: false),
                    RecibidoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IngresosRecurrentes", x => x.Id);
                    table.CheckConstraint("CK_IngresosRecurrentes_MontoNoNegativo", "[MontoEstimado] >= 0");
                    table.ForeignKey(
                        name: "FK_IngresosRecurrentes_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IngresosRecurrentes_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Metas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MontoObjetivo = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    MontoActual = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Moneda = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    FechaObjetivo = table.Column<DateOnly>(type: "date", nullable: true),
                    Prioridad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AporteMensualMinimo = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CuentaVinculadaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Icono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaAlcanzada = table.Column<DateOnly>(type: "date", nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metas", x => x.Id);
                    table.CheckConstraint("CK_Metas_ActualNoNegativo", "[MontoActual] >= 0");
                    table.CheckConstraint("CK_Metas_ObjetivoPositivo", "[MontoObjetivo] > 0");
                    table.ForeignKey(
                        name: "FK_Metas_Cuentas_CuentaVinculadaId",
                        column: x => x.CuentaVinculadaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transferencias",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovimientoOrigenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovimientoDestinoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaOrigenId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CuentaDestinoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MontoOrigen = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    MontoDestino = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TasaCambioAplicada = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Comision = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transferencias", x => x.Id);
                    table.CheckConstraint("CK_Transferencias_ComisionNoNegativa", "[Comision] >= 0");
                    table.CheckConstraint("CK_Transferencias_CuentasDistintas", "[CuentaOrigenId] <> [CuentaDestinoId]");
                    table.CheckConstraint("CK_Transferencias_MontosPositivos", "[MontoOrigen] > 0 AND [MontoDestino] > 0");
                    table.ForeignKey(
                        name: "FK_Transferencias_Cuentas_CuentaDestinoId",
                        column: x => x.CuentaDestinoId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transferencias_Cuentas_CuentaOrigenId",
                        column: x => x.CuentaOrigenId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Viajes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Destino = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: false),
                    PresupuestoTotal = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Moneda = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    NumeroViajeros = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Viajes", x => x.Id);
                    table.CheckConstraint("CK_Viajes_Fechas", "[FechaFin] >= [FechaInicio]");
                    table.CheckConstraint("CK_Viajes_PresupuestoNoNegativo", "[PresupuestoTotal] >= 0");
                    table.CheckConstraint("CK_Viajes_Viajeros", "[NumeroViajeros] >= 1");
                    table.ForeignKey(
                        name: "FK_Viajes_Metas_MetaId",
                        column: x => x.MetaId,
                        principalTable: "Metas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LineasPresupuestoViaje",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ViajeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MontoPlanificado = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasPresupuestoViaje", x => x.Id);
                    table.CheckConstraint("CK_LineasPresupuestoViaje_MontoNoNegativo", "[MontoPlanificado] >= 0");
                    table.ForeignKey(
                        name: "FK_LineasPresupuestoViaje_Viajes_ViajeId",
                        column: x => x.ViajeId,
                        principalTable: "Viajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Movimientos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CuentaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Signo = table.Column<int>(type: "int", nullable: false),
                    Moneda = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    MontoEnMonedaBase = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    TasaCambioAplicada = table.Column<decimal>(type: "decimal(19,8)", precision: 19, scale: 8, nullable: false),
                    TasaEsAproximada = table.Column<bool>(type: "bit", nullable: false),
                    FechaMovimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MetodoPago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Reparto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PagadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ViajeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransferenciaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    GastoRecurrenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeudaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimientos", x => x.Id);
                    table.CheckConstraint("CK_Movimientos_MontoPositivo", "[Monto] > 0");
                    table.CheckConstraint("CK_Movimientos_Signo", "[Signo] IN (-1, 1)");
                    table.CheckConstraint("CK_Movimientos_TasaPositiva", "[TasaCambioAplicada] > 0");
                    table.ForeignKey(
                        name: "FK_Movimientos_Categorias_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categorias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimientos_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimientos_Espacios_EspacioId",
                        column: x => x.EspacioId,
                        principalTable: "Espacios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimientos_Metas_MetaId",
                        column: x => x.MetaId,
                        principalTable: "Metas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimientos_Transferencias_TransferenciaId",
                        column: x => x.TransferenciaId,
                        principalTable: "Transferencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimientos_Usuarios_PagadoPorUsuarioId",
                        column: x => x.PagadoPorUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimientos_Viajes_ViajeId",
                        column: x => x.ViajeId,
                        principalTable: "Viajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Recomendaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Cuerpo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Insumos = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: true),
                    MontoSugerido = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: true),
                    Moneda = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: true),
                    ConfianzaBaja = table.Column<bool>(type: "bit", nullable: false),
                    MetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ViajeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaGeneracion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FechaExpiracion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaRespuesta = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RespondidaPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recomendaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Recomendaciones_Metas_MetaId",
                        column: x => x.MetaId,
                        principalTable: "Metas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Recomendaciones_Viajes_ViajeId",
                        column: x => x.ViajeId,
                        principalTable: "Viajes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AportesMeta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MetaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovimientoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Monto = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Moneda = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    AportadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrigenRecomendacion = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AportesMeta", x => x.Id);
                    table.CheckConstraint("CK_AportesMeta_MontoPositivo", "[Monto] > 0");
                    table.ForeignKey(
                        name: "FK_AportesMeta_Metas_MetaId",
                        column: x => x.MetaId,
                        principalTable: "Metas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AportesMeta_Movimientos_MovimientoId",
                        column: x => x.MovimientoId,
                        principalTable: "Movimientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PagosDeuda",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeudaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MovimientoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MontoTotal = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    MontoCapital = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    MontoInteres = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    MontoCargos = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Moneda = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    SaldoPosterior = table.Column<decimal>(type: "decimal(19,4)", precision: 19, scale: 4, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaActualizacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ActualizadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EspacioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Eliminado = table.Column<bool>(type: "bit", nullable: false),
                    FechaEliminacion = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EliminadoPorUsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosDeuda", x => x.Id);
                    table.CheckConstraint("CK_PagosDeuda_ComponentesNoNegativos", "[MontoCapital] >= 0 AND [MontoInteres] >= 0 AND [MontoCargos] >= 0");
                    table.CheckConstraint("CK_PagosDeuda_DesgloseCuadra", "[MontoCapital] + [MontoInteres] + [MontoCargos] = [MontoTotal]");
                    table.CheckConstraint("CK_PagosDeuda_TotalPositivo", "[MontoTotal] > 0");
                    table.ForeignKey(
                        name: "FK_PagosDeuda_Deudas_DeudaId",
                        column: x => x.DeudaId,
                        principalTable: "Deudas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PagosDeuda_Movimientos_MovimientoId",
                        column: x => x.MovimientoId,
                        principalTable: "Movimientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Monedas",
                columns: new[] { "Codigo", "Activa", "Decimales", "Nombre", "Simbolo" },
                values: new object[,]
                {
                    { "DOP", true, 2, "Peso dominicano", "RD$" },
                    { "EUR", true, 2, "Euro", "EUR" },
                    { "GBP", true, 2, "Libra esterlina", "GBP" },
                    { "USD", true, 2, "Dolar estadounidense", "US$" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AportesMeta_Meta_Fecha",
                table: "AportesMeta",
                columns: new[] { "MetaId", "Fecha" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AportesMeta_MovimientoId",
                table: "AportesMeta",
                column: "MovimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_Categorias_CategoriaPadreId",
                table: "Categorias",
                column: "CategoriaPadreId");

            migrationBuilder.CreateIndex(
                name: "UX_Categorias_Espacio_Padre_Nombre",
                table: "Categorias",
                columns: new[] { "EspacioId", "CategoriaPadreId", "Nombre" },
                unique: true,
                filter: "[CategoriaPadreId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_ConfiguracionesEspacio_Espacio",
                table: "ConfiguracionesEspacio",
                column: "EspacioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_Espacio_Activa",
                table: "Cuentas",
                columns: new[] { "EspacioId", "Activa" });

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_Moneda",
                table: "Cuentas",
                column: "Moneda");

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_PropietarioUsuarioId",
                table: "Cuentas",
                column: "PropietarioUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Deudas_CuentaVinculadaId",
                table: "Deudas",
                column: "CuentaVinculadaId");

            migrationBuilder.CreateIndex(
                name: "IX_Deudas_Espacio_Estado",
                table: "Deudas",
                columns: new[] { "EspacioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_GastosRecurrentes_CategoriaId",
                table: "GastosRecurrentes",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_GastosRecurrentes_CuentaId",
                table: "GastosRecurrentes",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_GastosRecurrentes_Espacio_Estado_Proxima",
                table: "GastosRecurrentes",
                columns: new[] { "EspacioId", "Estado", "ProximaFechaPago" });

            migrationBuilder.CreateIndex(
                name: "IX_IngresosRecurrentes_CategoriaId",
                table: "IngresosRecurrentes",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_IngresosRecurrentes_CuentaId",
                table: "IngresosRecurrentes",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_IngresosRecurrentes_Espacio_Estado_Proxima",
                table: "IngresosRecurrentes",
                columns: new[] { "EspacioId", "Estado", "ProximaFechaCobro" });

            migrationBuilder.CreateIndex(
                name: "IX_Invitaciones_Correo_Estado",
                table: "Invitaciones",
                columns: new[] { "Correo", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Invitaciones_EmitidaPorUsuarioId",
                table: "Invitaciones",
                column: "EmitidaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitaciones_EspacioId",
                table: "Invitaciones",
                column: "EspacioId");

            migrationBuilder.CreateIndex(
                name: "UX_Invitaciones_HashCodigo",
                table: "Invitaciones",
                column: "HashCodigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LineasPresupuesto_CategoriaId",
                table: "LineasPresupuesto",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "UX_LineasPresupuesto_Presupuesto_Categoria",
                table: "LineasPresupuesto",
                columns: new[] { "PresupuestoId", "CategoriaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_LineasPresupuestoViaje_Viaje_Categoria",
                table: "LineasPresupuestoViaje",
                columns: new[] { "ViajeId", "Categoria" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MembresiasEspacio_Espacio_Estado",
                table: "MembresiasEspacio",
                columns: new[] { "EspacioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "UX_MembresiasEspacio_Usuario_Espacio",
                table: "MembresiasEspacio",
                columns: new[] { "UsuarioId", "EspacioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Metas_CuentaVinculadaId",
                table: "Metas",
                column: "CuentaVinculadaId");

            migrationBuilder.CreateIndex(
                name: "IX_Metas_Espacio_Estado_Prioridad",
                table: "Metas",
                columns: new[] { "EspacioId", "Estado", "Prioridad" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_CategoriaId",
                table: "Movimientos",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_CuentaId",
                table: "Movimientos",
                column: "CuentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_Espacio_Categoria_Fecha",
                table: "Movimientos",
                columns: new[] { "EspacioId", "CategoriaId", "FechaMovimiento" },
                filter: "[Eliminado] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_Espacio_Cuenta_Fecha",
                table: "Movimientos",
                columns: new[] { "EspacioId", "CuentaId", "FechaMovimiento" },
                filter: "[Eliminado] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_Espacio_Fecha",
                table: "Movimientos",
                columns: new[] { "EspacioId", "FechaMovimiento" },
                descending: new[] { false, true },
                filter: "[Eliminado] = 0")
                .Annotation("SqlServer:Include", new[] { "Monto", "MontoEnMonedaBase", "Tipo", "CuentaId", "CategoriaId" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_Espacio_Meta",
                table: "Movimientos",
                columns: new[] { "EspacioId", "MetaId" },
                filter: "[MetaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_Espacio_Viaje",
                table: "Movimientos",
                columns: new[] { "EspacioId", "ViajeId" },
                filter: "[ViajeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_MetaId",
                table: "Movimientos",
                column: "MetaId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_PagadoPorUsuarioId",
                table: "Movimientos",
                column: "PagadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_Transferencia",
                table: "Movimientos",
                column: "TransferenciaId",
                filter: "[TransferenciaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_ViajeId",
                table: "Movimientos",
                column: "ViajeId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_Espacio_Estado_Programada",
                table: "Notificaciones",
                columns: new[] { "EspacioId", "Estado", "ProgramadaPara" });

            migrationBuilder.CreateIndex(
                name: "IX_PagosDeuda_Deuda_Fecha",
                table: "PagosDeuda",
                columns: new[] { "DeudaId", "Fecha" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_PagosDeuda_MovimientoId",
                table: "PagosDeuda",
                column: "MovimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_Presupuestos_Espacio_Estado_Periodo",
                table: "Presupuestos",
                columns: new[] { "EspacioId", "Estado", "InicioPeriodo", "FinPeriodo" });

            migrationBuilder.CreateIndex(
                name: "IX_Recomendaciones_Espacio_Estado_Fecha",
                table: "Recomendaciones",
                columns: new[] { "EspacioId", "Estado", "FechaGeneracion" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Recomendaciones_MetaId",
                table: "Recomendaciones",
                column: "MetaId");

            migrationBuilder.CreateIndex(
                name: "IX_Recomendaciones_ViajeId",
                table: "Recomendaciones",
                column: "ViajeId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Entidad",
                table: "RegistrosAuditoria",
                columns: new[] { "TipoEntidad", "EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Espacio_Fecha",
                table: "RegistrosAuditoria",
                columns: new[] { "EspacioId", "FechaHora" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Usuario_Fecha",
                table: "RegistrosAuditoria",
                columns: new[] { "UsuarioId", "FechaHora" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RolesReclamaciones_RoleId",
                table: "RolesReclamaciones",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_TasasCambio_Busqueda",
                table: "TasasCambio",
                columns: new[] { "MonedaOrigen", "MonedaDestino", "Fecha" },
                unique: true,
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_TasasCambio_MonedaDestino",
                table: "TasasCambio",
                column: "MonedaDestino");

            migrationBuilder.CreateIndex(
                name: "IX_TokensRenovacion_Usuario_Revocacion",
                table: "TokensRenovacion",
                columns: new[] { "UsuarioId", "FechaRevocacion" });

            migrationBuilder.CreateIndex(
                name: "UX_TokensRenovacion_Hash",
                table: "TokensRenovacion",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_CuentaDestinoId",
                table: "Transferencias",
                column: "CuentaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_CuentaOrigenId",
                table: "Transferencias",
                column: "CuentaOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_Espacio_Fecha",
                table: "Transferencias",
                columns: new[] { "EspacioId", "Fecha" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CorreoNormalizado",
                table: "Usuarios",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Usuarios",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosInicioSesion_UserId",
                table: "UsuariosInicioSesion",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosReclamaciones_UserId",
                table: "UsuariosReclamaciones",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosRoles_RoleId",
                table: "UsuariosRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Viajes_Espacio_Estado_Inicio",
                table: "Viajes",
                columns: new[] { "EspacioId", "Estado", "FechaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Viajes_MetaId",
                table: "Viajes",
                column: "MetaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AportesMeta");

            migrationBuilder.DropTable(
                name: "ConfiguracionesEspacio");

            migrationBuilder.DropTable(
                name: "GastosRecurrentes");

            migrationBuilder.DropTable(
                name: "IngresosRecurrentes");

            migrationBuilder.DropTable(
                name: "Invitaciones");

            migrationBuilder.DropTable(
                name: "LineasPresupuesto");

            migrationBuilder.DropTable(
                name: "LineasPresupuestoViaje");

            migrationBuilder.DropTable(
                name: "MembresiasEspacio");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropTable(
                name: "PagosDeuda");

            migrationBuilder.DropTable(
                name: "Recomendaciones");

            migrationBuilder.DropTable(
                name: "RegistrosAuditoria");

            migrationBuilder.DropTable(
                name: "RolesReclamaciones");

            migrationBuilder.DropTable(
                name: "TasasCambio");

            migrationBuilder.DropTable(
                name: "TokensRenovacion");

            migrationBuilder.DropTable(
                name: "UsuariosInicioSesion");

            migrationBuilder.DropTable(
                name: "UsuariosReclamaciones");

            migrationBuilder.DropTable(
                name: "UsuariosRoles");

            migrationBuilder.DropTable(
                name: "UsuariosTokens");

            migrationBuilder.DropTable(
                name: "Presupuestos");

            migrationBuilder.DropTable(
                name: "Deudas");

            migrationBuilder.DropTable(
                name: "Movimientos");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Categorias");

            migrationBuilder.DropTable(
                name: "Transferencias");

            migrationBuilder.DropTable(
                name: "Viajes");

            migrationBuilder.DropTable(
                name: "Metas");

            migrationBuilder.DropTable(
                name: "Cuentas");

            migrationBuilder.DropTable(
                name: "Espacios");

            migrationBuilder.DropTable(
                name: "Monedas");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
