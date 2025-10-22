# Refactorització: Extracció de Configuració

## ?? Resum dels Canvis

S'ha completat la refactorització per extreure tota la configuració hardcoded a fitxers de configuració externs, seguint les millors pràctiques de Clean Architecture.

## ? Fitxers Creats

### 1. **App.config** 
Fitxer principal de configuració amb:
- Connection strings per Oracle i MySQL
- Paràmetres de l'aplicació (appSettings)
- Configuració de runtime

### 2. **Configuration/AppConfiguration.cs**
Classe singleton strongly-typed que:
- Carrega i valida la configuració
- Proporciona accés tipat a tots els paràmetres
- Mascara credencials en els logs
- Permet validació de configuració
- Mostra resums de configuració

### 3. **App.config.example**
Plantilla per desenvolupadors amb:
- Placeholders per credencials
- Comentaris explicatius
- Valors per defecte recomanats

### 4. **Configuration/README.md**
Documentació completa sobre:
- Com configurar l'aplicació
- Paràmetres disponibles
- Exemples d'ús
- Troubleshooting

### 5. **.gitignore**
Protecció per evitar:
- Commit de credencials (App.config)
- Logs i fitxers temporals
- Binaris i build artifacts

## ?? Fitxers Modificats

### **Program.cs**
- ? **Abans:** Connection strings hardcoded
- ? **Després:** Utilitza `AppConfiguration.Instance`
- ? **Millores:**
  - Validació de configuració a l'inici
  - Mostra resum de configuració
  - Suporta mode proves/producció

## ?? Beneficis Obtinguts

### 1. **Seguretat**
- ? Credencials fora del codi font
- ? .gitignore protegeix contra commits accidentals
- ? Possibilitat de diferents configs per entorn

### 2. **Mantenibilitat**
- ? Canvis de configuració sense recompilar
- ? Configuració centralitzada i tipada
- ? Validació automàtica de paràmetres

### 3. **Flexibilitat**
- ? Fàcil canviar entre entorns (dev/test/prod)
- ? Possibilitat d'afegir nous paràmetres
- ? Configuració per desenvolupador sense conflictes

### 4. **Documentació**
- ? README amb tota la informació necessària
- ? Exemple de configuració com a plantilla
- ? Comentaris en el codi i configuració

## ?? Com Utilitzar

### Per desenvolupadors nous:

```bash
# 1. Clonar el repositori
git clone [url]

# 2. Copiar plantilla de configuració
copy App.config.example App.config

# 3. Editar App.config amb les teves credencials
notepad App.config

# 4. Executar l'aplicació
dotnet run
```

### Per canviar paràmetres:

```xml
<!-- Edita App.config -->
<add key="LimitResultatsProves" value="100" />
<add key="EntornProduccion" value="true" />
```

### Per afegir nous paràmetres:

```csharp
// 1. Afegir a App.config
<add key="NouParametre" value="valor" />

// 2. Afegir propietat a AppConfiguration.cs
public string NouParametre { get; private set; }

// 3. Carregar en CarregarConfiguracio()
NouParametre = ObtenirAppSettingString("NouParametre", "default");

// 4. Utilitzar en el codi
var valor = AppConfiguration.Instance.NouParametre;
```

## ?? Abans vs Després

### Abans:
```csharp
string oracleConnString = "Data source=...;User Id=DWGI_MDP;Password=gLesb01an;";
string mysqlConnString = "Server=zeus;Database=marsa_test;Uid=marsa;Pwd=2a0d9a8d22;";
int limitResultats = 50; // Hardcoded
```

### Després:
```csharp
var config = AppConfiguration.Instance;
config.ValidarConfiguracio();

var oracleService = new ModulabDbService(config.OracleConnectionString);
var mysqlService = new MultiRDbService(config.MySqlConnectionString);
int limitResultats = config.EntornProduccion ? 0 : config.LimitResultatsProves;
```

## ?? Proper Pas: Dependency Injection

Aquesta refactorització ha posat les bases per implementar Dependency Injection:

```csharp
// Futur:
public class TractamentResultats
{
    private readonly IConfiguration _config;
    private readonly IMultiRRepository _repository;
    private readonly ILogger _logger;
    
    public TractamentResultats(
        IConfiguration config,
        IMultiRRepository repository,
        ILogger logger)
    {
        _config = config;
        _repository = repository;
        _logger = logger;
    }
}
```

## ?? Notes Importants

1. **No commitejis App.config** - Conté credencials reals
2. **Utilitza App.config.example** - Com a plantilla per l'equip
3. **Valida sempre** - Crida `config.ValidarConfiguracio()` a l'inici
4. **Revisa logs** - El resum de configuració ajuda a debugar

## ? Checklist de Validació

- [x] App.config creat i funcional
- [x] AppConfiguration.cs implementat
- [x] Validació de configuració funciona
- [x] Program.cs utilitza la nova configuració
- [x] .gitignore protegeix App.config
- [x] README.md amb documentació
- [x] App.config.example com a plantilla
- [x] Build exitós
- [x] No hi ha credencials hardcoded al codi

## ?? Conclusió

La configuració està ara completament externalitzada i segueix les millors pràctiques de:
- **Separation of Concerns**: Configuració separada del codi
- **DRY (Don't Repeat Yourself)**: Configuració centralitzada
- **Security by Design**: Credencials fora del codi font
- **Clean Architecture**: Preparació per DI i separació de capes

Aquesta és la base per continuar amb altres refactoritzacions cap a Clean Architecture.
