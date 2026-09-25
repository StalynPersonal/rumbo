"""Recorre el flujo completo de Rumbo contra la API de verdad.

No sustituye a las pruebas automaticas: comprueba otra cosa. Las pruebas usan
WebApplicationFactory, que levanta la aplicacion en memoria; esto habla por HTTP con el
proceso real, con su configuracion real y su base de datos real.
"""

import json
import subprocess
import sys
import urllib.error
import urllib.request

# Se puede apuntar a otro servidor:  python herramientas/prueba-de-humo.py http://otra:5000
BASE = sys.argv[1].rstrip("/") if len(sys.argv) > 1 else "http://localhost:5199"

fallos = []


def llamar(metodo, ruta, cuerpo=None, token=None, esperado=200):
    peticion = urllib.request.Request(BASE + ruta, method=metodo)
    peticion.add_header("Content-Type", "application/json")

    if token:
        peticion.add_header("Authorization", "Bearer " + token)

    datos = json.dumps(cuerpo).encode() if cuerpo is not None else None

    try:
        with urllib.request.urlopen(peticion, datos) as respuesta:
            texto = respuesta.read().decode()
            try:
                leido = json.loads(texto) if texto else None
            except json.JSONDecodeError:
                # Swagger devuelve HTML, no JSON. No es un fallo de la API.
                leido = texto

            return respuesta.status, leido, respuesta.headers
    except urllib.error.HTTPError as error:
        texto = error.read().decode()
        cuerpo_error = None

        try:
            cuerpo_error = json.loads(texto)
        except Exception:
            cuerpo_error = texto

        return error.code, cuerpo_error, error.headers


def comprobar(descripcion, condicion, detalle=""):
    if condicion:
        print(f"  OK   {descripcion}")
    else:
        print(f"  FALLA {descripcion} {detalle}")
        fallos.append(descripcion)


def secreto(clave):
    salida = subprocess.run(
        ["dotnet", "user-secrets", "list", "--project", "src/Rumbo.Api"],
        capture_output=True, text=True, check=True).stdout

    for linea in salida.splitlines():
        if linea.startswith(clave + " = "):
            return linea.split(" = ", 1)[1].strip()

    raise SystemExit(f"No se encontro el secreto {clave}")


print("\n1. Acceso del administrador de plataforma")

codigo, sesion, _ = llamar("POST", "/api/v1/autenticacion/iniciar-sesion", {
    "correo": secreto("Rumbo:AdministradorInicial:Correo"),
    "clave": secreto("Rumbo:AdministradorInicial:Clave"),
})

comprobar("inicia sesion", codigo == 200, f"(codigo {codigo})")

if codigo == 429:
    raise SystemExit(
        "El limite de peticiones esta activo. Lo mas probable es que venga de la\n"
        "propia prueba: el apartado 9 gasta el cupo de autenticacion a proposito.\n"
        "Espera un minuto y vuelve a ejecutarla.")

if codigo != 200:
    raise SystemExit("Sin sesion no se puede seguir.")

token_admin = sesion["tokenAcceso"]

comprobar("es administrador de plataforma", sesion["usuario"]["esAdministradorPlataforma"])
comprobar(
    "NO pertenece a ningun espacio",
    len(sesion["espaciosDisponibles"]) == 0,
    "el admin no debe tener membresias")


print("\n2. El administrador NO puede ver datos financieros")

codigo, _, _ = llamar("GET", "/api/v1/cuentas", token=token_admin)
comprobar("cuentas le responde 403 o 400", codigo in (400, 403), f"(codigo {codigo})")

codigo, _, _ = llamar("GET", "/api/v1/panel", token=token_admin)
comprobar("panel le responde 403 o 400", codigo in (400, 403), f"(codigo {codigo})")


print("\n3. Invitacion y alta de un propietario")

import uuid
sufijo = uuid.uuid4().hex[:8]
correo = f"humo{sufijo}@ejemplo.com"

