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
