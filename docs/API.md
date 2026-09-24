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

## Auditoría

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /auditoria` | `auditoria.leer` | Historial del espacio, paginado |

Filtros: `Desde`, `Hasta`, `UsuarioId`, `TipoEntidad`, `EntidadId`, `Accion`, `Pagina`,
`TamanoPagina`.

Responde «¿quién cambió este gasto y cuándo?». **No devuelve el detalle de los cambios**: ese
campo guarda los valores anteriores y nuevos, que en un movimiento son importes, y exponerlo
sería una segunda vía para leer las finanzas saltándose los permisos del módulo.

Reservado a quien administra el hogar: el historial revela los hábitos de cada persona.

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


## Presupuestos

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /presupuestos?soloVigente=` | `presupuestos.leer` | Lista con el consumo de cada partida |
| `GET /presupuestos/{id}` | `presupuestos.leer` | Un presupuesto con sus partidas |
| `POST /presupuestos` | `presupuestos.escribir` | Crea uno con sus partidas |
| `PUT /presupuestos/{id}` | `presupuestos.escribir` | Modifica y **reemplaza** las partidas |
| `DELETE /presupuestos/{id}` | `presupuestos.escribir` | Elimina el presupuesto |

Un presupuesto **no impide gastar**: compara lo planificado con lo realmente gastado y avisa.
El consumo sale siempre del libro mayor —movimientos de esa categoría dentro del período—,
nunca de un contador aparte que pudiera desincronizarse.

Cada partida devuelve `montoGastado`, `montoDisponible`, `porcentajeConsumido`, un `nivel`
(`Normal`, `Aviso`, `Critico`, `Excedido`) según sus umbrales, el `ritmoDiarioNecesario` con lo
que queda y la `proyeccionAlCierre` si se mantiene el ritmo actual.

Los umbrales se toman de la configuración del espacio (80 / 90 / 100 por omisión) y se pueden
sobrescribir partida a partida.

Reglas de validación: al menos una partida; ninguna categoría repetida —el consumo se
compararía contra un límite ambiguo—; solo categorías de **gasto**; y umbrales ordenados
(aviso ≤ crítico ≤ excedido).

Una **transferencia nunca consume presupuesto**: mover dinero entre cuentas propias no es
gastar.

## Metas de ahorro

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /metas?incluirCerradas=` | `metas.leer` | Lista con la proyección de cada meta |
| `GET /metas/{id}` | `metas.leer` | Una meta |
| `POST /metas` | `metas.escribir` | Crea una meta |
| `PUT /metas/{id}` | `metas.escribir` | Modifica una meta |
| `PUT /metas/{id}/estado` | `metas.escribir` | Activa, pausa, alcanza o cancela |
| `DELETE /metas/{id}` | `metas.escribir` | Elimina una meta **sin aportes** |
| `GET /metas/{id}/aportes` | `metas.leer` | Historial de aportes |
| `POST /metas/{id}/aportes` | `metas.escribir` | Aporta desde una cuenta |

Una meta **no guarda dinero**: el dinero vive en la cuenta vinculada. La meta dice cuánto se
quiere reunir y para cuándo, y el sistema calcula el ritmo.

`aporteMensualNecesario` = *(objetivo − reunido) ÷ meses completos que faltan*. Con el ejemplo
de referencia: RD$180,000 de objetivo, RD$30,000 reunidos y 15 meses por delante → faltan
RD$150,000 y hacen falta **RD$10,000 al mes**. El semanal se calcula sobre los días reales, no
dividiendo el mensual entre cuatro: un mes no tiene cuatro semanas.

`vaAtrasada` compara el aporte necesario con el que la persona se comprometió a hacer: si el
necesario es mayor, al ritmo prometido no llegaría.

**Un aporte es una transferencia, no un gasto.** Sale de la cuenta de origen y entra en la
cuenta de ahorro de la meta, con los dos asientos, en una sola transacción. Contarlo como gasto
haría que ahorrar pareciera empobrecer.

Una meta **sin cuenta vinculada no admite aportes**: el acumulado subiría sin que ningún saldo
bajara, y el hogar creería tener ese dinero dos veces.

Una meta **con aportes no se elimina** —esos movimientos existen y apuntan a ella—; se cancela,
lo que la retira de la vista conservando el historial.

## Recomendaciones

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /recomendaciones?incluirRespondidas=` | `recomendaciones.leer` | Sugerencias vigentes |
| `POST /recomendaciones/recalcular` | `recomendaciones.leer` | Recalcula con los datos de hoy |
| `PUT /recomendaciones/{id}/respuesta` | `recomendaciones.responder` | Acepta o descarta |

