# 📋 VALIDACIÓ FLUX VIRUS RESPIRATORIS - EXEMPLE PRÀCTIC

## 🎯 Objectiu

Validar el flux complet de processament de **Virus Respiratoris (VR)** amb un exemple real.

---

## 🧪 EXEMPLE: SARS-CoV-2 (COVID-19)

### Dades de la Mostra (Oracle)

```json
{
  "ETIQUETA_ID": "VR001234",
  "PACIENT_SAP": "12345678",
  "CIP": "TEPA1980010100",
  "DATA_RESULTAT": "2025-01-21 10:30:00",
  "DATA_VALIDACIO": "2025-01-21 14:00:00",
  "MOSTRA_DESCRIPCIO": "EXUDAT NASOFARINGI",
  "PROVA_DESCRIPCIO": "PCR SARS-CoV-2",
  "AILLAMENT_DESCRIPCIO": "SARS-CoV-2",
  "MECANISME_RESISTENCIA1_ID": null,
  "MECANISME_RESISTENCIA2_ID": null,
  "MECANISME_RESISTENCIA3_ID": null,
  "MECANISME_RESISTENCIA4_ID": null,
  "MECANISME_RESISTENCIA5_ID": null
}
```

---

## 🔄 FLUX DE PROCESSAMENT ESPERAT

### FASE 1: Validació Inicial

```
✅ VALIDACIÓ BÀSICA
   ✔️ EtiquetaId: "VR001234" → OK
   ✔️ PacientSap: "12345678" → OK
   ✔️ Té resultats: 1 resultat → OK
   ✔️ Format NPacient: 8 dígits numèrics → OK

📊 RESULTAT: MOSTRA VÀLIDA → Continuar
```

---

### FASE 2: Classificació

```
🧪 CLASSIFICACIÓ DE RESULTATS

Resultat 1:
  • Microorganisme: "SARS-CoV-2"
  • Té microorganisme: SÍ
  • És especial: NO (VR no són "especials" en el sentit MMR)
  • Mecanismes: 0
  
❓ Classificació segons lògica ANTIGA (MMR):
  → Microorganisme NO especial + 0 mecanismes = NEGATIU ❌

📝 PROBLEMA DETECTAT:
   Els Virus Respiratoris NO tenen mecanismes de resistència
   però SÍ són rellevants epidemiològicament

✅ NOVA LÒGICA (després FASE 6):
   → Detectat Virus Respiratori → SEMPRE POSITIU
```

---

### FASE 3: Tipus d'Incorporació

```
🔎 DETERMINAR TIPUS D'INCORPORACIÓ

Consulta MySQL:
  SELECT * FROM pacients_diagnostics_mostra
  WHERE etiqueta = 'VR001234'
  AND dt_delete IS NULL;

Resultat: 0 files

📊 DECISIÓ: NOVA INCORPORACIÓ 🆕
```

---

### FASE 4: Comprovació Microorganismes

```
🦠 COMPROVACIÓ MICROORGANISMES

Microorganisme: "SARS-CoV-2"

Consulta MySQL:
  SELECT * FROM microorganismes
  WHERE UPPER(descripcio) = 'SARS-COV-2'
  AND actiu = 1
  AND dt_delete IS NULL;

Resultat: 
  {
    id: 145,
    codi: "SARS-CoV-2",
    descripcio: "SARS-CoV-2",
    especial: 0,
    tipus: 'R',  ← VIRUS RESPIRATORI
    actiu: 1
  }

✅ Microorganisme EXISTEIX
✅ Tipus: 'R' → VIRUS RESPIRATORI
📝 Classificat com VR
```

---

### FASE 5: Comprovació Mecanismes

```
🛡️ COMPROVACIÓ MECANISMES DE RESISTÈNCIA

Mecanismes a comprovar: 0 (cap)

✅ No té mecanismes → OK (normal per VR)
✅ No hi ha combinacions prohibides
✅ Continuar processament
```

