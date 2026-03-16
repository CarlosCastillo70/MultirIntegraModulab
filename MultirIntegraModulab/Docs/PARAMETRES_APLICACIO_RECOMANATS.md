# ?? PARÀMETRES D'APLICACIÓ RECOMANATS

## ?? Objectiu

Identificar i documentar tots els paràmetres de l'aplicació **MultirIntegraModulab** que es poden migrar des de `App.config` a la taula `parametres_aplicacio` per obtenir **major flexibilitat i mantenibilitat**.

---

## ?? Resum de Paràmetres

### Per Prioritat

| Prioritat | Categories | Paràmetres | Benefici |
|-----------|------------|------------|----------|
| ?? **Alta** | 3 | 25 | Impacte directe en processament |
| ?? **Mitjana** | 3 | 13 | Millora flexibilitat |
| ?? **Baixa** | 2 | 11 | Opcionals, milloren gestió |
| **TOTAL** | **8** | **~49** | **Configuració completa a BD** |

### Per Categoria

| # | Categoria | Paràmetres | Prioritat | Descripció |
|---|-----------|------------|-----------|------------|
| 1 | **CONFIG_GENERAL** | 6 | ?? Alta | Configuració central aplicació |
| 2 | **CONFIG_CARREGA** | 7 | ?? Alta | Tipus i paràmetres de càrrega |
| 3 | **MMR_CONFIG** | 5 | ?? Alta | Lògica multiresistents |
| 4 | **CONFIG_WEBSERVICE** | 3 | ?? Mitjana | URLs i configuració WS |
| 5 | **VR_CONFIG** | 3 | ?? Mitjana | Configuració virus respiratoris |
| 6 | **TIPUS_MOSTRA_EQUIV** | 4+ | ?? Mitjana | Equivalències tipus mostra (JSON) |
| 7 | **CONFIG_EMAIL** | 7 | ?? Baixa | Notificacions per email |
| 8 | **CONFIG_LOGGING** | 3 | ?? Baixa | Configuració logs |
| 9 | **VR_CENTRES** | N | ? Implementat | Centres que permeten VR |

---

## ?? PRIORITAT ALTA - Recomanat Migrar Primer

### 1. CONFIG_GENERAL (6 paràmetres)

Configuració general que afecta tota l'aplicació.

| Clau | Valor Actual | Tipus | Descripció |
|------|--------------|-------|------------|
| `LIMIT_RESULTATS_PROVES` | 0 | INT | Límit mostres/execució (0=il·limitat) |
| `WEBSERVICE_TIMEOUT` | 30 | INT | Timeout WS pacients (segons) |
| `MINUTS_VIGENCIA_CACHE` | 60 | INT | Vigència cache microorganismes |
| `DIES_RETENCIO_HISTORIAL` | 90 | INT | Retenció històric auditories |
| `PROCESSAR_EN_PARALEL` | 0 | BOOL | Activar processament paral·lel |
| `MAX_GRAU_PARALELISME` | 4 | INT | Màxim threads si paral·lel actiu |

**Beneficis**:
- ? Ajustar límits sense redeployment
- ? Optimitzar rendiment segons càrrega
- ? Gestionar cache dinàmicament

**Ús al codi**:
```csharp
int limit = _parametresHelper.ObtenirInt("CONFIG_GENERAL", "LIMIT_RESULTATS_PROVES", 0);
int timeout = _parametresHelper.ObtenirInt("CONFIG_GENERAL", "WEBSERVICE_TIMEOUT", 30);
bool paralel = _parametresHelper.ObtenirBool("CONFIG_GENERAL", "PROCESSAR_EN_PARALEL", false);
```

---

### 2. CONFIG_CARREGA (7 paràmetres)

Configuració dels diferents tipus de càrrega de dades.

| Clau | Valor Actual | Tipus | Descripció |
|------|--------------|-------|------------|
| `CARREGA_INCREMENTAL_ACTIVA` | true | BOOL | Activar càrrega incremental |
| `CARREGA_DIES_ENRERE_ACTIVA` | false | BOOL | Activar càrrega dies enrere |
| `CARREGA_RANG_DATES_ACTIVA` | false | BOOL | Activar càrrega rang dates |
| `DIES_REVISIO_SEGURETAT` | 7 | INT | Finestra validacions tardanes |
| `NOMBRE_DIES_ENRERE` | 1 | INT | Dies enrere per carregar |
| `DATA_INICI` | - | DATE | Data inici rang (dd/MM/yyyy) |
| `DATA_FI` | - | DATE | Data fi rang (dd/MM/yyyy) |