El motor es **determinista**: reglas y aritmética, sin inteligencia artificial. Dos ejecuciones
con los mismos datos dan el mismo resultado, y cada sugerencia guarda en `insumos` los datos y
la fórmula con los que se calculó, para poder responder de dónde sale el número.

**Ninguna recomendación mueve dinero.** Aceptar una deja constancia de la decisión; el aporte se
registra después con `POST /metas/{id}/aportes`, que la persona confirma. El sistema nunca
transfiere por su cuenta.

Al recalcular, las pendientes se sustituyen y las ya respondidas se conservan. Si una regla
falla, se registra el error y el motor continúa con las demás.

Con menos de tres meses de historial la sugerencia llega con `confianzaBaja: true`: la
aplicación debe presentarla como estimación, no como dato.

Reglas activas hoy: **aporte mensual para una meta** (cuánto haría falta apartar al mes y si el
flujo de caja lo permite) y **alerta de presupuesto** (una partida que va camino de excederse
antes de que acabe el período).


## Viajes

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /viajes?incluirCerrados=` | `viajes.leer` | Lista por fecha de salida |
| `GET /viajes/{id}` | `viajes.leer` | Un viaje con su desglose y gasto real |
| `GET /viajes/{id}/viabilidad` | `viajes.leer` | **¿Podemos permitirnos este viaje?** |
| `POST /viajes` | `viajes.escribir` | Crea un viaje con su desglose |
| `PUT /viajes/{id}` | `viajes.escribir` | Modifica y **reemplaza** el desglose |
| `PUT /viajes/{id}/estado` | `viajes.escribir` | Planificado, EnCurso, Finalizado, Cancelado |
| `DELETE /viajes/{id}` | `viajes.escribir` | Elimina un viaje **sin gastos** |

Un viaje tiene **dos caras** que conviene no confundir:

- El **presupuesto**: lo que se piensa gastar, desglosado en partidas (`Vuelos`, `Hospedaje`,
  `Alimentacion`, `Transporte`, `Actividades`, `Compras`, `Documentos`, `Seguro`, `Otros`).
- El **fondo**: lo que se lleva ahorrado para pagarlo. Eso es una **meta** con su cuenta de
  ahorro, referenciada con `metaId`. El fondo no se guarda en el viaje: se lee de la meta, para
  que las dos cifras no puedan decir cosas distintas sobre el mismo dinero.

El `presupuestoTotal` **no se envía**: es la suma de las partidas. Guardar las dos cosas dejaría
abierta la puerta a que no cuadraran, y entonces habría que decidir cuál manda.

Los **gastos del viaje sí son gastos** normales del hogar: se registran con `POST /movimientos`
indicando `viajeId` y, opcionalmente, `categoriaViaje` para imputarlos a una partida concreta.
Una `categoriaViaje` sin `viajeId` se rechaza. Los **aportes al fondo**, en cambio, son
transferencias hacia la cuenta de ahorro: no aparecen como gasto del viaje.

### ¿Podemos permitirnos este viaje?

`GET /viajes/{id}/viabilidad` devuelve **tres escenarios** en lugar de una sola cifra, porque
una respuesta única se lee como una promesa y el excedente mensual de un hogar no es constante:
un mes hay una reparación, otro una boda.

| Escenario | Supuesto |
|---|---|
| Conservador | Se aparta el **60 %** del excedente mensual |
| Esperado | Se aparta el **85 %** |
| Optimista | Se aparta **todo** el excedente |

El veredicto resume los tres:

- **`Si`** — se llega incluso en el escenario prudente.
- **`Ajustado`** — solo se llega apretando, sin fallar ningún mes.
- **`No`** — ni ahorrando todo el excedente se llega, o no hay excedente.

Además devuelve `aporteMensualNecesario`, `esfuerzoRequerido` (qué parte del excedente se comería
el viaje), una `explicacion` en español y, si hoy no alcanza, la `fechaViableMasCercana`
calculada con el escenario **prudente**: proponer una fecha que solo se cumple ahorrando hasta el
último peso sería repetir el problema con otra cara.

El excedente sale de `AnalizadorFlujoCaja`, que ya descuenta los compromisos recurrentes. Con
menos de tres meses de historial la respuesta llega con `confianzaBaja: true`.

**No mueve dinero, no crea aportes y no reserva nada.** Es una proyección para decidir.


## Deudas

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /deudas?incluirSaldadas=` | `deudas.leer` | Lista, de mayor a menor saldo |
| `GET /deudas/{id}` | `deudas.leer` | Una deuda con su avance |
| `POST /deudas` | `deudas.escribir` | Crea una deuda |
| `PUT /deudas/{id}` | `deudas.escribir` | Modifica (el saldo **no**) |
| `PUT /deudas/{id}/estado` | `deudas.escribir` | Activa, Saldada, Refinanciada |
| `DELETE /deudas/{id}` | `deudas.escribir` | Elimina una deuda **sin pagos** |
| `GET /deudas/{id}/pagos` | `deudas.leer` | Historial de pagos |
| `POST /deudas/{id}/pagos` | `deudas.escribir` | Registra un pago |

