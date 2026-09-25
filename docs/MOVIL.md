# La aplicación móvil, explicada

Este documento está escrito para alguien que **no conoce MAUI**. No es una referencia: es una
explicación de por qué el código móvil de Rumbo está hecho como está, y de qué significa cada
pieza cuando la veas por primera vez.

Si algo aquí te parece obvio, sáltatelo. Si algo no se entiende, es culpa del documento, no
tuya: avísame y lo reescribo.

---

## 1. Qué es .NET MAUI, en una frase

Escribes una interfaz **una vez**, en C# y XAML, y se compila a una aplicación nativa de
Android, iOS, Windows o macOS. Nosotros solo generamos **Android**, porque es lo que pediste y
porque cada plataforma extra multiplica el trabajo de probarla.

No es una página web dentro de una app. Los botones que veas son botones de Android de verdad.

---

## 2. Las tres piezas de cada pantalla

Todas las pantallas de Rumbo son **exactamente el mismo trío**. Sin excepciones. Cuando entiendas
una, entiendes las diez.

```
CuentasPagina.xaml        ← QUÉ se ve.  Botones, listas, textos.
CuentasPagina.xaml.cs     ← Vacío. Solo InitializeComponent().
CuentasModeloVista.cs     ← QUÉ hace.  Datos, lógica, llamadas a la API.
```

El fichero del medio existe porque MAUI lo exige, pero en Rumbo **siempre está vacío**. Si algún
día ves código ahí, es que alguien se saltó el patrón.

### Por qué separarlo así

Porque el XAML no se puede probar con una prueba automática y el C# sí. Si la lógica de «cuánto
falta para la meta» viviera dentro del botón, la única forma de comprobarla sería abriendo la
app y mirándola con los ojos.

Este patrón se llama **MVVM** (Modelo-Vista-ModeloVista). No necesitas saber el nombre para
usarlo.

---

## 3. El `BindingContext`: la idea que hay que entender sí o sí

Es **lo único** verdaderamente no obvio de MAUI. El resto es C# normal.

Cuando escribes esto en el XAML:

```xml
<Label Text="{Binding NombreDelHogar}" />
```

No estás diciendo «pon el texto `NombreDelHogar`». Estás diciendo: **«busca una propiedad que se
llame `NombreDelHogar` en mi `BindingContext`, y muestra su valor»**.

¿Y qué es el `BindingContext`? Es un objeto que la página tiene asignado. En Rumbo siempre es su
ModeloVista:

```csharp
public partial class CuentasPagina : ContentPage
{
    public CuentasPagina(CuentasModeloVista modeloVista)
    {
        InitializeComponent();

        // Esta línea conecta el XAML con el C#. Todo {Binding X} del XAML
        // buscará la propiedad X en este objeto.
        BindingContext = modeloVista;
    }
}
```

Así que:

```
{Binding NombreDelHogar}   →   modeloVista.NombreDelHogar
{Binding EstaCargando}     →   modeloVista.EstaCargando
{Binding CargarComando}    →   modeloVista.CargarComando
```

**Si un binding no muestra nada, el 90 % de las veces es una de estas tres cosas:**

1. El nombre está mal escrito (los bindings **no** dan error de compilación, fallan en silencio).
2. El `BindingContext` no se asignó.
3. La propiedad cambió pero no avisó de que había cambiado — y eso nos lleva al punto siguiente.

---

## 4. Por qué una propiedad normal no basta

Esto **no funciona** como esperarías:

```csharp
// MAL: la pantalla nunca se entera de que el valor cambió.
public string NombreDelHogar { get; set; }
```

Si asignas `NombreDelHogar = "Hogar García"`, el valor cambia en memoria… y la pantalla sigue
mostrando lo de antes. Nadie le avisó.

MAUI necesita que el objeto **grite** «¡esta propiedad cambió!». Eso lo hace una interfaz de .NET
llamada `INotifyPropertyChanged`. Para no escribir lo mismo en cada propiedad de cada pantalla,
hay una clase base:

```csharp
public class ModeloVistaBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// Asigna el valor y avisa a la pantalla, pero SOLO si de verdad cambió.
    protected bool Establecer<T>(
        ref T campo,
        T valor,
        [CallerMemberName] string? nombrePropiedad = null)
    {
        // Si el valor es el mismo, no se avisa. Avisar de cambios que no existen
        // hace que la pantalla se redibuje sin motivo.
        if (EqualityComparer<T>.Default.Equals(campo, valor))
        {
            return false;
        }

        campo = valor;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nombrePropiedad));

        return true;
    }
}
```

Y a partir de ahí, cada propiedad se escribe así:

```csharp
private string _nombreDelHogar = string.Empty;

public string NombreDelHogar
{
    get => _nombreDelHogar;
    set => Establecer(ref _nombreDelHogar, value);
}
```

Más largo que `{ get; set; }`, sí. Pero **ves lo que pasa**.

### Dos cosas que conviene entender de ese código

**`[CallerMemberName]`** es magia del compilador, no de MAUI: rellena automáticamente el nombre
del método o propiedad desde el que se llamó. Por eso no hay que escribir
`Establecer(ref _nombreDelHogar, value, "NombreDelHogar")`.

**`ref`** pasa el campo por referencia, no una copia, para que el método pueda modificarlo.

### Por qué no usamos `CommunityToolkit.Mvvm`

Existe una librería de Microsoft que reduce todo esto a un atributo:

```csharp
[ObservableProperty]
private string _nombreDelHogar = string.Empty;
```

Es menos código y es lo que usaría un equipo con experiencia. **No la usamos aquí a propósito**:
genera el código por detrás con un generador, y no ves nada de lo que ocurre. Escribimos las
sesenta líneas de `ModeloVistaBase` y `ComandoSimple` una vez, y a partir de ahí todo es C#
normal y visible.

Cuando el patrón te resulte aburrido de tan sabido, migrar al toolkit es mecánico y te lo
explico en media hora. Antes de eso, sería aprender a usar una herramienta sin saber qué hace.

---

## 5. Los comandos: qué pasa cuando pulsas un botón

En el XAML:

```xml
<Button Text="Guardar" Command="{Binding GuardarComando}" />
```

No hay ningún `Click`. El botón está enlazado a un **comando**, que es un objeto con dos cosas:
qué hacer, y si ahora mismo se puede hacer.