codigo, invitacion, _ = llamar("POST", "/api/v1/administracion/invitaciones", {
    "correo": correo,
    "nombreEspacioPropuesto": f"Hogar de humo {sufijo}",
    "tipoEspacio": "Pareja",
}, token=token_admin)

comprobar("crea la invitacion", codigo in (200, 201), f"(codigo {codigo})")

if codigo not in (200, 201):
    raise SystemExit(f"No se pudo invitar: {invitacion}")

codigo_invitacion = invitacion["codigo"]

comprobar("el codigo se devuelve una vez", len(codigo_invitacion) > 20)

codigo, alta, _ = llamar("POST", "/api/v1/autenticacion/registrar", {
    "codigoInvitacion": codigo_invitacion,
    "correo": correo,
    "nombreCompleto": "Persona de Prueba",
    "clave": "ClaveDePrueba2026!",
})

comprobar("se registra con el codigo", codigo == 200, f"(codigo {codigo}) {alta}")

if codigo != 200:
    raise SystemExit("Sin alta no se puede seguir.")

token = alta["tokenAcceso"]

comprobar("queda dentro de su espacio", alta["espacioActivo"] is not None)

codigo, _, _ = llamar("POST", "/api/v1/autenticacion/registrar", {
    "codigoInvitacion": codigo_invitacion,
    "correo": f"otro{sufijo}@ejemplo.com",
    "nombreCompleto": "Otra Persona",
    "clave": "ClaveDePrueba2026!",
})

comprobar("el codigo NO sirve dos veces", codigo != 200, f"(codigo {codigo})")


print("\n4. Cuentas y movimientos")

codigo, nomina, _ = llamar("POST", "/api/v1/cuentas", {
    "nombre": "Nomina", "tipo": "Bancaria", "moneda": "DOP", "saldoInicial": 80000,
    "propietarioUsuarioId": None, "esCompartida": True, "institucion": None,
    "ultimosDigitos": None, "notas": None, "limiteCredito": None, "diaCorte": None,
    "diaPago": None,
}, token=token)

comprobar("crea una cuenta", codigo in (200, 201), f"(codigo {codigo}) {nomina}")

codigo, ahorro, _ = llamar("POST", "/api/v1/cuentas", {
    "nombre": "Ahorro", "tipo": "Ahorro", "moneda": "DOP", "saldoInicial": 0,
    "propietarioUsuarioId": None, "esCompartida": True, "institucion": None,
    "ultimosDigitos": None, "notas": None, "limiteCredito": None, "diaCorte": None,
    "diaPago": None,
}, token=token)

comprobar("crea la cuenta de ahorro", codigo in (200, 201))

_, arbol, _ = llamar("GET", "/api/v1/categorias", token=token)

padre = next(c for c in arbol if c["tipo"] == "Gasto" and c["subcategorias"])
categoria = padre["subcategorias"][0]

comprobar("el espacio nace con categorias", len(arbol) > 0)

codigo, movimiento, _ = llamar("POST", "/api/v1/movimientos", {
    "tipo": "Gasto", "cuentaId": nomina["id"], "categoriaId": categoria["id"],
    "monto": 5500, "moneda": None, "fechaMovimiento": "2026-09-24",
    "descripcion": "Supermercado", "notas": None, "metodoPago": None,
    "reparto": "Compartido", "pagadoPorUsuarioId": None, "viajeId": None,
}, token=token)

comprobar("registra un gasto", codigo in (200, 201), f"(codigo {codigo}) {movimiento}")

_, cuenta_tras, _ = llamar("GET", f"/api/v1/cuentas/{nomina['id']}", token=token)

comprobar(
    "el saldo baja exactamente lo gastado",
    cuenta_tras["saldoActual"] == 74500,
    f"(quedo {cuenta_tras['saldoActual']}, se esperaba 74500)")


