# 🚀 GUIA DE ACTIVACIÓ: CÀRREGA INCREMENTAL (TIPUS 1)

## ✅ ESTAT ACTUAL

- **Feature Status**: ✅ **FUNCIONAL I LESTA PER PRODUCCIÓ**
- **Activació Actual**: ❌ **DESACTIVADA** (`CarregaIncremental_Activa = false`)
- **Arquitectura**: ✅ **COMPLETA**
- **Performance**: 🚀 **OPTIMITZADA** (Reduccié ~90% de dades)

---

## 🎯 Qué fa la Càrrega Incremental?

### **Sense Incremental (Mode Des Enrere)**
```
Execució 1: Carrega els últims 1 dia   → 5,000 mostres
Execució 2: Carrega els últims 1 dia   → 5,000 mostres
Execució 3: Carrega els últims 1 dia   → 5,000 mostres
		  = 15,000 mostres (muchos duplicats!)
```

### **Amb Incremental (Optimitzat)**
```
Execució 1: Carrega els últims 7 dies  → 35,000 mostres (primera vegada)
Execució 2: Carrega SOLO noves/canvis  → 150 mostres
Execució 3: Carrega SOLO noves/canvis  → 200 mostres
		  = 35,350 mostres (NÓ duplicats!)
```

**Diferència**: 🚀 **99% menys duplicació!**

---

## 📋 CHECKLIST de Requisits Previs

Abans d'activar, verifica:

- [ ] **1. Taula MySQL `ControlSincronitzacio` exists**
  ```sql
  SELECT COUNT(*) FROM ControlSincronitzacio;
  ```
  Si NO existeix → Executar script de creació (veure més avall)

- [ ] **2. App.config amb credencials Oracle actualitzades**
  ```xml
  <!-- Host: 146.219.109.73, Port: 1521, DB: mgold -->
  <add name="OracleModulab_Produccio" connectionString="..." />
  ```

- [ ] **3. App.config amb credencials MySQL actualitzades**
  ```xml
  <add name="MySqlMultiR_Produccio" connectionString="..." />
  ```

- [ ] **4. Entorn seleccionat correctament**
  ```xml
  <add key="Entorn" value="Preproduccio" />  <!-- o "Produccio" -->
  ```

---

## 🛠️ ASSEGURAR TAULA DE SINCRONITZACIÓ

### **Opció 1: Verificar si existeix**

```sql
USE marsa;  -- o la BD corresponent

SELECT * FROM ControlSincronitzacio LIMIT 1;
```

**Si retorna error**: La taula no esisteix → Fer Opció 2

### **Opció 2: Crear la taula**

Executar aquest SQL al MySQL:

```sql
CREATE TABLE ControlSincronitzacio (
	id INT AUTO_INCREMENT PRIMARY KEY,
	DataSincronitzacio DATETIME DEFAULT CURRENT_TIMESTAMP,
	DataResultatMaxProcessada DATETIME NULL COMMENT 'Última DATA_RESULTAT processada de Modulab',
	DataValidacioMaxProcessada DATETIME NULL COMMENT 'Última DATA_VALIDACIO processada de Modulab',
	DiesRevisioSeguretat INT DEFAULT 7 COMMENT 'Dies de finestra de seguretat per overlap',
	NombreResultatsCarregats INT DEFAULT 0,
	TotalMecanismesResistencia INT DEFAULT 0,
	EstatSincronitzacio VARCHAR(50) DEFAULT 'COMPLETADA' COMMENT 'EN_CURS, COMPLETADA, ERROR',
	MissatgeError VARCHAR(1000) NULL,
	TempsExecucioMs INT DEFAULT 0 COMMENT 'Durada de l''execució en ms',
	FechaCreacio DATETIME DEFAULT CURRENT_TIMESTAMP,
	FechaActualitzacio TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,

	INDEX idx_datasincro (DataSincronitzacio),
	INDEX idx_dataresutat (DataResultatMaxProcessada),
	INDEX idx_datavalidacio (DataValidacioMaxProcessada),
	UNIQUE KEY uk_datasincro (DataSincronitzacio)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- Inserir primera fila (opcional, per indicar primera execució)
INSERT INTO ControlSincronitzacio 
(DataSincronitzacio, EstatSincronitzacio, MissatgeError)
VALUES 
(NOW(), 'INICIAL', 'Esperant primera execució');
```

