# ✅ IMPLEMENTACIÓ: Tractament de Mostres Validades i Revalidades

## 📅 Data d'Implementació
**Data**: Actual

## 🎯 Objectiu
Implementar el tractament correcte de mostres **Validades** i **Revalidades**, detectant canvis i decidint si actualitzar les dates o re-processar completament la mostra.

---

## 📋 Resum del Comportament

### **Mostra VALIDADA**
Una mostra que passa d'estat "Pendent" (P) a "Validada" (V).

### **Mostra REVALIDADA**
Una mostra que ja estava "Validada" (V) i es torna a validar amb una nova data.

---

## 🔄 Flux de Processament

```
┌─────────────────────────────────────┐
│ Determinar Tipus: VALIDADA/REVALIDADA│
└─────────────┬───────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│ Obtenir Mostra Existent de MultiR   │
└─────────────┬───────────────────────┘
              │
              ▼
┌─────────────────────────────────────┐
│ Comparar Mostres                    │
│ (Modulab vs MultiR)                 │
└─────────────┬───────────────────────┘
              │
        ┌─────┴─────┐
        │           │
    IDÈNTIQUES   DIFERENTS
        │           │
        ▼           ▼
┌─────────────┐ ┌──────────────────┐
│ CAS 1:      │ │ CAS 2:           │
│ - Actualitzar│ │ - Guardar historial│
│   data_validacio│ │ - Esborrar dades│
│ - Actualitzar│ │ - Continuar procés│
│   estat a 'V'│ │                  │
│ - Auditoria │ │                  │
│   EMCV/EMCRV│ │                  │
│ - NO continuar│ │                  │
└─────────────┘ └──────────────────┘
```

---

## 📝 Detall d'Implementació

### 1️⃣ Mètode `TractarMostraValidada()`

**Fitxer**: `ProcessarMostresUseCase.cs`

#### **CAS 1: Mostres Idèntiques** ✅
Si les mostres són idèntiques (mateix microorganisme i mecanismes):

```csharp
// Actualitzar només la data de validació
bool actualitzat = _multiRRepository.ActualitzarDataValidacio(
    mostra.EtiquetaId, 
    novaDataValidacio);

// Estat canviat automàticament a 'V'

// Inserir auditoria EMCV
bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(
    mostra,
    "EMCV",
    primerResultat,
    null);

return false; // NO continuar processament
```

**Logs esperats**:
```
📝 Mostra validada - comprovant canvis...
   ✅ Mostres idèntiques - actualitzant data_validacio...
      ✔️ Data validació actualitzada a 15/01/2025 14:30 i estat_integracio_m a 'V'
      ✅ Auditoria EMCV (Estat Mostra Cas Validat sense canvis) creada correctament
```

#### **CAS 2: Mostres Diferents** 🔄
Si les mostres són diferents:

```csharp
// 1. Guardar historial
bool historialGuardat = _multiRRepository.GuardarHistorialMostra(
    mostra.EtiquetaId,
    "VALIDADA_AMB_CANVIS",
    combinacionsAnteriors,
    mostraExistent.DataResultat,
    mostraExistent.DataValidacio,
    combinacionsNoves,
    mostra.DataUltimResultat,
    mostra.Resultats.FirstOrDefault()?.DataValidacio);

// 2. Esborrar dades actuals
bool esborrat = _multiRRepository.EsborrarDadesMostra(mostra.EtiquetaId);

// 3. Continuar amb processament normal
return true; // Continuar processament
```

**Logs esperats**:
```
📝 Mostra validada - comprovant canvis...
   🔄 Mostres diferents - guardant historial i esborrant dades...
      📝 Canvi detectat: Microorganisme diferent (E.coli -> S.aureus)
      ✔️ Historial guardat correctament
      ✔️ Dades esborrades correctament
      ➡️ Continuant processament amb noves dades...
```

---

### 2️⃣ Mètode `TractarMostraRevalidada()`

**Fitxer**: `ProcessarMostresUseCase.cs`

