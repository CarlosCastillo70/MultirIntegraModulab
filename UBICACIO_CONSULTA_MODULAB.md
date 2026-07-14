# 📊 Ubicació de la Consulta de Dades de Modulab

## 📍 Fitxer Principal

La consulta de dades de Modulab es troba al fitxer:

```
MultirIntegraModulab/Infrastructure/Persistence/LegacyServices/ModulabDbService.cs
```

---

## 🔍 Mètodes Principals

### **1. CarregarResultatsDeMostres()**
📌 **Ubicació**: Línia ~60
```csharp
public ColeccioMostres CarregarResultatsDeMostres(
	int diesEndarrera = 1, 
	MultiRDbService mysqlService = null, 
	int limitRegistres = 0)
```

**Descripció**:
- Carrega resultats de mostres de laboratori de la BD Oracle Modulab
- Carrega mostres dels últims N dies (per defecte 1 dia)
- Permet filtrar per límit de registres (per a proves)

**Els pasos que fa**:
1. Precarrega microorganismes especials de MySQL si es passa el servei
2. S'obri connexió Oracle
3. Executa la consulta SQL `ObtenirConsultaResultatsProves()`
4. Processa cada registre amb `CrearRegistreDesDeReader()`
5. Retorna una col·lecció de `ColeccioMostres`

---

### **2. ObtenirConsultaResultatsProves()**
📌 **Ubicació**: Línia 218

**SQL Principal**:
```sql
SELECT
  PET.ETIQUETA_ID || LPAD(NVL(CONT.PREFIX, '0'), 3, '0') AS ETIQUETA_ID,
  PA.PACIENT_SAP,
  nvl(PA.CIP,'N/D') CIP,
  ME.COLEGIAT_ID,
  REPLACE (ME.NOM_METGE,'''','´') AS NOM_METGE,
  REPLACE (LTRIM(C.CENTRE_DESCRIPCIO),'''','´') AS CENTRE_DESCRIPCIO,
  PET.DATA_PETICIO_TRUNC,
  REPLACE (A.AILLAMENT_DESCRIPCIO,'''','´') AS AILLAMENT_DESCRIPCIO,
  DETALL.MECANISME_RESISTENCIA1_ID,
  REPLACE (MR.MECANISME_RESISTENCIA_DESCRIP,'''','´') AS MECANISME_RESISTENCIA_DESCRIP,
  -- ... més camps de mecanismes de resistència (2-5)
  REPLACE (S.SERVEI_DESCRIPCIO,'''','´') AS SERVEI_DESCRIPCIO,
  REPLACE (PR.PROVA_DESCRIPCIO,'''','´') AS PROVA_DESCRIPCIO,
  REPLACE (MOS.MOSTRA_DESCRIPCIO,'''','´') AS MOSTRA_DESCRIPCIO,
  REPLACE (DETALL.SHORTDESCRIPTION1,'''','´') AS SHORTDESCRIPTION1,
  DETALL.DATA_RESULTAT,
  DETALL.DATA_VALIDACIO 
FROM
  DWDIMICS.DIM_LAB_MEC_RESISTENCIA MR,
  DWDIMICS.DIM_LAB_MEC_RESISTENCIA MR2,
  DWDIMICS.DIM_LAB_MEC_RESISTENCIA MR3,
  DWDIMICS.DIM_LAB_MEC_RESISTENCIA MR4,
  DWDIMICS.DIM_LAB_MEC_RESISTENCIA MR5,
  DWDIMICS.DIM_LAB_CENTRE C,
  DWDIMICS.DIM_LAB_SERVEI S,
  DWDIMICS.DIM_LAB_AILLAMENT A,
  DWDIMICS.DIM_LAB_PROVA PR,
  DWDIMICS.DIM_LAB_METGE ME,
  DWDIMICS.DIM_LAB_PACIENTS_DT PA,
  DWDIMICS.DIM_LAB_PETICIONS_DT PET,
  DWDIMICS.V_DIM_LAB_CONTENIDOR_DT CONT,
  DWDIMICS.V_DIM_LAB_MOSTRA_DT MOS,
  DWFACTICS.FAC_LAB_PROVES_DT DETALL
WHERE
  ( PET.PACIENT_ID = PA.PACIENT_ID(+) AND  PET.ORIGEN = PA.ORIGEN(+)  )
  AND  ( PET.METGE_ID = ME.METGE_ID AND  PET.ORIGEN = ME.ORIGEN  )
  AND  ( PET.ORIGEN = S.ORIGEN(+) AND  PET.SERVEI_ID = S.SERVEI_ID(+)  )
  AND  ( PET.ORIGEN = DETALL.ORIGEN AND  PET.PETICIO_ID = DETALL.PETICIO_ID  )
  AND  ( DETALL.ORIGEN = PR.ORIGEN(+) AND  DETALL.PROVA_ID = PR.PROVA_ID(+)  )
  AND  ( A.ORIGEN(+) = DETALL.ORIGEN AND  A.AILLAMENT_ID(+)=DETALL.AILLAMENT_ID  )
  AND  ( S.ORIGEN = C.ORIGEN(+) AND  S.CENTRE_ID = C.CENTRE_ID(+)  )
  AND  ( MR.ORIGEN(+) = DETALL.ORIGEN AND  MR.MECANISME_RESISTENCIA_CODI(+) = DETALL.MECANISME_RESISTENCIA1_ID  )
  AND  ( MR2.ORIGEN(+) = DETALL.ORIGEN AND  MR2.MECANISME_RESISTENCIA_CODI(+) = DETALL.MECANISME_RESISTENCIA2_ID  )
  AND  ( MR3.ORIGEN(+) = DETALL.ORIGEN AND  MR3.MECANISME_RESISTENCIA_CODI(+) = DETALL.MECANISME_RESISTENCIA3_ID  )
  AND  ( MR4.ORIGEN(+) = DETALL.ORIGEN AND  MR4.MECANISME_RESISTENCIA_CODI(+) = DETALL.MECANISME_RESISTENCIA4_ID  )
  AND  ( MR5.ORIGEN(+) = DETALL.ORIGEN AND  MR5.MECANISME_RESISTENCIA_CODI(+) = DETALL.MECANISME_RESISTENCIA5_ID  )
  AND  ( CONT.ORIGEN(+) = DETALL.ORIGEN AND  CONT.CONTENIDOR_ID(+) = DETALL.CONTENIDOR_ID )
  AND  ( MOS.ORIGEN(+) = CONT.ORIGEN AND  MOS.MOSTRA_ID(+) = CONT.MOSTRA_ID )
  AND  (
	   ( PA.TIPUS is null )
	   AND
	   PET.ORIGEN  =  'DT'
	   AND
	   DETALL.TIPUS = 'A'
	   AND
	   (DETALL.DATA_VALIDACIO_TRUNC >= trunc(sysdate-:diesEndarrera) 
		OR DETALL.DATA_RESULTAT_TRUNC >= trunc(sysdate-:diesEndarrera)) 
	  )
ORDER BY ETIQUETA_ID
```

