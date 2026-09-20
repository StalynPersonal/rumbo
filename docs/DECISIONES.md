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

---

## D26 — Los servicios financieros viven en la capa de aplicación, con `IContextoRumbo`
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** Los servicios de autenticación están en Infraestructura por D16, porque dependen
de Identity. Los financieros no tienen esa atadura: son lógica de negocio pura.

**Decisión.** Viven en `Rumbo.Aplicacion/Modulos/`, contra una interfaz `IContextoRumbo` que
expone los `DbSet` del dominio. La capa referencia `Microsoft.EntityFrameworkCore` (la
biblioteca base, no el proveedor de SQL Server).

**Por qué es aceptable esa dependencia.** EF Core base no ata a ninguna base de datos concreta;
el proveedor lo elige Infraestructura. A cambio, los servicios escriben LINQ normal y la lógica
financiera queda donde pertenece.

**Lo que no cambia.** La implementación sigue siendo `ContextoRumbo`, así que los filtros
globales por espacio, el borrado lógico y los interceptores se aplican igual. La interfaz no es
una puerta trasera al aislamiento.

---

## D27 — Las transacciones se ejecutan a través de la estrategia de reintentos
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Problema encontrado.** `IContextoRumbo` exponía `IniciarTransaccionAsync`. Al registrar el
primer movimiento, la API devolvió 500: *«The configured execution strategy
'SqlServerRetryingExecutionStrategy' does not support user-initiated transactions»*. Los
reintentos ante fallos transitorios —imprescindibles en Azure SQL— son incompatibles con una
transacción abierta a mano: si la conexión se corta a mitad, el reintento no sabría qué rehacer.

**Decisión.** La interfaz expone **solo** `EjecutarEnTransaccionAsync(operación)`, que envuelve
el trabajo en `CreateExecutionStrategy()`. Todo el bloque se reintenta como una unidad.

**Por qué un método y no devolver la transacción.** Exponer únicamente esta forma hace
imposible cometer el error: no hay manera de abrir una transacción suelta desde la capa de
aplicación.

---

## D28 — La proyección a DTO es un árbol de expresiones, no un método
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Problema encontrado.** `Proyectar(movimiento)` era un método estático usado dentro de
`.Select(...)`. EF Core no puede traducir una llamada a método a SQL, así que la evaluó en
cliente sobre entidades cuyas navegaciones no estaban cargadas: `NullReferenceException` al
pedir el nombre de la cuenta. El compilador no avisa de nada.

**Decisión.** La proyección se declara como
`static readonly Expression<Func<Movimiento, MovimientoResumen>>`. Así la conversión ocurre
dentro de la consulta y solo se traen las columnas necesarias.

**Regla general.** Cualquier proyección usada en `.Select()` sobre un `IQueryable` debe ser una
expresión, nunca un método.

---

## D29 — `ConsultasMovimientos` centraliza la regla de qué cuenta como gasto
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** Los filtros `SoloGastos()`, `SoloIngresos()` e `IngresosYGastos()` viven en un
único sitio, y todo informe debe partir de ellos.

**Por qué.** «Una transferencia no es un gasto» es la regla que más fácil se rompe: basta con
que alguien escriba un informe nuevo y filtre a mano. Escrita una sola vez, olvidarla exige
saltársela a propósito.

---

## D30 — El estado del espacio se comprueba en cada petición
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Problema encontrado.** `EstadoEspacio.Suspendido` existía en el modelo desde la Fase 2, pero
**no hacía nada**: el middleware comprobaba que la membresía estuviera activa y no miraba el
estado del espacio. Suspender un hogar no le quitaba el acceso a nadie.

**Decisión.** `CacheMembresias` exige ahora que el espacio esté activo, además de la membresía.
Al suspender, se invalida la caché de todos sus miembros para que el corte sea inmediato.

