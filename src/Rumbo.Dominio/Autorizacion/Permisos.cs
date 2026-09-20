namespace Rumbo.Dominio.Autorizacion;

/// <summary>
/// Catalogo de permisos del sistema.
/// </summary>
/// <remarks>
/// <para>
/// La autorizacion se comprueba por PERMISO y no por rol. La diferencia importa: si los
/// controladores dijeran <c>[Authorize(Roles = "Propietario,Administrador")]</c>, anadir
/// manana un rol nuevo (un "Observador" que solo consulta, o un perfil para un adolescente
/// de la casa) obligaria a repasar y editar todos los controladores, y bastaria olvidar uno
/// para abrir un agujero.
/// </para>
/// <para>
/// Con permisos, el controlador declara QUE hace falta poder hacer y el mapa de
/// <see cref="MapaPermisos"/> decide quien puede. Anadir un rol es tocar un solo fichero.
/// </para>
/// </remarks>
public static class Permisos
{
    /// <summary>Permisos sobre las cuentas.</summary>
    public static class Cuentas
    {
        /// <summary>Consultar cuentas y saldos.</summary>
        public const string Leer = "cuentas.leer";

        /// <summary>Crear y modificar cuentas.</summary>
        public const string Escribir = "cuentas.escribir";

        /// <summary>Eliminar cuentas.</summary>
        public const string Eliminar = "cuentas.eliminar";
    }

    /// <summary>Permisos sobre los movimientos del libro mayor.</summary>
    public static class Movimientos
    {
        /// <summary>Consultar movimientos.</summary>
        public const string Leer = "movimientos.leer";

        /// <summary>Registrar y modificar movimientos.</summary>
        public const string Escribir = "movimientos.escribir";

        /// <summary>Eliminar movimientos.</summary>
        public const string Eliminar = "movimientos.eliminar";
    }

    /// <summary>Permisos sobre las categorias.</summary>
    public static class Categorias
    {
        /// <summary>Consultar categorias.</summary>
        public const string Leer = "categorias.leer";

        /// <summary>Crear y modificar categorias.</summary>
        public const string Escribir = "categorias.escribir";
    }

    /// <summary>Permisos sobre los presupuestos.</summary>
    public static class Presupuestos
    {
        /// <summary>Consultar presupuestos.</summary>
        public const string Leer = "presupuestos.leer";

        /// <summary>Crear y modificar presupuestos.</summary>
        public const string Escribir = "presupuestos.escribir";
    }

    /// <summary>Permisos sobre las metas de ahorro.</summary>
    public static class Metas
    {
        /// <summary>Consultar metas.</summary>
        public const string Leer = "metas.leer";

        /// <summary>Crear y modificar metas.</summary>
        public const string Escribir = "metas.escribir";
    }

    /// <summary>Permisos sobre las sugerencias del motor de recomendaciones.</summary>
    public static class Recomendaciones
    {
        /// <summary>Consultar sugerencias y pedir que se recalculen.</summary>
        public const string Leer = "recomendaciones.leer";

        /// <summary>Aceptar o descartar una sugerencia.</summary>
        /// <remarks>
        /// Responder no mueve dinero, pero deja constancia de una decision del hogar, por
        /// eso se separa de la simple lectura.
        /// </remarks>
        public const string Responder = "recomendaciones.responder";
    }

    /// <summary>Permisos sobre los viajes.</summary>
    public static class Viajes
    {
        /// <summary>Consultar viajes.</summary>
        public const string Leer = "viajes.leer";

        /// <summary>Crear y modificar viajes.</summary>
        public const string Escribir = "viajes.escribir";
    }

    /// <summary>Permisos sobre las deudas.</summary>
    public static class Deudas
    {
        /// <summary>Consultar deudas.</summary>
        public const string Leer = "deudas.leer";

        /// <summary>Crear y modificar deudas.</summary>
        public const string Escribir = "deudas.escribir";
    }

    /// <summary>Permisos sobre los informes.</summary>
    public static class Reportes
    {
        /// <summary>Consultar informes y el panel.</summary>
        public const string Leer = "reportes.leer";
    }

    /// <summary>Permisos sobre los avisos del hogar.</summary>
    public static class Notificaciones
    {
        /// <summary>Consultar los avisos y pedir que se regeneren.</summary>
        public const string Leer = "notificaciones.leer";

        /// <summary>Marcar como leidos o descartar avisos.</summary>
        /// <remarks>
        /// Lo tiene cualquier miembro: apartar un aviso propio no es una accion privilegiada,
        /// y obligar a pedir permiso para ello seria absurdo.
        /// </remarks>
        public const string Gestionar = "notificaciones.gestionar";
    }

    /// <summary>Permisos sobre la gestion del propio espacio.</summary>
    public static class Espacio
    {
        /// <summary>Ver los datos y los miembros del espacio.</summary>
        public const string Leer = "espacio.leer";

        /// <summary>Cambiar el nombre, el tipo o las preferencias.</summary>
        public const string Escribir = "espacio.escribir";

        /// <summary>Invitar personas al espacio.</summary>
        public const string Invitar = "espacio.invitar";

        /// <summary>Cambiar roles, suspender o expulsar miembros.</summary>
        public const string GestionarMiembros = "espacio.gestionar_miembros";

        /// <summary>
        /// Configurar el servidor de correo del espacio.
        /// </summary>
        /// <remarks>
        /// Permiso aparte y reservado al propietario. No son credenciales del sistema: son
        /// las de su cuenta de correo personal, y no tiene por que confiarselas a un
        /// administrador del hogar solo porque este pueda invitar gente.
        /// </remarks>
        public const string ConfigurarCorreo = "espacio.configurar_correo";

        /// <summary>Eliminar el espacio completo.</summary>
        public const string Eliminar = "espacio.eliminar";
    }

    /// <summary>Permisos sobre el historial de auditoria.</summary>
    public static class Auditoria
    {
        /// <summary>Consultar el historial del propio espacio.</summary>
        public const string Leer = "auditoria.leer";
    }
}