**Beneficis**:
- ? Canviar tipus de càrrega sense editar codi
- ? Ajustar finestra de seguretat segons necessitat
- ? Programar càrregues històriques fàcilment

**Ús al codi**:
```csharp
bool incremental = _parametresHelper.ObtenirBool("CONFIG_CARREGA", "CARREGA_INCREMENTAL_ACTIVA", true);
int diesRevisio = _parametresHelper.ObtenirInt("CONFIG_CARREGA", "DIES_REVISIO_SEGURETAT", 7);
int diesEnrere = _parametresHelper.ObtenirInt("CONFIG_CARREGA", "NOMBRE_DIES_ENRERE", 1);
```

---

### 3. MMR_CONFIG (5 paràmetres)

Configuració específica per lògica de multiresistents.

| Clau | Valor | Tipus | Descripció |
|------|-------|-------|------------|
| `DIES_VIGENCIA_POSITIUS_DEFAULT` | 365 | INT | Vigència per defecte si tipus mostra no té |
| `COMPORTAMENT_TIPUS_MOSTRA_DEFAULT` | 0 | INT | Comportament per defecte (0 o 1) |
| `ACTIVAR_COMPROVACIO_1` | 1 | BOOL | Activar comprovació 1 negatius |
| `ACTIVAR_COMPROVACIO_2` | 1 | BOOL | Activar comprovació 2 negatius |
| `PROCESSAR_MOSTRES_ANTIGUES` | 1 | BOOL | Processar mostres amb data anterior |

**Beneficis**:
- ? Ajustar lògica negatius segons protocol
- ? Desactivar comprovacions temporalment per proves
- ? Control fi sobre comportament MMR

**Ús al codi**:
```csharp
int diesVigencia = _parametresHelper.ObtenirInt("MMR_CONFIG", "DIES_VIGENCIA_POSITIUS_DEFAULT", 365);
bool comprovacio1 = _parametresHelper.ObtenirBool("MMR_CONFIG", "ACTIVAR_COMPROVACIO_1", true);
bool comprovacio2 = _parametresHelper.ObtenirBool("MMR_CONFIG", "ACTIVAR_COMPROVACIO_2", true);
```

---

## ?? PRIORITAT MITJANA

### 4. CONFIG_WEBSERVICE (3 paràmetres)

URLs i configuració WebServices per entorn.

| Clau | Tipus | Descripció |
|------|-------|------------|
| `URL_PRODUCCIO` | STRING | URL WS pacients (Producció) |
| `URL_PREPRODUCCIO` | STRING | URL WS pacients (Preproducció) |
| `RETRIES_MAX` | INT | Reintents màxims si falla |

**Benefici**: Canviar URL sense redeployment si hi ha canvis d'infraestructura.

---

### 5. VR_CONFIG (3 paràmetres)

Configuració específica per virus respiratoris.

| Clau | Valor | Tipus | Descripció |
|------|-------|-------|------------|
| `GENERAR_NOTA_CURS_CLINIC` | 1 | BOOL | Generar nota automàticament |
| `TIPUS_NOTA_PER_DEFECTE` | 1 | INT | Tipus nota si no definit |
| `REBUTJAR_SI_CENTRE_NO_CONFIGURAT` | 1 | BOOL | Rebutjar VR sense centre configurat |

**Benefici**: Control comportament VR sense codi.

---

### 6. TIPUS_MOSTRA_EQUIV (4+ paràmetres JSON)

Equivalències entre tipus de mostra per comprovacions.

| Clau | Valor (JSON) | Descripció |
|------|--------------|------------|
| `SANG` | `["SANG VENOSA","SANG ARTERIAL",...]` | Equivalents sang |
| `RESPIRATORI` | `["ESPUTO","EXSUDAT BRONQUIAL",...]` | Equivalents respiratori |
| `ORINA` | `["ORINA","ORINA MITJA MICCIÓ",...]` | Equivalents orina |
| `FROTIS_RECTAL` | `["FROTIS RECTAL","FROTIS ANAL"]` | Equivalents frotis |

**Benefici**: Gestionar equivalències sense codi, molt útil per Comprovació 2.

**Ús al codi**:
```csharp
var equivalentsSang = _parametresHelper.ObtenirJson<List<string>>(
    "TIPUS_MOSTRA_EQUIV", "SANG", new List<string>());

if (equivalentsSang.Contains(tipusMostra))
{
    // Són equivalents
}
```

---

## ?? PRIORITAT BAIXA (Opcionals)

### 7. CONFIG_EMAIL (7 paràmetres)

Configuració per enviar emails de notificació.

