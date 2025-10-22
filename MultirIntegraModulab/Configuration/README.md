# Configuració de MultirIntegraModulab

## ?? Introducció

Aquest projecte utilitza fitxers de configuració XML per gestionar les connection strings i paràmetres de l'aplicació.

## ?? Configuració Inicial

### 1. Crear el fitxer de configuració

Copia el fitxer `App.config.example` a `App.config`:

```bash
copy App.config.example App.config
```

### 2. Editar les credencials

Obre `App.config` i substitueix els placeholders amb les teves credencials:

**Oracle:**
```xml
<add name="OracleModulab"
     connectionString="Data source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL = TCP)(HOST = [TU_HOST])(PORT = 1522))) (CONNECT_DATA = (SERVICE_NAME = [TU_SERVICE])));User Id=[TU_USUARI];Password=[TU_PASSWORD];"
     providerName="Oracle.ManagedDataAccess.Client" />
```

**MySQL:**
```xml
<add name="MySqlMultiR"
     connectionString="Server=[TU_SERVER];Database=[TU_DATABASE];Uid=[TU_USUARI];Pwd=[TU_PASSWORD];"
     providerName="MySql.Data.MySqlClient" />
```

## ?? Paràmetres de Configuració

### Càrrega de Dades

| Paràmetre | Tipus | Per defecte | Descripció |
|-----------|-------|-------------|------------|
| `DiesEndarreraCarrega` | int | 1 | Dies enrere per carregar resultats |
| `LimitResultatsProves` | int | 50 | Límit de resultats (0 = il·limitat) |
| `EntornProduccion` | bool | false | Si és true, no aplica límits |

### Logging

| Paràmetre | Tipus | Per defecte | Descripció |
|-----------|-------|-------------|------------|
| `LogDirectory` | string | "Logs" | Directori dels logs |
| `LogLevel` | string | "Info" | Nivell: Debug, Info, Warning, Error |

### Cache

| Paràmetre | Tipus | Per defecte | Descripció |
|-----------|-------|-------------|------------|
| `MinutsVigenciaCache` | int | 30 | Vigència cache de microorganismes |

### Manteniment

| Paràmetre | Tipus | Per defecte | Descripció |
|-----------|-------|-------------|------------|
| `DiesRetencioHistorial` | int | 90 | Dies de retenció de l'historial |

### Processament

| Paràmetre | Tipus | Per defecte | Descripció |
|-----------|-------|-------------|------------|
| `ProcessarMostresEnParalel` | bool | false | Processament paral·lel (experimental) |
| `MaxGrauParalelisme` | int | 4 | Grau de paral·lelisme |

## ?? Seguretat

**IMPORTANT:** 

- ?? **NO commitejis** `App.config` al control de versions
- ? Només commiteja `App.config.example` amb placeholders
- ?? Les credencials NOMÉS en local o en variables d'entorn del servidor

### .gitignore recomanat

Afegeix això al teu `.gitignore`:

```
# Configuració amb credencials
App.config

# Logs
Logs/
*.log
```

## ?? Exemples d'Ús

### Entorn de Proves (Desenvolupament)

```xml
<add key="LimitResultatsProves" value="50" />
<add key="EntornProduccion" value="false" />
<add key="LogLevel" value="Debug" />
```

### Entorn de Producció

```xml
<add key="LimitResultatsProves" value="0" />
<add key="EntornProduccion" value="true" />
<add key="LogLevel" value="Info" />
```

## ??? Ús en el Codi

La configuració s'accedeix mitjançant el singleton `AppConfiguration`:

```csharp
using MultirIntegraModulab.Configuration;

// Obtenir la instància
var config = AppConfiguration.Instance;

// Validar configuració
config.ValidarConfiguracio();

// Usar els valors
var oracleConn = config.OracleConnectionString;
var diesEnrere = config.DiesEndarreraCarrega;

// Mostrar resum
Console.WriteLine(config.ObtenirResumConfiguracio());
```

## ?? Recàrrega de Configuració

Si necessites recarregar la configuració durant l'execució (útil per testing):

```csharp
AppConfiguration.RecarregarConfiguracio();
```

## ? Troubleshooting

### Error: "La connection string 'X' no està definida"

- Verifica que `App.config` existeix
- Comprova que el nom de la connection string és correcte
- Assegura't que el fitxer es copia a la carpeta de sortida

### Error: "ConfigurationErrorsException"

- El fitxer `App.config` pot estar mal format
- Verifica que tots els tags XML estan tancats correctament
- Assegura't que no hi ha caràcters especials sense escapar

### Els canvis no s'apliquen

- Reconstrueix el projecte (Rebuild)
- Verifica que `App.config` es copia a `bin/Debug/[AppName].exe.config`
- Tanca completament Visual Studio i torna a obrir

## ?? Referències

- [ConfigurationManager Class](https://docs.microsoft.com/en-us/dotnet/api/system.configuration.configurationmanager)
- [Connection Strings Syntax](https://www.connectionstrings.com/)