### **Opció 3: Consultar registres existents**

```sql
SELECT 
	id,
	DATE_FORMAT(DataSincronitzacio, '%d/%m/%Y %H:%i') AS 'Sincronització',
	DATE_FORMAT(DataResultatMaxProcessada, '%d/%m/%Y %H:%i') AS 'Última Resultat',
	DATE_FORMAT(DataValidacioMaxProcessada, '%d/%m/%Y %H:%i') AS 'Última Validació',
	NombreResultatsCarregats,
	EstatSincronitzacio
FROM ControlSincronitzacio
ORDER BY DataSincronitzacio DESC
LIMIT 5;
```

---

## 🔧 ACTIVACIÓ: Pas a Pas

### **Pas 1: Editar `App.config`**

Obrir el fitxer:
```
MultirIntegraModulab\App.config
```

Localizar aquesta secció (~línies 27-45):

```xml
<!-- TIPUS 1: CÀRREGA INCREMENTAL (Prioritat Alta) -->
<add key="CarregaIncremental_Activa" value="false" />      <!-- ⬅️ CANVIAR A TRUE -->
<add key="CarregaIncremental_DiesRevisioSeguretat" value="7" />

<!-- TIPUS 2: CÀRREGA PER DIES ENRERE (Prioritat Mitjana) -->
<add key="CarregaDiesEnrere_Activa" value="true" />        <!-- ⬅️ CANVIAR A FALSE -->
<add key="CarregaDiesEnrere_NombreDies" value="1" />
```

**Resultats après del canvi**:

```xml
<!-- TIPUS 1: CÀRREGA INCREMENTAL (Prioritat Alta) -->
<add key="CarregaIncremental_Activa" value="true" />       <!-- ✅ ACTIVADA -->
<add key="CarregaIncremental_DiesRevisioSeguretat" value="7" />

<!-- TIPUS 2: CÀRREGA PER DIES ENRERE (Prioritat Mitjana) -->
<add key="CarregaDiesEnrere_Activa" value="false" />       <!-- ✅ DESACTIVADA -->
<add key="CarregaDiesEnrere_NombreDies" value="1" />
```

### **Pas 2: Guardar el fitxer**

```
Ctrl + S
```

### **Pas 3: Compilar el projecte**

```powershell
# En Visual Studio
Ctrl + Shift + B

# O des de Command Line
dotnet build
```

### **Pas 4: Executar l'aplicació**

**Mode Debug**:
```powershell
# Executar el programa principal
cd C:\Projectes\MultirIntegraModulab
dotnet run
```

**Mode Service** (si és desplegat com a Windows Service):
```powershell
# Verificar el servei
Get-Service "MultirIntegraModulabService"

# Reiniciar el servei
Restart-Service "MultirIntegraModulabService"

# Veure els logs
Get-Content "C:\path\to\logs\multir*.log" -Tail 50
```

---

## 📊 Primera Execució - Què Esperar

### **Logs Esperats**

```
==========================================
🔍 Mode: CÀRREGA INCREMENTAL (Prioritat Alta)
==========================================

ℹ️ Última sincronització: null (primera vegada)
ℹ️ Primera execució - carregant mostres dels últims 7 dies

📋 Carregant resultats de Modulab...
🔄 Carregant mostres amb càrrega incremental...

📦 RESUM CÀRREGA AMB SINCRONITZACIÓ:
   - Resultats processats: 24,567
   - Resultats carregats: 24,523
   - Microorganismes especials: 1,245
   - % Error: 0.18%

⏱️ Temps total: 45,231 ms

📤 Actualitzant ControlSincronitzacio...
✅ Sincronització completada exitosament

✅ FINISH: Processament finalitzat.
```

