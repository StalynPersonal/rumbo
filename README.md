# Rumbo

> **Tus finanzas. Tus metas. Tu próximo destino.**

Rumbo es un **asistente financiero personal y familiar**. No es solo un registro de gastos: analiza
los ingresos y gastos reales del hogar y ayuda a responder preguntas como *«¿podemos permitirnos
este viaje?»* o *«¿cuánto tengo que ahorrar cada mes para llegar a mi meta?»*.

Está pensado para que una pareja o una familia compartan sus finanzas desde el teléfono, con
aislamiento estricto entre hogares (**multi-tenant**) y con el backend como única autoridad sobre
los cálculos.

## Qué incluye

| Módulo | Qué hace |
|---|---|
| Movimientos | Ingresos, gastos, transferencias y ajustes sobre cuentas |
| Cuentas | Bancarias, tarjetas de crédito, efectivo, ahorro, inversión |
| Categorías | Jerárquicas y personalizables |
| Presupuestos | Mensuales por categoría, con alertas configurables |
| Metas | Ahorro con cálculo de aporte mensual necesario |
| Viajes | Presupuesto, fondo de ahorro y análisis de viabilidad |
| Gastos recurrentes | Servicios y pagos con recordatorio |
| Deudas | Tarjetas, préstamos, hipoteca |
| Reportes | Por mes, categoría, usuario, personal vs compartido |
| Recomendaciones | Motor determinístico que **explica** de dónde sale cada sugerencia |

## Tecnologías

- **Backend:** .NET 10, ASP.NET Core Web API, Entity Framework Core, SQL Server / Azure SQL
- **Móvil:** .NET MAUI (Android, APK)
- **Nube:** Azure App Service, Azure SQL Database, Key Vault, Application Insights
- **Sin** Docker, Kubernetes ni microservicios: la solución debe ser fácil de entender y depurar

## Estructura del repositorio

```
src/
  Rumbo.Dominio/          Entidades, enums e invariantes. Sin dependencias.
  Rumbo.Aplicacion/       Casos de uso, servicios, validaciones y calculadoras.
  Rumbo.Contratos/        DTOs compartidos entre la API y la app móvil.
  Rumbo.Infraestructura/  EF Core, Identity, JWT, correo SMTP, tasas de cambio.
  Rumbo.Api/              API REST. Única puerta de entrada al sistema.
  Movil/Rumbo.Movil/      App .NET MAUI para Android (se crea en la Fase 8).
pruebas/
  Rumbo.PruebasUnitarias/     Calculadoras, motor de recomendaciones, validadores.
  Rumbo.PruebasIntegracion/   API real + SQL Server. Aislamiento entre espacios.
  Rumbo.PruebasArquitectura/  Reglas de dependencia entre capas.
docs/                     Documentación del proyecto (empezar por ARQUITECTURA.md).
```

Regla de dependencias: `Api → Aplicacion → Dominio`, `Infraestructura → Aplicacion, Dominio`.
El **Dominio no referencia nada**. La app móvil solo referencia `Rumbo.Contratos`.

## Requisitos para desarrollar

| Requisito | Versión |
|---|---|
| .NET SDK | 10.0.401 o superior (fijado en `global.json`) |
| SQL Server | 2022 Developer o Express, instancia `RUMBO2022` — ver [docs/BASE-DE-DATOS.md](docs/BASE-DE-DATOS.md) |
| EF Core Tools | `dotnet tool install --global dotnet-ef` |
| Workload MAUI | `dotnet workload install maui-android` (solo para la Fase 8) |

## Cómo ejecutar

```bash
dotnet build Rumbo.slnx          # compilar toda la solución
dotnet run --project src/Rumbo.Api
```

Luego:

- Estado de la API: <http://localhost:5114/salud>
- Documentación interactiva: <http://localhost:5114/swagger>
- Documento OpenAPI: <http://localhost:5114/openapi/v1.json>

Swagger solo se publica en los ambientes `Development` y `Staging`.

```bash
dotnet test Rumbo.slnx           # ejecutar todas las pruebas
```

## Documentación

| Documento | Contenido |
|---|---|
| [docs/ARQUITECTURA.md](docs/ARQUITECTURA.md) | Capas, flujo de autenticación y multi-tenant |
| [docs/GLOSARIO.md](docs/GLOSARIO.md) | **Normativo.** Nombres canónicos del dominio |
| [docs/DECISIONES.md](docs/DECISIONES.md) | Registro de decisiones técnicas y su porqué |
| [docs/BASE-DE-DATOS.md](docs/BASE-DE-DATOS.md) | Instalación de SQL Server y modelo de datos |

## Convención de idioma

**Todo el código está en español**: proyectos, clases, métodos, propiedades, rutas de la API y
nombres de tablas. Los identificadores van **sin tildes ni ñ** (`Categoria`, `Anio`, `MetodoPago`);
los textos que ve el usuario sí llevan ortografía correcta. Solo se conserva en inglés lo que impone
el framework (`Program.cs`, el sufijo `Controller`, `IdentityUser<>`, `DbContext`, la carpeta
`Migrations`). Antes de nombrar cualquier concepto nuevo, consulta
[docs/GLOSARIO.md](docs/GLOSARIO.md).

**La documentación del código también está en español y es obligatoria.** Todo miembro público
lleva su comentario XML `///`, y los comentarios explican el *porqué*, no el *qué*. Esto no es una
recomendación: `GenerateDocumentationFile` está activo y CS1591 **no** está silenciado, así que con
`TreatWarningsAsErrors` un miembro público sin documentar **no compila**. Los proyectos de
`pruebas/` quedan exentos, porque el nombre de cada `[Fact]` ya dice lo que verifica.

## Estado del proyecto

| Fase | Contenido | Estado |
|---|---|---|
| 0 | Análisis y arquitectura | ✅ Completada |
| 1 | Estructura de la solución | ✅ Completada |
| 2 | Dominio, EF Core y primera migración | ⏳ Siguiente |
| 3 | Autenticación, invitaciones y multi-tenancy | ⬜ |
| 4 | API financiera | ⬜ |
| 5 | Presupuestos, metas y recomendaciones | ⬜ |
| 6 | Viajes y viabilidad | ⬜ |
| 7 | Panel y reportes | ⬜ |
| 8 | Aplicación .NET MAUI | ⬜ |
| 9 | Seguridad y pruebas | ⬜ |
| 10 | Despliegue en Azure | ⬜ |
| 11 | Generación del APK | ⬜ |