Un pago se envía desglosado en **capital**, **interés** y **cargos**; el total es la suma y no
se envía. El asiento, el saldo de la cuenta y el saldo de la deuda se mueven en la misma
transacción.

**Un pago de deuda sí es un gasto**, a diferencia de una transferencia: el dinero sale de la
cuenta y no aparece en ningún otro sitio del hogar. Que la parte de capital reduzca además una
obligación no cambia que ese dinero ya no está disponible este mes.

El **saldo pendiente no se puede editar**: lo mueven los pagos, y solo ellos. Un capital mayor
que lo que se debe se rechaza —dejaría al banco debiendo dinero al hogar—, y al llegar a cero
la deuda pasa sola a `Saldada`.

`mesesEstimadosRestantes` divide el saldo entre la cuota (`pagoMensual`, o `pagoMinimo` si no
hay). Sin ninguna de las dos no se estima nada: inventar un plazo sería peor que no dar ninguno.

## Informes

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /reportes/resumen` | `reportes.leer` | Ingresos, gastos, balance y tasa de ahorro |
| `GET /reportes/categorias` | `reportes.leer` | Gasto por categoría, de mayor a menor |
| `GET /reportes/mensual` | `reportes.leer` | Serie mensual y promedios |
| `GET /reportes/reparto` | `reportes.leer` | Quién puso cuánto en lo compartido |

Filtros comunes: `Desde`, `Hasta`, `CuentaId`, `CategoriaId`, `UsuarioId`, `Reparto`. Sin fechas
se toman los últimos doce meses.

Todos pasan por el mismo `IQueryable` base, que **excluye las transferencias**. Es la única
forma de que la regla no se rompa cada vez que se escribe un informe nuevo. Y todos suman por el
equivalente en moneda base congelado a la fecha del movimiento, así que un mes ya cerrado no
cambia de resultado si mañana se mueve el tipo de cambio.

La serie mensual **incluye los meses sin movimientos**: un mes vacío es información, y saltárselo
deformaría la gráfica.

El informe de reparto **registra y reporta** el desbalance de los gastos compartidos, pero no
liquida entre personas ni mueve dinero. Quién salda y cómo es una conversación del hogar.

## Panel

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /panel` | `reportes.leer` | Toda la pantalla de inicio en una petición |