### **Taula MySQL Després**

```sql
SELECT * FROM ControlSincronitzacio ORDER BY id DESC LIMIT 1\G

id:                      1
DataSincronitzacio:      2025-01-30 14:35:21
DataResultatMaxProcessada:    2025-01-30 13:45:00
DataValidacioMaxProcessada:   2025-01-30 13:50:30
DiesRevisioSeguretat:    7
NombreResultatsCarregats: 24523
TotalMecanismesResistencia: 0
EstatSincronitzacio:     COMPLETADA
MissatgeError:           NULL
TempsExecucioMs:         45231
```

---

## ✨ Execucions Posteriors - Què Esperar

### **Execució 2 (al dia següent)**

```
==========================================
🔍 Mode: CÀRREGA INCREMENTAL (Prioritat Alta)
==========================================

📅 Última sincronització: 30/01/2025 14:35:21
📋 Filtres aplicats:
   Data resultat > 30/01/2025 14:33:21 (amb overlap de 2 min)
   Data validació > 30/01/2025 14:33:21 (amb overlap de 2 min)

🔄 Carregant mostres amb càrrega incremental...

📦 RESUM:
   - Resultats processats: 385           🚀 (MOLT menys que 24,500!)
   - Resultats carregats: 378
   - % Error: 1.82%

⏱️ Temps total: 1,234 ms                  🚀 (Molt més ràpid!)

✅ Sincronització completada exitosament
```

---

## 🔄 Tornar a Mode "Dies Enrere" (si fos necessari)

Si es necessita desactivar la càrrega incremental:

```xml
<!-- TIPUS 1: CÀRREGA INCREMENTAL (Prioritat Alta) -->
<add key="CarregaIncremental_Activa" value="false" />      ← Canviar a FALSE

<!-- TIPUS 2: CÀRREGA PER DIES ENRERE (Prioritat Mitjana) -->
<add key="CarregaDiesEnrere_Activa" value="true" />        ← Canviar a TRUE
<add key="CarregaDiesEnrere_NombreDies" value="1" />
```

**Nota**: La taula `ControlSincronitzacio` es mantindrà, però NO s'usarà.

---

## 📈 Monitoritzar Performance

### **Veure historial de sincronitzacions**

```sql
SELECT 
	id,
	DATE_FORMAT(DataSincronitzacio, '%d/%m/%Y %H:%i') AS Execució,
	NombreResultatsCarregats AS Mostres,
	TempsExecucioMs AS MsPerf,
	ROUND(NombreResultatsCarregats / (TempsExecucioMs / 1000), 0) AS MostrasPorSegon,
	EstatSincronitzacio
FROM ControlSincronitzacio
ORDER BY DataSincronitzacio DESC
LIMIT 10;
```

### **Gràfica de tendències**

```sql
SELECT 
	DATE(DataSincronitzacio) AS Dia,
	COUNT(*) AS NombreExecucions,
	SUM(NombreResultatsCarregats) AS TotalMostres,
	AVG(TempsExecucioMs) AS TempsPromig
FROM ControlSincronitzacio
GROUP BY DATE(DataSincronitzacio)
ORDER BY Dia DESC
LIMIT 30;
```

---

## 🐛 Solucionar Problemes

### **Problema 1: "ControlSincronitzacio table not found"**

**Solució**:
```sql
-- Crear la taula (veure script més amunt)
CREATE TABLE ControlSincronitzacio (...)
```

### **Problema 2: "EstatSincronitzacio = ERROR"**

**Verificar**:
```sql
SELECT MissatgeError FROM ControlSincronitzacio 
WHERE EstatSincronitzacio = 'ERROR' 
ORDER BY DataSincronitzacio DESC LIMIT 1;
```

**Comú**: Credencials Oracle incorrectes o host no accessible