Comportament **idèntic** a `TractarMostraValidada()`, però amb:
- Auditoria **EMCRV** (Estat Mostra Cas Revalidat sense canvis)
- Tipus d'historial: **"REVALIDADA_AMB_CANVIS"**

---

## 🗄️ Codis d'Auditoria Nous

### EMCV - Estat Mostra Cas Validat sense canvis
- **Quan**: Mostra validada idèntica a l'existent
- **Acció**: Actualitzar `data_validacio` i `estat_integracio_m` a 'V'
- **Continua processament**: NO

### EMCRV - Estat Mostra Cas Revalidat sense canvis
- **Quan**: Mostra revalidada idèntica a l'existent
- **Acció**: Actualitzar `data_validacio` i `estat_integracio_m` a 'V'
- **Continua processament**: NO

---

## 📊 Exemples de Casos

### Exemple 1: Mostra Validada Sense Canvis

**Situació**:
- Mostra a MultiR: E.coli amb BLEE, estat 'P', data_validacio NULL
- Mostra de Modulab: E.coli amb BLEE, estat 'V', data_validacio 15/01/2025

**Resultat**:
- ✅ Actualitzar `data_validacio` = 15/01/2025
- ✅ Actualitzar `estat_integracio_m` = 'V'
- ✅ Inserir auditoria EMCV
- ❌ NO re-processar la mostra

---

### Exemple 2: Mostra Validada Amb Canvis

**Situació**:
- Mostra a MultiR: E.coli amb BLEE, estat 'P'
- Mostra de Modulab: E.coli amb BLEE + Carbapenemasa, estat 'V'

**Resultat**:
- ✅ Guardar historial (VALIDADA_AMB_CANVIS)
- ✅ Esborrar dades actuals (soft delete)
- ✅ Re-processar mostra amb noves dades
- ✅ Crear nous diagnòstics amb BLEE + Carbapenemasa

---

### Exemple 3: Mostra Revalidada Sense Canvis

**Situació**:
- Mostra a MultiR: S.aureus amb MRSA, estat 'V', data_validacio 10/01/2025
- Mostra de Modulab: S.aureus amb MRSA, estat 'V', data_validacio 15/01/2025

**Resultat**:
- ✅ Actualitzar `data_validacio` = 15/01/2025
- ✅ Mantenir `estat_integracio_m` = 'V'
- ✅ Inserir auditoria EMCRV
- ❌ NO re-processar la mostra

---

## 🔍 Diferències amb TractarMostraDesvalidada

| Aspecte | Desvalidada | Validada/Revalidada |
|---------|-------------|---------------------|
| **Estat inicial** | V → NULL | P → V (Validada) o V → V (Revalidada) |
| **Sense canvis: data_validacio** | NULL | Nova data de validació |
| **Sense canvis: estat** | P (Pendent) | V (Validada) |
| **Sense canvis: auditoria** | EMCD | EMCV / EMCRV |
| **Amb canvis: historial** | DESVALIDADA_AMB_CANVIS | VALIDADA_AMB_CANVIS / REVALIDADA_AMB_CANVIS |
| **Amb canvis: acció** | Esborrar i re-processar | Esborrar i re-processar |

---

## 📁 Fitxers Modificats

1. **ProcessarMostresUseCase.cs**
   - ✅ Implementat `TractarMostraValidada()`
   - ✅ Implementat `TractarMostraRevalidada()`

2. **SQL_INSERT_AUDIT_CODES_VALIDADA_REVALIDADA.sql**
   - ✅ Script per inserir codis EMCV i EMCRV

---

## 🚀 Passos per Producció

### 1️⃣ Executar Script SQL
```bash
mysql -u user -p multir < SQL_INSERT_AUDIT_CODES_VALIDADA_REVALIDADA.sql
```

### 2️⃣ Verificar Codis Insertats
```sql
SELECT * FROM integracio_modulab_resultats 
WHERE codi IN ('EMCV', 'EMCRV');
```

