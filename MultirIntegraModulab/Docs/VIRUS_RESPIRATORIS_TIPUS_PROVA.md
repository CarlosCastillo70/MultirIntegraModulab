# ?? CONTROL D'INCORPORACIÓ VIRUS RESPIRATORIS PER TIPUS DE PROVA

## ?? Informació General

**Data d'Implementació**: Gener 2025  
**Versió**: 1.0  
**Estat**: ? Implementat i Build Exitós  
**Codi d'Auditoria Nou**: **TPNIVR**

---

## ?? Objectiu

Implementar un control a nivell de **tipus de prova** per determinar quins resultats de **virus respiratoris (VR)** s'han d'incorporar al sistema MultiR.

---

## ?? Problema Identificat

### Abans (Sense Control)

```
? TOTS els virus respiratoris s'incorporaven
? No es distingia entre proves específiques VR i proves generals
? Possible incorporació de VR no rellevants
```

### Després (Amb Control)

```
? Només s'incorporen VR de proves específiques
? Control granular per tipus de prova
? Evitar incorporacions no desitjades
? Auditoria completa de rebutjos
```

---

## ?? Arquitectura de la Solució

### 1?? Nou Camp a Base de Dades

**Taula**: `tipusprova`  
**Camp**: `incorpora_virus_respiratori INT(1)`  
**Valors**: `0` = NO incorporar, `1` = SÍ incorporar  
**Per defecte**: `0` (conservador)

```sql
ALTER TABLE tipusprova 
ADD COLUMN incorpora_virus_respiratori INT(1) DEFAULT 0 
COMMENT 'Indica si aquest tipus de prova permet incorporar VR (0=NO, 1=SÍ)';
```

### 2?? Nou Mètode al Repositori

**Interfície**: `IMultiRRepository.cs`

```csharp
/// <summary>
/// Comprova si un tipus de prova permet incorporar virus respiratoris
/// </summary>
/// <param name="codiProva">Codi de la prova (PROVA_DESCRIPCIO de Modulab)</param>
/// <returns>True si incorpora_virus_respiratori = 1, False en cas contrari</returns>
bool TipusProvaPermitIncorporarVirusRespiratori(string codiProva);
```

**Implementació**: `MultiRDbService.TipusProva.cs`

```csharp
public bool TipusProvaPermitIncorporarVirusRespiratori(string codiProva)
{
    // Consulta SQL:
    // SELECT incorpora_virus_respiratori 
    // FROM tipusprova 
    // WHERE UPPER(codi) = UPPER(@codiProva) 
    //   AND actiu = 1
    
    // Retorna:
    // - true si incorpora_virus_respiratori = 1
    // - false si incorpora_virus_respiratori = 0 o NULL
    // - false si no existeix el tipus de prova
}
```

### 3?? Integració al Flux VR

**Use Case**: `ProcessarMostraVirusRespiratoriUseCase.cs`

**Punt d'integració**: **FASE 0** (abans de processar pacient)

```csharp
// FASE 0: COMPROVAR TIPUS DE PROVA (NOMÉS PER VR)
string tipusProva = mostra.Resultats[0].ProvaDescripcio;

bool permitIncorporar = _multiRRepository
    .TipusProvaPermitIncorporarVirusRespiratori(tipusProva);

if (!permitIncorporar)
{
    // NO incorporar ? Auditoria TPNIVR
    foreach (var resultat in mostra.Resultats)
    {
        _multiRRepository.InserirAuditoriaIntegracioModulab(
            mostra, 
            "TPNIVR", 
            resultat);
    }
    
    return ResultatError("Tipus prova no permet incorporar VR");
}

// Tipus prova permet incorporar VR ? Continuar processament
```

---

## ?? Flux de Decisió

```
??????????????????????????????????
?  Mostra VR a processar         ?
??????????????????????????????????
            ?
            ?
  ???????????????????????
  ? Obtenir tipus prova ?
  ? (ProvaDescripcio)   ?
  ???????????????????????
            ?
            ?
?????????????????????????????????????
? Consultar tipusprova              ?
? SELECT incorpora_virus_respiratori?
? WHERE codi = ?                    ?
?????????????????????????????????????
       ?          ?
      = 1        = 0
       ?          ?
       ?          ?
  ? INCORPORAR  ? NO INCORPORAR
       ?          ?
       ?          ?
 Flux VR normal  Auditoria TPNIVR
```

