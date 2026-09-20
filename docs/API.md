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

## Perfil de usuario

| Ruta | Qué hace |
|---|---|
| `GET /usuarios/yo` | Perfil de quien llama |
| `PUT /usuarios/yo` | Cambia nombre y cultura |

**Solo el propio perfil.** No hay ninguna ruta que reciba un identificador de usuario: una
persona no tiene por qué consultar los datos de otra, ni siquiera de su mismo hogar. Para ver
a los demás miembros está `GET /espacios/actual/miembros`, que devuelve lo justo.

El correo no se cambia aquí: es la credencial de acceso, y modificarlo sin verificar la
dirección nueva permitiría secuestrar una cuenta.

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
| `GET /administracion/espacios` | Lista los hogares, **sin datos financieros** |
| `PUT /administracion/espacios/{id}/estado` | Suspende o reactiva un hogar |
| `GET /administracion/usuarios` | Lista las cuentas |
| `PUT /administracion/usuarios/{id}/estado?activo=` | Habilita o deshabilita una cuenta |
| `GET /administracion/metricas` | Recuentos agregados |

**Suspender un espacio** corta el acceso de todos sus miembros en su siguiente petición, sin
borrar nada, y es reversible. **Deshabilitar una cuenta** revoca todas sus sesiones de
inmediato; un administrador no puede deshabilitarse a sí mismo.

Las métricas son **recuentos, nunca importes**: saber cuántos hogares hay es gestión, saber
cuánto dinero mueven sería entrar en sus finanzas.

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

## Gastos e ingresos recurrentes

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /gastos-recurrentes` | `movimientos.leer` | Lista, con los días que faltan para vencer |
| `POST /gastos-recurrentes` | `movimientos.escribir` | Crea una obligación |
| `PUT /gastos-recurrentes/{id}` | `movimientos.escribir` | Modifica |
| `PUT /gastos-recurrentes/{id}/estado?estado=` | `movimientos.escribir` | Pausa, reactiva o finaliza |
| `DELETE /gastos-recurrentes/{id}` | `movimientos.eliminar` | Elimina |
| `POST /gastos-recurrentes/{id}/pagar` | `movimientos.escribir` | **Confirma el pago** |
| `GET/POST/PUT/DELETE /ingresos-recurrentes` | idem | Equivalente para ingresos |
| `POST /ingresos-recurrentes/{id}/cobrar` | `movimientos.escribir` | Confirma el cobro |

**No son movimientos, sino plantillas.** Sirven para avisar de lo que viene y para que el
motor de recomendaciones sepa cuánto dinero está ya comprometido.

**Rumbo nunca crea el movimiento por su cuenta al llegar la fecha.** Avisa, y el movimiento se
registra al confirmar el pago. Generarlo automáticamente haría que el saldo dejara de reflejar
la realidad en cuanto un pago se retrasara o cambiara de importe, que es lo habitual en un
recibo de luz.

Al confirmar, **el importe real manda sobre el estimado** y la próxima fecha se calcula desde
la que *vencía*, no desde la del pago: si se paga con tres días de retraso cada mes, calcularla
desde el pago iría corriendo el vencimiento y en un año el recibo cambiaría de semana.

## Monedas y tasas de cambio

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /monedas` | autenticado | Catálogo ISO-4217 |
| `GET /tasas-cambio?origen=&destino=` | autenticado | Tasas de un par, de la más reciente |
| `POST /tasas-cambio` | `espacio.escribir` | Registra o **actualiza** una tasa |
| `DELETE /tasas-cambio/{id}` | `espacio.escribir` | Elimina una tasa |
| `GET /tasas-cambio/convertir` | autenticado | Previsualiza una conversión |

Son datos **globales**: el catálogo y las cotizaciones son información pública, no de un hogar.
Por eso una tasa que carga un espacio la ven todos.

La tasa se expresa como *cuántas unidades de destino equivalen a una de origen*: con el dólar a
60 pesos, `USD → DOP` es `60`. La conversión inversa se deduce sola, así que no hace falta
cargar `DOP → USD`.

Registrar dos veces el mismo par y la misma fecha **actualiza** la tasa en lugar de crear otra:
dos valores para el mismo día harían que el mismo movimiento se convirtiera distinto según cuál
se leyera.

### Moneda

Cada movimiento guarda su importe, su moneda y su equivalente en la moneda base del espacio,
**congelado con la tasa de la fecha del movimiento**. Los informes suman por ese campo, así que
un mes ya cerrado no cambia de resultado si mañana se mueve el tipo de cambio.

Si falta la tasa exacta se usa la anterior más cercana y el movimiento queda marcado con
`tasaEsAproximada: true`. Si no hay ninguna, la operación se rechaza con un mensaje claro:
inventar una paridad produciría un importe plausible y falso.
