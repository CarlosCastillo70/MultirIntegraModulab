# 🔢 Sistema de Desactivació per Mostres Negatives Consecutives

## 📅 Data d'Implementació
**Data**: 22 Gener 2025  
**Versió**: 1.0  
**Estat**: ✅ **Implementat i Compilat**

---

## 🎯 Objectiu

Implementar el **tercer motiu automàtic** per desactivar diagnòstics vigents: quan s'han donat un nombre suficient de **mostres negatives consecutives** per a cada tipus de mostra associat al diagnòstic.

---

## 📋 Descripció General

Aquest sistema avalua si un diagnòstic multiresistent (tipus 'M') ha acumulat prou mostres negatives consecutives sense cap positiu posterior, per considerar que el pacient ha superat la colonització/infecció.

### ⚠️ Important
- **Només aplica a diagnòstics multiresistents** (`tipus_microorganisme = 'M'`)
- **No aplica a Virus Respiratoris** (`tipus_microorganisme = 'R'`)
- Les mostres es compten **a partir de la data de diagnòstic** (`data_diagnostic`)
- **Un positiu posterior reinicia el comptador** de negatives per aquell tipus de mostra

---

## 🏗️ Arquitectura del Sistema

### 1. Fonts d'Informació (3 Fonts Acumulables)

El sistema combina 3 fonts per determinar els **tipus de mostra** i les **quantitats necessàries**:

#### 🔹 Font 1: Taula de Regles (`tipusmostra_referencia`)

Conté patrons per associar microorganisme + mecanisme → tipus de mostra amb quantitats.

**Camps clau:**
```sql
microorganisme_patro VARCHAR(100)  -- Ex: 'Pseudomonas%', '%', 'Candida auris'
mecanisme_patro VARCHAR(100)       -- Ex: 'BLEE', 'SENSE', '%'
resultat TEXT                      -- Ex: 'Frotis rectal|5|Orina|6'
prioritat INT                      -- Ordre d'aplicació (1 = més prioritat)
actiu TINYINT(1)                   -- Si la regla està activa
```

**Consulta:**
```sql
SELECT resultat 
FROM tipusmostra_referencia
WHERE 'Pseudomonas aeruginosa' LIKE microorganisme_patro
  AND 'BLEE' LIKE mecanisme_patro
  AND (actiu = 1 OR actiu IS NULL)
ORDER BY prioritat ASC
LIMIT 1
```

⚠️ **Nota**: Si el mecanisme és buit/null, es busca com `'SENSE'`.

#### 🔹 Font 2: Mostres Positives del Diagnòstic

Obté els diferents tipus de mostra segons els positius que ha tingut el pacient per aquest diagnòstic.

**Consulta:**
```sql
SELECT pdm.tipus_mostra_m, pdm.data_mostra
FROM mostra_microorganisme mm
INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id
WHERE mm.pacient_diagnostic_id = @diagnosticId
  AND pdm.valoracio = '2'  -- Positiva
  AND pdm.dt_delete IS NULL
ORDER BY pdm.data_mostra DESC
```

#### 🔹 Font 3: Acumulació amb Quantitats Màximes

El sistema acumula les quantitats de les dues fonts anteriors, quedant-se amb el **màxim** per cada tipus de mostra.

**Regles d'acumulació:**

1. **Des de la taula de regles**: Parsejar `resultat` i extreure parelles `tipus|quantitat`
   - Si el tipus no existeix → Afegir-lo
   - Si existeix amb quantitat inferior → Actualitzar-lo

2. **Des de mostres positives**: Per cada tipus de mostra positiva
   - Si no existeix → Afegir amb quantitat = **3**
   - Si existeix amb quantitat < 3 → Actualitzar a **3**

**Exemple de resultat final:**
```javascript
{
	"Frotis rectal": 5,      // De la regla (quantitat 5)
	"Orina": 6,              // De la regla (quantitat 6)
	"Aspirat traqueal": 3    // De mostra positiva (quantitat 3)
}
```

---

## 🔍 Lògica de Comptatge de Negatives Consecutives

### Regla Principal

> **"Necessitem X mostres negatives consecutives DESPRÉS de l'últim positiu (o des de la data de diagnòstic)"**

### Comportament amb Positius

- **Mostres negatives** → Incrementen el comptador
- **Mostra positiva** → **REINICIA el comptador a 0** per aquell tipus de mostra

### Exemple Pràctic

**Configuració**: Orina necessita **6 negatives**

