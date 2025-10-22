# 📋 Implementació Comprovació 2 - Mostres Negatives

## 🎯 Objectiu

Implementar la segona comprovació per determinar si cal incorporar un resultat negatiu:

**Comprovació 2**: Tipus de mostra a incorporar si el pacient ha tingut algun positiu per aquest tipus de mostra o equivalents, i el positiu és vigent.

## 📖 Descripció de la Comprovació

Si el comportament del tipus de mostra **no és 1**, es consulta l'historial del pacient per veure si té algun positiu **per aquest tipus de mostra o equivalents**, i aquest positiu encara **és vigent**.

### Conceptes Clau

1. **Tipus de mostra equivalent**: Tipus de mostra relacionats que es consideren equivalents a l'efecte de comptar positius (taula `tipusmostra_equivalents`)

2. **Positiu vigent**: Un positiu que no ha superat els `dies_vigencia_positiu` definits per al tipus de mostra:
   - Si `dies_vigencia_positiu IS NULL` → Sempre vigent
   - Si `data_mostra >= CURRENT_DATE - dies_vigencia_positiu` → Vigent

### Lògica Implementada

**Consulta SQL completa**:
```sql
SELECT COUNT(*) AS positius_vigents_tipus_mostra_i_equivalents 
FROM pacients_diagnostics_mostra pdm		 
JOIN tipusmostra_m tm ON pdm.tipus_mostra_m = tm.descripcio 		 
WHERE pdm.npat = 'PACIENT_SAP' 
  AND ( 
    UPPER(tm.descripcio) = UPPER(mostra_descripcio) 
    OR tm.id IN ( 
        SELECT tipusmostra_id_equivalent 
        FROM tipusmostra_equivalents 
        WHERE tipusmostra_id = ( 
            SELECT id  
            FROM tipusmostra_m tmm  
            WHERE UPPER(tmm.descripcio) = UPPER(mostra_descripcio) 
        ) 
    ) 
  ) 
  AND pdm.valoracio = '2' 
  AND ( 
    tm.dies_vigencia_positiu IS NULL 
    OR pdm.data_mostra >= DATE_SUB(CURRENT_DATE, INTERVAL tm.dies_vigencia_positiu DAY) 
  ) 
  AND pdm.dt_delete IS NULL 
  AND tm.dt_delete IS NULL
```

**Flux de decisió**:
1. Si **Comprovació 1** és positiva → Incorporar (no cal fer Comprovació 2)
2. Si **Comprovació 1** és negativa → Aplicar Comprovació 2
3. Si **Comprovació 2** és positiva → Incorporar el negatiu ✅
4. Si **Comprovació 2** és negativa → NO incorporar el negatiu ❌

## 🔧 Canvis Implementats

### 1. Interfície IMultiRRepository

**Fitxer**: `MultirIntegraModulab\Domain\Interfaces\IMultiRRepository.cs`

Afegit nou mètode:

```csharp
/// <summary>
/// Comprova si el pacient té algun diagnòstic positiu vigent per un tipus de mostra específic
/// i els seus tipus de mostra equivalents.
/// </summary>
bool PacientTePositiusVigentsTipusMostraIEquivalents(string pacientSap, string tipusMostra);
```

### 2. Servei de Base de Dades

**Fitxer**: `MultirIntegraModulab\Infrastructure\Persistence\LegacyServices\MultiRDbService.TipusMostra.cs`

Implementat el mètode `PacientTePositiusVigentsTipusMostraIEquivalents`:

#### Característiques
- Consulta la taula `pacients_diagnostics_mostra` amb JOIN a `tipusmostra_m`
- Inclou tipus de mostra equivalents via subconsulta a `tipusmostra_equivalents`
- Filtra per `valoracio = '2'` (positius)
- Comprova vigència segons `dies_vigencia_positiu`
- Retorna `bool` indicant si el pacient té almenys un positiu vigent
- Logging informatiu del nombre de positius vigents trobats

### 3. Enum TipusComprovacioNegatiu

**Fitxer**: `MultirIntegraModulab\Application\UseCases\ProcessarMostres\ProcessarMostraNegativaUseCase.cs`

Nou enum per identificar quin tipus de comprovació ha passat:

```csharp
/// <summary>
/// Tipus de comprovació que ha determinat que cal incorporar un negatiu
/// </summary>
public enum TipusComprovacioNegatiu
{
    /// <summary>
    /// No cal incorporar el negatiu
    /// </summary>
    Cap = 0,
    
    /// <summary>
    /// Comprovació 1: Tipus de mostra amb comportament 1 i pacient amb positius
    /// </summary>
    Comprovacio1 = 1,
    
    /// <summary>
    /// Comprovació 2: Pacient amb positius vigents per aquest tipus de mostra o equivalents
    /// </summary>
    Comprovacio2 = 2
}
```

### 4. Comptadors a ResultatProcessamentNegatiu

Afegits nous comptadors per fer seguiment de quin tipus de comprovació ha passat:

```csharp
// Comptadors per comprovacions
public int IncorporatsPerComprovacio1 { get; set; }
public int IncorporatsPerComprovacio2 { get; set; }
```

### 5. Use Case ProcessarMostraNegativaUseCase

**Fitxer**: `MultirIntegraModulab\Application\UseCases\ProcessarMostres\ProcessarMostraNegativaUseCase.cs`

Implementada la lògica completa de les dues comprovacions:

```csharp
// FASE 1: COMPROVACIONS
TipusComprovacioNegatiu tipusComprovacio = TipusComprovacioNegatiu.Cap;

// Comprovació 1: Comportament 1 + Positius generals
int? comportament = _multiRRepository.ObtenirComportamentTipusMostra(...);
if (comportament == 1)
{
    bool tePositius = _multiRRepository.PacientTePositiusAlgunTipusMostra(...);
    if (tePositius)
    {
        calIncorporarNegatiu = true;
        tipusComprovacio = TipusComprovacioNegatiu.Comprovacio1;
    }
}

// Comprovació 2: Positius vigents per tipus mostra + equivalents
if (!calIncorporarNegatiu)
{
    bool tePositiusVigents = _multiRRepository
        .PacientTePositiusVigentsTipusMostraIEquivalents(...);
    
    if (tePositiusVigents)
    {
        calIncorporarNegatiu = true;
        tipusComprovacio = TipusComprovacioNegatiu.Comprovacio2;
    }
}

// Incrementar comptador segons tipus
if (calIncorporarNegatiu)
{
    if (tipusComprovacio == TipusComprovacioNegatiu.Comprovacio1)
        resultat.IncorporatsPerComprovacio1++;
    else if (tipusComprovacio == TipusComprovacioNegatiu.Comprovacio2)
        resultat.IncorporatsPerComprovacio2++;
}
```

## 📊 Logging Implementat

### Comprovació 2 amb èxit:
```
🔍 Aplicant Comprovació 2: Positius vigents per aquest tipus de mostra o equivalents
  Pacient 12345678 té 2 positiu(s) vigent(s) per tipus mostra 'Sang' o equivalents
✓ Comprovació 2 COMPLERTA: Pacient té positius vigents → Cal incorporar el negatiu
✓ Resultat negatiu CAL incorporar (via Comprovacio2), processant...
```

### Comprovació 2 sense èxit:
```
🔍 Aplicant Comprovació 2: Positius vigents per aquest tipus de mostra o equivalents
  Pacient 12345678 NO té positius vigents per tipus mostra 'Orina' o equivalents
ℹ️ Resultat negatiu NO cal incorporar segons comprovacions
✓ Auditoria NMRCM creada per mostra ETQ123456
```

### Flux complet:
```
🔍 Comprovant si cal incorporar el negatiu per tipus mostra: Frotis rectal
ℹ️ Tipus de mostra amb comportament 0 (no aplica comprovació 1)
🔍 Aplicant Comprovació 2: Positius vigents per aquest tipus de mostra o equivalents
  Pacient 12345678 té 1 positiu(s) vigent(s) per tipus mostra 'Frotis rectal' o equivalents
✓ Comprovació 2 COMPLERTA: Pacient té positius vigents → Cal incorporar el negatiu
✓ Resultat negatiu CAL incorporar (via Comprovacio2), processant...
```

## 📈 Estadístiques al Resultat

El `ResultatProcessamentNegatiu` ara proporciona informació detallada:

```csharp
Mostra negativa ETQ123 processada correctament: 
  2 diagnòstics creats, 1 diagnòstics existents, 
  2 mostres creades, 1 mostres existents, 
  3 relacions creades, 0 duplicades, 
  3 resultats processats, 2 no incorporats, 
  1 incorporats per comprovació 1,    // ← NOU
  2 incorporats per comprovació 2,    // ← NOU
  6 auditories
```

