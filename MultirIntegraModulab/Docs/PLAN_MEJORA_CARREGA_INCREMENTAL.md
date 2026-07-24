# Plan de Mejora: Sistema de Carga Incremental

## ISSUE ENCONTRADA EN ANÁLISIS

Después de revisar profundamente tu enfoque de carga incremental, he identificado que **funciona pero tiene riesgos importantes de pérdida de datos y duplicación**.

---

## PROBLEMA PRINCIPAL: Sin Deduplicación = Duplicados

Tu sistema actual:
1. Captura resultado X (sin validar)
2. Lo procesa e integra
3. Captura resultado X nuevamente (cuando se valida)
4. Lo procesa e integra OTRA VEZ

**Resultado:** Dos registros idénticos en base de datos.

### Ejemplo Real:
```
Resultado de laboratorio:
- Etiqueta: LAB-123456
- Paciente: SAP-789
- Prueba: Cultivo
- Resultado inicial: Péndiente de validación
- Validación: Confirmado negativo

CICLO 1 (15:00):
  SELECT ... WHERE DataResultat >= '2026-01-27 14:48'
  → Encuentra LAB-123456 sin validación
  → LO INTEGRA en sistema

CICLO 2 (15:15):
  SELECT ... WHERE DataResultat >= '2026-01-27 14:48' 
			OR DataValidacio >= NULL
			OR DataValidacio IS NULL
  → Encuentra LAB-123456 (ahora con validación)
  → LO INTEGRA DE NUEVO en sistema

RESULTADO: LAB-123456 está DUPLICADO en tu sistema
```

---

## SOLUCIÓN RECOMENDADA #1: Deduplicación en la Fuente

### Implementar antes de `AfegirResultat()`:

```csharp
// En ProcessamentMostresService.cs o ProcessarMostraUseCase

public void ProcesarMostraConDeduplicacion(Mostra mostra)
{
	// Crear clave única
	string claveUnica = $"{mostra.EtiquetaId}_{mostra.PacientSap}_{mostra.ProvaDescripcio}_{mostra.DataResultat:yyyyMMdd}";

	// Verificar si ya existe
	var existente = _multiRRepository.ObtenerResultadoPorClaveUnica(claveUnica);

	if (existente != null)
	{
		// ¿Ha cambiado el resultado?
		if (ResultadoHaCambiado(existente, mostra))
		{
			// Actualizar el registro existente
			_multiRRepository.ActualizarResultado(existente.Id, mostra);
			_logger.Info($"✏️ Resultado actualizado (cambio detectado): {claveUnica}");
		}
		else
		{
			// SKIP: Mismo resultado, no duplicar
			_logger.Info($"⏭️ Resultado duplicado ignorado: {claveUnica}");
		}
	}
	else
	{
		// Nuevo resultado: insertar
		_multiRRepository.CrearResultado(mostra);
		_logger.Info($"✅ Resultado nuevo insertado: {claveUnica}");
	}
}

private bool ResultadoHaCambiado(Resultado existente, Mostra nueva)
{
	// Comparar campos relevantes
	return existente.Microorganismo != nueva.Microorganismo
		|| existente.Resistencia != nueva.Resistencia
		|| existente.DataValidacio != nueva.DataValidacio;
}
```

**Beneficio:** No duplicas, detectas cambios.

---

## SOLUCIÓN RECOMENDADA #2: Mejor Tracking de Resultados Pendientes

### Modificar `DadesSincronitzacio`:

```csharp
public class DadesSincronitzacio
{
	// Campos ACTUALES (que ya tienes):
	public DateTime? DataResultatMaxProcessada { get; set; }
	public DateTime? DataValidacioMaxProcessada { get; set; }

	// NUEVOS campos para mejor control:

	/// <summary>
	/// Fecha MÍNIMA de resultados que aún NO tienen validación
	/// Se usa para capturar resultados pendientes que pueden validarse tardíamente
	/// </summary>
	public DateTime? DataResultadoMinimaConPendienteValidacion { get; set; }

	/// <summary>
	/// Número de resultados sin validación al momento de esta sincronización
	/// Usado para logging/monitoreo
	/// </summary>
	public int NombreResultadosPendientesValidacion { get; set; }

	/// <summary>
	/// Hash SHA256 de los últimos 10 resultados para change detection rápida
	/// </summary>
	public string HashUltimosResultados { get; set; }
}
```

**Uso en la consulta:**
```csharp
private string ObtenirConsultaAmbFiltresSincronitzacio(
	DateTime? dataResultatFiltre,
	DateTime? dataValidacioFiltre,
	DateTime? dataResultadoMinimaConPendiente,  // NUEVO
	int diesRevisio,
	int limitRegistres)
{
	var filtres = new List<string>();

	// Filtre 1: Resultados nuevos
	if (dataResultatFiltre.HasValue)
	{
		filtres.Add($"rt.RESULTDATE >= TO_TIMESTAMP('{dataResultatFiltre:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')");
	}

	// Filtre 2: Validaciones nuevas de resultados ya capturados
	if (dataValidacioFiltre.HasValue)
	{
		filtres.Add($"rt.FVDATE >= TO_TIMESTAMP('{dataValidacioFiltre:yyyy-MM-dd HH:mm:ss}', 'YYYY-MM-DD HH24:MI:SS')");
	}

	// NUEVO Filtre 3: Resultados sin validación que pueden validarse tardíamente
	if (dataResultadoMinimaConPendiente.HasValue)
	{
		// Capturar TODO lo que está entre la fecha mínima de pendientes y ahora
		// que AHORA tiene validación pero NO tenía antes
		string dataMin = dataResultadoMinimaConPendiente.Value.ToString("yyyy-MM-dd HH:mm:ss");
		filtres.Add($"(rt.RESULTDATE >= TO_TIMESTAMP('{dataMin}', 'YYYY-MM-DD HH24:MI:SS') AND rt.FVDATE IS NOT NULL)");
	}

	// Unir con OR
	if (filtres.Count == 0)
		filtres.Add("1=0");

	return "... WHERE " + string.Join(" OR ", filtres);
}
```

---

## SOLUCIÓN RECOMENDADA #3: Aumentar Overlap Según Ciclo

### Cambiar en `Program.cs`:

**Actual (INSUFICIENTE):**
```csharp
DateTime? dataResultatFiltre = ultimaSincronitzacio.DataResultatMaxProcessada?.AddMinutes(-2);
```

**Mejor (RECOMENDADO):**
```csharp
// Leer del config la frecuencia de ciclos
int cicloMinutos = 15; // O desde config

// Overlap debe ser al menos 2x la frecuencia del ciclo
int overlapMinutos = Math.Max(cicloMinutos * 2, 30); // Al menos 30 minutos

// Para recuperación ante fallos:
// Si hay fallos ocasionales, el overlap de 2 min pierde datos
// Con 5 ciclos de margin, recuperas incluso si 1 ciclo falla

DateTime? dataResultatFiltre = ultimaSincronitzacio.DataResultatMaxProcessada?.AddMinutes(-overlapMinutos);
```

**Configurar en App.config:**
```xml
<!-- Frequencia de ciclos de carga incremental en minutos -->
<add key="CarregaIncremental_CicloMinutos" value="15" />

<!-- Múltiplo de overlap para garantizar no perder datos -->
<!-- Si ciclo=15min y multiplo=2, overlap=30min -->
<add key="CarregaIncremental_OverlapMultiplo" value="2" />
```

---

## SOLUCIÓN RECOMENDADA #4: Versioning para Detectar Cambios

### Agregar tabla de auditoría:

```sql
CREATE TABLE integracio_modulab_historico (
	id INT PRIMARY KEY AUTO_INCREMENT,
	etiqueta_id VARCHAR(50),
	pacient_sap VARCHAR(50),
	prova_id INT,
	data_resultat DATETIME,
	data_validacio DATETIME NULL,

	-- Versioning
	version INT DEFAULT 1,
	hash_contenido VARCHAR(64),  -- SHA256

	-- Auditoría
	data_captura DATETIME,
	data_cambio DATETIME NULL,
	razon_cambio VARCHAR(200),  -- "Resultado sin validar", "Validación agregada", "Datos corregidos"

	INDEX idx_etiqueta_pacient (etiqueta_id, pacient_sap),
	INDEX idx_data_captura (data_captura)
);
```

### En código:
```csharp
public void GuardarResultadoConVersioning(Resultado resultado)
{
	// 1. Calcular hash actual
	string hashActual = CalcularHashSHA256(resultado);

	// 2. Buscar versión anterior
	varVersionAnterior = _historialRepository.ObtenerUltimaVersion(resultado.EtiquetaId, resultado.PacientSap, resultado.ProvaId);

	if (VersionAnterior == null)
	{
		// Primera captura
		_historialRepository.CrearVersion(resultado, hashActual, 1, "Primer captura - Sin validación");
	}
	else if (hashActual != VersionAnterior.HashContenido)
	{
		// Ha cambiado - crear nueva versión
		int nuevaVersion = VersionAnterior.Version + 1;
		string razon = GenerarRazonCambio(VersionAnterior, resultado);
		_historialRepository.CrearVersion(resultado, hashActual, nuevaVersion, razon);
		_logger.Info($"🔄 Versión {nuevaVersion} creada: {razon}");
	}
	else
	{
		// Idéntico - SKIP
		_logger.Debug($"⏭️ Contenido idéntico, no crear versión duplicada");
	}
}

private string GenerarRazonCambio(ResultadoHistorico anterior, Resultado actual)
{
	var cambios = new List<string>();

	if (anterior.DataValidacio == null && actual.DataValidacio != null)
		cambios.Add("Validación agregada");

	if (anterior.Microorganismo != actual.Microorganismo)
		cambios.Add($"Microorganismo: {anterior.Microorganismo} → {actual.Microorganismo}");

	if (anterior.Resistencia != actual.Resistencia)
		cambios.Add($"Resistencia: {anterior.Resistencia} → {actual.Resistencia}");

	return string.Join("; ", cambios);
}
```

---

## TABLA RESUMEN: Implementación por Facilidad

| Solución | Facilidad | Impacto | Timeline |
|----------|-----------|--------|----------|
| Deduplicación simple | 🟢 Fácil | 🔴 CRÍTICO | 1-2 días |
| Overlap dinámico | 🟢 Fácil | 🟠 Alto | 1 día |
| Tracking pendientes | 🟡 Media | 🟠 Alto | 2-3 días |
| Versioning | 🔴 Complejo | 🟡 Medio | 1 semana |
| Change detection | 🔴 Complejo | 🟡 Medio | 1 semana |

---

## ACCIÓN INMEDIATA RECOMENDADA

### Sprint 1 (Esta semana):
1. ✅ **Implementar deduplicación** (ya tienes mi fix del NULL)
2. ✅ **Aumentar overlap de 2 a 30 minutos**
3. ✅ **Mejorar logging** de duplicados encontrados

### Sprint 2 (Próxima semana):
4. Agregar `DataResultadoMinimaConPendiente` a tracking
5. Implementar hash-based change detection

### Sprint 3 (Futuro):
6. Versioning completo con auditoría

---

## VERDAD INCÓMODA

Tu sistema actual FUNCIONA pero tiene un "goteo" de duplicados y cambios no detectados. Mientras sea bajo volume, puede pasar desapercibido. Pero cuando escales o agregues más datos históricos, **la deuda técnica será CRÍTICA**.

Recomiendo implementar la deduplicación YA, antes de que sea demasiado tarde.
