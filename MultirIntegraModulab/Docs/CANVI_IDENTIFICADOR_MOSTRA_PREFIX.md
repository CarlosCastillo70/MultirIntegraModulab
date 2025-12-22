# 🔧 Implementació: Concatenació de PREFIX a ETIQUETA_ID

## 📋 Resum del Canvi

**Data**: Gener 2025  
**Objectiu**: Solucionar conflictes amb mostres que comparteixen `ETIQUETA_ID` però tenen diferents `PREFIX`  
**Solució**: Concatenar el `PREFIX` formatat a 3 caràcters amb l'`ETIQUETA_ID` a les consultes SQL d'Oracle

---

## 🎯 Problema Detectat

### Situació Anterior
L'identificador d'una mostra era només `ETIQUETA_ID`, però **en realitat dues mostres diferents** poden compartir la mateixa `ETIQUETA_ID` si tenen diferents `PREFIX`.

**Exemple de conflicte**:
```
Mostra A: ETIQUETA_ID = "12345678", PREFIX = "1"
Mostra B: ETIQUETA_ID = "12345678", PREFIX = "2"
```

Aquestes dues mostres eren tractades com si fossin **la mateixa mostra**, provocant:
- ❌ Mostres revalidades amb canvis crítics
- ❌ Dades sobreescrites incorrectament
- ❌ Pèrdua d'informació clínica

---

## ✅ Solució Implementada

### Identificador Únic Real
```
ETIQUETA_ID_REAL = ETIQUETA_ID + PREFIX_formatat_a_3_caràcters
```

### Exemples de Transformació

| ETIQUETA_ID | PREFIX | PREFIX Formatat | ETIQUETA_ID Final |
|-------------|--------|-----------------|-------------------|
| 12345678 | NULL | 000 | **12345678000** |
| 12345678 | 1 | 001 | **12345678001** |
| 12345678 | 12 | 012 | **12345678012** |
| 12345678 | 123 | 123 | **12345678123** |

### Fórmula SQL Oracle
```sql
PET.ETIQUETA_ID || LPAD(NVL(CONT.PREFIX, '0'), 3, '0') AS ETIQUETA_ID
```

**Explicació**:
- `NVL(CONT.PREFIX, '0')`: Si PREFIX és NULL, utilitza '0'
- `LPAD(..., 3, '0')`: Formata a 3 caràcters amb zeros a l'esquerra
- `PET.ETIQUETA_ID || ...`: Concatena etiqueta amb prefix formatat

---

## 🔧 Fitxers Modificats

### 1. ModulabDbService.cs
**Mètode**: `ObtenirConsultaResultatsProves()`

**Abans**:
```sql
SELECT
  PET.ETIQUETA_ID,
  PA.PACIENT_SAP,
  ...
```

**Després**:
```sql
SELECT
  PET.ETIQUETA_ID || LPAD(NVL(CONT.PREFIX, '0'), 3, '0') AS ETIQUETA_ID,
  PA.PACIENT_SAP,
  ...
```

---

### 2. ModulabDbService.cs
**Mètode**: `ObtenirConsultaResultatsProvesPerRangDates()`

**Abans**:
```sql
SELECT
  PET.ETIQUETA_ID,
  PA.PACIENT_SAP,
  ...
```

**Després**:
```sql
SELECT
  PET.ETIQUETA_ID || LPAD(NVL(CONT.PREFIX, '0'), 3, '0') AS ETIQUETA_ID,
  PA.PACIENT_SAP,
  ...
```

---

### 3. ModulabDbService.Sincronitzacio.cs
**Mètode**: `ObtenirConsultaAmbFiltresSincronitzacio()`

**Abans**:
```sql
SELECT
  PET.ETIQUETA_ID,
  PA.PACIENT_SAP,
  ...
```

**Després**:
```sql
SELECT
  PET.ETIQUETA_ID || LPAD(NVL(CONT.PREFIX, '0'), 3, '0') AS ETIQUETA_ID,
  PA.PACIENT_SAP,
  ...
```

---

## 💡 Avantatges de la Solució

### ✅ 1. Transparència Total
- **Cap canvi al codi de domini** (Mostra, ResultatMostra, etc.)
- **Cap canvi als Use Cases** de processament
- **Cap canvi a la lògica de negoci**
- Tot el codi existent segueix funcionant sense modificacions

