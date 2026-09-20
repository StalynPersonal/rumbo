# Registro de decisiones

Cada decisión técnica relevante se anota aquí con su fecha, las alternativas que se descartaron y
sus consecuencias. El objetivo es que ninguna fase contradiga en silencio a otra, y que quien llegue
nuevo entienda **por qué** las cosas son como son.

Formato: `D<número> — Título`, con contexto, decisión, alternativas y consecuencias.

---

## D1 — Todo el código en español, sin tildes ni ñ en identificadores
**Fecha:** 2026-09-19 · **Estado:** aceptada

**Contexto.** El equipo piensa y habla el dominio en español.

**Decisión.** Proyectos, namespaces, clases, métodos, propiedades, DTOs, rutas de la API y nombres
de tablas y columnas van en español. Los identificadores **sin tildes ni ñ** (`Categoria`, `Anio`).
Los textos visibles al usuario sí llevan ortografía correcta.

**Alternativas descartadas.** Código en inglés y solo la UI en español (rompe la correspondencia
entre el lenguaje del dominio y el del código); identificadores con tildes (C# los admite, pero
complican búsquedas, scripts SQL escritos a mano y herramientas de línea de comandos).

**Consecuencias.** Se mantiene [GLOSARIO.md](GLOSARIO.md) como documento normativo. Quedan en
inglés los nombres que impone el framework.

---

## D2 — `Movimiento`, no `Transaccion`
**Fecha:** 2026-09-19 · **Estado:** aceptada

**Decisión.** La entidad central del libro mayor se llama `Movimiento`.

**Por qué.** `Transaccion` colisiona conceptualmente con la transacción de base de datos y con
`IDbContextTransaction`. En una revisión de código o en un log, «la transacción falló» sería
ambiguo.

---

## D3 — `Espacio` como unidad de multi-tenancy
**Fecha:** 2026-09-19 · **Estado:** aceptada

**Decisión.** El *tenant* se llama `Espacio`.

**Alternativas descartadas.** `Hogar` (quedaría contradictorio cuando exista `TipoEspacio.Negocio`);
`Tenant` (inglés, contra D1); `Inquilino` (traducción literal sin sentido en este dominio).

---

## D4 — Documentación XML obligatoria, en español
**Fecha:** 2026-09-19 · **Estado:** aceptada

**Contexto.** El usuario quiere poder leer y aprender del código, y que la documentación no se
quede atrás respecto al código.

**Decisión.** `GenerateDocumentationFile` activado y **CS1591 no silenciado** en los proyectos de
`src/`. Como `TreatWarningsAsErrors` está activo, **un miembro público sin comentario `///` no
compila**. Los comentarios explican el *porqué*, no el *qué*.

**Excepción.** Los proyectos de `pruebas/` quedan exentos: el nombre de cada `[Fact]` ya describe
lo que verifica, y exigir `///` ahí sería ruido.

**Consecuencias.** Escribir una clase pública nueva obliga a documentarla en el mismo commit. Es
deliberado.

---

## D5 — Gestión central de paquetes
**Fecha:** 2026-09-19 · **Estado:** aceptada

**Decisión.** Las versiones de NuGet se declaran una sola vez en `Directory.Packages.props`; los
`.csproj` referencian paquetes sin atributo `Version`.

**Por qué.** Impide que la API y la app móvil acaben con versiones distintas del mismo paquete, un
fallo que suele aparecer tarde y es difícil de diagnosticar.

---

## D6 — El administrador de plataforma no lee datos financieros
**Fecha:** 2026-09-19 · **Estado:** aceptada

**Contexto.** El alta de usuarios es cerrada: un `AdministradorPlataforma` reparte códigos de
invitación.

**Decisión.** Ese rol gestiona espacios, usuarios, invitaciones y métricas agregadas, pero **no
puede leer movimientos, cuentas, saldos, metas ni viajes**. Se implementa con un
`ContextoAdministracion` que solo expone esas tablas, y es el único punto del sistema autorizado a
usar `IgnoreQueryFilters()`.

**Por qué.** Mínimo privilegio sobre datos financieros de terceros. Un rol con acceso total sería
un riesgo permanente a cambio de una comodidad que no se necesita.

**Además.** El administrador es invisible en cada espacio de forma **estructural**: no tiene fila en
`MembresiasEspacio`, y el listado de miembros se construye desde esa tabla. No depende de un filtro
que alguien pueda olvidar en un endpoint nuevo.

---

## D7 — SQL Server 2022 en una instancia nueva para desarrollo
**Fecha:** 2026-09-19 · **Estado:** aceptada

**Contexto.** La máquina de desarrollo tiene SQL Server **2014** en la instancia por defecto. El
proveedor EF Core 10 da soporte oficial a 2016 o superior, y producción será Azure SQL (nivel 160).