---

### FASE 6: PUNT DE BIFURCACIÓ ⚡

```
╔═══════════════════════════════════════════════════════════════╗
║  🔬 DETERMINANT TIPUS DE MICROORGANISME                       ║
╚═══════════════════════════════════════════════════════════════╝

Per cada resultat de la mostra:
  Resultat 1:
    • Microorganisme: "SARS-CoV-2"
    • Consultar tipus a BD...
    
    SELECT tipus 
    FROM microorganismes
    WHERE descripcio = 'SARS-CoV-2';
    
    Resultat: tipus = 'R'
    
    ✅ VIRUS RESPIRATORI DETECTAT!

📊 DECISIÓ: Mostra VR → Activar FLUX VIRUS RESPIRATORI
```

---

### FLUX VR: Processar Mostra Virus Respiratori

```
╔═══════════════════════════════════════════════════════════════╗
║  🦠 FLUX VIRUS RESPIRATORI ACTIVAT                            ║
╚═══════════════════════════════════════════════════════════════╝

🔍 LOG ESPERAT:
   "🦠 FLUX VIRUS RESPIRATORI activat"
   "  Microorganisme: SARS-CoV-2"
   "  Mecanisme: (cap - VR no tenen mecanismes)"
   "  Tipus: VR → SEMPRE positiu"

📝 CARACTERÍSTIQUES VR:
   • SEMPRE positius (no hi ha VR negatius)
   • NO tenen mecanismes de resistència
   • SEMPRE s'incorporen (sense comprovacions)
   • Processament simplificat

───────────────────────────────────────────────────────────────

🏥 PROCESSAR PACIENT

1️⃣ Comprovar existència pacient:
   SELECT * FROM pacients
   WHERE npat = '12345678';
   
   Resultat: NO EXISTEIX
   
2️⃣ Consultar WebService:
   http://10.80.160.178/flamma/ws/consultaPacient/...
   
   Resposta:
   {
     "NHC": "0012345678",
     "CIP": "TEPA1980010100",
     "APELLIDO1": "TEST",
     "APELLIDO2": "PACIENT",
     "NOMBRE": "VR",
     "DNAIX": "1980-01-01",
     "SEXE": "H"
   }
   
3️⃣ Crear pacient:
   INSERT INTO pacients
   (npat, nom, cognom1, cognom2, cip, dt_create, usuari)
   VALUES
   ('12345678', 'VR', 'TEST', 'PACIENT', 'TEPA1980010100', NOW(), 'MODULAB');
   
   ✅ Pacient creat

───────────────────────────────────────────────────────────────

🧬 PROCESSAR DIAGNOSTIC

Microorganisme: "SARS-CoV-2"
Mecanisme: NULL (VR no tenen mecanismes)

4️⃣ Comprovar diagnostic existent:
   SELECT id FROM pacients_diagnostics
   WHERE npat = '12345678'
   AND microorganisme = 'SARS-CoV-2'
   AND mecanisme IS NULL  ← Important: NULL per VR
   AND dt_delete IS NULL;
   
   Resultat: NO EXISTEIX
   
5️⃣ Crear diagnostic:
   INSERT INTO pacients_diagnostics
   (npat, microorganisme, mecanisme, tipus_mecanisme, 
    usuari, consolidat, data_ingres, data_alta)
   VALUES
   ('12345678', 'SARS-CoV-2', NULL, NULL,
    'MODULAB', 'N', '9999-12-31', '9999-12-31');
   
   ✅ Diagnostic creat: ID = 5001

───────────────────────────────────────────────────────────────

📋 PROCESSAR MOSTRA DIAGNÒSTIC

6️⃣ Comprovar mostra diagnòstic:
   SELECT id FROM pacients_diagnostics_mostra
   WHERE npat = '12345678'
   AND data_mostra = '2025-01-21'
   AND tipus_mostra_m = 'EXUDAT NASOFARINGI';
   
   Resultat: NO EXISTEIX
   
7️⃣ Crear mostra diagnòstic:
   INSERT INTO pacients_diagnostics_mostra
   (npat, data_mostra, tipus_mostra_m, tipus_prova, 
    etiqueta, data_resultat, data_validacio,
    valoracio, estat_integracio_m, 
    microorganisme_mecanisme_captat,
    usuari, consolidat)
   VALUES
   ('12345678', '2025-01-21', 'EXUDAT NASOFARINGI', 'PCR SARS-CoV-2',
    'VR001234', '2025-01-21 10:30:00', '2025-01-21 14:00:00',
    '2', 'V',  ← SEMPRE '2' (positiu) i 'V' (validat)
    'SARS-CoV-2',  ← Sol microorganisme, sense mecanisme
    'MODULAB', 'N');
   
   ✅ Mostra diagnòstic creada: ID = 7001

───────────────────────────────────────────────────────────────

🔗 PROCESSAR MOSTRA_MICROORGANISME

8️⃣ Comprovar relació:
   SELECT * FROM mostra_microorganisme
   WHERE pacient_diagnostic_id = 5001
   AND pacient_diagnostic_mostra_id = 7001;
   
   Resultat: NO EXISTEIX
   
9️⃣ Crear relació:
   INSERT INTO mostra_microorganisme
   (pacient_diagnostic_id, pacient_diagnostic_mostra_id)
   VALUES
   (5001, 7001);
   
   ✅ Relació creada

───────────────────────────────────────────────────────────────

🏷️ PROCESSAR TIPUS DE MOSTRA

🔟 Comprovar tipus de mostra:
   SELECT * FROM tipusmostra_m
   WHERE UPPER(codi) = 'EXUDAT NASOFARINGI'
   AND actiu = 1;
   
   Resultat: NO EXISTEIX
   
1️⃣1️⃣ Crear tipus de mostra:
   INSERT INTO tipusmostra_m
   (codi, descripcio, actiu, comportament, dies_vigencia_positiu)
   VALUES
   ('EXUDAT NASOFARINGI', 'EXUDAT NASOFARINGI', 1, 0, 455);
   
   ✅ Tipus de mostra creat

───────────────────────────────────────────────────────────────

🧪 PROCESSAR TIPUS DE PROVA

1️⃣2️⃣ Comprovar tipus de prova:
   SELECT * FROM tipusprova
   WHERE UPPER(codi) = 'PCR SARS-COV-2'
   AND actiu = 1;
   
   Resultat: NO EXISTEIX
   
1️⃣3️⃣ Crear tipus de prova:
   INSERT INTO tipusprova
   (codi, descripcio, actiu, comportament)
   VALUES
   ('PCR SARS-CoV-2', 'PCR SARS-CoV-2', 1, 0);
   
   ✅ Tipus de prova creat

───────────────────────────────────────────────────────────────

📅 ACTUALITZAR DATES

1️⃣4️⃣ Actualitzar data_diagnostic (pacients_diagnostics):
   UPDATE pacients_diagnostics
   SET data_diagnostic = (
     SELECT MIN(pdm.data_mostra)
     FROM mostra_microorganisme mm
     JOIN pacients_diagnostics_mostra pdm 
       ON mm.pacient_diagnostic_mostra_id = pdm.id
     WHERE mm.pacient_diagnostic_id = 5001
   )
   WHERE id = 5001;
   
   ✅ Data diagnostic actualitzada: 2025-01-21

1️⃣5️⃣ Actualitzar data_diagnostic (pacients_diagnostics_mostra):
   UPDATE pacients_diagnostics_mostra pdm
   SET data_diagnostic = (
     SELECT MIN(pdm_sub.data_mostra)
     FROM pacients_diagnostics_mostra pdm_sub
     JOIN mostra_microorganisme mm 
       ON mm.pacient_diagnostic_mostra_id = pdm_sub.id
     WHERE mm.pacient_diagnostic_id = 5001
   )
   WHERE pdm.id = 7001;
   
   ✅ Data diagnostic mostra actualitzada: 2025-01-21

───────────────────────────────────────────────────────────────

📝 AUDITORIA

1️⃣6️⃣ Crear auditoria:
   INSERT INTO auditoria_integracio_modulab
   (etiqueta_id, pacient_sap, data_mostra, data_resultat,
    data_validacio, microorganisme, tipus_mostra, resultat)
   VALUES
   ('VR001234', '12345678', '2025-01-21', '2025-01-21 10:30:00',
    '2025-01-21 14:00:00', 'SARS-CoV-2', 'EXUDAT NASOFARINGI', 'OK');
   
   ✅ Auditoria creada

───────────────────────────────────────────────────────────────

✅ PROCESSAMENT VR COMPLETAT
```

