# API de Rumbo

REST sobre HTTPS, JSON, versionada en la ruta (`/api/v1/`). La documentación interactiva está
en `/swagger` (solo en Development y Staging) y el documento OpenAPI en `/openapi/v1.json`.

## Principios

**Todo va en español**: rutas, campos y mensajes de error.

**El espacio nunca viaja en la URL.** Todas las rutas operan sobre el espacio activo, que sale
del token firmado. Una ruta con el espacio en la URL sería justo el punto por donde alguien
intentaría entrar en otro hogar cambiando un identificador. Para cambiar de hogar se usa
`POST /autenticacion/cambiar-espacio`, que verifica la membresía y emite un token nuevo.

**Errores en formato `ProblemDetails`** (RFC 9457), siempre con un `traceId`:

```json
{
  "title": "Solicitud no válida",
  "status": 400,
  "detail": "Ese código de invitación ya se usó o fue anulado.",
  "instance": "POST /api/v1/autenticacion/registrar",
  "traceId": "0HNOMPURUQSJI:00000001"
}
```

| Código | Cuándo |
|---|---|
| `400` | Regla de negocio incumplida. El `detail` está redactado para la persona |
| `401` | Sin token, token inválido o credenciales incorrectas |
| `403` | Autenticado pero sin el permiso necesario en ese espacio |
| `404` | No existe **o pertenece a otro espacio** — no se distinguen, a propósito |
| `409` | Dos personas modificaron lo mismo a la vez |
| `423` | Cuenta bloqueada por intentos fallidos |

## Autenticación

`POST /api/v1/autenticacion/...`

| Ruta | Anónimo | Qué hace |
|---|---|---|
| `registrar` | Sí | Alta con código de invitación. **No hay registro libre** |
| `iniciar-sesion` | Sí | Devuelve token de acceso (15 min) y de renovación (30 días) |
| `renovar` | Sí | Rota el token de renovación y emite credenciales nuevas |
| `cerrar-sesion` | No | Revoca el token de renovación |
| `cambiar-clave` | No | Cierra **todas** las sesiones abiertas |
| `olvide-clave` | Sí | Responde `204` exista o no la cuenta |
| `restablecer-clave` | Sí | Fija una contraseña nueva con el código del correo |
| `cambiar-espacio` | No | Emite un token para otro de sus espacios |

El token va en `Authorization: Bearer <token>`.

## Espacios

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /espacios/actual` | `espacio.leer` | Datos del hogar |
| `PUT /espacios/actual` | `espacio.escribir` | Cambia nombre y tipo |
| `GET /espacios/actual/miembros` | `espacio.leer` | Personas del hogar con su rol |
| `PUT /espacios/actual/miembros/{id}/rol` | `espacio.gestionar_miembros` | Cambia el rol |
| `PUT /espacios/actual/miembros/{id}/estado` | `espacio.gestionar_miembros` | Suspende o expulsa |
| `GET/PUT /espacios/actual/configuracion` | `espacio.leer` / `espacio.escribir` | Umbrales y preferencias |
| `GET/PUT /espacios/actual/correo` | `espacio.configurar_correo` | SMTP propio del hogar |
| `POST /espacios/actual/correo/probar` | `espacio.configurar_correo` | Comprueba la conexión |

## Invitaciones

| Ruta | Permiso | Qué hace |
|---|---|---|
| `POST /invitaciones` | `espacio.invitar` | Invita a alguien al hogar |
| `GET /invitaciones` | `espacio.leer` | Invitaciones del hogar, **sin sus códigos** |
| `DELETE /invitaciones/{id}` | `espacio.invitar` | Anula una pendiente |

El código en claro se devuelve **una sola vez**, al crearla.

## Administración de plataforma

Reservado al rol `AdministradorPlataforma`. **No incluye ningún endpoint que devuelva datos
financieros**: ese rol gestiona altas, no finanzas.

| Ruta | Qué hace |
|---|---|
| `POST /administracion/invitaciones` | Invita a alguien a crear su propio espacio |
| `GET /administracion/invitaciones` | Lista las invitaciones de propietario |
| `DELETE /administracion/invitaciones/{id}` | Anula una pendiente |
| `GET/PUT /administracion/correo` | SMTP de la plataforma |
| `POST /administracion/correo/probar` | Comprueba la conexión |

## Cuentas

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /cuentas` | `cuentas.leer` | Lista con saldo, también convertido a moneda base |
| `GET /cuentas/{id}` | `cuentas.leer` | Una cuenta |
| `POST /cuentas` | `cuentas.escribir` | Crea una cuenta |
| `PUT /cuentas/{id}` | `cuentas.escribir` | Modifica una cuenta |
| `DELETE /cuentas/{id}` | `cuentas.eliminar` | Solo si no tiene movimientos |
| `POST /cuentas/{id}/reconciliar` | `cuentas.escribir` | Recalcula el saldo desde el libro mayor |