**Decisión.** Instalar SQL Server 2022 Developer como instancia nombrada `RUMBO2022`, conviviendo
con la 2014, que queda intacta. Pasos en [BASE-DE-DATOS.md](BASE-DE-DATOS.md).

**Alternativa descartada.** Usar la 2014 con `UseCompatibilityLevel(120)`: funcionaría, pero
renunciando a `OPENJSON`, `STRING_AGG`, `AT TIME ZONE` y funciones JSON, y creando divergencia entre
desarrollo y producción. Queda documentada como plan B.

---

## D8 — Formato de solución `.slnx`
**Fecha:** 2026-09-19 · **Estado:** aceptada

**Contexto.** El SDK de .NET 10 genera el nuevo formato XML de solución (`Rumbo.slnx`) en lugar del
`.sln` clásico.

**Decisión.** Se mantiene `.slnx`. Es legible, no tiene los GUID del formato antiguo y produce
conflictos de merge mucho menores. Visual Studio 2022 17.13+ y Rider lo abren sin problema.

**Nota.** El plan de la Fase 0 mencionaba `Rumbo.sln`; el cambio es solo de formato, no de
estructura.

---

## D9 — La app .NET MAUI se crea en la Fase 8
**Fecha:** 2026-09-19 · **Estado:** aceptada

**Contexto.** El plan preveía crear los seis proyectos en la Fase 1, pero el workload
`maui-android` todavía no está instalado en la máquina.

**Decisión.** `src/Movil/Rumbo.Movil` se crea al empezar la Fase 8, junto con la instalación del
workload.

**Por qué.** Añadirlo ahora haría fallar `dotnet build` de toda la solución, y la CI del backend
tendría que compilar workloads de Android sin necesitarlos. Las carpetas y la estructura ya están
decididas; solo se retrasa la creación del proyecto.

---

## D10 — Swagger UI sobre el OpenAPI integrado de ASP.NET Core
**Fecha:** 2026-09-19 · **Estado:** aceptada

**Decisión.** El documento OpenAPI lo genera `Microsoft.AspNetCore.OpenApi` (incluido en el
framework desde .NET 9) y se sirve en `/openapi/v1.json`. Para la interfaz visual se usa solo el
paquete `Swashbuckle.AspNetCore.SwaggerUI`, apuntado a ese documento.

**Por qué.** Evita tener dos generadores de documento compitiendo. Swagger se publica únicamente en
`Development` y `Staging`, nunca en producción.

---

## D11 — Las entidades de Identity viven en Infraestructura, no en Dominio
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** `Usuario` hereda de `IdentityUser<Guid>`, que viene de un paquete externo, y la
regla D-arquitectura dice que `Rumbo.Dominio` no depende de nada.

**Decisión.** `Usuario`, `Rol` y `RolesPlataforma` viven en `Rumbo.Infraestructura/Identidad`.
Las entidades del dominio se refieren a las personas por su `Guid`, sin propiedad de
navegación; las relaciones se declaran desde la configuración de EF Core con
`HasOne<Usuario>().WithMany()`.

**Consecuencia positiva inesperada.** El dominio no sabe *cómo* se autentica alguien. Si algún
día se cambia Identity por otra cosa, el negocio no se entera.

---

## D12 — Los filtros globales se aplican recorriendo el ensamblado del dominio
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** La primera versión de `AplicarFiltrosGlobales` recorría
`constructor.Model.GetEntityTypes()` dentro de `OnModelCreating`.

**Problema encontrado.** Con .NET 10, eso rompe la configuración de *passkeys* de ASP.NET Core
Identity: el modelo falla al construirse con `The entity type 'IdentityPasskeyData' requires a
primary key to be defined`. Enumerar el modelo a medio construir obliga a EF Core a
materializar tipos que todavía no ha terminado de clasificar, y `IdentityPasskeyData` —que
debe ser un **tipo complejo**— queda registrado como entidad.

**Decisión.** Descubrir las entidades por reflexión sobre el ensamblado
`Rumbo.Dominio` (las que implementan `IEntidadDeEspacio` o `IBorradoLogico`) en lugar de
enumerar el modelo.

**Alternativa descartada.** El remedio que circula en internet para ese error es
`modelBuilder.Entity<IdentityPasskeyData>().HasNoKey()`. Es un parche que ataca el síntoma:
convierte en tabla sin clave algo que debe ser un tipo complejo, generando un esquema
distinto del que Identity espera. Además no resuelve la causa, que estaba en nuestro código.

**Consecuencia.** El conjunto de entidades aisladas queda explícito, no se tocan las de
Identity, y una prueba de arquitectura verifica que ninguna entidad de negocio se quede fuera.

---

