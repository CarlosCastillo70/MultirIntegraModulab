# 📋 Sistema de Vigència dels Diagnòstics

## 📅 Data d'Implementació
**Data**: 22 Gener 2025  
**Versió**: 2.0 (FASE 2 Completada)  
**Estat**: ✅ **Filtre `vigent = 'S'` Implementat** (0 errors de compilació en fitxers modificats)

---

## 🎯 Objectiu

Implementar un sistema per controlar la vigència dels diagnòstics a la taula `pacients_diagnostics`, permetent:
- Marcar diagnòstics com a no vigents
- Registrar qui ha fet el canvi i quan
- Reactivar diagnòstics si cal
- Consultar l'històric de canvis

---

## 🏗️ Arquitectura de la Solució

### 1. Camps Nous a `pacients_diagnostics`

```sql
vigent CHAR(1) DEFAULT 'S' 
  -- 'S' = Vigent
  -- 'N' = No vigent

responsable_no_vigent VARCHAR(100) DEFAULT NULL
  -- Usuari que ha marcat com a no vigent

data_no_vigent DATETIME DEFAULT NULL
  -- Data quan s'ha marcat com a no vigent
```

### 2. Índexs per Rendiment

```sql
-- Índex simple per vigent
INDEX idx_vigent (vigent)

-- Índex compost per pacient i vigent
INDEX idx_npat_vigent (npat, vigent)
```

---

## 📝 Script SQL d'Implementació

**Fitxer**: `MultirIntegraModulab/Docs/SQL/ALTER_TABLE_PACIENTS_DIAGNOSTICS_VIGENCIA.sql`

```sql
-- Afegir camps
ALTER TABLE pacients_diagnostics 
ADD COLUMN vigent CHAR(1) DEFAULT 'S';

ALTER TABLE pacients_diagnostics 
ADD COLUMN responsable_no_vigent VARCHAR(100) DEFAULT NULL;

ALTER TABLE pacients_diagnostics 
ADD COLUMN data_no_vigent DATETIME DEFAULT NULL;

-- Afegir índexs
ALTER TABLE pacients_diagnostics 
ADD INDEX idx_vigent (vigent);

ALTER TABLE pacients_diagnostics 
ADD INDEX idx_npat_vigent (npat, vigent);

-- Inicialitzar registres existents
UPDATE pacients_diagnostics 
SET vigent = 'S' 
WHERE vigent IS NULL 
  AND dt_delete IS NULL;
```

---

## 💻 Implementació en Codi

### Interfície (IMultiRRepository.cs)

```csharp
/// <summary>
/// Marca un diagnòstic com a no vigent
/// </summary>
bool MarcarDiagnosticNoVigent(int diagnosticId, string responsable);

/// <summary>
/// Reactiva un diagnòstic (el torna a marcar com a vigent)
/// </summary>
bool ReactivarDiagnostic(int diagnosticId, string responsable);
```

### Implementació (MultiRDbService.Vigencia.cs)

```csharp
public bool MarcarDiagnosticNoVigent(int diagnosticId, string responsable)
{
    // Validacions
    if (diagnosticId <= 0) return false;
    if (string.IsNullOrWhiteSpace(responsable)) return false;

    // UPDATE
    UPDATE pacients_diagnostics 
    SET vigent = 'N',
        responsable_no_vigent = @responsable,
        data_no_vigent = NOW(),
        dt_update = NOW()
    WHERE id = @diagnosticId
      AND dt_delete IS NULL
      AND vigent = 'S'

    // Logging
    Logger.Info($"Diagnòstic {diagnosticId} marcat com a NO vigent per {responsable}");
}
```

---

## 🎯 Casos d'Ús

### CAS 1: Marcar Diagnòstic com a No Vigent

```csharp
// Exemple: Usuari marca manualment un diagnòstic com a no vigent
int diagnosticId = 12345;
string usuari = "dra.lopez@hospital.cat";

bool success = _multiRRepository.MarcarDiagnosticNoVigent(diagnosticId, usuari);

if (success)
{
    Console.WriteLine($"✔️ Diagnòstic {diagnosticId} marcat com a NO vigent");
}
```

**Resultat a BD**:
```
id: 12345
vigent: 'N'
responsable_no_vigent: 'dra.lopez@hospital.cat'
data_no_vigent: '2025-01-22 10:30:00'
dt_update: '2025-01-22 10:30:00'
```