```csharp
public class ComandoSimple : ICommand
{
    private readonly Func<Task> _accion;
    private readonly Func<bool>? _puedeEjecutarse;
    private bool _enEjecucion;

    public event EventHandler? CanExecuteChanged;

    public ComandoSimple(Func<Task> accion, Func<bool>? puedeEjecutarse = null)
    {
        _accion = accion;
        _puedeEjecutarse = puedeEjecutarse;
    }

    /// MAUI llama a esto para decidir si el botón está habilitado.
    public bool CanExecute(object? parametro) =>
        !_enEjecucion && (_puedeEjecutarse?.Invoke() ?? true);

    public async void Execute(object? parametro)
    {
        if (!CanExecute(parametro))
        {
            return;
        }

        // El bloqueo evita el doble toque. En una pantalla de dinero esto no es
        // cosmético: dos toques rápidos en "Guardar" registrarían DOS movimientos.
        _enEjecucion = true;
        Refrescar();

        try
        {
            await _accion();
        }
        finally
        {
            // En finally: si la acción falla, el botón tiene que volver a
            // habilitarse. Si no, la pantalla se queda muerta tras el primer error.
            _enEjecucion = false;
            Refrescar();
        }
    }

    public void Refrescar() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

> **`async void`.** En C# es casi siempre un error, porque una excepción dentro de un
> `async void` no se puede capturar desde fuera y tumba el proceso. Aquí es obligatorio: la
> interfaz `ICommand` lo define así. Por eso el cuerpo está envuelto en `try/finally` y las
> acciones capturan sus propias excepciones.

---

## 6. `EstaCargando` y `MensajeError`: nada de fallos silenciosos

Cada ModeloVista de Rumbo tiene estas dos propiedades, y cada pantalla las muestra:

```csharp
public bool EstaCargando { get; set; }      // Muestra la ruedita
public string? MensajeError { get; set; }   // Muestra el error en rojo
```

El patrón se repite en cada operación:

```csharp
private async Task CargarAsync()
{
    EstaCargando = true;
    MensajeError = null;

    try
    {
        Cuentas = await _servicioCuentas.ListarAsync();
    }
    catch (Exception excepcion)
    {
        // El usuario tiene que ENTERARSE. Un catch vacío convierte un error en
        // una pantalla en blanco sin explicación, que es mucho peor.
        MensajeError = excepcion.Message;
    }
    finally
    {
        EstaCargando = false;
    }
}
```

Depurar es parte del objetivo de aprender. Una app que falla en silencio no se puede depurar.

---

## 7. Cómo habla la app con la API

Tres capas, de abajo arriba:

```
ClienteApi              ← Uno solo. Sabe la dirección del servidor y pone el token.
ServicioApiCuentas      ← Uno por módulo. Métodos con nombre: ListarAsync(), CrearAsync()...
CuentasModeloVista      ← La pantalla. Llama al servicio y muestra el resultado.
```

Los servicios son planos y aburridos a propósito:

```csharp
public class ServicioApiCuentas(ClienteApi cliente)
{
    public async Task<List<CuentaResumen>> ListarAsync() =>
        await cliente.ObtenerAsync<List<CuentaResumen>>("api/v1/cuentas");
}
```

Fíjate en `CuentaResumen`: es **la misma clase** que usa el servidor. Viene del proyecto
`Rumbo.Contratos`, que ambos referencian. Por eso, si un día cambio un campo en la API, la app
**deja de compilar** en lugar de fallar en el móvil de tu pareja un domingo.

### El `DelegatingHandler`: el único punto realmente no obvio

El token de acceso dura **15 minutos**. Si la app tuviera que comprobar la caducidad antes de
cada llamada, ese código estaría repetido en los cuarenta métodos de los servicios, y el día que
se te olvide en uno, esa pantalla fallará al azar.

La solución es interceptar **todas** las llamadas en un solo sitio:

```csharp
public class ManejadorDeToken(ServicioSesion sesion) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage peticion,
        CancellationToken cancelacion)
    {
        peticion.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await sesion.ObtenerTokenAsync());

        var respuesta = await base.SendAsync(peticion, cancelacion);

        // Si el servidor dice 401, el token caducó. Se renueva UNA vez y se
        // reintenta. Si vuelve a fallar, es que la sesión terminó de verdad.
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (await sesion.RenovarAsync())
            {
                peticion.Headers.Authorization = new AuthenticationHeaderValue(
                    "Bearer", await sesion.ObtenerTokenAsync());

                respuesta = await base.SendAsync(peticion, cancelacion);
            }
        }

        return respuesta;
    }
}
```

Un `DelegatingHandler` se mete **en medio** del `HttpClient` y la red: toda petición pasa por
él, salga de donde salga. Se registra una vez al arrancar la app y ya no hay que acordarse.

> **Renovar una sola vez.** Si reintentara en bucle, un token revocado de verdad provocaría
> llamadas infinitas contra el servidor. Un intento, y si falla se cierra la sesión.

---

## 8. Dónde se guarda el token

En **`SecureStorage`**, que en Android usa el almacén de claves del sistema:

```csharp
await SecureStorage.SetAsync("token_renovacion", token);
```

**Nunca** en `Preferences`, que es texto plano y cualquier app con acceso al almacenamiento
podría leer. Es la diferencia entre guardar la llave en una caja fuerte o debajo del felpudo.

---

## 9. Navegación con Shell

`AppShell.xaml` declara las pantallas y su menú. Navegar es indicar una ruta:

```csharp
await Shell.Current.GoToAsync("cuentas");           // Ir a una pantalla
await Shell.Current.GoToAsync("cuentas/detalle?id=" + id);  // Con parámetro
await Shell.Current.GoToAsync("..");                // Volver atrás
```

Las rutas se registran una vez en `AppShell.xaml.cs`. Si `GoToAsync` lanza una excepción que
dice que no encuentra la ruta, es que falta registrarla ahí.

---

## 10. Inyección de dependencias en `MauiProgram.cs`

Es el `Program.cs` de la app. Aquí se registra todo lo que después llega solo a los
constructores:

```csharp
servicios.AddSingleton<ServicioSesion>();       // Uno para toda la app
servicios.AddSingleton<ServicioApiCuentas>();
servicios.AddTransient<CuentasPagina>();        // Uno nuevo cada vez
servicios.AddTransient<CuentasModeloVista>();
```

- **`Singleton`**: una sola instancia mientras la app viva. Para la sesión y los servicios de
  API, que no guardan estado de pantalla.
- **`Transient`**: una nueva cada vez que se pide. Para páginas y ModelosVista, para que al
  volver a entrar en una pantalla esté limpia y no con los datos de la vez anterior.

Cuando MAUI crea `CuentasPagina`, ve que su constructor pide un `CuentasModeloVista`, lo crea, ve
que este pide un `ServicioApiCuentas`, y así hasta abajo. Tú no construyes nada a mano.

---

## 11. Cómo probarla en tu teléfono

1. En el móvil: **Ajustes → Acerca del teléfono** y toca siete veces en **Número de compilación**.
   Se activan las opciones de desarrollador.
2. **Ajustes → Opciones de desarrollador → Depuración por USB**: activar.
3. Conecta el cable y acepta el aviso de confianza que sale en el teléfono.
4. Comprueba que el ordenador lo ve:

```bash
adb devices
```

5. Ejecuta:

```bash
dotnet build src/Movil/Rumbo.Movil -t:Run -f net10.0-android
```

### El problema que te vas a encontrar seguro

**El móvil no puede llegar a `localhost`.** Para el teléfono, `localhost` es él mismo, no tu PC.

Opciones, de más simple a menos:

- **Emulador de Android**: la dirección `10.0.2.2` apunta al PC anfitrión.
- **Móvil real en la misma wifi**: usa la IP de tu PC (`ipconfig` → algo como `192.168.1.40`).
  Tendrás que permitirlo en el cortafuegos de Windows.
- **Ya desplegado en Azure**: la dirección de producción funciona sin más.

La dirección vive en un solo sitio (`ConfiguracionApi.cs`), no repartida por los servicios.

---

## 12. Los errores más comunes al empezar

| Lo que ves | Qué suele ser |
|---|---|
| Un `Label` vacío donde debería haber texto | Nombre mal escrito en el `{Binding}`. Los bindings fallan en silencio |
| La pantalla no se actualiza al cambiar un dato | La propiedad no usa `Establecer(...)`, o es `{ get; set; }` normal |
| El botón está gris y no responde | El `CanExecute` del comando devuelve `false` |
| «No se encontró la ruta» al navegar | Falta registrarla en `AppShell.xaml.cs` |
| La app no conecta con la API | `localhost` desde el móvil. Ver el punto 11 |
| Error de certificado en depuración | El certificado de desarrollo no vale para el móvil. Se usa HTTP en depuración y HTTPS en producción |
| Todo compila pero la pantalla sale en blanco | Excepción en el constructor del ModeloVista. Mira la consola de depuración |

---

## 13. Por dónde empezar a leer el código

En este orden:

1. **`ModeloVistaBase.cs`** y **`ComandoSimple.cs`** — sesenta líneas entre las dos, y son la
   base de todo lo demás.
2. **`IniciarSesionPagina.xaml`** y su ModeloVista — están comentados línea a línea. Es la
   plantilla de la que salen las demás.
3. **`ClienteApi.cs`** y **`ManejadorDeToken.cs`** — cómo se habla con el servidor.
4. Cualquier otra pantalla. Para entonces ya no habrá sorpresas.

---

## 14. Lo que esta app deliberadamente **no** hace

- **No calcula nada financiero.** Todos los números vienen de la API. Si la app calculara el
  aporte mensual de una meta por su cuenta, tarde o temprano mostraría una cifra distinta a la
  del servidor, y entonces ninguna de las dos sería creíble.
- **No guarda datos para funcionar sin conexión.** Sin internet, muestra un error claro. La
  sincronización offline es un problema difícil de verdad y no es v1.
- **No decide permisos.** Puede ocultar un botón por comodidad, pero quien dice que no es
  siempre el servidor. Un cliente móvil está en manos del usuario: cualquiera puede modificarlo.
- **No mueve dinero sola.** Igual que la API: toda operación financiera la confirma una persona.


---

## 15. Lo que ya está construido

```
src/Movil/Rumbo.Movil/
  Comun/
    ModeloVistaBase.cs          ← Léelo primero. La base de todas las pantallas.
    ComandoSimple.cs            ← Léelo segundo. Lo que pasa al pulsar un botón.
  Servicios/
    ConfiguracionApi.cs         ← La dirección del servidor, en UN solo sitio.
    AlmacenSesion.cs            ← Los tokens, en SecureStorage.
    ManejadorDeToken.cs         ← El punto no obvio. Renueva el token solo.
    ClienteApi.cs               ← El único sitio que habla con el servidor.
    ServicioSesion.cs           ← Entrar, salir, cambiar de espacio.
    ServicioApiPanel.cs         ← Un servicio por módulo. Plano y aburrido.
    ServicioApiFinanzas.cs      ← Cuentas, categorías y movimientos.
  ModelosVista/
    IniciarSesionModeloVista.cs ← LA PLANTILLA. Comentada línea a línea.
    PanelModeloVista.cs
    MovimientosModeloVista.cs
    CuentasModeloVista.cs
  Vistas/
    Rutas.cs                    ← Las rutas de navegación, en un solo sitio.
    IniciarSesionPagina.xaml    ← LA PLANTILLA del XAML. También comentada.
    IniciarSesionPagina.xaml.cs ← Dos líneas. Siempre dos líneas.
    PanelPagina.xaml / .cs
    MovimientosPagina.xaml / .cs
    CuentasPagina.xaml / .cs
  AppShell.xaml                 ← Las pestañas y las rutas.
  MauiProgram.cs                ← El arranque y el registro de dependencias.