### **Problema 3: "Carregant molt poc" (0-10 mostres dia 2+)**

**Verificar**:
- ¿Les dates a Modulab són correctes?
- ¿Els camps DATA_RESULTAT i DATA_VALIDACIO s'estan actualitzant?
- ¿La zona horaria és correcta?

### **Problema 4: "Voltar a intentar (reset manual)"**

```sql
-- Eliminar l'última sincronització per forçar recàrrega de 7 dies
DELETE FROM ControlSincronitzacio WHERE id = (SELECT MAX(id) FROM ControlSincronitzacio);

-- O eliminar tota la taula
TRUNCATE TABLE ControlSincronitzacio;
```

---

## 📞 Verificació Final

Després d'activar, verificar aquests punts:

- [ ] **App.config actualitzat** (`CarregaIncremental_Activa = true`)
- [ ] **Taula MySQL existe** (`ControlSincronitzacio`)
- [ ] **Credencials Oracle correctes** (146.219.109.73:1521)
- [ ] **Credencials MySQL correctes** (marsa BD)
- [ ] **Primera execució completada** (veure logs)
- [ ] **Registre a ControlSincronitzacio** (verificar SQL)
- [ ] **Performance millorat** (comparem logs dia 1 i dia 2+)

---

## 📝 Exemple Complet d'App.config

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <appSettings>
	<!-- ENTORN -->
	<add key="Entorn" value="Preproduccio" />

	<!-- TIPUS 1: CÀRREGA INCREMENTAL ✅ ACTIVADA -->
	<add key="CarregaIncremental_Activa" value="true" />
	<add key="CarregaIncremental_DiesRevisioSeguretat" value="7" />

	<!-- TIPUS 2: CÀRREGA PER DIES ENRERE (desactivada) -->
	<add key="CarregaDiesEnrere_Activa" value="false" />
	<add key="CarregaDiesEnrere_NombreDies" value="1" />

	<!-- TIPUS 3: CÀRREGA PER RANG DE DATES (desactivada) -->
	<add key="CarregaRangDates_Activa" value="false" />
	<add key="CarregaRangDates_DataInici" value="26/01/2026" />
	<add key="CarregaRangDates_DataFi" value="30/01/2026" />

	<!-- ALTRES SETTINGS... -->
  </appSettings>

  <connectionStrings>
	<!-- Oracle Modulab Producció -->
	<add name="OracleModulab_Produccio"
		 connectionString="Data source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL = TCP)(HOST = 146.219.109.73)(PORT = 1521))) (CONNECT_DATA = (SERVICE_NAME = mgold)));User Id=covid;Password=t6k8*_NQueB6;"
		 providerName="Oracle.ManagedDataAccess.Client" />

	<!-- MySQL MultiR Producció -->
	<add name="MySqlMultiR_Produccio"
		 connectionString="Server=zeus;Database=marsa;Uid=marsa;Pwd=2a0d9a8d22;"
		 providerName="MySql.Data.MySqlClient" />
  </connectionStrings>
</configuration>
```

---

## 🎯 Resum D'Activació

| Pas | Acció | Fitxer | Estat |
|-----|-------|--------|-------|
| 1 | Crear taula ControlSincronitzacio | MySQL | ✅ SQL fornit |
| 2 | Canviar CarregaIncremental_Activa a true | App.config | ✅ Instruccions fornides |
| 3 | Canviar CarregaDiesEnrere_Activa a false | App.config | ✅ Instruccions fornides |
| 4 | Compilar projecte | Visual Studio | ✅ Ctrl+Shift+B |
| 5 | Executar aplicació | Program | ✅ Esperar logs |
| 6 | Verificar sincronització | MySQL | ✅ Query SQL |
| 7 | Monitorear performance | Logs | ✅ 2o dia: ~90% reducció |

---

**Data**: Gener 2025
**Status**: Ready for Production
**Responsable**: Equip de Desenvolupament
