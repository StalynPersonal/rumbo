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