---

## ?? Nou Codi d'Auditoria

### TPNIVR - Tipus Prova No Incorpora Virus Respiratori

**Significat**: El tipus de prova NO permet incorporar virus respiratoris.

**Quan es genera**:
- Mostra detectada com a VR (tipus = 'R')
- Però el tipus de prova té `incorpora_virus_respiratori = 0`

**Acció**: La mostra NO s'incorpora a MultiR

**Observacions**: 
- Per cada resultat VR de la mostra
- Permet traçabilitat completa de rebutjos

**Taula auditoria**:
```sql
INSERT INTO auditoria_integracio_modulab
(etiqueta_id, pacient_sap, microorganisme, tipus_prova, 
 tipus_mostra, data_resultat, data_validacio, resultat)
VALUES
('ETQ001', '12345678', 'SARS-CoV-2', 'CULTIU RUTINARI',
 'EXUDAT NASAL', '2025-01-21', NULL, 'TPNIVR');
```

---

## ?? Exemple Pràctic

### Cas 1: Tipus Prova Permet VR (incorpora_virus_respiratori = 1)

```
?? ENTRADA:
   • Etiqueta: VR001234
   • Pacient: 12345678
   • Tipus Prova: PCR SARS-CoV-2
   • Microorganisme: SARS-CoV-2
   • Tipus: Virus Respiratori (R)

?? COMPROVACIÓ TIPUS PROVA:
   SELECT incorpora_virus_respiratori
   FROM tipusprova
   WHERE codi = 'PCR SARS-CoV-2'
   AND actiu = 1;
   
   Resultat: incorpora_virus_respiratori = 1

? DECISIÓ: INCORPORAR
   • Tipus prova PERMET incorporar VR
   • Continuar amb flux VR normal
   
?? SORTIDA:
   ? Mostra incorporada a MultiR
   ?? Auditoria: OK / OKVR
```

### Cas 2: Tipus Prova NO Permet VR (incorpora_virus_respiratori = 0)

```
?? ENTRADA:
   • Etiqueta: VR001235
   • Pacient: 87654321
   • Tipus Prova: CULTIU RUTINARI
   • Microorganisme: Coronavirus
   • Tipus: Virus Respiratori (R)

?? COMPROVACIÓ TIPUS PROVA:
   SELECT incorpora_virus_respiratori
   FROM tipusprova
   WHERE codi = 'CULTIU RUTINARI'
   AND actiu = 1;
   
   Resultat: incorpora_virus_respiratori = 0

? DECISIÓ: NO INCORPORAR
   • Tipus prova NO permet incorporar VR
   • Generar auditoria TPNIVR
   
?? SORTIDA:
   ?? Mostra NO incorporada a MultiR
   ?? Auditoria: TPNIVR (per cada resultat VR)
   ?? Flux VR aturat
```

### Cas 3: Tipus Prova Nou (no existeix a BD)

```
?? ENTRADA:
   • Tipus Prova: PCR RESPIRATORI NOU

?? COMPROVACIÓ TIPUS PROVA:
   SELECT incorpora_virus_respiratori
   FROM tipusprova
   WHERE codi = 'PCR RESPIRATORI NOU'
   AND actiu = 1;
   
   Resultat: 0 files (no existeix)

?? ACCIÓ AUTOMÀTICA:
   1. Sistema crea tipus prova amb:
      - incorpora_virus_respiratori = 0 (per defecte)
      - actiu = 1
      - comportament = 0
   
? DECISIÓ: NO INCORPORAR (per defecte)
   • Tipus prova nou creat amb valor 0
   • Requereix configuració manual posterior
   
?? SORTIDA:
   ?? Mostra NO incorporada
   ?? Auditoria: TPNIVR
   ?? Acció: Revisar i actualitzar tipus prova si cal
```

---

## ?? Configuració Inicial

### Script SQL Recomanat

