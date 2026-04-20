# Nou Mètode: ObtenirValorsPerClau

## ?? Resum del Canvi

S'ha afegit un nou mètode `ObtenirValorsPerClau()` per recuperar els **valors** dels paràmetres filtrant per **clau**, utilitzat específicament per obtenir les adreces d'email dels destinataris de les alertes MDO.

---

## ?? Objectiu

Proporcionar un mètode per recuperar tots els valors (no les claus) dels registres de `parametres_aplicacio` que tenen una clau específica, especialment útil per obtenir llistes de configuració com emails, centres, etc.

---

## ?? Nou Mètode Implementat

### 1. `MultiRDbService.Parametres.cs`

```csharp
/// <summary>
/// Obté tots els valors dels paràmetres actius que tenen una clau específica
/// Útil per obtenir llistes de valors com emails, centres, etc.
/// </summary>
/// <param name="clau">Clau del paràmetre (ex: EMAIL_MDO)</param>
/// <returns>Llista de valors dels paràmetres actius amb aquesta clau</returns>
public List<string> ObtenirValorsPerClau(string clau)
{
    var valors = new List<string>();

    if (string.IsNullOrWhiteSpace(clau))
    {
        Logger.Warning("Intentant obtenir valors amb clau buida");
        return valors;
    }

    string sql = @"
        SELECT valor 
        FROM parametres_aplicacio 
        WHERE clau = @clau
          AND actiu = 1
          AND dt_delete IS NULL
          AND valor IS NOT NULL
          AND valor != ''
        ORDER BY valor";

    try
    {
        using (var conn = new MySqlConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@clau", clau);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string valor = reader.GetString("valor");
                        if (!string.IsNullOrWhiteSpace(valor))
                        {
                            valors.Add(valor.Trim());
                        }
                    }
                }
            }
        }

        Logger.Info($"Carregats {valors.Count} valors per la clau '{clau}'");
    }
    catch (Exception ex)
    {
        Logger.Error($"Error obtenint valors per clau {clau}", ex);
    }

    return valors;
}
```

**Característiques:**
- Filtra per `clau` (no per categoria)
- Retorna el camp **`valor`** (no `clau`)
- Només retorna registres actius (`actiu = 1`)
- Exclou valors null o buits
- Fa `Trim()` dels valors per eliminar espais
- Ordena els resultats per valor
- Registra al log el nombre de valors carregats

---

### 2. Afegit a la Interfície `IMultiRRepository.cs`

```csharp
/// <summary>
/// Obté tots els valors dels paràmetres actius que tenen una clau específica
/// Útil per obtenir llistes de valors com emails, centres, etc.
/// </summary>
/// <param name="clau">Clau del paràmetre (ex: EMAIL_MDO)</param>
/// <returns>Llista de valors dels paràmetres actius amb aquesta clau</returns>
List<string> ObtenirValorsPerClau(string clau);
```

---

### 3. Implementat a `MultiRRepository.cs`

```csharp
public List<string> ObtenirValorsPerClau(string clau) =>
    _multiRDbService.ObtenirValorsPerClau(clau);
```

---

### 4. Utilitzat a `ProcessarMostresUseCase.cs`

**Mètode:** `EnviarEmailAlertaMDO()`

```csharp
// Obtenir destinataris d'emails de MDO des de parametres_aplicacio
// Buscar tots els registres amb clau 'EMAIL_MDO' i retornar els seus valors (les adreces d'email)
var emailsMDO = _multiRRepository.ObtenirValorsPerClau("EMAIL_MDO");
```

---

## ??? Estructura de la Base de Dades

### Configuració Correcta:

```sql
-- Estructura correcta per emails MDO
INSERT INTO parametres_aplicacio (categoria, clau, valor, dt_create, dt_update, actiu)
VALUES 
('CONFIG_GENERAL', 'EMAIL_MDO', 'mdo@hospital.cat', NOW(), NOW(), 1),
('CONFIG_GENERAL', 'EMAIL_MDO', 'urgencies@hospital.cat', NOW(), NOW(), 1),
('CONFIG_GENERAL', 'EMAIL_MDO', 'epidemiologia@hospital.cat', NOW(), NOW(), 1);
```

**Camps:**
- **categoria**: `'CONFIG_GENERAL'` (categoria de configuració)
- **clau**: `'EMAIL_MDO'` (identificador constant)
- **valor**: `'mdo@hospital.cat'` (l'adreça d'email destinatària) ?? **Aquest és el que es retorna**
- **actiu**: `1` (activat)

---

## ?? Comparació de Mètodes

### Mètode 1: `ObtenirParametresPerCategoria(categoria)`

**Entrada:** Categoria  
**Retorna:** Llista de **claus**  
**Filtra per:** `categoria = @categoria`

**Exemple:**
```csharp
var claus = ObtenirParametresPerCategoria("VR_CENTRES");
// Retorna: ["HOSPITAL SANT PAU", "HOSPITAL CLINIC", "HOSPITAL VALL HEBRON"]
```

**Taula:**
```
categoria    | clau                      | valor
-------------|---------------------------|------------------
VR_CENTRES   | HOSPITAL SANT PAU         | Centre principal
VR_CENTRES   | HOSPITAL CLINIC           | Centre secundari
VR_CENTRES   | HOSPITAL VALL HEBRON      | Centre terciari
```

---

### Mètode 2: `ObtenirParametre(categoria, clau)`

**Entrada:** Categoria + Clau  
**Retorna:** **Un únic valor** (string)  
**Filtra per:** `categoria = @categoria AND clau = @clau`

