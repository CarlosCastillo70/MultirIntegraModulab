# Implementació: Actualització Data Última Mostra en Seguiments

## Context
Quan s'incorpora una mostra (positiva o negativa) de **MultiResistent**, cal actualitzar automàticament el camp `dt_ultima_mostra` en els seguiments oberts del pacient per al tipus de mostra corresponent.

## Canvis a la Base de Dades

### Nous camps afegits:

**Taula `pacients_seguiments`:**
```sql
ALTER TABLE pacients_seguiments 
ADD COLUMN dt_ultima_mostra TIMESTAMP DEFAULT NULL NULL 
COMMENT 'Data del darrer resultat de mostra';
```

**Taula `pacients_seguiments_mostres`:**
```sql
ALTER TABLE pacients_seguiments_mostres 
ADD COLUMN dt_ultima_mostra TIMESTAMP DEFAULT NULL NULL 
COMMENT 'Data del darrer resultat de mostra';
```

## Arquitectura de la Solució

### 1. Nou Mètode al Servei de Base de Dades

**Fitxer**: `MultiRDbService.Seguiments.cs`

**Mètode**: `ActualitzarDataUltimaMostra(string npat, string tipusMostra)`

#### Funcionalitat:
1. **Validació de paràmetres**: npat i tipusMostra
2. **Cerca de seguiments oberts**:
   - Consulta `pacients_seguiments` i `pacients_seguiments_mostres`
   - Filtra per `estat = 'O'` i tipus de mostra
3. **Actualització de dates**:
   - Actualitza `dt_ultima_mostra` a `pacients_seguiments` amb `NOW()`
   - Actualitza `dt_ultima_mostra` a `pacients_seguiments_mostres` amb `NOW()`
4. **Retorna**: True si s'ha actualitzat almenys un seguiment

#### SQL executat:
```sql
-- Actualitzar pacients_seguiments
UPDATE pacients_seguiments
SET dt_ultima_mostra = NOW()
WHERE id = @seguimentId

-- Actualitzar pacients_seguiments_mostres
UPDATE pacients_seguiments_mostres
SET dt_ultima_mostra = NOW()
WHERE id = @mostraSeguimentId
```

### 2. Interfície del Domini

**Fitxer**: `IMultiRRepository.cs`

Afegit mètode:
```csharp
bool ActualitzarDataUltimaMostra(string npat, string tipusMostra);
```

### 3. Adaptador de Repositori

**Fitxer**: `MultiRRepository.cs`

Implementació de l'adaptador que delega al servei de base de dades.

### 4. Integració amb Use Cases

#### 4.1. ProcessarMostraPositivaUseCase

**Punt d'integració**: Després de crear una nova mostra diagnòstic positiva

```csharp
if (tipusMicroorganisme == Domain.Enums.TipusMicroorganisme.Multiresistent)
{
	// ... actualització de targetes ...

	// Actualitzar data última mostra
	try
	{
		_multiRRepository.ActualitzarDataUltimaMostra(
			mostra.PacientSap,
			resultatMostra.MostraDescripcio);
	}
	catch (Exception exData)
	{
		_logger.Warning($"⚠️ Error actualitzant data última mostra: {exData.Message}");
	}
}
```

#### 4.2. ProcessarMostraNegativaUseCase

**Punt d'integració**: Després de crear una nova mostra diagnòstic negativa

```csharp
if (nouMostraDiagnosticId > 0)
{
	mostraDiagnosticIdFinal = nouMostraDiagnosticId;
	mostraDiagnosticCreada = true;
	resultat.MostresDiagnosticCreades++;

	// Actualitzar data última mostra (només per Multiresistent)
	var tipusMicroorganisme = _multiRRepository.ObtenirTipusMicroorganisme(resultatMostra.AillamentDescripcio);

	if (tipusMicroorganisme == Domain.Enums.TipusMicroorganisme.Multiresistent)
	{
		try
		{
			_multiRRepository.ActualitzarDataUltimaMostra(
				mostra.PacientSap,
				resultatMostra.MostraDescripcio);
		}
		catch (Exception exData)
		{
			_logger.Warning($"⚠️ Error actualitzant data última mostra: {exData.Message}");
		}
	}
}
```

## Flux d'Execució

### Per Mostres Positives:
```
1. Mostra positiva MultiResistent entra al sistema
2. ProcessarMostraPositivaUseCase crea la mostra diagnòstic
3. Actualitza quantitat de targetes (si cal)
4. Crida ActualitzarDataUltimaMostra()
   - Troba seguiments oberts per aquest tipus de mostra
   - Actualitza dt_ultima_mostra a NOW() a ambdues taules
5. Continua amb el processament normal
```