**Lección.** Un campo de estado sin nada que lo consulte es peor que no tenerlo: da la falsa
sensación de que la función existe. Ahora hay una prueba que lo verifica de punta a punta.

---

## D31 — El administrador de plataforma gestiona, pero nunca ve finanzas
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** Los endpoints de administración devuelven nombre, tipo, estado, número de
miembros y correo del propietario —para poder contactarlo— y **ningún importe**. Las métricas
son recuentos.

**Por qué el correo del propietario sí.** Si un hogar tiene un problema, hay que poder
escribirle. Es un dato de contacto, no financiero.

**Verificación.** `ElAdministradorVeLosEspaciosSinDatosFinancieros` registra una cuenta con un
saldo reconocible y comprueba que ese número **no aparece en el JSON en crudo** de la respuesta
del administrador.

---

## D32 — Enmienda a D6: el alcance del administrador se garantiza con una prueba, no con un contexto aparte
**Fecha:** 2026-09-20 · **Estado:** aceptada · **Enmienda a:** D6

**Qué decía D6.** Que el administrador de plataforma usaría un `ContextoAdministracion` que
solo expusiera `Espacios`, `Usuarios`, `MembresiasEspacio` e `Invitaciones`.

**Qué pasó.** No se implementó. `ServicioAdministracion` usa `ContextoRumbo` directamente.
Funcionalmente da igual —esas tablas son globales y las financieras están filtradas— pero **la
garantía documentada no existía**: nada impedía añadir mañana una consulta a `Movimientos`.

**Decisión.** En lugar de crear el segundo contexto, se añade
`PruebasAlcanceDelAdministrador`, que falla si `ServicioAdministracion` menciona cualquier
tabla financiera, y si algún controlador financiero menciona el rol de plataforma.

**Por qué es mejor que el contexto aparte.** Un segundo `DbContext` significa mantener dos
configuraciones de modelo que pueden divergir en silencio, y no impide que alguien inyecte el
contexto principal. La prueba da la misma garantía, se verifica sola en cada compilación y
explica el motivo cuando falla.

---

## D33 — `IgnoreQueryFilters` solo se usa donde está justificado por escrito
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Problema encontrado.** El plan prometía una prueba de arquitectura que prohibiera
`IgnoreQueryFilters` fuera del módulo de administración. **Nunca se escribió**, y mientras
tanto apareció un uso en `ResolvedorCorreo`, justo fuera de donde estaba permitido.

**Decisión.** Dos medidas:

1. Se **eliminó** ese uso. Estaba pensado para un futuro proceso en segundo plano que enviara
   avisos de varios hogares, pero saltarse el aislamiento «por si acaso» abre un agujero real
   hoy a cambio de una comodidad futura. Con el filtro activo, pedir la configuración de otro
   espacio no devuelve nada y el correo sale por el servidor de plataforma: comportamiento
   seguro.
2. Se escribió `PruebasSaltoDeFiltros`, que recorre el **código fuente** —en el binario la
   llamada queda diluida en la expresión LINQ— y exige que cada uso figure en una lista con su
   justificación. Hoy solo hay uno, en una prueba que verifica el borrado lógico.

---

## D34 — El historial de auditoría no expone los importes
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** El permiso `auditoria.leer` existía desde la Fase 3 sin ningún endpoint que lo
usara. Al implementarlo apareció la pregunta de qué devolver.

**Decisión.** `GET /auditoria` devuelve quién, qué, cuándo y sobre qué entidad, pero **no el
campo de cambios**, que guarda los valores anteriores y nuevos.

**Por qué.** En un movimiento esos valores son importes. Exponerlos convertiría el historial en
una segunda vía para leer las finanzas del hogar, saltándose los permisos del módulo de
movimientos: alguien con `auditoria.leer` pero sin `movimientos.leer` vería el dinero igual.

El endpoint queda además reservado a quien administra el hogar: el historial revela los hábitos
de cada persona, y no todos los miembros tienen por qué poder auditarse entre sí.


---