#### Escenari 1: Sense positius posteriors ✅
```
Data Diagnòstic: 01/01/2025

Orina: N, N, N, N, N, N
	   1  2  3  4  5  6  ✅ COMPLEIX (6 negatives)
```

#### Escenari 2: Amb positiu intermedi ❌
```
Data Diagnòstic: 01/01/2025

Orina: N, N, P, N, N, N
	   ↓  ↓  ↓  ↓  ↓  ↓
	   ✓  ✓  🔄 1  2  3  ❌ NO COMPLEIX (només 3 després del positiu)
```

#### Escenario 3: Negatives després de positiu ✅
```
Data Diagnòstic: 01/01/2025

Orina: N, N, P, N, N, N, N, N, N
	   ↓  ↓  ↓  ↓  ↓  ↓  ↓  ↓  ↓
	   ✓  ✓  🔄 1  2  3  4  5  6  ✅ COMPLEIX (6 negatives després del positiu)
```

---

## 💻 Implementació en Codi

### DTOs Creats

1. **`ReglaTipusMostra.cs`**: Representa una regla de `tipusmostra_referencia`
2. **`MostraPositivaDiagnostic.cs`**: Mostra positiva d'un diagnòstic
3. **`MostraDiagnostic.cs`**: Mostra (positiva/negativa) posterior a la data de diagnòstic
4. **`DiagnosticPerRevisar.cs`**: Afegit camp `DataDiagnostic`
5. **`ResumRevisioVigenciaDto.cs`**: Afegit camp `MarcatsPerMostresNegatives`

### Nous Mètodes al Repositori (`IMultiRRepository`)

```csharp
/// <summary>
/// Obté la regla de tipus de mostra per un microorganisme i mecanisme
/// </summary>
ReglaTipusMostra ObtenirReglaTipusMostra(string microorganisme, string mecanisme);

/// <summary>
/// Obté les mostres positives d'un diagnòstic
/// </summary>
List<MostraPositivaDiagnostic> ObtenirMostresPositivesDiagnostic(int diagnosticId);

/// <summary>
/// Obté totes les mostres (positives i negatives) d'un diagnòstic posteriors a la data de diagnòstic
/// </summary>
List<MostraDiagnostic> ObtenirMostresDiagnostic(int diagnosticId, DateTime dataDiagnostic);
```

### Nou Servei: `MostresNegativesService`

**Ubicació**: `MultirRevisioVigencia\Application\Services\MostresNegativesService.cs`

**Mètodes principals:**

```csharp
/// <summary>
/// Calcula els tipus de mostra i les quantitats necessàries per desactivar un diagnòstic
/// </summary>
Dictionary<string, int> CalcularTipusMostraQuantitats(DiagnosticPerRevisar diagnostic)

/// <summary>
/// Comprova si un diagnòstic compleix els requisits de mostres negatives consecutives
/// </summary>
bool CompleixRequisitsMostresNegatives(DiagnosticPerRevisar diagnostic, out string detalls)
```

### Integració al UseCase

**Fitxer**: `RevisarVigenciaDiagnosticsUseCase.cs`

```csharp
// COMPROVACIÓ 3: Mostres negatives consecutives (només per Multiresistents)
if (!hauDeMarcarNoVigent && diagnostic.TipusMicroorganisme == "M")
{
	_logger.Info($"   🔍 Comprovant mostres negatives consecutives...");

	string detalls;
	bool compleixMostresNegatives = _mostresNegativesService
		.CompleixRequisitsMostresNegatives(diagnostic, out detalls);

	if (compleixMostresNegatives)
	{
		_logger.Info($"   ⚠️ Compleix requisits de mostres negatives: {detalls}");
		hauDeMarcarNoVigent = true;
		esPerMostresNegatives = true;
		motiu = "N";  // ← MOTIU: Negatives
	}
}
```

---

## 🔖 Codis de Motius

| Codi | Descripció | Condició |
|------|-----------|----------|
| **E** | Èxitus | Pacient mort (`data_exitus IS NOT NULL`) |
| **V** | Vigència superada | Dies sense positius > `vigencia_inactiu` |
| **N** | **Mostres negatives** | **Prou negatives consecutives per tots els tipus** |

---

## 📊 Exemple de Log