```sql
-- ========================================
-- CONFIGURACIÓ INICIAL: Tipus Prova VR
-- ========================================

-- 1. MARCAR PROVES PCR ESPECÍFIQUES PER VR
UPDATE tipusprova 
SET incorpora_virus_respiratori = 1
WHERE actiu = 1
  AND (
    UPPER(codi) LIKE '%PCR%SARS%'
    OR UPPER(codi) LIKE '%PCR%COVID%'
    OR UPPER(codi) LIKE '%PCR%INFLUENZA%'
    OR UPPER(codi) LIKE '%PCR%VIRUS RESPIRATORI%'
    OR UPPER(codi) LIKE '%PANEL RESPIRATORI%'
  );

-- 2. MARCAR PROVES GENERALS COM A NO VR
UPDATE tipusprova 
SET incorpora_virus_respiratori = 0
WHERE actiu = 1
  AND (
    UPPER(codi) LIKE '%CULTIU%'
    OR UPPER(codi) LIKE '%ANTIBIOGRAMA%'
    OR UPPER(codi) LIKE '%MICROSCOPIA%'
  );

-- 3. VERIFICAR CONFIGURACIÓ
SELECT 
    CASE incorpora_virus_respiratori
        WHEN 1 THEN '? SÍ incorpora VR'
        WHEN 0 THEN '? NO incorpora VR'
        ELSE '?? NULL (tractat com NO)'
    END as estat,
    COUNT(*) as total,
    GROUP_CONCAT(codi SEPARATOR '\n') as proves
FROM tipusprova
WHERE actiu = 1
GROUP BY incorpora_virus_respiratori;
```

---

## ?? Logs i Traces

### Log Tipus Prova Permet VR

```
?? Comprovant tipus de prova: 'PCR SARS-CoV-2'
? Tipus de prova 'PCR SARS-CoV-2' permet incorporar virus respiratoris
?? Continuant amb flux VR...
```

### Log Tipus Prova NO Permet VR

```
?? Comprovant tipus de prova: 'CULTIU RUTINARI'
?? El tipus de prova 'CULTIU RUTINARI' NO permet incorporar virus respiratoris
?? La mostra NO es processarà
?? Auditoria TPNIVR generada
```

### Log Tipus Prova Nou

```
?? Comprovant tipus de prova: 'PCR NOU'
?? Tipus prova 'PCR NOU' no existeix a BD
?? Tipus prova creat amb incorpora_virus_respiratori = 0 (per defecte)
?? El tipus de prova 'PCR NOU' NO permet incorporar virus respiratoris
?? La mostra NO es processarà
?? ACCIÓ REQUERIDA: Revisar i configurar tipus prova si cal incorporar VR
```

---

## ?? Criteris de Configuració

### ? Marcar incorpora_virus_respiratori = 1

**Proves que SÍ han d'incorporar VR**:
- PCR específics per SARS-CoV-2, COVID-19, Influenza
- Tests ràpids d'antígens per virus respiratoris
- Panells respiratoris (multiplex)
- Seqüenciació per a virus respiratoris
- Cultius virals específics per VR

**Exemples**:
```
? PCR SARS-CoV-2
? PCR Influenza A/B
? Panel Respiratori Multiplex
? Test Ràpid Antigen COVID-19
? PCR Virus Respiratori Sincitial (VRS)
```

### ? Marcar incorpora_virus_respiratori = 0

**Proves que NO han d'incorporar VR**:
- Cultius bacterians generals
- Antibiogrames
- Microscopies
- Proves sense relació amb virus respiratoris
- Proves bioquímiques generals

**Exemples**:
```
? CULTIU RUTINARI
? ANTIBIOGRAMA STANDARD
? MICROSCOPIA DIRECTA
? CULTIU D'ORINA
? HEMOCULTIU
```

---

## ?? Gestió d'Errors

### Error 1: Camp NULL

**Problema**: `incorpora_virus_respiratori` és NULL

**Comportament**: Es tracta com a `0` (NO incorporar)

**Solució**: Actualitzar manualment:
```sql
UPDATE tipusprova 
SET incorpora_virus_respiratori = 1
WHERE codi = 'PCR ESPECÍFIC';
```

### Error 2: Tipus Prova No Existeix