## D35 — Las partidas de un presupuesto se reemplazan en bloque
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** Al editar un presupuesto hay que decidir qué hacer con sus partidas: casarlas una
a una con las existentes o sustituirlas todas.

**Decisión.** `PUT /presupuestos/{id}` borra las partidas anteriores y crea las que llegan.

**Por qué.** Una partida no guarda historial propio: el gasto real vive en los movimientos y se
calcula por categoría y rango de fechas. Casarlas una a una añadiría código de reconciliación
que no protege nada. Además se rechaza que una categoría aparezca dos veces: el consumo se
compararía contra un límite ambiguo.

---

## D36 — Un presupuesto solo admite categorías de gasto
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** Una partida sobre una categoría de tipo `Ingreso` se rechaza con 400.

**Por qué.** Un presupuesto limita gasto. «Presupuestar» un ingreso no significa nada, y el
cálculo de consumo —gastado sobre asignado— daría siempre cero, con un aviso que nunca salta.
Es mejor rechazarlo al crearlo que dejar una partida muerta en la pantalla.

---

## D37 — Aceptar una recomendación no mueve dinero
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** El endpoint natural de una sugerencia de aporte sería «aceptar y transferir».

**Decisión.** `PUT /recomendaciones/{id}/respuesta` solo cambia el estado a `Aceptada` o
`Descartada`. El aporte se registra después con `POST /metas/{id}/aportes`, donde la persona
confirma cuenta, importe y fecha. Hay una prueba de integración que comprueba que tras aceptar
una sugerencia ningún saldo cambió y no existe ni un solo movimiento.

**Por qué.** Es el principio innegociable del proyecto: la aplicación no modifica dinero como
consecuencia de una recomendación. Un sistema que transfiere por su cuenta, aunque acierte,
deja de ser fiable el día que se equivoca.

El aporte que nace de una sugerencia se marca con `OrigenRecomendacion`, lo que permite medir
después si las recomendaciones sirven de algo sin que eso implique automatismo alguno.

---

## D38 — Al recalcular, las pendientes se sustituyen y las respondidas se conservan
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** `POST /recomendaciones/recalcular` borra las sugerencias en estado `Pendiente` y
guarda las recién generadas. Las `Aceptada` y `Descartada` no se tocan.

**Por qué.** Si se acumularan, en una semana habría veinte sugerencias contradictorias
calculadas con datos distintos y ninguna sería de fiar. Y volver a proponer algo que la persona
ya rechazó, cada vez que se recalcula, sería molesto: las respondidas son su historial de
decisiones.

Cada regla se ejecuta dentro de un `try`: si una falla, se registra el error y el motor sigue
con las demás. Es preferible mostrar cuatro sugerencias de cinco que ninguna.

---

## D39 — Dos permisos para las recomendaciones: leer y responder
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** `recomendaciones.leer` (miembro) permite consultarlas y pedir un recálculo;
`recomendaciones.responder` (administrador) permite aceptarlas o descartarlas.

**Por qué.** Recalcular solo deriva datos que ya existen, así que cualquier miembro puede
hacerlo. Responder, en cambio, deja constancia de una decisión del hogar sobre su plan de
ahorro, y eso encaja con el mismo criterio que ya separa `metas.leer` de `metas.escribir`.


---

## D40 — El presupuesto de un viaje es la suma de sus partidas
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** `SolicitudGuardarViaje` no lleva un campo de presupuesto total. El servidor lo
calcula sumando las partidas, y se rechaza que una partida aparezca dos veces.

**Por qué.** Guardar el total y el desglose por separado deja abierta la puerta a que no
cuadren, y entonces hay que decidir cuál manda: exactamente el tipo de ambigüedad que arruina
un informe. Con una sola fuente, el desglose siempre suma el total.

---

## D41 — Un movimiento de viaje puede indicar su partida
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** `Movimiento` tenía `ViajeId`, lo que permitía comparar el gasto real con el
presupuesto **total** del viaje, pero no con cada partida.