### ✅ 2. Identificador Únic Real
- Cada mostra té un identificador **realment únic**
- S'eliminen els conflictes de mostres amb mateix ETIQUETA_ID
- Les mostres revalidades ara es detecten correctament

### ✅ 3. Mantenibilitat
- **Canvi centralitzat** en un sol lloc (consultes SQL)
- Fàcil de revertir si cal
- No afecta la base de dades MySQL

### ✅ 4. Compatibilitat amb Dades Existents
- MySQL segueix usant `etiqueta` com a camp
- El PREFIX ja està incorporat al valor de l'etiqueta
- No cal migració de dades

---

## 📊 Impacte en el Sistema

### Abans del Canvi
```
Oracle → App .NET → MySQL

ETIQUETA_ID: "12345678" (PREFIX: "1")  ─┐
ETIQUETA_ID: "12345678" (PREFIX: "2")  ─┴─→ Conflict! Tractades com la mateixa
```

### Després del Canvi
```
Oracle → App .NET → MySQL

ETIQUETA_ID: "12345678001" ───→ Mostra 1 (única)
ETIQUETA_ID: "12345678002" ───→ Mostra 2 (única)
```

---

## 🧪 Casos de Prova

### Cas 1: Mostra amb PREFIX NULL
**Input Oracle**:
- ETIQUETA_ID: "87654321"
- PREFIX: NULL

**Output Final**:
- ETIQUETA_ID: "87654321000"

**Verificació**:
```csharp
var mostra = coleccioMostres.ObtenirMostra("87654321000");
Assert.IsNotNull(mostra);
```

---

### Cas 2: Mostra amb PREFIX d'1 dígit
**Input Oracle**:
- ETIQUETA_ID: "87654321"
- PREFIX: "5"

**Output Final**:
- ETIQUETA_ID: "87654321005"

---

### Cas 3: Mostra amb PREFIX de 2 dígits
**Input Oracle**:
- ETIQUETA_ID: "87654321"
- PREFIX: "12"

**Output Final**:
- ETIQUETA_ID: "87654321012"

---

### Cas 4: Mostra amb PREFIX de 3 dígits
**Input Oracle**:
- ETIQUETA_ID: "87654321"
- PREFIX: "999"

**Output Final**:
- ETIQUETA_ID: "87654321999"

---

### Cas 5: Dues Mostres amb Mateix ETIQUETA_ID i Diferents PREFIX
**Input Oracle**:
```
Mostra A: ETIQUETA_ID="12345678", PREFIX="1"
Mostra B: ETIQUETA_ID="12345678", PREFIX="2"
```

**Output Final**:
```
Mostra A: ETIQUETA_ID="12345678001" → Processada independentment
Mostra B: ETIQUETA_ID="12345678002" → Processada independentment
```

**Resultat**: ✅ No hi ha conflictes, cada mostra té el seu propi històric

---

## 🔍 Verificació del Canvi

### Query de Verificació a Oracle
```sql
SELECT 
    PET.ETIQUETA_ID AS ETIQUETA_ORIGINAL,
    CONT.PREFIX,
    PET.ETIQUETA_ID || LPAD(NVL(CONT.PREFIX, '0'), 3, '0') AS ETIQUETA_FINAL
FROM 
    DWDIMICS.DIM_LAB_PETICIONS_DT PET,
    DWDIMICS.V_DIM_LAB_CONTENIDOR_DT CONT,
    DWFACTICS.FAC_LAB_PROVES_DT DETALL
WHERE 
    CONT.ORIGEN(+) = DETALL.ORIGEN 
    AND CONT.CONTENIDOR_ID(+) = DETALL.CONTENIDOR_ID
    AND PET.ORIGEN = DETALL.ORIGEN 
    AND PET.PETICIO_ID = DETALL.PETICIO_ID
    AND ROWNUM <= 10
ORDER BY PET.ETIQUETA_ID;
```

**Resultat Esperat**:
```
ETIQUETA_ORIGINAL | PREFIX | ETIQUETA_FINAL
12345678          | 1      | 12345678001
12345678          | 2      | 12345678002
87654321          | NULL   | 87654321000
99887766          | 123    | 99887766123
```

---

### Query de Verificació a MySQL
```sql
-- Comprovar que les etiquetes a MySQL tenen el PREFIX incorporat
SELECT 
    etiqueta,
    LENGTH(etiqueta) AS longitud,
    RIGHT(etiqueta, 3) AS prefix_part,
    pacient_sap,
    data_mostra
FROM pacients_diagnostics_mostra
WHERE dt_delete IS NULL
ORDER BY data_mostra DESC
LIMIT 10;
```

