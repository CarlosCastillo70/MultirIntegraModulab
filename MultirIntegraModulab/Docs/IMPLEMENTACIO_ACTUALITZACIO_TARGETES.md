# Implementació: Actualització Automàtica de Targetes en Seguiments

## Context
Quan es detecta una mostra positiva de **MultiResistent** (no Virus Respiratori), cal recalcular automàticament el nombre de targetes necessàries en els seguiments oberts per assolir l'objectiu de descolonització: **3 mostres negatives consecutives**.

**NOTA**: Aquest document descriu l'actualització de la **quantitat de targetes**. Per a l'actualització de la **data última mostra**, consulteu: [`IMPLEMENTACIO_DATA_ULTIMA_MOSTRA.md`](IMPLEMENTACIO_DATA_ULTIMA_MOSTRA.md)

## Funcionalitats Relacionades

Aquest fitxer forma part d'un conjunt de funcionalitats de seguiments:

| Funcionalitat | Fitxer | Descripció |
|--------------|--------|------------|
| **Quantitat de targetes** | Aquest fitxer | Recalcula el nombre de targetes necessàries |
| **Data última mostra** | [`IMPLEMENTACIO_DATA_ULTIMA_MOSTRA.md`](IMPLEMENTACIO_DATA_ULTIMA_MOSTRA.md) | Actualitza `dt_ultima_mostra` quan arriba una mostra |

Ambdues funcionalitats:
- S'executen automàticament quan s'incorpora una mostra MultiResistent (positiva o negativa)
- Actualitzen els seguiments oberts del pacient
- NO s'apliquen a Virus Respiratoris

## Arquitectura de la Solució

### 1. Nou Fitxer de Servei de Base de Dades
**Fitxer**: `MultirIntegraModulab/Infrastructure/Persistence/LegacyServices/MultiRDbService.Seguiments.cs`

**Mètode principal**: `ActualitzarQuantitatTargetes(string npat, string tipusMostra)`

#### Algorisme implementat (8 passos):

1. **Validació de paràmetres**: npat i tipusMostra
2. **Obtenir seguiments oberts**: 
   - Consulta `pacients_seguiments` i `pacients_seguiments_mostres`
   - Filtra per `estat = 'O'` i tipus de mostra
3. **Obtenir mostres del seguiment**:
   - Consulta `pacients_diagnostics_mostra`
   - **CRÍTIC**: Filtra per `data_mostra >= data_inici_seguiment`
   - Només mostres no esborrades i amb valoració
4. **Trobar última mostra positiva**:
   - Cerca des del final cap al principi
   - Identifica mostres amb `valoracio = '2'` (positiu)
5. **Comptar mostres després de l'última positiva**:
   - Si hi ha positiva: `mostres_després = total - index_positiva - 1`
   - Si no hi ha positiva: `mostres_després = total`
6. **Calcular espais lliures necessaris**:
   - `espais_lliures = max(0, 3 - mostres_després)`
7. **Calcular nova quantitat**:
   - `nova_quantitat = total_mostres + espais_lliures`
8. **Actualitzar només si cal**:
   - Només si `nova_quantitat > quantitat_actual`

### 2. Interfície del Domini
**Fitxer**: `MultirIntegraModulab/Domain/Interfaces/IMultiRRepository.cs`

Afegit mètode:
```csharp
bool ActualitzarQuantitatTargetes(string npat, string tipusMostra);
```

### 3. Adaptador de Repositori
**Fitxer**: `MultirIntegraModulab/Infrastructure/Persistence/Repositories/MultiRRepository.cs`

Implementació de l'adaptador que delega al servei de base de dades.

### 4. Integració amb Use Case
**Fitxer**: `MultirIntegraModulab/Application/UseCases/ProcessarMostres/ProcessarMostraPositivaUseCase.cs`

**Punt d'integració**: Després de crear una nova mostra diagnòstic positiva (línia ~376)

#### Flux d'integració:
1. Crear mostra diagnòstic positiva
2. Obtenir tipus de microorganisme: `ObtenirTipusMicroorganisme()`
3. **Si és MultiResistent**:
   - Cridar `ActualitzarQuantitatTargetes()`
   - Gestionar errors amb try-catch (no bloquejar processament)
4. Continuar amb el processament normal

## Exemples de Càlcul

### Exemple 1: N-- + Positiu nou
```
Mostres: [N] (1 mostra)
Última positiva: cap
Mostres després: 1
Espais necessaris: 3 - 1 = 2
Nova quantitat: 1 + 2 = 3 targetes
Resultat: N-- → NP---
```