## 🎯 Matriu de Decisions

| Comportament | Positius generals | Positius vigents tipus mostra | Decisió | Via |
|--------------|-------------------|-------------------------------|---------|-----|
| 1 | Sí | - | ✅ Incorporar | Comprovació 1 |
| 1 | No | Sí | ✅ Incorporar | Comprovació 2 |
| 1 | No | No | ❌ No incorporar | - |
| 0 (o altre) | - | Sí | ✅ Incorporar | Comprovació 2 |
| 0 (o altre) | - | No | ❌ No incorporar | - |

## 🔍 Taules Utilitzades

### tipusmostra_m
- **codi/descripcio**: Identificador del tipus de mostra
- **comportament**: Indica si aplica comprovació 1 (valor 1)
- **dies_vigencia_positiu**: Dies que un positiu es considera vigent

### tipusmostra_equivalents
- **tipusmostra_id**: ID del tipus de mostra principal
- **tipusmostra_id_equivalent**: ID del tipus de mostra equivalent

### pacients_diagnostics_mostra
- **npat**: Identificador del pacient
- **tipus_mostra_m**: Tipus de mostra del diagnòstic
- **valoracio**: '2' = Positiu
- **data_mostra**: Data de la mostra (per calcular vigència)

## 🎯 Codis d'Auditoria

- **NMRCM** (No supera la comprovació de mostra): No cal incorporar (cap comprovació passada)
- **OK**: Negatiu incorporat correctament (alguna comprovació passada)

## ✅ Validació

- ✅ Build exitosa sense errors
- ✅ Tots els fitxers compilen correctament
- ✅ Logging estructurat per ambdues comprovacions
- ✅ Gestió d'errors completa
- ✅ Comptadors separats per cada comprovació
- ✅ Enum per identificar tipus de comprovació
- ✅ Segueix Clean Architecture
- ✅ Compleix amb SOLID

## 🔄 Flux Complet de Processament

```
┌─────────────────────────────────────────┐
│   Mostra Negativa a Processar          │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  Obtenir comportament tipus mostra      │
└──────────────┬──────────────────────────┘
               │
               ▼
       ┌───────────────┐
       │ Comportament  │
       │    == 1?      │
       └───┬───────┬───┘
           │       │
          Sí      No
           │       │
           ▼       │
  ┌────────────────┐│
  │ COMPROVACIÓ 1  ││
  │ Positius       ││
  │ generals?      ││
  └────┬───────┬───┘│
       │       │    │
      Sí      No    │
       │       │    │
       │       └────┼────────────┐
       │            │            │
       │            ▼            ▼
       │   ┌─────────────────────────┐
       │   │    COMPROVACIÓ 2        │
       │   │  Positius vigents per   │
       │   │  tipus mostra/equiv?    │
       │   └────┬─────────┬──────────┘
       │        │         │
       │       Sí        No
       │        │         │
       ▼        ▼         ▼
  ┌────────┐ ┌────────┐ ┌─────────────┐
  │INCORP. │ │INCORP. │ │ NO INCORP.  │
  │Compr.1 │ │Compr.2 │ │ (NMRCM)     │
  └────────┘ └────────┘ └─────────────┘
```

## 🔜 Avantatges de la Implementació

1. **Traçabilitat**: Sabem exactament per què s'ha incorporat cada negatiu
2. **Anàlisi**: Els comptadors permeten analitzar l'eficàcia de cada comprovació
3. **Logging detallat**: Cada pas és visible als logs
4. **Mantenibilitat**: Enum i estructura clara faciliten futurs canvis
5. **Performance**: Consultes SQL optimitzades amb JOINs i subconsultes eficients

## 📝 Notes Tècniques

- La **Comprovació 1** té prioritat sobre la **Comprovació 2** (es comprova primer)
- Si la **Comprovació 1** passa, **no es fa la Comprovació 2** (optimització)
- Els tipus de mostra equivalents permeten flexibilitat en la classificació
- La vigència es calcula dinàmicament amb `DATE_SUB(CURRENT_DATE, ...)`
- Totes les consultes respecten `dt_delete IS NULL` (soft deletes)

---

**Data d'implementació**: Gener 2025  
**Estat**: ✅ Completat i validat  
**Versió**: 1.0.0