```

### Sobre las rutas y las pestañas

Las tres pantallas del día a día viven dentro de un `TabBar` llamado `principal`, así que su
ruta completa lleva **dos partes**:

```csharp
await Shell.Current.GoToAsync("//principal/panel");   // Correcto
await Shell.Current.GoToAsync("//panel");             // Shell no la encuentra
```

Por eso existe `Vistas/Rutas.cs`: escritas como cadenas sueltas por el código, una ruta mal
tecleada no da error de compilación y falla al ejecutar con una excepción que no explica nada.

### Dos detalles del alta rápida que no son casualidad

**El formulario va arriba y siempre visible**, no escondido tras un botón «+». Registrar un
gasto es lo que se hace varias veces al día; si cuesta tres toques llegar al formulario, la
gente deja de registrarlo — y sin datos no hay análisis, que es todo el propósito de Rumbo.

**Gasto/Ingreso es un interruptor, no una lista.** El 95 % de lo que se registra a diario es un
gasto, y un toque de más cada vez acaba en lo mismo: que no se registre nada.

**Borrar pide dos toques.** Un movimiento es dinero del hogar y el botón está justo debajo de la
lista: un toque por accidente no debería poder alterar un saldo. El texto del botón cambia a
«Sí, borrar» al pedir confirmación, y se resuelve en el modelo de vista en lugar de con un
diálogo del sistema, para que la lógica siga siendo probable sin levantar una pantalla.

**Un traspaso no se corrige por una de sus patas.** Cambiar el importe de un lado sin el otro
dejaría dinero apareciendo de la nada, así que los campos se deshabilitan y se explica por qué.
Borrarlo sí funciona: se borra el traspaso **entero**, con sus dos asientos.

### Compilar y generar el APK

```bash
# Compilar
dotnet build src/Movil/Rumbo.Movil

