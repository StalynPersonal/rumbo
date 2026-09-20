# Base de datos

## 1. Instalación de SQL Server 2022 para desarrollo

### Por qué una instancia nueva

Esta máquina ya tiene **SQL Server 2014 Standard** en la instancia por defecto (`MSSQLSERVER`).
No la tocamos: sigue disponible para lo que ya la use.

Rumbo necesita una instancia más moderna por dos razones concretas:

1. El proveedor `Microsoft.EntityFrameworkCore.SqlServer` 10 da soporte oficial a **SQL Server 2016
   o superior**. Con 2014 habría que fijar `UseCompatibilityLevel(120)` y renunciar a parte de lo
   que EF Core genera.
2. Producción será **Azure SQL Database**, que corre con nivel de compatibilidad 160. Desarrollar
   contra 2014 significa que algo puede funcionar en un sitio y fallar en el otro; esa clase de
   divergencia siempre aparece tarde.

Con 2022 disponemos además de `OPENJSON`, `STRING_AGG`, `AT TIME ZONE` y las funciones JSON, que
usaremos en `RegistroAuditoria.Cambios` y `Recomendacion.Insumos`.

SQL Server permite varias instancias en la misma máquina sin conflicto, así que las dos conviven.

### Pasos

Necesitas permisos de administrador en el equipo.

1. **Descargar SQL Server 2022 Developer Edition** (gratuita para desarrollo) desde
   <https://www.microsoft.com/sql-server/sql-server-downloads>.
   Elegir la descarga **Developer**, que baja un instalador pequeño.

2. Ejecutar el instalador y elegir **Personalizada** (*Custom*). Descargará el medio completo y
   abrirá el *SQL Server Installation Center*.

3. **Instalación → Nueva instalación independiente de SQL Server**.

4. En **Tipo de instalación**, elegir **Realizar una nueva instalación** (no *Agregar
   características a una instancia existente*). Esto es lo que crea una instancia separada en vez
   de tocar el 2014.

5. En **Selección de características**, marcar solo:
   - Servicios de Motor de base de datos

   No hace falta Analysis Services, Reporting Services ni Integration Services.

6. En **Configuración de instancia**, elegir **Instancia con nombre** y escribir:

   ```
   RUMBO2022
   ```

7. En **Configuración del motor de base de datos**:
   - Modo de autenticación: **Modo mixto**. Poner una contraseña fuerte para `sa` y guardarla en un
     gestor de contraseñas. En el día a día usaremos autenticación de Windows; el modo mixto queda
     disponible por si hace falta.
   - En *Especificar administradores de SQL Server*, pulsar **Agregar usuario actual**.

8. Terminar la instalación.

9. **Habilitar TCP/IP** (necesario para conectarse por herramientas y para las pruebas de
   integración):
   - Abrir *SQL Server Configuration Manager*.
   - *Configuración de red de SQL Server* → *Protocolos de MSSQL$RUMBO2022*.
   - Poner **TCP/IP** en **Habilitado**.
   - Reiniciar el servicio `SQL Server (RUMBO2022)`.

10. Comprobar que el servicio **SQL Server Browser** está en ejecución y con inicio automático
    (es lo que permite resolver `localhost\RUMBO2022` por nombre).

### Verificación

Desde PowerShell:

```powershell
Get-Service MSSQL`$RUMBO2022
Invoke-Sqlcmd -ServerInstance "localhost\RUMBO2022" -Query "SELECT @@VERSION"
```

Debe responder con una versión **16.x** (SQL Server 2022).

Si `Invoke-Sqlcmd` no está disponible:

```powershell
Install-Module SqlServer -Scope CurrentUser
```

### Cadena de conexión

Ya está configurada en `src/Rumbo.Api/appsettings.Development.json`:

```
Server=localhost\RUMBO2022;Database=Rumbo;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

- `Trusted_Connection=True` → autenticación de Windows, sin contraseñas en ficheros.
- `TrustServerCertificate=True` → acepta el certificado autofirmado del SQL Server local. **Solo
  para desarrollo**; en Azure no se usa.

### Plan B

Si la instalación no fuera posible, se puede desarrollar contra la instancia 2014 existente
cambiando la cadena de conexión a `Server=localhost;...` y añadiendo `UseCompatibilityLevel(120)`
al configurar EF Core. Es una solución degradada: quedaría documentada aquí y en
[DECISIONES.md](DECISIONES.md) por el riesgo que implica.

## 2. Modelo de datos

Se documentará al completar la **Fase 2** (entidades, relaciones, índices, restricciones y primera
migración). Los nombres de todas las tablas y columnas siguen el [GLOSARIO.md](GLOSARIO.md).

### Convenciones ya decididas

| Aspecto | Convención | Razón |
|---|---|---|
| Clave primaria | `Guid` con `Guid.CreateVersion7()` | Secuencial en el tiempo, no fragmenta el índice agrupado como el v4, y no es adivinable como un entero |
| Dinero | `decimal(19,4)` | Nunca `float` ni `double`: perderían precisión en los céntimos |
| Moneda | `char(3)` ISO-4217 junto a cada monto | El sistema es multi-moneda desde el primer día |
| Fecha contable | `DateOnly` | Un gasto registrado a las 11 p.m. no debe saltar al mes siguiente por zona horaria |
| Fecha de sistema | `DateTimeOffset` | Instantes reales, con desplazamiento explícito |
| Borrado | Lógico (`Eliminado`) en lo financiero | Los movimientos nunca se borran físicamente: auditoría y reconciliación |
| Concurrencia | `rowversion` en `Cuentas` y `Metas` | Dos personas del mismo hogar pueden escribir a la vez |
| Borrado en cascada | Desactivado (`Restrict`) por defecto | Ninguna cascada debe poder borrar historial financiero |
| Índices | `EspacioId` como primera columna | Rendimiento y aislamiento visible en el plan de ejecución |