**Exemple:**
```csharp
var valor = ObtenirParametre("CONFIG_GENERAL", "SMTP_SERVER");
// Retorna: "smtp.hospital.cat"
```

**Taula:**
```
categoria       | clau        | valor
----------------|-------------|------------------
CONFIG_GENERAL  | SMTP_SERVER | smtp.hospital.cat
```

---

### Mètode 3: `ObtenirValorsPerClau(clau)` ?? **NOU**

**Entrada:** Clau  
**Retorna:** Llista de **valors**  
**Filtra per:** `clau = @clau`

**Exemple:**
```csharp
var emails = ObtenirValorsPerClau("EMAIL_MDO");
// Retorna: ["mdo@hospital.cat", "urgencies@hospital.cat", "epidemiologia@hospital.cat"]
```

**Taula:**
```
categoria       | clau       | valor
----------------|------------|---------------------------
CONFIG_GENERAL  | EMAIL_MDO  | mdo@hospital.cat
CONFIG_GENERAL  | EMAIL_MDO  | urgencies@hospital.cat
CONFIG_GENERAL  | EMAIL_MDO  | epidemiologia@hospital.cat
```

---

## ?? Flux d'Utilització per MDO

```
1. Es detecta una mostra MDO
    ?
2. EnviarEmailAlertaMDO(mostra)
    ?
3. ObtenirValorsPerClau("EMAIL_MDO")
    ?
4. SELECT valor FROM parametres_aplicacio WHERE clau = 'EMAIL_MDO' AND actiu = 1
    ?
5. Retorna: ["mdo@hospital.cat", "urgencies@hospital.cat", "epidemiologia@hospital.cat"]
    ?
6. EnviarEmailMDO(mostra, emailsMDO)
    ?
7. S'envien emails a tots els destinataris
```

---

## ?? Exemples d'Ús

### Configurar Destinataris MDO:

```sql
-- Afegir destinataris
INSERT INTO parametres_aplicacio (categoria, clau, valor, dt_create, dt_update, actiu)
VALUES 
('CONFIG_GENERAL', 'EMAIL_MDO', 'mdo@hospital.cat', NOW(), NOW(), 1),
('CONFIG_GENERAL', 'EMAIL_MDO', 'urgencies@hospital.cat', NOW(), NOW(), 1);

-- Consultar destinataris configurats
SELECT valor AS email
FROM parametres_aplicacio 
WHERE clau = 'EMAIL_MDO' 
  AND actiu = 1;
```

### Desactivar un Destinatari:

```sql
UPDATE parametres_aplicacio 
SET actiu = 0, dt_update = NOW()
WHERE clau = 'EMAIL_MDO' 
  AND valor = 'antic@hospital.cat';
```

### Afegir un Nou Destinatari:

```sql
INSERT INTO parametres_aplicacio (categoria, clau, valor, dt_create, dt_update, actiu)
VALUES ('CONFIG_GENERAL', 'EMAIL_MDO', 'nou@hospital.cat', NOW(), NOW(), 1);
```

---

## ? Validació

Per verificar que el mètode funciona correctament:

### 1. Configurar destinataris de prova:

```sql
INSERT INTO parametres_aplicacio (categoria, clau, valor, dt_create, dt_update, actiu)
VALUES 
('CONFIG_GENERAL', 'EMAIL_MDO', 'test1@hospital.cat', NOW(), NOW(), 1),
('CONFIG_GENERAL', 'EMAIL_MDO', 'test2@hospital.cat', NOW(), NOW(), 1);
```

### 2. Executar el mètode des del codi:

```csharp
var emails = _multiRRepository.ObtenirValorsPerClau("EMAIL_MDO");
// Esperem: ["test1@hospital.cat", "test2@hospital.cat"]
```

### 3. Verificar el log:

```
Carregats 2 valors per la clau 'EMAIL_MDO'
```

---

## ?? Beneficis

1. **Flexibilitat**: Permet múltiples valors per la mateixa clau
2. **Simplicitat**: Estructura clara i fàcil de mantenir
3. **Reutilitzable**: Es pot utilitzar per altres configuracions (ex: llistes de centres, serveis, etc.)
4. **Mantenible**: Els emails es gestionen a la base de dades sense recompilar
5. **Auditable**: Tots els canvis queden registrats amb `dt_create` i `dt_update`
6. **Validació**: Filtra automàticament valors null o buits

---

## ?? Estat de la Implementació

? **Completat:**
- Mètode `ObtenirValorsPerClau()` a `MultiRDbService.Parametres.cs`
- Afegit a la interfície `IMultiRRepository`
- Implementat a `MultiRRepository`
- Utilitzat a `ProcessarMostresUseCase.cs`
- Documentació SQL actualitzada
- Documentació EMAIL_ALERTA_MDO.md actualitzada
- Compilació exitosa

---

## ?? Data d'Implementació

**Data:** 2025-01-XX  
**Versió:** 1.0  
**Context:** Millora per obtenir emails MDO des de parametres_aplicacio

---

## ?? Referències

- **Codi Font:**
  - `MultirIntegraModulab\Infrastructure\Persistence\LegacyServices\MultiRDbService.Parametres.cs`
  - `MultirIntegraModulab\Domain\Interfaces\IMultiRRepository.cs`
  - `MultirIntegraModulab\Infrastructure\Persistence\Repositories\MultiRRepository.cs`
  - `MultirIntegraModulab\Application\UseCases\ProcessarMostres\ProcessarMostresUseCase.cs`

- **Documentació relacionada:**
  - `EMAIL_ALERTA_MDO.md`
  - `SQL_CONFIG_EMAIL_MDO.sql`
  - `MDO_DETECCIO.md`