**Problema**: El tipus de prova no està a la taula

**Comportament**: 
1. Sistema crea tipus prova amb `incorpora_virus_respiratori = 0`
2. Mostra NO s'incorpora
3. Auditoria TPNIVR

**Solució**: 
```sql
-- Després de la primera execució, actualitzar:
UPDATE tipusprova 
SET incorpora_virus_respiratori = 1
WHERE codi = 'NOU TIPUS PROVA';
```

### Error 3: Configuració Incorrecta

**Problema**: Tipus prova marcat incorrectament

**Símptomes**:
- VR que SÍ s'han d'incorporar ? Rebutjades (TPNIVR)
- VR que NO s'han d'incorporar ? Incorporades (OKVR)

**Detecció**:
```sql
-- Revisar auditories TPNIVR
SELECT 
    tipus_prova,
    COUNT(*) as total_rebutjats,
    GROUP_CONCAT(DISTINCT microorganisme) as microorganismes
FROM auditoria_integracio_modulab
WHERE resultat = 'TPNIVR'
  AND data_resultat >= DATE_SUB(NOW(), INTERVAL 7 DAY)
GROUP BY tipus_prova
ORDER BY total_rebutjats DESC;
```

**Solució**:
```sql
-- Corregir configuració
UPDATE tipusprova 
SET incorpora_virus_respiratori = 1  -- o 0, segons calgui
WHERE codi = 'TIPUS PROVA INCORRECTE';
```

---

## ?? Monitoratge i Manteniment

### Consultes Recomanades

#### 1. Distribució de configuració
```sql
SELECT 
    CASE incorpora_virus_respiratori
        WHEN 1 THEN 'Permet VR'
        WHEN 0 THEN 'NO permet VR'
        ELSE 'NULL (tractat com NO)'
    END as configuracio,
    COUNT(*) as total_proves
FROM tipusprova
WHERE actiu = 1
GROUP BY incorpora_virus_respiratori;
```

#### 2. Proves que permeten VR
```sql
SELECT 
    codi,
    descripcio,
    incorpora_virus_respiratori
FROM tipusprova
WHERE incorpora_virus_respiratori = 1
  AND actiu = 1
ORDER BY codi;
```

#### 3. Rebutjos per tipus prova (últims 30 dies)
```sql
SELECT 
    tipus_prova,
    COUNT(*) as total_rebutjos,
    COUNT(DISTINCT etiqueta_id) as mostres_diferents,
    COUNT(DISTINCT pacient_sap) as pacients_diferents
FROM auditoria_integracio_modulab
WHERE resultat = 'TPNIVR'
  AND data_resultat >= DATE_SUB(NOW(), INTERVAL 30 DAY)
GROUP BY tipus_prova
ORDER BY total_rebutjos DESC;
```

#### 4. Tipus prova nous (no configurats)
```sql
SELECT 
    codi,
    descripcio,
    dt_create,
    incorpora_virus_respiratori
FROM tipusprova
WHERE dt_create >= DATE_SUB(NOW(), INTERVAL 7 DAY)
  AND actiu = 1
ORDER BY dt_create DESC;
```

---

## ? Checklist d'Implementació

- [x] **Camp BD afegit** (`incorpora_virus_respiratori INT(1)`)
- [x] **Índex creat** (`idx_incorpora_vr`)
- [x] **Interfície actualitzada** (`IMultiRRepository`)
- [x] **Mètode implementat** (`TipusProvaPermitIncorporarVirusRespiratori`)
- [x] **Integració al flux VR** (FASE 0)
- [x] **Codi auditoria creat** (TPNIVR)
- [x] **Build exitós** (0 errors, 0 warnings)
- [x] **Logging implementat** (amb indentació correcta)
- [x] **Script SQL preparat** (ALTER TABLE + exemples)
- [x] **Documentació completa** (aquest fitxer)
- [ ] **Execució script SQL a producció** (PENDENT)
- [ ] **Configuració inicial** (marcar proves VR = 1) (PENDENT)
- [ ] **Tests unitaris** (RECOMANAT)
- [ ] **Validació amb dades reals** (PENDENT)

---

## ?? Fitxers Creats/Modificats