print("\n5. La regla innegociable: una transferencia no es un gasto")

codigo, _, _ = llamar("POST", "/api/v1/movimientos/transferencias", {
    "cuentaOrigenId": nomina["id"], "cuentaDestinoId": ahorro["id"],
    "monto": 20000, "montoDestino": None, "fechaMovimiento": "2026-09-24",
    "descripcion": "Al ahorro", "comision": 0, "notas": None, "metaId": None,
    "viajeId": None,
}, token=token)

comprobar("registra la transferencia", codigo in (200, 201), f"(codigo {codigo})")

_, resumen, _ = llamar(
    "GET", "/api/v1/reportes/resumen?Desde=2026-09-01&Hasta=2026-09-30", token=token)

comprobar(
    "el informe cuenta SOLO el gasto, no la transferencia",
    resumen["totalGastos"] == 5500,
    f"(conto {resumen['totalGastos']}, se esperaba 5500)")

_, nomina_tras, _ = llamar("GET", f"/api/v1/cuentas/{nomina['id']}", token=token)
_, ahorro_tras, _ = llamar("GET", f"/api/v1/cuentas/{ahorro['id']}", token=token)

comprobar(
    "el dinero cambio de sitio sin crearse ni destruirse",
    nomina_tras["saldoActual"] + ahorro_tras["saldoActual"] == 74500,
    f"({nomina_tras['saldoActual']} + {ahorro_tras['saldoActual']})")


print("\n6. El panel, en una sola peticion")

codigo, panel, _ = llamar("GET", "/api/v1/panel", token=token)

comprobar("responde", codigo == 200, f"(codigo {codigo})")
comprobar("trae el patrimonio", panel["patrimonio"]["totalDisponible"] == 74500)
comprobar("trae el mes en curso", panel["mesEnCurso"] is not None)
comprobar("trae las secciones vacias, no ausentes", panel["metas"] == [])


print("\n6b. Planificar: lo que la aplicacion movil puede crear")

codigo, meta, _ = llamar("POST", "/api/v1/metas", {
    "nombre": "Fondo de emergencia", "descripcion": None, "montoObjetivo": 100000,
    "moneda": None, "fechaObjetivo": None, "prioridad": "Critica",
    "aporteMensualMinimo": None, "cuentaVinculadaId": ahorro["id"], "icono": None,
}, token=token)

comprobar("crea una meta", codigo in (200, 201), f"(codigo {codigo}) {meta}")

codigo, tras_aporte, _ = llamar("POST", f"/api/v1/metas/{meta['id']}/aportes", {
    "cuentaOrigenId": nomina["id"], "monto": 4000, "fecha": "2026-09-24",
    "descripcion": None, "origenRecomendacion": False,
}, token=token)

comprobar("aporta a la meta", codigo in (200, 201), f"(codigo {codigo})")
# El fondo de la meta cuenta SOLO sus aportes. La transferencia suelta de antes movio
# dinero a la misma cuenta de ahorro, pero no iba etiquetada con la meta, asi que no suma:
# el saldo de una cuenta y lo reunido para una meta son dos cosas distintas.
comprobar("el fondo cuenta solo los aportes a la meta", tras_aporte["montoActual"] == 4000,
          f"(quedo {tras_aporte['montoActual']}, se esperaba 4000)")

_, resumen_tras, _ = llamar(
    "GET", "/api/v1/reportes/resumen?Desde=2026-09-01&Hasta=2026-09-30", token=token)

comprobar("el aporte NO cuenta como gasto", resumen_tras["totalGastos"] == 5500,
          f"(conto {resumen_tras['totalGastos']}, se esperaba 5500)")

_, arbol_gasto, _ = llamar("GET", "/api/v1/categorias", token=token)
gasto_padre = next(c for c in arbol_gasto if c["tipo"] == "Gasto" and c["subcategorias"])
cat_presu = gasto_padre["subcategorias"][0]