### Per Mostres Negatives:
```
1. Mostra negativa MultiResistent entra al sistema
2. ProcessarMostraNegativaUseCase determina si cal incorporar-la
3. Si cal, crea la mostra diagnòstic negativa
4. Comprova si és MultiResistent
5. Crida ActualitzarDataUltimaMostra()
   - Troba seguiments oberts per aquest tipus de mostra
   - Actualitza dt_ultima_mostra a NOW() a ambdues taules
6. Continua amb el processament normal
```

## Punts Crítics de la Implementació

### ✅ Imprescindibles

1. **Només MultiResistent**:
   ```csharp
   if (tipusMicroorganisme == Domain.Enums.TipusMicroorganisme.Multiresistent)
   ```
   → NO s'aplica a Virus Respiratoris!

2. **Només seguiments oberts**:
   ```sql
   AND ps.estat = 'O'
   ```

3. **Actualització de ambdues taules**:
   - `pacients_seguiments.dt_ultima_mostra`
   - `pacients_seguiments_mostres.dt_ultima_mostra`

4. **Gestió d'errors no bloquejant**:
   ```csharp
   try { ... } catch { warning } // Continua processament
   ```

### 📊 Logs Generats

- **Debug**: Detalls per cada seguiment actualitzat
- **Info**: Confirmació d'actualització global
- **Warning**: Errors no bloquejants

**Exemple de log**:
```
📅 Actualitzant data última mostra en seguiments per pacient 12345678, tipus mostra 'Frotis rectal'
📋 Trobats 2 seguiment(s) obert(s) per actualitzar
   ✔️ Actualitzat dt_ultima_mostra a pacients_seguiments (ID 123)
   ✔️ Actualitzat dt_ultima_mostra a pacients_seguiments_mostres (ID 456)
   ✔️ Actualitzat dt_ultima_mostra a pacients_seguiments (ID 789)
   ✔️ Actualitzat dt_ultima_mostra a pacients_seguiments_mostres (ID 012)
✅ Data última mostra actualitzada en 2 seguiment(s)
```

## Casos d'Ús

### Cas 1: Pacient amb seguiment obert - Nova mostra positiva
```
Estat inicial:
- Seguiment ID 123: dt_ultima_mostra = 2025-02-10 10:30:00

Nova mostra positiva: 2025-02-13 15:45:00

Resultat:
- Seguiment ID 123: dt_ultima_mostra = 2025-02-13 15:45:23 (NOW())
```

### Cas 2: Pacient amb seguiment obert - Nova mostra negativa
```
Estat inicial:
- Seguiment ID 456: dt_ultima_mostra = 2025-02-12 08:15:00

Nova mostra negativa: 2025-02-13 16:20:00

Resultat:
- Seguiment ID 456: dt_ultima_mostra = 2025-02-13 16:20:45 (NOW())
```

### Cas 3: Pacient sense seguiments oberts
```
Nova mostra positiva: 2025-02-13 14:00:00

Resultat:
- No s'actualitza res (log: "No hi ha seguiments oberts per actualitzar")
```

### Cas 4: Virus Respiratori (NO actualitza)
```
Nova mostra positiva de Virus Respiratori

Resultat:
- NO s'actualitza dt_ultima_mostra (només per MultiResistent)
```

## Beneficis de la Implementació

1. **Traçabilitat**: Es pot saber quan va arribar l'última mostra d'un seguiment
2. **Informació actualitzada**: Els seguiments mostren sempre la data de l'última mostra
3. **Automatització**: No cal intervenció manual
4. **Consistència**: Les dues taules es mantenen sincronitzades
5. **Robusta**: Gestiona errors sense bloquejar el processament

## Compatibilitat

- **Mostres positives**: ✅ Implementat
- **Mostres negatives**: ✅ Implementat
- **Virus Respiratori**: ❌ NO aplica (només MultiResistent)
- **Seguiments tancats**: ❌ NO actualitza (només seguiments oberts)

## Testing Recomanat

1. **Test amb seguiment obert + mostra positiva**: Hauria d'actualitzar dt_ultima_mostra
2. **Test amb seguiment obert + mostra negativa**: Hauria d'actualitzar dt_ultima_mostra
3. **Test sense seguiments oberts**: No hauria d'actualitzar res
4. **Test amb Virus Respiratori**: NO hauria d'actualitzar dt_ultima_mostra
5. **Test amb múltiples seguiments oberts**: Hauria d'actualitzar tots correctament

## Relació amb Altres Funcionalitats

Aquesta funcionalitat és complementària a:
- **ActualitzarQuantitatTargetes**: Actualitza el nombre de targetes necessàries
- Ambdues es criden quan s'incorpora una mostra MultiResistent

## Data d'Implementació
13 de febrer de 2025

## Desenvolupador
Implementació automatitzada amb GitHub Copilot