```
Processant diagnòstic ID 12345 - Pacient: 10640856
   Microorganisme: Pseudomonas aeruginosa (Tipus: M)
   Mecanisme: BLEE
   ✓ Pacient NO és èxitus
   ✓ Diagnòstic encara vigent (dies de vigència)
   🔍 Comprovant mostres negatives consecutives...
   📊 Calculant tipus de mostra i quantitats necessàries...
   ✓ Regla trobada: 'Frotis rectal|5|Orina|6'
   ✓ Mostres positives trobades: 2
   📋 Tipus de mostra i quantitats necessàries:
	  • Aspirat traqueal: 3 mostres negatives
	  • Frotis rectal: 5 mostres negatives
	  • Orina: 6 mostres negatives
   📊 Mostres trobades posteriors a 15/01/2025: 18
	  ✅ Aspirat traqueal: 4/3 negatives consecutives
	  ✅ Frotis rectal: 7/5 negatives consecutives
	  ✅ Orina: 8/6 negatives consecutives
   ✅ El diagnòstic compleix els requisits de mostres negatives consecutives
   ⚠️ Compleix requisits de mostres negatives: Aspirat traqueal: 4/3; Frotis rectal: 7/5; Orina: 8/6
   ✅ Diagnòstic marcat com a no vigent correctament
```

---

## 🔧 Configuració

No cal configuració addicional. El sistema utilitza:

- **Taula**: `tipusmostra_referencia` (regles preconfigurades)
- **Taula**: `pacients_diagnostics_mostra` (mostres del pacient)
- **Taula**: `mostra_microorganisme` (relació mostra-diagnòstic)

---

## ✅ Criteris de Desactivació

Per desactivar un diagnòstic per mostres negatives, **TOTS els tipus de mostra** han de complir:

1. **Tenir prou negatives consecutives** (segons la quantitat configurada)
2. **No tenir cap positiu posterior** a les negatives comptabilitzades

---

## 🚀 Execució

El sistema s'executa automàticament al cridar:

```bash
MultirRevisioVigencia.exe
```

**Resum mostrat:**
```
=======================================================
  RESUM DE LA REVISIÓ
=======================================================
Total diagnòstics revisats:      150
Diagnòstics marcats no vigents:  12
  - Per èxitus del pacient:      3
  - Per superar vigència:        5
  - Per mostres negatives:       4  ← NOU
Diagnòstics amb error:           0
Durada:                          8.45 segons
=======================================================
```

---

## 📝 Registre a Base de Dades

Quan es marca com a no vigent per mostres negatives:

```sql
UPDATE pacients_diagnostics 
SET vigent = 'N',
	data_no_vigent = NOW(),
	responsable_no_vigent = 'MULTIR_AUTOM',
	motiu_no_vigent = 'N',  -- ← Codi per Negatives
	dt_update = NOW()
WHERE id = @diagnosticId
```

---

## 🔍 Exemples de Casos d'Ús

### Cas 1: Diagnòstic amb regla específica

**Input:**
- Microorganisme: `Pseudomonas aeruginosa`
- Mecanisme: `BLEE`
- Regla: `Frotis rectal|5|Orina|6`

**Càlcul:**
```
Frotis rectal: 5 negatives necessàries
Orina: 6 negatives necessàries
```

### Cas 2: Diagnòstic amb mostres positives addicionals

**Input:**
- Microorganisme: `Escherichia coli`
- Mecanisme: `BLEE`
- Regla: `Frotis rectal|5`
- Mostres positives: `Orina`, `Aspirat traqueal`

**Càlcul:**
```
Frotis rectal: 5 negatives (de la regla)
Orina: 3 negatives (de mostra positiva)
Aspirat traqueal: 3 negatives (de mostra positiva)
```

### Cas 3: Sense regla, només mostres positives

**Input:**
- Microorganisme: `Klebsiella pneumoniae`
- Mecanisme: `NOCOD`
- Regla: *(no trobada)*
- Mostres positives: `Frotis rectal`, `Orina`

**Càlcul:**
```
Frotis rectal: 3 negatives (de mostra positiva)
Orina: 3 negatives (de mostra positiva)
```

---

## 🎯 Avantatges

✅ **Automatització**: Desactivació automàtica sense intervenció manual  
✅ **Precisió**: Només desactiva quan TOTS els tipus compleixen  
✅ **Traçabilitat**: Logs detallats de tot el procés  
✅ **Flexible**: Basat en regles configurables a BD  
✅ **Robust**: Gestiona positius posteriors correctament  

---

## 📚 Documentació Relacionada

- **README.md**: Documentació general del projecte
- **SISTEMA_VIGENCIA_DIAGNOSTICS.md**: Sistema de vigència (motius E i V)
- **COMPROVACIONS_NEGATIUS_RESUM.md**: Sistema de comprovació de negatius per incorporació

---

**Fi del Document**