# Ejecutar en un dispositivo o emulador conectado
dotnet build src/Movil/Rumbo.Movil -t:Run -f net10.0-android

# APK instalable (Release)
dotnet publish src/Movil/Rumbo.Movil -f net10.0-android -c Release

# Queda en:
# src/Movil/Rumbo.Movil/bin/Release/net10.0-android/publish/com.rumbo.finanzas-Signed.apk
```

> **CUIDADO CON EL APK DE DEPURACIÓN. No es instalable por sí solo.**
>
> En Debug, Android usa *despliegue rápido*: el APK **no lleva dentro el código de la
> aplicación**. Los ensamblados se empujan al dispositivo por separado cuando ejecutas con
> `-t:Run`. Por eso el APK de Debug pesa lo mismo tengas tres pantallas o nueve.
>
> Lo puedes comprobar tú mismo: un APK es un ZIP.
>
> ```bash
> python -c "import zipfile; z=zipfile.ZipFile('ruta/al.apk'); print([x for x in z.namelist() if 'Rumbo' in x])"
> ```
>
> En el de Debug no aparece nada de Rumbo. En el de Release aparece
> `lib/arm64-v8a/libaot-Rumbo.Movil.dll.so`, que es tu código ya compilado a nativo.
>
> Para **probar mientras desarrollas**, usa `-t:Run` con el móvil conectado. Para **pasarle el
> APK a alguien**, usa Release.

> **El proyecto NO está en `Rumbo.slnx` a propósito.** Si estuviera, `dotnet build` en la raíz y
> la CI del backend intentarían compilar Android, y el runner tendría que instalar el workload
> en cada ejecución: una CI de dos minutos pasaría a quince a cambio de nada. En Visual Studio
> hay que abrir el proyecto aparte.

### Las once pantallas

| Pantalla | Dónde está | Qué hace |
|---|---|---|
| Acceso | — | Entrar. Es la plantilla comentada |
| Inicio | Pestaña 1 | Todo el panel en una petición |
| Movimientos | Pestaña 2 | Lista, **alta rápida**, corregir y borrar |
| Cuentas | Pestaña 3 | Saldos, total en moneda base y **crear cuenta** |
| Más | Pestaña 4 | Menú de lo demás |
| Presupuesto | Menú Más | Partidas con barra, nivel de alerta y **añadir partida** |
| Metas | Menú Más | Progreso, **crear meta** y **aportar** |
| Viajes | Menú Más | **Crear viaje** y **¿podemos permitírnoslo?** con tres escenarios |
| Informes | Menú Más | Mes a mes y en qué se va el dinero |
| Deudas | Menú Más | Avance, **crear deuda** y **registrar pago** |
| Avisos | Menú Más | Pagos que vencen y metas alcanzadas |
| Ajustes | Menú Más | Hogar activo, versión y cerrar sesión |

**Por qué cuatro pestañas y un menú.** Lo que se usa varias veces al día va en pestañas;
planificar o consultar informes se hace de vez en cuando. Meterlo todo en pestañas dejaría
nueve iconos diminutos donde nadie acierta al primer toque.

Las pantallas del menú se abren con navegación normal (`"presupuestos"`, **sin** doble barra):
se apilan encima y el botón Atrás del teléfono devuelve al menú. Con doble barra se borraría el
historial y Atrás sacaría de la aplicación.

Además hay que **registrarlas** en `AppShell.xaml.cs` con `Routing.RegisterRoute`. Sin ese
registro, `GoToAsync` lanza una excepción diciendo que no encuentra la ruta, y ese error no
explica que falta justo esa línea.