| Clau | Exemple | Descripció |
|------|---------|------------|
| `ENVIAR_EMAIL_LOG` | 0 | Enviar email amb log |
| `SMTP_SERVER` | smtp.hospital.cat | Servidor SMTP |
| `SMTP_PORT` | 587 | Port SMTP |
| `SMTP_USAR_SSL` | 1 | Utilitzar SSL/TLS |
| `EMAIL_FROM` | multir@hospital.cat | Remitent |
| `EMAILS_DESTINATARIS` | admin@hospital.cat;... | Destinataris (;) |
| `EMAIL_NOMES_ERRORS` | 1 | Només enviar si errors |

**Nota**: Pots mantenir-ho a App.config si prefereixes per seguretat (credencials).

---

### 8. CONFIG_LOGGING (3 paràmetres)

Configuració de logging.

| Clau | Valor | Descripció |
|------|-------|------------|
| `LOG_DIRECTORY` | Logs | Directori logs |
| `LOG_LEVEL` | Info | Nivell: Debug/Info/Warning/Error |
| `DIES_RETENCIO_LOGS` | 30 | Retenció fitxers log |

**Nota**: Pots mantenir-ho a App.config.

---

## ?? PARÀMETRES AVANÇATS (Futur)

### 9. NOTIFICACIONS_VR (Per microorganisme)

Emails específics per cada tipus de VR detectat.

```sql
INSERT INTO parametres_aplicacio (categoria, clau, valor, descripcio, tipus_dada)
VALUES
('NOTIFICACIONS_VR', 'SARS-CoV-2', 'epidemio@hospital.cat;prevencion@hospital.cat', 
 'Emails per notificar SARS-CoV-2', 'STRING'),
('NOTIFICACIONS_VR', 'INFLUENZA_A', 'epidemio@hospital.cat', 
 'Emails per notificar Influenza A', 'STRING');
```

**Ús**:
```csharp
if (microorganisme == "SARS-CoV-2")
{
    string emails = _parametresHelper.ObtenirString("NOTIFICACIONS_VR", "SARS-CoV-2", null);
    if (emails != null)
    {
        EnviarNotificacio(emails.Split(';'), "Nou SARS-CoV-2 detectat");
    }
}
```

---

### 10. NOTIFICACIONS_MMR (Per microorganisme)

Emails específics per MMR.

```sql
INSERT INTO parametres_aplicacio (categoria, clau, valor, descripcio, tipus_dada)
VALUES
('NOTIFICACIONS_MMR', 'MRSA', 'prevencion@hospital.cat;uci@hospital.cat', 
 'Emails per notificar MRSA', 'STRING'),
('NOTIFICACIONS_MMR', 'VRE', 'prevencion@hospital.cat', 
 'Emails per notificar VRE', 'STRING');
```

---

## ?? Comparativa: App.config vs Base de Dades

| Aspecte | App.config | Taula BD |
|---------|------------|----------|
| **Edició** | ? Requereix editar fitxer XML | ? Simple UPDATE SQL |
| **Deployment** | ? Requereix redeployment | ? No requereix redeployment |
| **Auditoria** | ? No hi ha històric | ? dt_create, dt_update, dt_delete |
| **Qui pot canviar** | ? Només IT/Desenvolupadors | ? DBAs, analistes funcionals |
| **Traçabilitat** | ? Control de versions Git | ? usuari_modificacio + timestamps |
| **Entorns** | ? Fitxers separats per entorn | ? Mateix codi, diferents valors BD |
| **Complexitat** | ? Més simple per paràmetres estàtics | ? Més complex per paràmetres dinàmics |
| **Seguretat** | ? Millor per credencials | ? Credencials a BD (encriptar) |

---

## ?? Recomanacions de Migració

### Fase 1: Paràmetres Funcionals (RECOMANAT)

? **Migrar a BD**:
- CONFIG_GENERAL (excepto credencials)
- CONFIG_CARREGA
- MMR_CONFIG
- VR_CONFIG
- VR_CENTRES (ja implementat)
- TIPUS_MOSTRA_EQUIV

**Raó**: Canvien segons criteri funcional/clínic.

### Fase 2: Configuració Tècnica (OPCIONAL)

?? **Mantenir a App.config** o migrar amb precaució:
- CONFIG_WEBSERVICE (URLs sensibles)
- CONFIG_EMAIL (credencials SMTP)
- CONFIG_LOGGING (path filesystem)

**Raó**: Més adequat a fitxer de configuració per seguretat.

### Fase 3: Notificacions (FUTUR)

?? **Afegir quan sigui necessari**:
- NOTIFICACIONS_VR
- NOTIFICACIONS_MMR

**Raó**: Funcionalitat encara no implementada.

---

## ?? Implementació: Servei Híbrid

### Opció 1: Lectura BD amb Fallback App.config