**Lo que no se puede cambiar**: tipo, moneda y saldo inicial. Los movimientos ya registrados
dependen de ellos; cambiar la moneda de una cuenta con historial reinterpretaría importes
pasados.

`?corregir=true` en la reconciliación aplica la corrección si hay desviación.

## Categorías

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /categorias` | `categorias.leer` | Árbol de dos niveles |
| `POST /categorias` | `categorias.escribir` | Crea una categoría |
| `PUT /categorias/{id}` | `categorias.escribir` | Modifica nombre, icono, color, orden |
| `DELETE /categorias/{id}` | `categorias.escribir` | Solo propias, sin movimientos ni hijas |

Cada espacio nace con **79 categorías** predeterminadas. Las del sistema se pueden renombrar o
desactivar, pero no eliminar: los informes predefinidos se quedarían sin datos.

El tipo y el padre no se pueden cambiar: mover una categoría reclasificaría movimientos ya
registrados y alteraría informes de meses cerrados.

## Movimientos

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /movimientos` | `movimientos.leer` | Libro mayor, paginado y con filtros |
| `GET /movimientos/{id}` | `movimientos.leer` | Un movimiento |
| `POST /movimientos` | `movimientos.escribir` | Registra ingreso, gasto o ajuste |
| `PUT /movimientos/{id}` | `movimientos.escribir` | Modifica; ajusta el saldo por la diferencia |
| `DELETE /movimientos/{id}` | `movimientos.eliminar` | Borrado lógico; revierte el saldo |
| `POST /movimientos/transferencias` | `movimientos.escribir` | Traspaso entre cuentas |
| `DELETE /movimientos/transferencias/{id}` | `movimientos.eliminar` | Deshace las **dos** patas |

Filtros de `GET /movimientos`: `Desde`, `Hasta`, `CuentaId`, `CategoriaId`, `Tipo`, `Reparto`,
`PagadoPorUsuarioId`, `ViajeId`, `MetaId`, `Busqueda`, `Pagina`, `TamanoPagina` (máximo 200).

### Reglas del libro mayor

**El importe siempre es positivo.** La dirección la determina el tipo, no el signo. Un importe
negativo se rechaza: si se admitiera, registrar un gasto podría aumentar el saldo.

**Una transferencia son dos asientos.** `POST /movimientos/transferencias` crea la salida y la
entrada en una sola transacción, unidas por el mismo `transferenciaId`. **Ninguna de las dos
cuenta como ingreso ni como gasto** (`cuentaParaIngresosYGastos: false`): mover RD$10,000 de la
cuenta de nómina a la de ahorro no es gastar RD$10,000.

No se puede crear un movimiento suelto de tipo `Transferencia`, ni borrar ni editar una sola
pata: dejaría dinero apareciendo o desapareciendo de la nada.

**La comisión sí es un gasto** y se registra como movimiento aparte, con su categoría: ese
dinero sí sale del hogar.

**Un aporte a una meta es una transferencia**, no un gasto. Se indica `metaId` en el traspaso y
el sistema deja constancia del aporte. Ahorrar no es gastar.

### Moneda

Cada movimiento guarda su importe, su moneda y su equivalente en la moneda base del espacio,
**congelado con la tasa de la fecha del movimiento**. Los informes suman por ese campo, así que
un mes ya cerrado no cambia de resultado si mañana se mueve el tipo de cambio.

Si falta la tasa exacta se usa la anterior más cercana y el movimiento queda marcado con
`tasaEsAproximada: true`. Si no hay ninguna, la operación se rechaza con un mensaje claro:
inventar una paridad produciría un importe plausible y falso.