**Decisión.** Se añade `Movimiento.CategoriaViaje` (nullable) y el campo opcional
`categoriaViaje` en `POST /movimientos`. Migración `AgregarPartidaDeViajeAlMovimiento`.

**Por qué.** La comparación por partida es justo donde se ve que el hospedaje se disparó y los
vuelos salieron baratos; el total solo dice que se gastó de más. Indicar una partida **sin**
viaje se rechaza: sería un dato huérfano que después aparece en un informe de viajes sin
pertenecer a ninguno.

Es un campo añadido, no un cambio de decisión previa: el modelo del plan seguía siendo válido,
solo incompleto para lo que la pantalla de viaje necesita mostrar.

---

## D42 — La viabilidad de un viaje se responde con tres escenarios
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** `GET /viajes/{id}/viabilidad` devuelve un escenario conservador (60 % del
excedente mensual), uno esperado (85 %) y uno optimista (100 %), más un veredicto `Si`,
`Ajustado` o `No` con su explicación en español.

**Por qué.** Una cifra única —«sí, podéis»— se lee como una promesa, y el excedente mensual de
un hogar no es constante: un mes hay una reparación, otro una boda. Tres escenarios dicen la
verdad: con qué supuesto se llega y con cuál no. El conservador no supone que el hogar ahorre
todo lo que le sobra, porque nadie lo hace.

La `fechaViableMasCercana` se calcula con el escenario **prudente**, no con el optimista:
proponer una fecha que solo se cumple ahorrando hasta el último peso sería repetir el problema
con otra cara.

Cuando el hogar no tiene excedente, el veredicto es `No` y se dice por qué. Fingir que se puede
ahorrar sin margen sería el peor consejo que puede dar una aplicación de finanzas.

**No mueve dinero, no crea aportes y no reserva nada.**

---

## D43 — El fondo de un viaje se lee de su meta, no se guarda en el viaje
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** `ViajeDetalle.FondoActual` sale de `Meta.MontoActual` de la meta vinculada.

**Por qué.** Duplicar la cifra abriría la puerta a que el viaje y la meta dijeran cosas
distintas sobre el mismo dinero. Además mantiene la separación entre las dos caras del viaje: el
presupuesto es intención de gasto, el fondo es dinero real que vive en una cuenta.

---

## D44 — Dos reglas nuevas: viabilidad de viaje e ingreso extraordinario
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** Se añaden `ReglaViabilidadDeViaje` (prioridad 15) y
`ReglaDestinoDeIngresoExtra` (prioridad 10) al motor.

**Por qué.** La primera convierte la pregunta del viaje en un aviso que llega solo: vale mucho
más saberlo nueve meses antes que tres semanas antes. Solo habla cuando el veredicto **no** es
`Si`, porque una aplicación que dice algo cada vez que se abre acaba ignorándose.

La segunda detecta un ingreso un 40 % por encima de la media mensual dentro de los últimos 30
días y propone destinarlo a la meta activa más prioritaria. Un bono suele disolverse en el gasto
corriente sin que nadie decida nada; la regla pone la decisión encima de la mesa mientras el
dinero todavía está. Propone **una** meta y no un reparto: media docena de aportes simbólicos no
acercan ninguna, y uno solo a la más urgente sí.

Ninguna de las dos mueve dinero.


---

## D45 — Un pago de deuda sí es un gasto
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** En contabilidad, devolver capital reduce un pasivo y no es un gasto; solo el
interés lo es. Eso invitaba a registrar únicamente el interés.

**Decisión.** El pago completo —capital + interés + cargos— se registra como **un gasto**. El
desglose se guarda en `PagoDeuda` para poder analizarlo.