```csharp
public class ConfigurationServiceHibrid : IConfigurationService
{
    private readonly IMultiRRepository _repository;
    private readonly IConfigurationService _appConfigService;

    public int DiesEndarreraCarrega
    {
        get
        {
            // 1. Intentar BD primer
            var valorBD = _repository.ObtenirParametre("CONFIG_CARREGA", "NOMBRE_DIES_ENRERE");
            if (!string.IsNullOrEmpty(valorBD) && int.TryParse(valorBD, out int dies))
                return dies;
            
            // 2. Fallback a App.config
            return _appConfigService.DiesEndarreraCarrega;
        }
    }
    
    // ... altres propietats similars
}
```

### Opció 2: ParametresHelper (Recomanat)

```csharp
// Ja implementat per VR_CENTRES
public class ParametresHelper
{
    public int ObtenirInt(string categoria, string clau, int valorPerDefecte)
    {
        var valor = _repository.ObtenirParametre(categoria, clau);
        return int.TryParse(valor, out int resultat) ? resultat : valorPerDefecte;
    }
    
    public bool ObtenirBool(string categoria, string clau, bool valorPerDefecte)
    {
        var valor = _repository.ObtenirParametre(categoria, clau);
        if (string.IsNullOrEmpty(valor)) return valorPerDefecte;
        return valor == "1" || valor.ToUpper() == "TRUE";
    }
    
    public List<string> ObtenirJson<T>(string categoria, string clau, T valorPerDefecte)
    {
        var valor = _repository.ObtenirParametre(categoria, clau);
        if (string.IsNullOrEmpty(valor)) return valorPerDefecte;
        return JsonConvert.DeserializeObject<T>(valor);
    }
}
```

**Ús**:
```csharp
// En lloc de:
int dies = _configurationService.DiesEndarreraCarrega;

// Ara:
int dies = _parametresHelper.ObtenirInt("CONFIG_CARREGA", "NOMBRE_DIES_ENRERE", 1);
```

---

## ? Checklist de Migració

### Preparació
- [ ] Crear taula `parametres_aplicacio` (si no existeix)
- [ ] Executar script `SQL_INSERT_PARAMETRES_RECOMANATS.sql`
- [ ] Verificar inserció correcta
- [ ] Backup de App.config actual

### Fase 1: Paràmetres Funcionals
- [ ] Migrar CONFIG_GENERAL a BD
- [ ] Migrar CONFIG_CARREGA a BD
- [ ] Migrar MMR_CONFIG a BD
- [ ] Migrar VR_CONFIG a BD
- [ ] Afegir TIPUS_MOSTRA_EQUIV

### Fase 2: Adaptar Codi
- [ ] Crear/estendre ParametresHelper
- [ ] Modificar ConfigurationService (o crear híbrid)
- [ ] Actualitzar crides al codi per llegir de BD
- [ ] Tests unitaris per verificar

### Fase 3: Validació
- [ ] Executar aplicació en preproducció
- [ ] Verificar que llegeix paràmetres correctament
- [ ] Provar canvis dinàmics (UPDATE paràmetre)
- [ ] Revisar logs per errors

### Fase 4: Producció
- [ ] Executar script a producció
- [ ] Deploy nova versió
- [ ] Monitorar primera execució
- [ ] Documentar nous paràmetres per usuaris

---

## ?? Documentació Relacionada

- **`SQL_CREATE_PARAMETRES_APLICACIO.sql`** - Creació taula
- **`SQL_INSERT_PARAMETRES_RECOMANATS.sql`** - Inserció paràmetres (aquest)
- **`VIRUS_RESPIRATORIS_CENTRES.md`** - Exemple implementat (VR_CENTRES)
- **`MultiRDbService.Parametres.cs`** - Mètodes ja implementats

---

## ?? Resum Executiu

| Aspecte | Detall |
|---------|--------|
| **Paràmetres identificats** | ~49 |
| **Categories** | 8 + 1 implementada |
| **Prioritat Alta** | 18 paràmetres (CONFIG_GENERAL, CONFIG_CARREGA, MMR_CONFIG) |
| **Script SQL preparat** | ? Sí |
| **Codi necessari** | ?? Adaptar ConfigurationService o crear híbrid |
| **Temps estimat migració** | 4-8 hores |
| **Benefici principal** | Configuració dinàmica sense redeployment |

---

**Document creat**: Gener 2025  
**Versió**: 1.0  
**Estat**: ? Anàlisi Complet - Preparat per Implementar  

?? **Recomanació**: Començar per **CONFIG_GENERAL**, **CONFIG_CARREGA** i **MMR_CONFIG** (Prioritat Alta)