## D13 — Dos filtros con nombre en lugar de uno combinado
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** `FiltroEspacio` y `FiltroBorradoLogico` se declaran por separado, usando los
filtros con nombre que introduce EF Core 10.

**Por qué.** Antes de EF 10 había que combinarlos con `&&`, y `IgnoreQueryFilters()` los
desactivaba todos a la vez. Eso significaba que la reconciliación de saldos, que necesita ver
registros borrados **de su propio espacio**, tenía que renunciar también al aislamiento. Con
filtros separados se puede desactivar solo uno.

---

## D14 — La semilla no incluye tasas de cambio
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** La migración inicial siembra el catálogo de monedas (DOP, USD, EUR, GBP) pero
**ninguna tasa de cambio**.

**Por qué.** Sembrar un tipo de cambio sería fabricar un dato financiero. Cualquier importe
convertido con una cifra inventada saldría mal, y el usuario no tendría forma de saber que el
número no es real. Es preferible que el sistema avise de que falta la tasa a que devuelva un
resultado plausible y falso.

**Consecuencia.** Antes de registrar movimientos en una moneda distinta de la base del
espacio hay que cargar tasas. El conversor (Fase 4) usará la tasa anterior más cercana
marcando `TasaEsAproximada`, y si no hay ninguna rechazará la operación con un mensaje claro.

---

## D15 — Las migraciones quedan exentas del estilo de código
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** `dotnet ef` genera las migraciones con namespaces de llaves y sin comentarios
XML. Con `TreatWarningsAsErrors` y la regla de documentación obligatoria (D4), el proyecto no
compilaba después de generar la primera.

**Decisión.** Una sección `[**/Migrations/*.cs]` en `.editorconfig` las marca como
`generated_code = true` y silencia esas reglas.

**Alternativa descartada.** Editar a mano cada migración para adaptarla al estilo: se
regeneran, así que el arreglo se perdería en la siguiente.

---

## D16 — El servicio de autenticación vive en Infraestructura
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** `ServicioAutenticacion` necesita `UserManager<Usuario>`, y `Usuario` hereda de
`IdentityUser<Guid>`, que está en Infraestructura por D11.

**Decisión.** La *interfaz* `IServicioAutenticacion` está en `Rumbo.Aplicacion/Contratos`;
la implementación, en `Rumbo.Infraestructura/Identidad`. Los controladores dependen de la
interfaz, nunca de la clase.

**Por qué no forzarlo a Aplicación.** Habría exigido una abstracción propia sobre Identity
—un `IGestorUsuarios` que replicara su API— que solo serviría para satisfacer una regla, sin
ninguna ventaja práctica.

---

## D17 — Los permisos se incrustan en el token; la membresía se verifica en cada petición
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** El JWT lleva los permisos del rol, pero el middleware **consulta la base de
datos en cada petición** para confirmar que la membresía sigue activa (con caché de 30 s).

**El equilibrio.** Si todo se leyera del token, expulsar a alguien no surtiría efecto hasta
que caducara (15 minutos). Si todo se consultara, cada comprobación de permiso costaría una
consulta. La solución intermedia: un **cambio de rol** tarda como mucho 15 minutos en
aplicarse, pero **revocar el acceso por completo** es prácticamente inmediato. Se optimiza el
caso que importa: echar a alguien del hogar.

---

## D18 — Las pruebas nunca pueden borrar una base que no sea de pruebas
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** La primera versión de `FabricaApiDePrueba` configuraba la cadena de conexión
con `ConfigureAppConfiguration`. Con el hospedaje mínimo de .NET, esa fuente quedó **por
debajo** de `appsettings.Development.json`, así que las pruebas apuntaron a la base de
desarrollo, y `EnsureDeletedAsync` **borró la base `Rumbo` real**.

**Decisión.** Dos medidas, no una:

1. La configuración de prueba se aplica con `UseSetting`, que escribe en la configuración del
   host y sí tiene precedencia.
2. Antes de borrar nada, se comprueba que el nombre de la base empiece por el prefijo de
   pruebas (`RumboApi_` o `RumboPruebas_`). Si no, se lanza una excepción y **no se borra**.

**Por qué las dos.** La primera arregla la causa; la segunda impide que un fallo equivalente
—otra librería, otro orden de configuración, un descuido futuro— vuelva a destruir datos. Una
guarda de tres líneas frente a una pérdida irreversible es un intercambio evidente.

---

## D19 — Sin holgura de reloj en la validación del JWT
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** `ClockSkew = TimeSpan.Zero`.

**Por qué.** El valor por defecto son 5 minutos, pensados para servidores con relojes
desincronizados. Sobre un token de 15 minutos, eso alarga su vida útil un tercio. Cliente y
servidor sincronizan por NTP, así que la holgura no aporta nada y solo amplía la ventana de
un token robado.