**Por qué.** El dinero sale de la cuenta de verdad, y a diferencia de una transferencia no
aparece en ningún otro sitio del hogar. Si solo se registrara el interés, el saldo de la cuenta
quedaría descuadrado con el libro mayor, que es la línea que este proyecto no cruza. Y para un
presupuesto familiar, la cuota del carro es dinero que este mes ya no está disponible: contarla
como gasto es lo honesto, aunque un contador lo clasificaría distinto.

El desglose sigue disponible: `interesTotalPagado` permite ver cuánto se llevó el banco.

---

## D46 — El pago de deuda vive en el módulo de movimientos
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** `IServicioMovimientos.PagarDeudaAsync` está en el módulo del libro mayor;
`ServicioDeudas.PagarAsync` solo delega.

**Por qué.** Un pago mueve el saldo de una cuenta, y **toda** la lógica que toca saldos vive en
un solo sitio: conversión de moneda, signo, actualización del saldo y transacción. Duplicarla en
el módulo de deudas habría significado dos implementaciones que se desincronizan a la primera
corrección. Así el asiento, el saldo de la cuenta, el registro del pago y el saldo de la deuda
se mueven en la misma transacción: o pasa todo o no pasa nada.

---

## D47 — El saldo de una deuda solo lo mueven los pagos
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** `PUT /deudas/{id}` ignora el campo `saldoActual`. Solo se fija al crear.

**Por qué.** Si se pudiera editar a mano, el saldo y el historial de pagos dirían cosas distintas
y no habría forma de saber cuál es la buena. Es la misma razón por la que el saldo de una cuenta
no se edita: la verdad son los asientos.

---

## D48 — El panel no recalcula nada
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Decisión.** `ServicioPanel` reutiliza `IServicioReportes`, `IServicioPresupuestos`,
`IServicioMetas` y `IServicioRecomendaciones`. No tiene aritmética propia.

**Por qué.** Si el panel calculara los presupuestos por su cuenta, tarde o temprano mostraría un
porcentaje distinto al de la pantalla de presupuestos, y entonces ninguna de las dos sería
creíble. Hay una prueba de integración que compara las dos cifras directamente.

Se devuelve todo en una respuesta a propósito: la aplicación móvil se abre con una llamada en
lugar de ocho. En una conexión lenta, ocho peticiones son ocho oportunidades de que la pantalla
se quede a medias.

---

## D49 — `IDirectorioUsuarios` para traducir identificadores a nombres
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** El informe de reparto necesita escribir «María» junto a una cifra, pero los
usuarios viven en las tablas de Identity, que son cosa de infraestructura, y
`IContextoRumbo` —a propósito— no las expone.

**Decisión.** Una interfaz en la capa de aplicación con un solo método: dame estos
identificadores, devuélveme sus nombres. La implementación vive en infraestructura.

**Por qué.** Mantiene la regla de dependencias intacta sin abrir el contexto entero. Y expone lo
mínimo: solo el nombre para mostrar, ni correo ni roles ni nada más.

---

## D50 — Un aviso no se repite aunque ya se haya leído
**Fecha:** 2026-09-20 · **Estado:** aceptada

**Contexto.** La primera versión comparaba solo contra los avisos **sin leer**. Eso hacía que un
aviso ya leído volviera a aparecer en el siguiente recálculo.

**Decisión.** La comprobación de duplicados mira **todas** las notificaciones existentes, leídas
y descartadas incluidas. Los avisos que sí deben repetirse llevan la fecha dentro de sus datos,
así que el del mes que viene es un aviso distinto y se genera sin problema.

**Por qué.** Algo que ya se atendió no debería reclamar atención otra vez, y descartar un aviso
tiene que significar algo. Recibir cinco veces «el recibo de la luz vence pronto» hace que se
dejen de leer todos.

Se descubrió también que la regla de «meta alcanzada» buscaba metas **activas** con el objetivo
cubierto, y esas no existen: el propio aporte cierra la meta en cuanto el acumulado llega al
objetivo. La regla nunca se habría disparado. Ahora busca las que están en estado `Alcanzada`.