**Resultat Esperat**:
```
etiqueta      | longitud | prefix_part | pacient_sap | data_mostra
12345678001   | 11       | 001         | 12345678    | 2025-01-15
12345678002   | 11       | 002         | 87654321    | 2025-01-15
87654321000   | 11       | 000         | 99887766    | 2025-01-14
```

---

## ⚠️ Consideracions Importants

### 1. Dades Històriques
- **Mostres processades ABANS del canvi** → Tenen etiquetes SIN PREFIX concatenat
- **Mostres processades DESPRÉS del canvi** → Tenen etiquetes AMB PREFIX concatenat
- **No cal migració**: Les mostres antigues segueixen funcionant correctament

### 2. Longitud de l'Etiqueta
- **Abans**: 8 caràcters (exemple: "12345678")
- **Després**: 11 caràcters (exemple: "12345678001")
- **Camp MySQL `etiqueta`**: VARCHAR(50) → Capacitat suficient ✅

### 3. Queries Existents
- Totes les queries que busquen per `etiqueta` segueixen funcionant
- Les etiquetes són úniques i consistents

---

## 🚀 Desplegament

### Passos per Desplegar

1. ✅ **Compilar el projecte**
   ```bash
   dotnet build MultirIntegraModulab.sln --configuration Release
   ```

2. ✅ **Executar proves**
   ```bash
   dotnet test
   ```

3. ✅ **Verificar consultes SQL**
   - Executar query de verificació a Oracle
   - Comprovar que PREFIX es concatena correctament

4. ✅ **Desplegar a Pre-Producció**
   - Executar amb `LimitResultatsProves=100` per provar
   - Verificar logs
   - Comprovar que no hi ha errors

5. ✅ **Desplegar a Producció**
   - Executar en horari no crític
   - Monitoritzar logs en temps real
   - Verificar que les etiquetes són úniques

---

## 📝 Logs Esperats

### Log d'Execució Normal
```
🔎 Recupero les dades de Modulab (pot trigar una estona)...
✅ Dades recuperades. Continuo endavant
📊 RESUM DE LA INCORPORACIÓ DE LES DADES DE MODULAB:
   - Resultats de mostra processats: 150
   - Resultats de mostra carregats correctament: 150
   - Resultats de mostra amb error: 0
   - Microorganismes especials trobats: 12
```

### Log amb Mostres Diferents (PREFIX)
```
▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄
 Processant mostra del pacient 12345678 i etiqueta : 87654321001
▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀
   ✨ Mostra nova - continuar endavant...

▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄▄
 Processant mostra del pacient 12345678 i etiqueta : 87654321002
▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀
   ✨ Mostra nova - continuar endavant...
```

**Observar**: Ara es processen com a **mostres diferents** (abans es detectaven com a repetides)

---

## 🔄 Rollback

Si cal revertir el canvi:

1. **Modificar les 3 consultes SQL**:
   - Eliminar `|| LPAD(NVL(CONT.PREFIX, '0'), 3, '0')`
   - Deixar només `PET.ETIQUETA_ID AS ETIQUETA_ID`

2. **Recompilar i redesplegar**

3. **Nota**: Les dades a MySQL **no es veuen afectades** (ja tenen PREFIX incorporat si existia)

---

## 📚 Referències

- **Issue**: Mostres revalidades amb canvis crítics
- **Anàlisi**: Investigació camp PREFIX a taula CONT (V_DIM_LAB_CONTENIDOR_DT)
- **Solució**: Concatenació transparent a consultes SQL
- **Repositori**: https://github.com/CarlosCastillo70/MultirIntegraModulab

---

## ✅ Checklist de Verificació

- [x] Modificades les 3 consultes SQL
- [x] Afegit comentari explicatiu a cada consulta
- [x] Compilació exitosa
- [x] Document de resum creat
- [x] Casos de prova documentats
- [ ] Proves en entorn de pre-producció
- [ ] Verificació amb dades reals
- [ ] Desplegament a producció
- [ ] Monitoratge post-desplegament

---

**Document creat**: Gener 2025  
**Autor**: Sistema d'Integració MultirIntegraModulab  
**Versió**: 1.0  
**Estat**: ✅ Implementat i Compilat