codigo, presupuesto, _ = llamar("POST", "/api/v1/presupuestos", {
    "nombre": "Presupuesto de prueba", "tipoPeriodo": "Mensual",
    "inicioPeriodo": "2026-09-01", "finPeriodo": "2026-09-30",
    "moneda": None, "notas": None,
    "lineas": [{"categoriaId": cat_presu["id"], "montoAsignado": 8000,
                "umbralAviso": None, "umbralCritico": None, "umbralExcedido": None}],
}, token=token)

comprobar("crea un presupuesto", codigo in (200, 201), f"(codigo {codigo}) {presupuesto}")

codigo, viaje, _ = llamar("POST", "/api/v1/viajes", {
    "nombre": "Viaje de prueba", "destino": "Cartagena", "descripcion": None,
    "fechaInicio": "2027-06-10", "fechaFin": "2027-06-20", "moneda": None,
    "numeroViajeros": 2, "metaId": None,
    "lineas": [{"categoria": "Otros", "montoPlanificado": 120000, "notas": None}],
}, token=token)

comprobar("crea un viaje", codigo in (200, 201), f"(codigo {codigo}) {viaje}")
comprobar("el total sale de las partidas", viaje["presupuestoTotal"] == 120000)

codigo, viabilidad, _ = llamar(
    "GET", f"/api/v1/viajes/{viaje['id']}/viabilidad", token=token)

comprobar("responde si el viaje es viable", codigo == 200)
comprobar("con tres escenarios", len(viabilidad["escenarios"]) == 3)
comprobar("y un veredicto", viabilidad["veredicto"] in ("Si", "Ajustado", "No"))


print("\n6c. Deudas y avisos")

codigo, deuda, _ = llamar("POST", "/api/v1/deudas", {
    "nombre": "Prestamo del carro", "tipo": "Vehiculo", "acreedor": None,
    "montoOriginal": 500000, "saldoActual": 300000, "moneda": None,
    "tasaInteres": None, "pagoMinimo": None, "pagoMensual": 15000,
    "diaVencimiento": None, "fechaInicio": "2025-01-15",
    "cuentaVinculadaId": None, "responsableUsuarioId": None, "notas": None,
}, token=token)

comprobar("crea una deuda", codigo in (200, 201), f"(codigo {codigo}) {deuda}")
comprobar("estima el plazo con la cuota", deuda["mesesEstimadosRestantes"] == 20,
          f"(dijo {deuda.get('mesesEstimadosRestantes')}, se esperaba 20)")

codigo, pago, _ = llamar("POST", f"/api/v1/deudas/{deuda['id']}/pagos", {
    "cuentaOrigenId": nomina["id"], "categoriaId": cat_presu["id"],
    "montoCapital": 12000, "montoInteres": 3000, "montoCargos": 0,
    "fecha": "2026-09-24", "descripcion": None, "notas": None,
}, token=token)

comprobar("registra el pago", codigo in (200, 201), f"(codigo {codigo}) {pago}")
comprobar("el total es capital + interes", pago["montoTotal"] == 15000)
comprobar("la deuda baja SOLO el capital", pago["saldoPosterior"] == 288000,
          f"(quedo {pago['saldoPosterior']}, se esperaba 288000)")

codigo, rechazado, _ = llamar("POST", f"/api/v1/deudas/{deuda['id']}/pagos", {
    "cuentaOrigenId": nomina["id"], "categoriaId": cat_presu["id"],
    "montoCapital": 999999, "montoInteres": 0, "montoCargos": 0,
    "fecha": "2026-09-24", "descripcion": None, "notas": None,
}, token=token)

comprobar("un capital mayor que la deuda se rechaza", codigo == 400, f"(codigo {codigo})")

codigo, avisos, _ = llamar("POST", "/api/v1/notificaciones/generar", {}, token=token)