---

## 📊 RESULTAT FINAL ESPERAT

### Resum de Processament

```
📊 RESUM D'EXECUCIÓ - VIRUS RESPIRATORI
══════════════════════════════════════════════════════
📥 Total mostres llegides:        1
✅ Total processades:             1
   └─ 🦠 VR (Virus Respiratori):  1
      └─ SARS-CoV-2:              1
❌ No incorporades:                0
🚫 Errors:                         0
⏱️  Temps d'execució:              3.2 segons
══════════════════════════════════════════════════════

📝 DETALL PROCESSAMENT:
   • Pacients creats: 1
   • Diagnòstics creats: 1
   • Mostres creades: 1
   • Relacions creades: 1
   • Tipus mostra creats: 1
   • Tipus prova creats: 1
   • Auditories OK: 1
```

---

### Dades a MySQL Resultants

#### 1. pacients
```sql
SELECT * FROM pacients WHERE npat = '12345678';

+----+----------+------+----------+----------+------------+------+
| id | npat     | nom  | cognom1  | cognom2  | cip        | sexe |
+----+----------+------+----------+----------+------------+------+
| 1  |12345678  | VR   | TEST     | PACIENT  |TEPA19...   | H    |
+----+----------+------+----------+----------+------------+------+
```

#### 2. pacients_diagnostics
```sql
SELECT * FROM pacients_diagnostics 
WHERE npat = '12345678';

+------+----------+---------------+----------------+-----------+
| id   | npat     | microorganisme| mecanisme      | data_diag |
+------+----------+---------------+----------------+-----------+
| 5001 |12345678  | SARS-CoV-2    | NULL           |2025-01-21 |
+------+----------+---------------+----------------+-----------+
```

