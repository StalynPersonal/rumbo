using Microsoft.EntityFrameworkCore;

using Rumbo.Contratos.Autenticacion;
using Rumbo.Dominio.Entidades.Financiero;
using Rumbo.Dominio.Entidades.Identidad;
using Rumbo.Dominio.Enums;
using Rumbo.Dominio.Excepciones;
using Rumbo.Infraestructura.Persistencia.Semilla;

namespace Rumbo.Infraestructura.Identidad;

/// <summary>
/// Parte del servicio de autenticacion que construye la respuesta y crea espacios nuevos.
/// </summary>
public partial class ServicioAutenticacion
{
    /// <summary>
    /// Emite las credenciales y reune el contexto que necesita la aplicacion movil.
    /// </summary>
    /// <param name="usuario">Usuario autenticado.</param>
    /// <param name="espacioId">Espacio activo, o <c>null</c> si no tiene ninguno.</param>
    /// <param name="direccionIp">IP desde la que se conecta.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>Credenciales y contexto de la sesion.</returns>
    private async Task<RespuestaAutenticacion> ConstruirRespuestaAsync(
        Usuario usuario,
        Guid? espacioId,
        string? direccionIp,
        CancellationToken cancelacion)
    {
        var espacios = await contexto.MembresiasEspacio
            .Where(m => m.UsuarioId == usuario.Id && m.Estado == EstadoMembresia.Activa)
            .Include(m => m.Espacio)
            .OrderBy(m => m.FechaIngreso)
            .Select(m => new EspacioResumen(
                m.EspacioId,
                m.Espacio!.Nombre,
                m.Espacio.Tipo.ToString(),
                m.Rol.ToString(),
                m.Espacio.MonedaBase))
            .ToListAsync(cancelacion);

        var espacioActivo = espacios.FirstOrDefault(e => e.Id == espacioId);

        var rolEnEspacio = espacioActivo is null
            ? (RolEspacio?)null
            : Enum.Parse<RolEspacio>(espacioActivo.Rol);

        var esAdministrador = await usuarios.IsInRoleAsync(
            usuario, RolesPlataforma.AdministradorPlataforma);

        var (tokenAcceso, expiracion) = tokens.GenerarTokenAcceso(
            usuario.Id,
            usuario.Email!,
            esAdministrador,
            espacioActivo?.Id,
            rolEnEspacio);

        var (tokenRenovacion, hash) = tokens.GenerarTokenRenovacion();

        contexto.TokensRenovacion.Add(new TokenRenovacion
        {
            UsuarioId = usuario.Id,
            Hash = hash,
            FechaCreacion = fechaHora.AhoraUtc,
            FechaExpiracion = fechaHora.AhoraUtc.AddDays(_jwt.DiasTokenRenovacion),
            DireccionIp = direccionIp,
        });

        await contexto.SaveChangesAsync(cancelacion);

        return new RespuestaAutenticacion(
            tokenAcceso,
            expiracion,
            tokenRenovacion,
            new UsuarioAutenticado(usuario.Id, usuario.Email!, usuario.NombreCompleto, esAdministrador),
            espacioActivo,
            espacios);
    }

    /// <summary>
    /// Crea un espacio nuevo con su configuracion y sus categorias, y deja al usuario como
    /// propietario.
    /// </summary>
    /// <param name="usuario">Persona que sera propietaria.</param>
    /// <param name="invitacion">Invitacion que origina el espacio.</param>
    /// <returns>La membresia creada.</returns>
    private MembresiaEspacio CrearEspacioParaPropietario(Usuario usuario, Invitacion invitacion)
    {
        var espacio = new Espacio
        {
            Id = Guid.CreateVersion7(),
            Nombre = invitacion.NombreEspacioPropuesto ?? $"Finanzas de {usuario.NombreCompleto}",
            // Se respeta el tipo que eligio quien emitio la invitacion. Ignorarlo y poner
            // siempre Personal haria que un hogar de pareja naciera mal clasificado.
            Tipo = invitacion.TipoEspacioPropuesto ?? TipoEspacio.Personal,
            MonedaBase = "DOP",
            ZonaHoraria = "America/Santo_Domingo",
            Estado = EstadoEspacio.Activo,
        };

        contexto.Espacios.Add(espacio);
        contexto.ConfiguracionesEspacio.Add(new ConfiguracionEspacio { EspacioId = espacio.Id });

        // Un espacio sin categorias no sirve para nada: no se podria clasificar el primer
        // gasto. Se crean las predeterminadas de inmediato.
        SembrarCategorias(espacio.Id);

        var membresia = new MembresiaEspacio
        {
            UsuarioId = usuario.Id,
            EspacioId = espacio.Id,
            Rol = RolEspacio.Propietario,
            Estado = EstadoMembresia.Activa,
            FechaIngreso = fechaHora.AhoraUtc,
        };

        contexto.MembresiasEspacio.Add(membresia);

        return membresia;
    }

    /// <summary>Anade al usuario a un espacio existente, con el rol de la invitacion.</summary>
    /// <param name="usuario">Persona invitada.</param>
    /// <param name="invitacion">Invitacion que da acceso.</param>
    /// <param name="cancelacion">Token de cancelacion.</param>
    /// <returns>La membresia creada.</returns>
    private async Task<MembresiaEspacio> UnirAEspacioExistenteAsync(
        Usuario usuario,
        Invitacion invitacion,
        CancellationToken cancelacion)
    {
        var espacioId = invitacion.EspacioId
            ?? throw new ExcepcionInvitacionInvalida(
                "La invitación no indica a qué espacio pertenece.");

        var disponible = await contexto.Espacios
            .AnyAsync(e => e.Id == espacioId && e.Estado == EstadoEspacio.Activo, cancelacion);

        if (!disponible)
        {
            throw new ExcepcionInvitacionInvalida("El espacio de la invitación ya no está disponible.");
        }

        var membresia = new MembresiaEspacio
        {
            UsuarioId = usuario.Id,
            EspacioId = espacioId,
            Rol = invitacion.RolAsignado ?? RolEspacio.Miembro,
            Estado = EstadoMembresia.Activa,
            FechaIngreso = fechaHora.AhoraUtc,
        };

        contexto.MembresiasEspacio.Add(membresia);

        return membresia;
    }

    /// <summary>Crea el arbol de categorias predeterminadas de un espacio nuevo.</summary>
    /// <param name="espacioId">Espacio al que pertenecen.</param>
    private void SembrarCategorias(Guid espacioId)
    {
        var orden = 0;

        foreach (var plantilla in CategoriasPredeterminadas.Todas())
        {
            var padre = new Categoria
            {
                Id = Guid.CreateVersion7(),
                EspacioId = espacioId,
                Nombre = plantilla.Nombre,
                Tipo = plantilla.Tipo,
                Icono = plantilla.Icono,
                EsDelSistema = true,
                Orden = orden++,
            };

            contexto.Categorias.Add(padre);

            var ordenHija = 0;

            foreach (var nombreHija in plantilla.Subcategorias)
            {
                contexto.Categorias.Add(new Categoria
                {
                    Id = Guid.CreateVersion7(),
                    EspacioId = espacioId,
                    CategoriaPadreId = padre.Id,
                    Nombre = nombreHija,
                    Tipo = plantilla.Tipo,
                    Icono = plantilla.Icono,
                    EsDelSistema = true,
                    Orden = ordenHija++,
                });
            }
        }
    }
}
