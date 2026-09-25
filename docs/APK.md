# Firmar y distribuir el APK

Cómo generar el APK de Rumbo firmado con tu propia clave, instalarlo y actualizarlo. Y qué
haría falta si algún día quisieras publicarlo en Google Play.

## 1. El keystore: léelo antes de crear nada

Android identifica una aplicación por **la firma de su APK**, no por su nombre. Eso tiene una
consecuencia que conviene entender antes de seguir:

> **Si pierdes el keystore, no podrás volver a actualizar esta aplicación. Nunca.**
>
> No hay recuperación, ni soporte al que escribir, ni truco. Tendrías que crear una aplicación
> distinta, con otro identificador, y pedirle a cada persona que desinstale la anterior —
> perdiendo de paso la sesión guardada en el teléfono.

Así que, antes de generarlo: decide dónde vas a guardar la copia de seguridad. Un gestor de
contraseñas, un disco cifrado, lo que sea — pero **fuera de esta carpeta y fuera de este
ordenador**.

### Crearlo

`keytool` viene con el JDK que instaló el workload de MAUI. Si no lo encuentra el terminal,
está en `C:\Program Files\Microsoft\jdk-*\bin`.

```bash
keytool -genkeypair -v \
  -keystore rumbo.keystore \
  -alias rumbo \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000 \
  -storetype pkcs12
```

Te preguntará una contraseña y algunos datos (nombre, organización, país). Los datos dan igual
para un APK que instalas tú; la **contraseña no**.

- **`-validity 10000`** son unos 27 años. Google Play exige que la clave sea válida hasta 2033
  como mínimo, y una clave caducada no puede firmar actualizaciones.
- **`-storetype pkcs12`** es el formato estándar. El antiguo `JKS` sigue funcionando pero
  `keytool` avisa de que está obsoleto en cada uso.

### Dónde ponerlo

**Fuera del repositorio.** Por ejemplo en `C:\Users\<tú>\claves\rumbo.keystore`.

El `.gitignore` ya excluye `*.keystore` y `*.jks`, pero eso es una red de seguridad, no un
sitio donde guardarlo: un fichero ignorado sigue estando en tu disco, dentro de una carpeta que
algún día comprimes y compartes.

## 2. Generar el APK firmado

La configuración de firma vive en `Rumbo.Movil.csproj` y **solo se activa si le pasas la ruta
del keystore**. Ni la ruta ni la contraseña están en ningún fichero del repositorio.

```bash
dotnet publish src/Movil/Rumbo.Movil -f net10.0-android -c Release \
  -p:RumboKeystore="C:\Users\tu-usuario\claves\rumbo.keystore" \
  -p:RumboKeystoreAlias=rumbo \
  -p:RumboKeystorePassword="tu-contraseña"
```

El APK queda en:

```
src/Movil/Rumbo.Movil/bin/Release/net10.0-android/publish/com.rumbo.finanzas-Signed.apk
```

> **La contraseña en la línea de comandos queda en el historial del terminal.** Para uso
> personal puede valerte; si te molesta, defínela como variable de entorno de la sesión y pasa
> `-p:RumboKeystorePassword=$env:RUMBO_CLAVE`.

### Limpia antes de firmar

```bash
rm -rf src/Movil/Rumbo.Movil/bin src/Movil/Rumbo.Movil/obj
```

**La compilación incremental no vuelve a firmar.** Si ya existe un APK de una compilación
anterior, el empaquetado se considera al día y el APK se queda con la firma que tenía — la de
depuración. El comando termina bien y el archivo tiene fecha nueva.

Nos pasó mientras se escribía esta guía: el APK decía estar recién generado y su certificado
seguía siendo `CN=Android Debug`. Por eso el punto 3 no es opcional.

### Sin keystore también compila

Si omites esos parámetros, el proyecto compila igual y firma con la clave de depuración de
Android. Sirve para probar, **no para repartir**: esa clave la tiene todo el mundo, es distinta
en cada máquina, y un APK firmado con ella no puede actualizar a otro firmado con la tuya.

## 3. Comprobar que el APK es de verdad

Este paso no es opcional. Un APK es un ZIP, y verificarlo cuesta una línea:

```bash
python -c "import zipfile; z=zipfile.ZipFile('ruta/al.apk'); print([x for x in z.namelist() if 'Rumbo' in x])"
```

Tiene que aparecer `lib/arm64-v8a/libaot-Rumbo.Movil.dll.so`. Si la lista sale **vacía**,
compilaste en Debug: ese APK no lleva el código de la aplicación dentro y no arrancará
instalado a mano. (Ver D70 en `DECISIONES.md`; nos pasó.)

Y para comprobar la firma:

```bash
# apksigner viene con el SDK de Android
apksigner verify --print-certs com.rumbo.finanzas-Signed.apk
```

Debe mostrar tu certificado, no uno con `CN=Android Debug`.

## 4. Instalarlo en el teléfono

### Por cable

```bash
adb install -r com.rumbo.finanzas-Signed.apk
```

`-r` reinstala conservando los datos, que es lo que quieres al actualizar.

### Sin cable