#### 3. pacients_diagnostics_mostra
```sql
SELECT * FROM pacients_diagnostics_mostra 
WHERE etiqueta = 'VR001234';

+------+----------+------------+--------------------+----------+------------+
| id   | npat     | data_mostra| tipus_mostra_m     | etiqueta | valoracio  |
+------+----------+------------+--------------------+----------+------------+
| 7001 |12345678  | 2025-01-21 | EXUDAT NASOFARINGI | VR001234 | 2          |
+------+----------+------------+--------------------+----------+------------+
```

#### 4. mostra_microorganisme
```sql
SELECT * FROM mostra_microorganisme 
WHERE pacient_diagnostic_id = 5001;

+----+--------------------------+------------------------------------+
| id | pacient_diagnostic_id    | pacient_diagnostic_mostra_id       |
+----+--------------------------+------------------------------------+
| 1  | 5001                     | 7001                               |
+----+--------------------------+------------------------------------+
```

#### 5. auditoria_integracio_modulab
```sql
SELECT * FROM auditoria_integracio_modulab 
WHERE etiqueta_id = 'VR001234';

+----+----------+------------+---------------+--------------------+----------+
| id | etiqueta | npat       | microorganisme| tipus_mostra       | resultat |
+----+----------+------------+---------------+--------------------+----------+
| 1  | VR001234 | 12345678   | SARS-CoV-2    | EXUDAT NASOFARINGI | OK       |
+----+----------+------------+---------------+--------------------+----------+
```