**Taules implicadas**:
| Taula | Significat |
|-------|-----------|
| `DWDIMICS.DIM_LAB_MEC_RESISTENCIA` | Dimensions de mecanismes de resistència |
| `DWDIMICS.DIM_LAB_CENTRE` | Centres de salut |
| `DWDIMICS.DIM_LAB_SERVEI` | Serveis de laboratori |
| `DWDIMICS.DIM_LAB_AILLAMENT` | Tipus d'aillaments (microorganismes) |
| `DWDIMICS.DIM_LAB_PROVA` | Tipus de proves |
| `DWDIMICS.DIM_LAB_METGE` | Dades dels metges |
| `DWDIMICS.DIM_LAB_PACIENTS_DT` | Dades dels pacients |
| `DWDIMICS.DIM_LAB_PETICIONS_DT` | Peticions de laboratori |
| `DWDIMICS.V_DIM_LAB_CONTENIDOR_DT` | Vista de contenidors |
| `DWDIMICS.V_DIM_LAB_MOSTRA_DT` | Vista de mostres |
| `DWFACTICS.FAC_LAB_PROVES_DT` | Fets de les proves (resultats) |

**Condicions principals**:
- ✅ Només pacients sense tipus especial (`PA.TIPUS is null`)
- ✅ Origen = 'DT' (Datatransfer)
- ✅ Tipus = 'A' (Actiu)
- ✅ **Dates dins dels últims N dies** (paràmetre `:diesEndarrera`)

---