### CAS 2: Consultar Diagnòstics No Vigents d'un Pacient

```sql
-- Llista de diagnòstics no vigents
SELECT 
    pd.id,
    pd.microorganisme,
    pd.mecanisme,
    pd.data_diagnostic,
    pd.responsable_no_vigent,
    pd.data_no_vigent,
    DATEDIFF(NOW(), pd.data_no_vigent) as dies_no_vigent
FROM pacients_diagnostics pd
WHERE pd.npat = '12345678'
  AND pd.vigent = 'N'
  AND pd.dt_delete IS NULL
ORDER BY pd.data_no_vigent DESC;
```

### CAS 3: Reactivar un Diagnòstic

```csharp
// Exemple: Reactivar un diagnòstic marcat per error
int diagnosticId = 12345;
string usuari = "admin@hospital.cat";

bool success = _multiRRepository.ReactivarDiagnostic(diagnosticId, usuari);

if (success)
{
    Console.WriteLine($"✔️ Diagnòstic {diagnosticId} reactivat");
}
```

**Resultat a BD**:
```
id: 12345
vigent: 'S'
responsable_no_vigent: NULL
data_no_vigent: NULL
dt_update: '2025-01-22 11:00:00'
```

### CAS 4: Estadístiques de Diagnòstics No Vigents

```sql
-- Resum per responsable
SELECT 
    pd.responsable_no_vigent,
    COUNT(*) as total_marcats,
    MIN(pd.data_no_vigent) as primera_data,
    MAX(pd.data_no_vigent) as ultima_data
FROM pacients_diagnostics pd
WHERE pd.vigent = 'N'
  AND pd.dt_delete IS NULL
GROUP BY pd.responsable_no_vigent
ORDER BY total_marcats DESC;

-- Diagnòstics marcats en l'últim mes
SELECT 
    pd.id,
    pd.npat,
    pd.microorganisme,
    pd.mecanisme,
    pd.responsable_no_vigent,
    pd.data_no_vigent
FROM pacients_diagnostics pd
WHERE pd.vigent = 'N'
  AND pd.data_no_vigent >= DATE_SUB(NOW(), INTERVAL 30 DAY)
  AND pd.dt_delete IS NULL
ORDER BY pd.data_no_vigent DESC;
```

---

## 📊 Impacte en el Sistema Actual

### 1. Creació de Diagnòstics (Automàtic - Modulab)

**NO cal modificar** el procés actual. Els diagnòstics nous es creen amb `vigent = 'S'` per defecte.

```csharp
// A CrearDiagnosticPacient - NO cal canviar res
// El camp 'vigent' tindrà valor 'S' per defecte
```

### 2. Consultes de Diagnòstics Positius

**Caldrà afegir** el filtre `vigent = 'S'` a les consultes que recuperen diagnòstics per processar negatius:

#### Abans:
```sql
SELECT DISTINCT pd.id
FROM pacients_diagnostics_mostra pdm 
INNER JOIN mostra_microorganisme mm ON pdm.id = mm.pacient_diagnostic_mostra_id
INNER JOIN pacients_diagnostics pd ON mm.pacient_diagnostic_id = pd.id
WHERE pdm.npat = 'PACIENT_SAP' 
  AND pdm.valoracio = '2'
  AND pdm.dt_delete IS NULL
  AND pd.dt_delete IS NULL;
```

#### Després:
```sql
SELECT DISTINCT pd.id
FROM pacients_diagnostics_mostra pdm 
INNER JOIN mostra_microorganisme mm ON pdm.id = mm.pacient_diagnostic_mostra_id
INNER JOIN pacients_diagnostics pd ON mm.pacient_diagnostic_id = pd.id
WHERE pdm.npat = 'PACIENT_SAP' 
  AND pdm.valoracio = '2'
  AND pdm.dt_delete IS NULL
  AND pd.dt_delete IS NULL
  AND pd.vigent = 'S';  -- ⬅️ NOU FILTRE
```

### 3. Mètodes Actualitzats

**Mètodes amb filtre `vigent = 'S'` implementat** (Fase 2 completada):