---

## 🔍 LOGS ESPERATS (Amb Indentació)

```
2025-01-21 15:00:00 INFO : ▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄
2025-01-21 15:00:00 INFO :  Processant mostra 1 de 1
2025-01-21 15:00:00 INFO :  Pacient: 12345678 - Etiqueta: VR001234
2025-01-21 15:00:00 INFO : ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀

2025-01-21 15:00:00 INFO :   ✅ Mostra VR001234 vàlida

2025-01-21 15:00:00 INFO :   🧪 Mostra classificada: 1 POSITIU

2025-01-21 15:00:00 INFO :   🔎 Tipus incorporació: NOVA

2025-01-21 15:00:00 INFO :   🦠 Comprovant microorganismes
2025-01-21 15:00:00 INFO :     Microorganisme: SARS-CoV-2 → EXISTEIX (tipus: VR)

2025-01-21 15:00:00 INFO :   🛡️ Comprovant mecanismes
2025-01-21 15:00:00 INFO :     Cap mecanisme (normal per VR)

2025-01-21 15:00:00 INFO :   🔬 Determinant tipus de microorganisme...
2025-01-21 15:00:00 INFO :     🦠 VIRUS RESPIRATORI detectat: 'SARS-CoV-2'
2025-01-21 15:00:00 INFO :     ➡️ La mostra es processarà com a VIRUS RESPIRATORI

2025-01-21 15:00:00 INFO :   🦠 FLUX VIRUS RESPIRATORI activat

2025-01-21 15:00:00 INFO :     🏥 Processant pacient 12345678
2025-01-21 15:00:00 INFO :       Pacient NO existeix → Consultant WebService
2025-01-21 15:00:00 INFO :       ✅ Pacient creat

2025-01-21 15:00:00 INFO :     🧬 Processant resultat VR
2025-01-21 15:00:00 INFO :       Microorganisme: SARS-CoV-2
2025-01-21 15:00:00 INFO :       Mecanisme: (cap)
2025-01-21 15:00:00 INFO :       Tipus: VR → SEMPRE positiu

2025-01-21 15:00:00 INFO :       💾 Creant diagnostic...
2025-01-21 15:00:00 INFO :         ✅ Diagnostic creat: ID 5001

2025-01-21 15:00:00 INFO :       💾 Creant mostra diagnòstic...
2025-01-21 15:00:00 INFO :         ✅ Mostra diagnòstic creada: ID 7001
2025-01-21 15:00:00 INFO :         Valoració: 2 (POSITIU)
2025-01-21 15:00:00 INFO :         Estat: V (VALIDAT)

2025-01-21 15:00:00 INFO :       🔗 Creant relació mostra_microorganisme...
2025-01-21 15:00:00 INFO :         ✅ Relació creada

2025-01-21 15:00:00 INFO :       📅 Actualitzant dates diagnòstic...
2025-01-21 15:00:00 INFO :         ✅ Data diagnòstic: 2025-01-21

2025-01-21 15:00:00 INFO :       📝 Auditoria: OK

2025-01-21 15:00:00 INFO :   ✅ Mostra VR processada correctament

2025-01-21 15:00:01 INFO : 
2025-01-21 15:00:01 INFO : ========================================
2025-01-21 15:00:01 INFO : Processament finalitzat
2025-01-21 15:00:01 INFO : ========================================
2025-01-21 15:00:01 INFO : S'han processat: 1 mostres
2025-01-21 15:00:01 INFO : Noves -> 1
2025-01-21 15:00:01 INFO : Positives -> 1 (VR: 1)
2025-01-21 15:00:01 INFO : Errors -> 0
2025-01-21 15:00:01 INFO : Durada : 3.2s
2025-01-21 15:00:01 INFO : ========================================
```

---