comprobar("genera los avisos", codigo == 200, f"(codigo {codigo}) {avisos}")

codigo, segunda, _ = llamar("POST", "/api/v1/notificaciones/generar", {}, token=token)

comprobar("generar dos veces NO duplica", segunda["generadas"] == 0,
          f"(genero {segunda['generadas']} la segunda vez)")

codigo, lista_avisos, _ = llamar("GET", "/api/v1/notificaciones", token=token)

comprobar("se pueden listar", codigo == 200)

codigo, _, _ = llamar("PUT", "/api/v1/notificaciones/leidas", {}, token=token)

comprobar("se marcan como leidos", codigo == 200, f"(codigo {codigo})")

_, sin_leer, _ = llamar("GET", "/api/v1/notificaciones?soloSinLeer=true", token=token)

comprobar("y despues no queda ninguno sin leer", sin_leer == [])


print("\n7. Aislamiento entre espacios")

correo2 = f"vecino{sufijo}@ejemplo.com"

codigo, invitacion2, _ = llamar("POST", "/api/v1/administracion/invitaciones", {
    "correo": correo2, "nombreEspacioPropuesto": f"Hogar vecino {sufijo}",
    "tipoEspacio": "Pareja",
}, token=token_admin)

_, alta2, _ = llamar("POST", "/api/v1/autenticacion/registrar", {
    "codigoInvitacion": invitacion2["codigo"], "correo": correo2,
    "nombreCompleto": "Vecino", "clave": "ClaveDePrueba2026!",
})

token2 = alta2["tokenAcceso"]

codigo, _, _ = llamar("GET", f"/api/v1/cuentas/{nomina['id']}", token=token2)

comprobar(
    "la cuenta de otro hogar responde 404, no 403",
    codigo == 404,
    f"(codigo {codigo}; un 403 confirmaria que existe)")

_, cuentas2, _ = llamar("GET", "/api/v1/cuentas", token=token2)

comprobar("el vecino no ve ninguna cuenta ajena", cuentas2 == [])

_, panel2, _ = llamar("GET", "/api/v1/panel", token=token2)

comprobar("su panel esta a cero", panel2["patrimonio"]["totalDisponible"] == 0)


print("\n8. Seguridad del transporte")

_, _, cabeceras = llamar("GET", "/salud")

for cabecera, valor in [
    ("X-Content-Type-Options", "nosniff"),
    ("X-Frame-Options", "DENY"),
    ("Referrer-Policy", "no-referrer"),
]:
    comprobar(f"cabecera {cabecera}", cabeceras.get(cabecera) == valor)

comprobar("no se anuncia el servidor", cabeceras.get("Server") is None)

codigo, _, _ = llamar("GET", "/api/v1/cuentas")
comprobar("sin token no se entra", codigo == 401, f"(codigo {codigo})")

codigo, _, _ = llamar("GET", "/swagger/index.html")
comprobar("swagger existe en desarrollo", codigo == 200, f"(codigo {codigo})")


# OJO: este apartado gasta el cupo de autenticacion a proposito. Si la prueba se
# ejecuta dos veces seguidas, la segunda empezara con un 429 al iniciar sesion. No es
# un fallo: es el limitador haciendo su trabajo. Espera un minuto.
print("\n9. Limite de peticiones")

vistos = []

for _ in range(8):
    c, _, _ = llamar("POST", "/api/v1/autenticacion/iniciar-sesion", {
        "correo": "nadie@ejemplo.com", "clave": "MalaClave1!",
    })
    vistos.append(c)

comprobar("los intentos en serie acaban en 429", 429 in vistos, f"(codigos {vistos})")


print("\n" + "=" * 60)

if fallos:
    print(f"FALLARON {len(fallos)} COMPROBACIONES:")
    for fallo in fallos:
        print("  -", fallo)
    sys.exit(1)

print("TODO CORRECTO. El sistema completo funciona de punta a punta.")