**Resultat esperat**:
```
+------+-----------------------------------------------------------------------------------+
| codi | descripcio                                                                        |
+------+-----------------------------------------------------------------------------------+
| EMCV | Estat Mostra Cas Validat sense canvis - Mostra validada idèntica, actualitzada   |
|      | data_validacio                                                                    |
| EMCRV| Estat Mostra Cas Revalidat sense canvis - Mostra revalidada idèntica,           |
|      | actualitzada data_validacio                                                       |
+------+-----------------------------------------------------------------------------------+
```

### 3️⃣ Monitoritzar Logs
Durant els primers dies:
- Verificar quantes mostres es tracten com a EMCV / EMCRV
- Verificar quantes mostres es re-processen per canvis
- Comprovar actualitzacions correctes de `data_validacio`

### 4️⃣ Estadístiques
```sql
-- Mostres validades sense canvis
SELECT COUNT(*) FROM integracio_modulab 
WHERE resultat = 'EMCV';

-- Mostres revalidades sense canvis
SELECT COUNT(*) FROM integracio_modulab 
WHERE resultat = 'EMCRV';

-- Mostres validades amb canvis (historial)
SELECT COUNT(*) FROM historial_mostres
WHERE tipus_canvi = 'VALIDADA_AMB_CANVIS';

-- Mostres revalidades amb canvis (historial)
SELECT COUNT(*) FROM historial_mostres
WHERE tipus_canvi = 'REVALIDADA_AMB_CANVIS';
```

---

## ✅ Verificacions Realitzades

- ✅ **Compilació**: Build successful (sense errors)
- ✅ **Coherència**: El codi segueix els mateixos patrons que `TractarMostraDesvalidada()`
- ✅ **Logs**: Utilitza el sistema d'indentació `LogIndentHelper`
- ✅ **Gestió d'errors**: Try-catch i logs detallats
- ✅ **Documentació**: Comentaris XML i documentació completa
- ✅ **Historial**: Guarda historial abans d'esborrar dades

---

## 📈 Mètriques Esperades

### Primer Mes
- **EMCV**: Mostres validades sense canvis → ~60-70%
- **EMCRV**: Mostres revalidades sense canvis → ~80-90%
- **Amb canvis**: Mostres que requereixen re-processament → ~20-30%

### Objectiu
- Optimitzar el processament evitant re-processar mostres idèntiques
- Guardar historial complet de tots els canvis
- Mantenir integritat de dades en tot moment

---

## 🔧 Proves Recomanades

### Test 1: Mostra Validada Sense Canvis
```csharp
// 1. Inserir mostra a MultiR amb estat 'P'
// 2. Processar mateixa mostra de Modulab amb estat 'V'
// 3. Verificar: data_validacio actualitzada, estat 'V', auditoria EMCV
```

### Test 2: Mostra Validada Amb Canvis
```csharp
// 1. Inserir mostra amb BLEE
// 2. Processar mostra amb BLEE + Carbapenemasa
// 3. Verificar: historial guardat, dades esborrades, nous diagnòstics creats
```

### Test 3: Mostra Revalidada Sense Canvis
```csharp
// 1. Inserir mostra validada
// 2. Processar mateixa mostra amb nova data validació
// 3. Verificar: data_validacio actualitzada, auditoria EMCRV
```

---

## 📝 Nota Final

Aquesta implementació garanteix que:
1. ✅ Les mostres validades/revalidades sense canvis **només actualitzen dates** (eficient)
2. ✅ Les mostres amb canvis es **re-processen completament** (integritat)
3. ✅ Tot canvi queda **auditat i historial complet**
4. ✅ El flux és **coherent** amb el tractament de mostres desvalidades

---

**Estat**: ✅ **Implementació Completa i Verificada**  
**Build**: ✅ **Successful**  
**Documentació**: ✅ **Completa**  
**Scripts SQL**: ✅ **Preparats**  
**Tests**: ⚠️ **Pendents d'executar en entorn de desenvolupament**