### Exemple 2: NN- + Positiu nou
```
Mostres: [N, N] (2 mostres)
Després d'afegir P: [N, N, P] (3 mostres)
Última positiva: índex 2
Mostres després: 3 - 2 - 1 = 0
Espais necessaris: 3 - 0 = 3
Nova quantitat: 3 + 3 = 6 targetes
Resultat: NN- → NNP---
```

### Exemple 3: NNPP--- + Positiu nou
```
Mostres abans: [N, N, P, P] (4 mostres)
Després d'afegir P: [N, N, P, P, P] (5 mostres)
Última positiva: índex 4
Mostres després: 5 - 4 - 1 = 0
Espais necessaris: 3 - 0 = 3
Nova quantitat: 5 + 3 = 8 targetes
Resultat: NNPP--- → NNPPP---
```

## Punts Crítics de la Implementació

### ✅ Imprescindibles
1. **Filtre per data d'inici del seguiment**:
   ```sql
   AND data_mostra >= data_inici_seguiment
   ```
   → Sense això, comptaria mostres antigues fora del seguiment actual!

2. **Només seguiments oberts**:
   ```sql
   AND ps.estat = 'O'
   ```

3. **Només MultiResistent**:
   ```csharp
   if (tipusMicroorganisme == Domain.Enums.TipusMicroorganisme.Multiresistent)
   ```
   → NO s'aplica a Virus Respiratoris!

4. **Gestió d'errors no bloquejant**:
   ```csharp
   try { ... } catch { warning } // Continua processament
   ```

### 📊 Logs Generats
- **Debug**: Detalls de càlcul per cada seguiment
- **Info**: Confirmació d'actualització de targetes + **Patró visual de mostres**
- **Warning**: Errors no bloquejants

#### Patró Visual de Mostres
La funció mostra un patró visual de les mostres al log per facilitar la comprensió:

**Codis utilitzats**:
- **P** = Positiu (valoració = '2')
- **N** = Negatiu (valoració = '1')
- **X** = No vàlid (valoració = '3')
- **?** = Pendent (valoració = '0')
- **-** = Espai lliure (targetes pendents)

**Exemple de log**:
```
📋 Trobades 5 mostra(es) amb tipus mostra 'Frotis rectal' des de l'inici del seguiment
🔍 Patró actual de mostres: PNNPN
   🔴 Última mostra positiva trobada a l'índex 4 (Data: 13/02/2025)
Mostres després de l'última positiva: 0
Espais lliures necessaris: 3
Nova quantitat calculada: 8 (actual: 7) → Patró resultant: PNNPNP---
✅ Targetes actualitzades: 7 → 8 (seguiment ID 789)
```

Veure més exemples a: `EXEMPLES_LOGS_PATRO_MOSTRES.md`

## Taules de Base de Dades Utilitzades

### Lectura
- `pacients_seguiments`: id, npat, data_inici_seguiment, estat
- `pacients_seguiments_mostres`: id, seguiment_id, tipus_mostra, quantitat
- `pacients_diagnostics_mostra`: id, npat, tipus_mostra_m, data_mostra, valoracio

### Escriptura
- `pacients_seguiments_mostres`: UPDATE quantitat

### Codis de Valoració
- `'1'`: Negatiu
- `'2'`: Positiu ⚠️
- `'3'`: No vàlid
- `'0'`: Pendent

## Beneficis de la Implementació

1. **Automàtica**: No cal intervenció manual
2. **Segura**: Filtra correctament per seguiments actius i dates
3. **Robusta**: Gestiona errors sense bloquejar el processament
4. **Traçable**: Logs extensius per debugging
5. **Mantenible**: Segueix Clean Architecture (Domain, Application, Infrastructure)
6. **Testable**: Lògica aïllada i desacoblada

## Notes d'Implementació per Altres Aplicatius

Si necessiteu implementar aquesta funcionalitat en un altre sistema:

1. Adaptar les consultes SQL al vostre SGBD
2. Mantenir el filtre per `data_inici_seguiment`
3. Respectar l'ordre cronològic de les mostres
4. Gestionar errors de forma no bloquejant
5. Afegir logs extensius per facilitar debugging

## Testing Recomanat

1. **Test amb seguiment sense mostres**: Hauria de mantenir la quantitat actual
2. **Test amb seguiment amb mostres negatives**: Hauria de calcular correctament els espais
3. **Test amb seguiment amb positius i negatius**: Hauria de trobar l'última positiva
4. **Test amb múltiples seguiments oberts**: Hauria d'actualitzar tots correctament
5. **Test amb Virus Respiratori**: NO hauria d'actualitzar targetes

## Data d'Implementació
13 de febrer de 2025

## Desenvolupador
Implementació automatitzada amb GitHub Copilot