- ✅ `ObtenirDiagnosticsPositiusPacientAlgunTipusMostra`
- ✅ `ObtenirDiagnosticsPositiusVigentsTipusMostraIEquivalents`
- ✅ `ObtenirDiagnosticsPositiusPacientPerTipusMostra`
- ✅ `PacientTePositiusAlgunTipusMostra`
- ✅ `PacientTePositiusVigentsTipusMostraIEquivalents`
- ✅ `ComprovarDiagnosticExisteix` **(ACTUALITZAT)**

**Mètodes que NO necessiten el filtre**:

- `CrearDiagnosticPacient` - És un INSERT amb `vigent = 'S'` per defecte
- `ObtenirInformDiagnostic` - Obté informació específica per ID
- `ActualitzarDataDiagnosticPacientsDiagnostics` - Actualitza dates (històric)
- `ActualitzarDataDiagnosticPacientsDiagnosticsMostra` - Actualitza dates (històric)
- `EsborrarDadesMostra` - Esborra diagnòstics orfes independentment de vigència

### ✅ Mètodes Actualitzats

| # | Mètode | Fitxer | Estat |
|---|--------|--------|-------|
| 1 | `ObtenirDiagnosticsPositiusPacientAlgunTipusMostra` | MultiRDbServiceExtensions.cs | ✅ |
| 2 | `ObtenirDiagnosticsPositiusVigentsTipusMostraIEquivalents` | MultiRDbServiceExtensions.cs | ✅ |
| 3 | `ObtenirDiagnosticsPositiusPacientPerTipusMostra` | MultiRDbServiceExtensions.cs | ✅ |
| 4 | `PacientTePositiusAlgunTipusMostra` | MultiRDbService.TipusMostra.cs | ✅ |
| 5 | `PacientTePositiusVigentsTipusMostraIEquivalents` | MultiRDbService.TipusMostra.cs | ✅ |
| 6 | `ComprovarDiagnosticExisteix` | MultiRDbServiceExtensions.cs | ✅ **NOU** |

---

## 📋 Taula Resum

| # | Mètode | Tipus Consulta | Filtre `vigent='S'` | Estat |
|---|--------|----------------|---------------------|-------|
| 1 | `ObtenirDiagnosticsPositiusPacientAlgunTipusMostra` | SELECT positius | ✅ **Sí** | ✅ CORRECTE |
| 2 | `ObtenirDiagnosticsPositiusVigentsTipusMostraIEquivalents` | SELECT positius | ✅ **Sí** | ✅ CORRECTE |
| 3 | `ObtenirDiagnosticsPositiusPacientPerTipusMostra` | SELECT positius | ✅ **Sí** | ✅ CORRECTE |
| 4 | `PacientTePositiusAlgunTipusMostra` | SELECT positius | ✅ **Sí** | ✅ CORRECTE |
| 5 | `PacientTePositiusVigentsTipusMostraIEquivalents` | SELECT positius | ✅ **Sí** | ✅ CORRECTE |
| 6 | `ComprovarDiagnosticExisteix` | SELECT per associar | ✅ **Sí** | ✅ CORRECTE |
| 7 | `CrearDiagnosticPacient` | INSERT | ❌ No necessari | ✅ CORRECTE |
| 8 | `ObtenirInformDiagnostic` | SELECT per ID | ❌ No necessari | ✅ CORRECTE |
| 9 | `ActualitzarDataDiagnosticPacientsDiagnostics` | UPDATE dates | ❌ No necessari | ✅ CORRECTE |
| 10 | `ActualitzarDataDiagnosticPacientsDiagnosticsMostra` | UPDATE dates | ❌ No necessari | ✅ CORRECTE |
| 11 | `EsborrarDadesMostra` | DELETE orfes | ❌ No necessari | ✅ CORRECTE |

---

## 🎉 FASE 2 COMPLETADA: Filtre `vigent = 'S'` Implementat

**Data**: 22 Gener 2025  
**Estat**: ✅ **Implementat i Validat**

S'ha afegit el filtre `AND pd.vigent = 'S'` als següents **6 mètodes**:

---

## ✨ Resum Final

✅ **TOTES LES CONSULTES ESTAN CORRECTAMENT IMPLEMENTADES**

- **6 mètodes** que necessiten el filtre `vigent = 'S'` **ja el tenen implementat**
- **5 mètodes** que NO necessiten el filtre (INSERT, UPDATE dates, SELECT per ID) **estan correctes sense el filtre**

🎉 **El sistema de vigència està completament implementat i funcional!**