## ✅ CRITERIS DE VALIDACIÓ

### ✔️ El processament és correcte si:

1. **Detecció VR**: Sistema detecta que SARS-CoV-2 és tipus 'R'
2. **Bifurcació**: Activa flux VR (no MMR)
3. **Sempre positiu**: Valoració = '2' (independentment de mecanismes)
4. **Mecanisme NULL**: No intenta buscar mecanismes
5. **Incorporació**: SEMPRE s'incorpora (sense comprovacions de comportament)
6. **Pacient**: Crea/obtén pacient correctament
7. **Diagnostic**: Crea diagnostic amb mecanisme = NULL
8. **Mostra**: Crea mostra diagnòstic amb valoració = '2'
9. **Relacions**: Crea mostra_microorganisme correctament
10. **Auditoria**: Genera auditoria amb codi 'OK'
11. **Resum**: Comptabilitza com a positiu VR
12. **Logs**: Mostra "FLUX VIRUS RESPIRATORI activat"

---

## ❌ ERRORS A EVITAR

### 🚫 Errors que NO han de passar:

1. ❌ **Classificar VR com negatiu** (per no tenir mecanismes)
2. ❌ **Buscar mecanismes** per VR
3. ❌ **Aplicar comprovacions de comportament** per negatius
4. ❌ **Intentar crear/buscar mecanismes** (han de ser NULL)
5. ❌ **No incorporar VR** per no complir comprovacions
6. ❌ **Activar flux MMR** en lloc de flux VR
7. ❌ **Crear micro_mecanisme_mostra** per VR (no n'hi ha)
8. ❌ **Error per mecanisme NULL** a les queries

---

## 🎯 CASOS EXTREMS A PROVAR

### Cas 1: VR amb Multiple Resultats

```json
{
  "ETIQUETA_ID": "VR001235",
  "Resultats": [
    {
      "AILLAMENT_DESCRIPCIO": "SARS-CoV-2",
      "MECANISMES": []
    },
    {
      "AILLAMENT_DESCRIPCIO": "Influenza A",
      "MECANISMES": []
    }
  ]
}
```

**Esperat**: 
- 2 diagnòstics creats (un per cada VR)
- Ambdós amb mecanisme = NULL
- Ambdós valoració = '2'

---

### Cas 2: VR Repetit

```json
{
  "ETIQUETA_ID": "VR001234",  // Mateix que abans
  "DATA_RESULTAT": "2025-01-21 10:30:00",  // Iguals
  "DATA_VALIDACIO": "2025-01-21 14:00:00"  // Iguals
}
```

**Esperat**:
- Tipus: REPETIDA
- Auditoria: EMCR
- No processar

---

### Cas 3: Mostra Mixta (VR + MMR)

```json
{
  "ETIQUETA_ID": "MIX001236",
  "Resultats": [
    {
      "AILLAMENT_DESCRIPCIO": "SARS-CoV-2",  // VR
      "MECANISMES": []
    },
    {
      "AILLAMENT_DESCRIPCIO": "Escherichia coli",  // MMR
      "MECANISMES": ["BLEE"]
    }
  ]
}
```

**Esperat**:
- Resultat 1 → Flux VR
- Resultat 2 → Flux MMR
- Mostra classificada: MIXTA
- 2 diagnòstics creats (1 VR + 1 MMR)

---

## 📚 REFERÈNCIES

- **Fase 3 Documentació**: `VIRUS_RESPIRATORIS_FASE_3.md`
- **Use Case VR**: `ProcessarMostraVirusRespiratoriUseCase.cs`
- **Enum TipusMicroorganisme**: `Domain/Enums/TipusMicroorganisme.cs`
- **Repository**: `MultiRDbService.TipusMicroorganisme.cs`

---

**Document creat**: Gener 2025  
**Versió**: 1.0  
**Estat**: ✅ Preparat per validació  
**Tipus**: Exemple pràctic + Criteris de validació