---

## D20 — Los miembros se consultan en dos pasos, no con un `join`
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** Listar los miembros de un espacio exige unir `MembresiasEspacio` (dominio) con
`Usuarios` (Identity, infraestructura). Por D11 el dominio no conoce el tipo `Usuario`, así
que no hay propiedad de navegación entre ambos.

**Problema encontrado.** El `join` escrito en LINQ **no lo traduce EF Core**: falla en tiempo
de ejecución con `The LINQ expression could not be translated`. El compilador no avisa.

**Decisión.** Dos consultas y combinación en memoria: primero las membresías, después los
usuarios por sus identificadores.

**Por qué es aceptable.** Un hogar tiene dos o tres personas, y una familia rara vez pasa de
diez. Son dos consultas por índice sobre conjuntos diminutos. La alternativa —añadir una
navegación del dominio hacia Identity— rompería una regla de arquitectura por un problema que
no existe a esta escala.

---

## D21 — La caché de membresías se invalida al cambiar rol o estado
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** El middleware verifica la membresía en cada petición y cachea el resultado 30
segundos. La documentación afirmaba que revocar un acceso surtía efecto «casi de inmediato».

**Problema encontrado.** Una prueba lo desmintió: con la caché sin invalidar, suspender a
alguien tardaba **hasta 30 segundos** en tener efecto.

**Decisión.** `CacheMembresias.Invalidar` se llama siempre que cambia el rol o el estado de
una membresía. Retirar un acceso surte efecto en la siguiente petición.

**Limitación documentada.** La caché es por instancia. Con varias instancias, la invalidación
solo alcanza a una; habrá que pasar a caché distribuida si se escala horizontalmente.

---

## D22 — El tipo de espacio elegido al invitar se persiste en la invitación
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Problema encontrado.** El administrador elegía «Pareja» al invitar, pero el alta creaba
siempre un espacio `Personal`: `Invitacion` no guardaba el tipo, así que la elección se perdía
entre la invitación y el registro. Se detectó probando el flujo a mano.

**Decisión.** `Invitacion.TipoEspacioPropuesto` persiste la elección, y el alta la respeta.
Migración `AgregarTipoEspacioPropuestoAInvitacion`.

---

## D23 — Dos niveles de servidor SMTP, ambos configurables desde la aplicación
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** El usuario pidió que cada propietario de espacio pueda usar su propio correo con
sus credenciales, y que el de plataforma también sea editable.

**Decisión.** Dos tablas: `ConfiguracionCorreoPlataforma` (global, una fila) y
`ConfiguracionCorreoEspacio` (una por espacio). Orden de preferencia al enviar: espacio →
plataforma → `appsettings`.

**Por qué sigue haciendo falta el de plataforma.** Dos correos no tienen espacio del que sacar
credenciales: la invitación a un futuro propietario, que se envía *antes* de que su espacio
exista, y el restablecimiento de contraseña, que pertenece a la persona y no a un hogar —
alguien puede estar en varios. Sin un servidor de plataforma, nadie podría darse de alta ni
recuperar su cuenta.

**Por qué `appsettings` se mantiene como último recurso.** Es el arranque en frío: permite
enviar la primera invitación cuando la base de datos todavía no tiene ninguna configuración.

---

## D24 — Las contraseñas SMTP se cifran, no se hashean
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** `IProtectorSecretos` cifra de forma reversible con Data Protection, bajo un
propósito aislado.

**Por qué no hash.** Es el error conceptual fácil de cometer aquí. Una contraseña de acceso se
hashea porque solo hace falta *comprobarla*. Una credencial SMTP hay que **recuperarla en
claro** para autenticarse contra el servidor de correo: hashearla la inutilizaría.

**Por qué importa cifrarla.** No es una credencial del sistema, es la contraseña del correo
personal de un usuario. En texto plano, cualquiera con acceso de lectura a la base de datos se
llevaría una cuenta de correo ajena.

**Consecuencia para la Fase 10.** Las claves de Data Protection pasan de «conveniente» a
**crítico**: si se pierden al reiniciar App Service, las contraseñas SMTP guardadas dejan de
poder descifrarse. Deben persistirse en Blob Storage y protegerse con Key Vault.

---

## D25 — `espacio.configurar_correo` es un permiso aparte, solo del Propietario
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** Configurar el correo del espacio no usa `espacio.escribir`, sino un permiso
propio concedido únicamente al rol `Propietario`.

**Por qué.** Un `Administrador` del hogar ya puede invitar gente y gestionar presupuestos, pero
eso no implica que deba poder ver ni cambiar las credenciales del correo personal del
propietario. Son dos niveles de confianza distintos y conviene no mezclarlos.