### **3. CarregarResultatsDeMostresPerPacient()**
📌 **Ubicació**: Línia ~443
```csharp
public ColeccioMostres CarregarResultatsDeMostresPerPacient(
	string pacientSap, 
	int diesEndarrera = 1, 
	int limitRegistres = 0, 
	MultiRDbService mysqlService = null)
```

**Descripció**:
- Carrega resultats només per a un pacient específic (per SAP ID)

---

### **4. CarregarResultatsDeMostresPerRangDates()**
📌 **Ubicació**: Línia ~477
```csharp
public ColeccioMostres CarregarResultatsDeMostresPerRangDates(
	DateTime dataInici, 
	DateTime dataFi, 
	MultiRDbService mysqlService = null)
```

**Descripció**:
- Carrega resultats dins d'un rang específic de dates
- Útil per reprocesaments històrics

---

## 🗂️ Estructura dels Fitxers

```
MultirIntegraModulab.Service/
  ├── ModulabDbService.cs (prin - 798 línies)
  │   ├── Mètodes de càrrega de dades
  │   └── Parsing de resultats Oracle
  │
  ├── ModulabDbService.Sincronitzacio.cs
  │   └── Mètodes de sincronització
  │
  └── IDbService.cs (Interface)
	  └── Contracte que implementa ModulabDbService
```

---

## 🔐 Connexió a Oracle

La connexió s'obté del `App.config`:

| Camp | Valor Nou |
|------|-----------|
| **Connection String** | `OracleModulab_Produccio` o `OracleModulab_Preproduccio` |
| **Host** | 146.219.109.73 |
| **Port** | 1521 |
| **Database** | mgold |
| **User** | covid |
| **Password** | t6k8*_NQueB6 |

**Selecció d'entorn**:
```xml
<add key="Entorn" value="Preproduccio" />  <!-- Canviar a "Produccio" si cal -->
```

---

## 📈 Flux de Dades

```
App.config (Credencials)
	↓
ModulabDbService.CarregarResultatsDeMostres()
	↓
ObtenirConsultaResultatsProves() [SQL en MGOLD]
	↓
CrearRegistreDesDeReader() [Mapeig de camps]
	↓
ColeccioMostres (Resultats estructurats)
	↓
ProcessarMostres (Lògica de negoci)
```

---

## 🔄 Altres Mètodes de Consulta

| Mètode | Propòsit | Ubicació |
|--------|----------|----------|
| `GetCurrentDate()` | Obté data actual d'Oracle | Línia 25 |
| `GetTableRecordCount()` | Obté nombre de registres d'una taula | Línia 41 |
| `CarregarResultademostres()` | Càrrega per dies enrere | Línia 60 |
| `CarregarResultatsDeMostresPerPacient()` | Càrrega per pacient | Línia 443 |
| `CarregarResultatsDeMostresPerRangDates()` | Càrrega per rang de dates | Línia 477 |

---

## 💡 Punts Importants

1. **La consulta usa OUTER JOINS (+)** per a taules dimensions opcionals
2. **Filtra sempre per dates relatives** (últims N dies, no dates fixes)
3. **Precarrega dades de MySQL** si es passa el servei (microorganismes especials)
4. **Limit amb ROWNUM** si es fa proves amb registres limitats
5. **Paràmetre de paràmetres**: `:diesEndarrera` es passa com a `OracleParameter`

---

## 📝 Exemple d'Ús

```csharp
// Instanciar el servei
var modulabService = new ModulabDbService(connectionString, logger);

// Opció 1: Carrega dels últims 1 dia
var mostres = modulabService.CarregarResultatsDeMostres(
	diesEndarrera: 1,
	mysqlService: multiRService,
	limitRegistres: 0  // 0 = sense límit
);

// Opció 2: Carrega per pacient específic
var mostresPerPacient = modulabService.CarregarResultatsDeMostresPerPacient(
	pacientSap: "SAP001",
	diesEndarrera: 7
);

// Opció 3: Carrega per rang de dates
var mostresHistorico = modulabService.CarregarResultatsDeMostresPerRangDates(
	dataInici: new DateTime(2025, 01, 01),
	dataFi: new DateTime(2025, 01, 31)
);
```

---

**Data d'actualització**: Gener 2025
**Base de dades**: Oracle Modulab (mgold)
**Responsable**: Equip de Desenvolupament
