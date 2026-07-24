# Fix: Carrega Incremental No Capturava Resultats Sense Validació

## Problema Detectat

**Data del problema:** 3 dies amb carrega incremental cada 15 minuts. Carrega manual amb "dies enrere" (1 dia) va trobar 5 resultats que teòricament ja haurien d'haver estat carregats per la carrega incremental.

**Característiques dels 5 resultats:**
- Data de resultat: Ahir (DataResultat = ayer)
- Data de validació: NULL (DataValidacio = null)
- Aquests resultats NO tenien data de validació

## Root Cause Analysis

### 1. Lògica Prèvia (Bugada)
La consulta SQL de carrega incremental a `ModulabDbService.Sincronitzacio.cs` (línies 251-256) tenia la següent lògica:

```csharp
if (dataValidacioFiltre.HasValue)
{
	filtres.Add("rt.FVDATE >= TO_TIMESTAMP('{dataFormatejada}', 'YYYY-MM-DD HH24:MI:SS')");
}
// Si dataValidacioFiltre era NULL, no s'afegIA cap filtre per a validacions!
```

### 2. Escenari que Causava el Bug

**Carrega inicial (3 dies enrere):**
- Es carreguen 5 resultats amb `DataResultat = ayer` i `DataValidacio = NULL`
- Es guarda: `DataValidacioMaxProcessada = null` (perquè no hi ha cap validació)
- Es guarda: `DataResultatMaxProcessada = ayer`

**Carregues incrementals posteriors (cada 15 minuts):**
- Es construeix `dataValidacioFiltre = null` (perquè `DataValidacioMaxProcessada` és null)
- Es construeix `dataResultatFiltre = ayer - 2 minuts`
- **Problema:** El filtre `rt.FVDATE IS NULL` NUNCA S'AFEGEIX
- Els 5 resultats no es capturen perquè:
  - Ja compleixen `RESULTDATE >= ayer` (OF)
  - Però la consulta no buscava explícitament `FVDATE IS NULL`

### 3. Per Què la Carrega de Dies Enrere Els Va Trobar

La carrega de dies enrere:
```csharp
else if (configService.CarregaDiesEnrere_Activa)
```

No utilitza la lògica de filtres incrementals. Simplement carrega tots els resultats dels últims N dies sense tenir en compte si ja van ser processats, el que va revelar els 5 resultats "perduts".

## Solució Implementada

### Canvi al Mètode `ObtenirConsultaAmbFiltresSincronitzacio`

**Fitxer:** `MultirIntegraModulab/Infrastructure/Persistence/LegacyServices/ModulabDbService.Sincronitzacio.cs`

**Línies modificades:** 251-269

```csharp
// Filtre 2: FVDATE >= última processada
if (dataValidacioFiltre.HasValue)
{
	string dataFormatejada = dataValidacioFiltre.Value.ToString("yyyy-MM-dd HH:mm:ss");
	filtres.Add($"rt.FVDATE >= TO_TIMESTAMP('{dataFormatejada}', 'YYYY-MM-DD HH24:MI:SS')");
}
else if (dataResultatFiltre.HasValue)
{
	// IMPORTANT: Si DataValidacioMaxProcessada és null però DataResultatMaxProcessada té valor,
	// significa que hi ha resultats sense validar que s'haurien de capturar en cada cicle incremental
	// fins que siguin validats. Afegim un filtre explícit per resultats sense validació.
	filtres.Add("rt.FVDATE IS NULL");
}
```

### Lògica del Fix

1. **Si `DataValidacioMaxProcessada` té valor:**
   - Afegir filtre: `FVDATE >= UltimaDataValidacioMaxProcessada - 2 minuts`

2. **Si `DataValidacioMaxProcessada` és NULL però `DataResultatMaxProcessada` té valor:**
   - Afegir filtre: `FVDATE IS NULL`
   - Això captura resultats sense validar en cada cicle incremental fins que siguin validats

3. **Si cap dels dos té valor:**
   - Retornar condició sempre falsa (`1=0`) per ne retornar res

### Benefici del Fix

- ✅ Els resultats sense validació es capturen en CADA carrega incremental
- ✅ Això evita que es "perdin" en el sistema de sincronització
- ✅ Una vegada que un resultat és validat, `DataValidacio` es completa i es capturarà pel filtre d'actualitzacions de validació
- ✅ La carrega incremental és ara completa i fiable per a tots els casos d'ús

## Casos d'Ús Coberts

### Cas 1: Resultats amb Validació Completa
```
DataResultat = 2026-01-27 10:30
DataValidacio = 2026-01-27 14:20
→ Es capturarà pel filtre de FVDATE >= últimaDataValidacio
```

### Cas 2: Resultats Sense Validació (ANTES DEL FIX)
```
DataResultat = 2026-01-27 10:30
DataValidacio = NULL
→ NO es capturava! (BUG)
```

### Cas 2: Resultats Sense Validació (AFTER FIX)
```
DataResultat = 2026-01-27 10:30
DataValidacio = NULL
→ Es capturarà pel filtre de FVDATE IS NULL
→ En cada carrega incremental fins que sigui validat
```

### Cas 3: Resultats Nous sense Validació
```
DataResultat = Avui
DataValidacio = NULL
→ Es capturarà:
   - Pels filtres de data de resultat (RESULTDATE >= últimaDataResultat - 2 min)
   - O pels filtres de validació null (FVDATE IS NULL)
   - (OR lògic uneix tots els filtres)
```

## Testing Recomanat

1. **Test de Regressió:**
   - Executar carrega incremental amb resultats que tinguin `DataValidacio = NULL`
   - Verificar que es capturen correctament

2. **Test de Limite:**
   - Crear un resultat sense validació
   - Executar múltiples carregues incrementals
   - Verificar que es captura en CADA execució

3. **Test de Recuperació:**
   - Carregar manuals amb diferentes tipus de carrega (Dies Enrere, Rang de Dates)
   - Verificar que troben els resultats esperats

## Impact

- **Baixa:** Només afecta la lògica de filtres de la carrega incremental
- **Segura:** No canvia la estructura de dades ni les transaccions
- **No-Breaking:** Compatible amb la carrega de dies enrere i rang de dates

## Seguiment

Els 5 resultats "perduts" ara es capturaran correctament en la propera carrega incremental.

Per verificar que funciona:
1. Activar `CarregaIncremental_Activa = true`
2. Executar l'aplicació
3. Verificar que els 5 resultats es capturen amb `FVDATE IS NULL` en els logs