Copia el APK al teléfono (correo, nube, lo que sea) y ábrelo desde el explorador de archivos.
Android pedirá permiso para instalar de orígenes desconocidos la primera vez.

### Si la instalación falla

| Error | Qué significa |
|---|---|
| `INSTALL_FAILED_UPDATE_INCOMPATIBLE` | El APK instalado está firmado con otra clave. Suele pasar al pasar del APK de depuración al firmado. **Desinstala la aplicación** y vuelve a instalar: perderás la sesión guardada, nada más |
| `INSTALL_FAILED_VERSION_DOWNGRADE` | Intentas instalar una versión anterior. Sube `ApplicationVersion` o desinstala |
| `INSTALL_PARSE_FAILED_NO_CERTIFICATES` | El APK no está firmado. Usaste el sin firmar (`com.rumbo.finanzas.apk` en vez del `-Signed`) |
| La app se instala pero se cierra al abrir | Casi siempre el APK de Debug instalado a mano. Ver el punto 3 |

## 5. Subir de versión

Dos números, y hacen cosas distintas:

```xml
<ApplicationDisplayVersion>1.1.0</ApplicationDisplayVersion>  <!-- Lo que ve la persona -->
<ApplicationVersion>2</ApplicationVersion>                    <!-- Lo que compara Android -->
```

`ApplicationVersion` es un entero que **tiene que subir en cada publicación**. Android se niega
a instalar encima algo con un número igual o menor. `ApplicationDisplayVersion` es el texto que
se muestra y el que la aplicación le manda al servidor.

**No hay que tocar nada más.** `ConfiguracionApi.VersionInstalada` lee la versión del propio
paquete instalado, así que no existe una constante que se pueda quedar desactualizada.

Después de publicar una versión nueva, actualiza lo que el servidor anuncia:

```bash
az webapp config appsettings set --name rumbo-api --resource-group rumbo-rg --settings \
  VersionApp__VersionRecomendada=1.1.0 \
  VersionApp__UrlDescarga="https://donde-lo-subas/rumbo-1.1.0.apk"
```

Eso hace que las aplicaciones instaladas avisen de que hay algo nuevo. **No sube
`VersionMinima`**: esa solo se toca cuando una versión antigua hace algo *incorrecto*. Obligar a
actualizar por gusto acostumbra a la gente a ignorar el aviso, y entonces el día que importa de
verdad nadie lo lee.

## 6. La dirección del servidor

Está en `Servicios/ConfiguracionApi.cs` y la decide el **compilador**, no una variable que
alguien pueda olvidarse de cambiar:

```csharp
#if DEBUG
    public const string DireccionBase = "http://10.0.2.2:5114/";
#else
    public const string DireccionBase = "https://rumbo-api.azurewebsites.net/";
#endif
```

En Debug es HTTP contra tu máquina, porque el certificado de desarrollo del PC no vale para el
teléfono. En Release es HTTPS obligatorio, que es lo único aceptable con datos financieros.

Si tu API queda en otra dirección, cámbiala en esa línea **antes** de generar el APK.

## 7. Qué haría falta para Google Play

Rumbo no está pensada para la tienda: es una aplicación para tu hogar, con registro cerrado por
invitación. Pero si algún día quisieras publicarla, esto es lo que te pedirían:

| Requisito | Estado hoy |
|---|---|
| **Formato AAB**, no APK | Cambiar a `dotnet publish -p:AndroidPackageFormat=aab`. Google firma el APK final por ti |
| **Cuenta de desarrollador** (25 USD, pago único) | Pendiente |
| **Política de privacidad** con URL pública | Pendiente. Rumbo guarda datos financieros: es obligatoria y tiene que ser real |
| **Declaración de seguridad de datos** | Habría que declarar: datos financieros, cifrados en tránsito, no compartidos con terceros, con opción de borrado |
| **`targetSdkVersion` reciente** | Lo pone el SDK de MAUI. Google exige estar a un año del último Android |
| Capturas, icono 512×512, descripción | Pendiente |
| **Revisión manual** | Las aplicaciones financieras la reciben, y suele tardar más |
| Firma gestionada por Play | Google guarda la clave de firma y tú conservas la de subida. Es más seguro que custodiarla tú |

Lo que **no** haría falta cambiar: el registro por invitación es perfectamente válido en Play.
Sí tendrías que explicar en la ficha que la aplicación no es de uso abierto, o los revisores la
rechazarán por «no se puede probar».

## 8. Automatizar la generación

Hay un workflow en `.github/workflows/movil-apk.yml`. Se dispara a mano y necesita dos secretos
en GitHub:

| Secreto | Qué es |
|---|---|
| `ANDROID_KEYSTORE_BASE64` | El keystore codificado: `base64 -w0 rumbo.keystore` |
| `ANDROID_KEYSTORE_PASSWORD` | La contraseña |

El APK queda como artefacto de la ejecución, descargable durante 90 días.

> **Piénsatelo antes de subir el keystore a GitHub**, aunque sea como secreto. Para una
> aplicación familiar, generar el APK en tu máquina es más simple y no pone la clave en ningún
> servidor ajeno. El workflow está ahí por si lo prefieres, no porque haga falta.