Devuelve patrimonio consolidado, mes en curso y mes anterior, las cinco categorías donde más se
gasta, las partidas de presupuesto en alerta, los compromisos que vencen en quince días, el
avance de las metas, las sugerencias pendientes y cuántos avisos hay sin leer.

Una sola petición en lugar de ocho: en una conexión móvil lenta, ocho peticiones son ocho
oportunidades de que la pantalla se quede a medias.

El panel **no recalcula nada**: reutiliza los mismos servicios que las pantallas de detalle. Si
tuviera su propia aritmética, tarde o temprano mostraría una cifra distinta y ninguna de las dos
sería creíble.

El `patrimonioNeto` resta las deudas. Mostrar solo el saldo de las cuentas daría una sensación de
holgura que no se corresponde con la realidad de un hogar endeudado.

## Notificaciones

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /notificaciones?soloSinLeer=` | `notificaciones.leer` | Lista los avisos |
| `POST /notificaciones/generar` | `notificaciones.leer` | Revisa el hogar y crea los que procedan |
| `PUT /notificaciones/{id}/leida` | `notificaciones.gestionar` | Marca uno como leído |
| `PUT /notificaciones/leidas` | `notificaciones.gestionar` | Marca todos como leídos |
| `DELETE /notificaciones/{id}` | `notificaciones.gestionar` | Descarta uno |

Se **persisten** aunque todavía no exista transporte push: cuando se enchufe, el historial ya
estará ahí y no habrá que inventarlo.

**No se duplican.** Antes de crear uno se comprueba que no exista ya otro del mismo tipo sobre lo
mismo, leído o no. Los que sí deben repetirse llevan la fecha dentro de sus datos, así que el
aviso del mes que viene es un aviso distinto.

Descartar **no borra**: saber que se avisó y que la persona lo apartó permite medir si los avisos
sirven de algo.


## Aplicación móvil

| Ruta | Permiso | Qué hace |
|---|---|---|
| `GET /app/version?version=` | anónimo | Dice si la versión instalada sigue valiendo |
| `GET /salud` | anónimo | Comprobación de vida: solo dice si el proceso responde |
| `GET /salud/preparada` | anónimo | Además comprueba que se llega a la base de datos |

`GET /app/version` es **anónimo** a propósito: la aplicación necesita preguntarlo antes de que
nadie inicie sesión. Si la versión instalada ya no sirve, lo útil es decirlo en la pantalla de
acceso, no después de que la persona se pelee con un error raro.

Devuelve `versionMinima`, `versionRecomendada`, `urlDescarga` y dos banderas:
`hayActualizacion` y `actualizacionObligatoria`.

**El servidor no corta el acceso a las versiones antiguas.** La v1 de la API sigue funcionando
para todas; solo avisa. Dejar a alguien sin ver sus finanzas por no haber actualizado sería peor
que la versión vieja. Y si la aplicación no manda versión, o manda algo que no se entiende, no
se marca nada como obligatorio: no se bloquea a ciegas.

`versionMinima` solo se sube cuando una versión antigua hace algo **incorrecto**, no cada vez que
se publica un APK. Obligar a actualizar por gusto acostumbra a la gente a ignorar el aviso, y
entonces el día que importa de verdad nadie lo lee.

### Los dos endpoints de salud

Responden preguntas distintas y por eso son dos. `/salud` dice si el proceso vive, y es el que
usa Azure como comprobación de vida; si mirara la base de datos, una caída momentánea de SQL
haría que la plataforma reiniciara la aplicación, que no arregla nada y encima tira las sesiones.
`/salud/preparada` sí consulta la base, y lo usa el despliegue para decidir si una instancia
recién publicada puede recibir tráfico.