### ? Creats (2 fitxers)

1. **`Docs/SQL_ALTER_TIPUSPROVA_VR.sql`**
   - Script ALTER TABLE
   - Exemples de configuració
   - Queries de verificació

2. **`Docs/VIRUS_RESPIRATORIS_TIPUS_PROVA.md`** (aquest document)

### ? Modificats (3 fitxers)

3. **`Domain/Interfaces/IMultiRRepository.cs`**
   - Afegit mètode `TipusProvaPermitIncorporarVirusRespiratori`

4. **`Infrastructure/Persistence/LegacyServices/MultiRDbService.TipusProva.cs`**
   - Implementat mètode
   - Consulta SQL
   - Logging

5. **`Infrastructure/Persistence/Repositories/MultiRRepository.cs`**
   - Delegació al servei

6. **`Application/UseCases/ProcessarMostres/ProcessarMostraVirusRespiratoriUseCase.cs`**
   - Afegida FASE 0 (comprovació tipus prova)
   - Generació auditoria TPNIVR
   - Return early si no permet incorporar

---

## ?? Beneficis del Control

### 1. ? **Granularitat**
- Control a nivell de tipus de prova
- Configuració flexible per cada prova
- Adaptable a criteris clínics canviants

### 2. ? **Seguretat**
- Evita incorporacions no desitjades
- Valor per defecte = 0 (conservador)
- Auditoria completa de rebutjos

### 3. ? **Traçabilitat**
- Codi TPNIVR específic
- Logs detallats
- Fàcil identificar proves mal configurades

### 4. ? **Mantenibilitat**
- Configuració a BD (no codi)
- Canvis sense redeployment
- Històric de configuracions

### 5. ? **Escalabilitat**
- Suporta nous tipus de prova
- Fàcil afegir nous criteris
- Compatible amb evolució del sistema

---

## ?? Pròxims Passos

### PAS 1: Executar Script SQL

```bash
mysql -u user -p multir < Docs/SQL_ALTER_TIPUSPROVA_VR.sql
```

### PAS 2: Configurar Proves VR

```sql
-- Marcar proves PCR per VR
UPDATE tipusprova 
SET incorpora_virus_respiratori = 1
WHERE UPPER(codi) LIKE '%PCR%'
  AND (
    UPPER(codi) LIKE '%SARS%'
    OR UPPER(codi) LIKE '%COVID%'
    OR UPPER(codi) LIKE '%INFLUENZA%'
  );
```

### PAS 3: Validar amb Dades Reals

1. Executar sistema amb mostres VR reals
2. Revisar auditories TPNIVR
3. Ajustar configuració si cal

### PAS 4: Monitoratge Continu

- Revisar setmanalment nous tipus prova
- Analitzar rebutjos TPNIVR
- Actualitzar configuració segons criteris clínics

---

## ?? Contacte i Suport

Per dubtes sobre aquesta funcionalitat:
- Revisar aquest document complet
- Consultar logs (`Logger.Info()`)
- Revisar taula `auditoria_integracio_modulab` (resultat = 'TPNIVR')

---

## ?? Resum Executiu

| Aspecte | Estat | Notes |
|---------|-------|-------|
| **Camp BD** | ? Preparat | Script SQL disponible |
| **Mètode Repositori** | ? Implementat | Consulta SQL + logging |
| **Integració Flux VR** | ? Completa | FASE 0 abans processament |
| **Codi Auditoria** | ? Creat | TPNIVR |
| **Build** | ? Exitós | 0 errors, 0 warnings |
| **Script SQL** | ? Preparat | Executar a producció |
| **Documentació** | ? Completa | Aquest fitxer |
| **Tests** | ? Pendent | Recomanat afegir |
| **Configuració Inicial** | ? Pendent | Marcar proves VR = 1 |

---

**Versió del Document**: 1.0  
**Data**: Gener 2025  
**Estat**: ? **IMPLEMENTACIÓ COMPLETADA** (Build Successful)  
**Pròxim Pas**: Executar script SQL i configurar proves VR

?? **CONTROL D'INCORPORACIÓ VR PER TIPUS DE PROVA IMPLEMENTAT AMB ÈXIT** ??
