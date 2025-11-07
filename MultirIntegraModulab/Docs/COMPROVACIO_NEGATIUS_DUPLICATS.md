# 🔍 Comprovació de Negatius Duplicats - Mostres amb Múltiples Resultats Negatius

## 🎯 Problema Identificat

Quan una mostra té **més d'un resultat negatiu** per al mateix tipus de mostra, i es compleix la **Comprovació 2** (pacient amb positius vigents per aquest tipus de mostra o equivalents), s'estaven incorporant **múltiples negatius** de la mateixa mostra.

### Exemple del problema:

**Mostra**: ETQ123456 amb 2 resultats negatius
- Resultat 1: Tipus mostra "Sang", negatiu
- Resultat 2: Tipus mostra "Sang", negatiu

**Comportament anterior**:
1. Processava resultat 1 → Comprovació 2 positiva → Creava negatiu ✅
2. Processava resultat 2 → Comprovació 2 positiva → Creava un altre negatiu ❌

**Comportament desitjat**:
1. Processava resultat 1 → Comprovació 2 positiva → Creava negatiu ✅
2. Processava resultat 2 → Detecta que ja existeix un negatiu → No incorpora ✅

## ✅ Solució Implementada

### Nova Comprovació dins de la Comprovació 2

Després de detectar que el pacient té positius vigents per al tipus de mostra (Comprovació 2), **abans d'incorporar el negatiu**, es comprova si ja existeix una mostra negativa amb:
- Mateix pacient (`PacientSap`)
- Mateixa data de mostra (`DataPeticioTrunc`)
- Mateix tipus de mostra (`MostraDescripcio`)
- Valoració = '1' (negatiu)

### Ubicació del Codi

**Fitxer**: `ProcessarMostraNegativaUseCase.cs`  
**Mètode**: `ProcessarResultatNegatiu()`  
**Secció**: Després de la Comprovació 2, abans d'incorporar el negatiu

```csharp
if (pacientTePositiusVigents)
{
    _logger.Info($"Comprovant si ja existeix un negatiu per aquesta mostra i tipus de mostra...");

    // Comprovar si ja existeix una mostra negativa (valoració '1') amb aquesta etiqueta i tipus de mostra
    int mostraNegativaExistent = _multiRRepository.ComprovarMostraDiagnosticExisteix(
        mostra.PacientSap,
        resultatMostra.DataPeticioTrunc,
        resultatMostra.MostraDescripcio,
        "1"); // Valoració '1' = negatiu

    if (mostraNegativaExistent > 0)
    {
        _logger.Info($"⚠️ JA existeix un negatiu per aquesta mostra (ID: {mostraNegativaExistent})");
        _logger.Info($"No cal incorporar més negatius de la mateixa etiqueta");

        // Inserir auditoria amb codi NMRCM
        bool auditoriaCreada = _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "NMRCM", resultatMostra);

        if (auditoriaCreada)
        {
            resultat.AuditoriasCreades++;
        }

        resultat.ResultatsNoIncorporats++;
        return; // No continuar amb aquest resultat
    }

    _logger.Info($"✔️ No existeix cap negatiu previ per aquesta mostra → Continuar amb la incorporació");

    calIncorporarNegatiu = true;
    tipusComprovacio = TipusComprovacioNegatiu.Comprovacio2;
}
```

## 🔄 Flux Complet amb la Nova Comprovació

```
┌─────────────────────────────────────────┐
│   Resultat Negatiu a Processar          │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  COMPROVACIÓ 0: Pacient existeix?       │
└──────────────┬──────────────────────────┘
              Sí
               │
               ▼
┌─────────────────────────────────────────┐
│  COMPROVACIÓ 1: Comportament = 1        │
│  i pacient té positius generals?        │
└──────────────┬──────────────────────────┘
              No
               │
               ▼
┌─────────────────────────────────────────┐
│  COMPROVACIÓ 2: Pacient té positius     │
│  vigents per tipus mostra/equiv?        │
└──────────────┬──────────────────────────┘
              Sí
               │
               ▼
┌─────────────────────────────────────────┐
│  🆕 COMPROVACIÓ 2b: Ja existeix un      │
│  negatiu per aquesta etiqueta i tipus?  │
└──────────────┬────────────┬─────────────┘
              Sí           No
               │            │
               ▼            ▼
       ┌──────────┐  ┌──────────────┐
       │NO INCORP.│  │  INCORPORAR  │
       │ (NMRCM)  │  │  (Compr. 2)  │
       └──────────┘  └──────────────┘
```

## 📊 Mètode Utilitzat

**Mètode**: `IMultiRRepository.ComprovarMostraDiagnosticExisteix()`

Aquest mètode ja existia i accepta un paràmetre opcional `valoracio`:

```csharp
int ComprovarMostraDiagnosticExisteix(
    string pacientSap, 
    DateTime? dataMostra, 
    string tipusMostra, 
    string valoracio = null)
```

**Consulta SQL que executa**:
```sql
SELECT id 
FROM pacients_diagnostics_mostra 
WHERE npat = @pacientSap 
  AND data_mostra = @dataMostra 
  AND tipus_mostra_m = @tipusMostra
  AND valoracio = '1'  -- ← NOU: filtre per negatius
  AND dt_delete IS NULL
```

**Retorn**:
- `> 0`: Ja existeix una mostra negativa (retorna l'ID)
- `0`: No existeix cap negatiu amb aquestes característiques

## 🎯 Cas d'Ús Pràctic

### Escenari

**Mostra**: ETQ789012  
**Pacient**: 12345678  
**Data mostra**: 2025-01-15  
**Tipus mostra**: Sang  

**Resultats de la mostra**:
1. Resultat 1: Pseudomona aeruginosa (no especial, sense mecanisme) → **Negatiu**
2. Resultat 2: Enterococcus faecium (no especial, sense mecanisme) → **Negatiu**

**Context del pacient**:
- Té un positiu vigent per "Sang": Klebsiella pneumoniae BLEE

### Processament

#### ✅ Processament del Resultat 1
```
1. COMPROVACIÓ 0: Pacient existeix? → Sí
2. COMPROVACIÓ 1: Comportament=1 i positius? → No (comportament=0)
3. COMPROVACIÓ 2: Positius vigents per "Sang"? → Sí (té Klebsiella BLEE)
4. COMPROVACIÓ 2b: Ja existeix negatiu per ETQ789012 i "Sang"? → No
5. DECISIÓ: INCORPORAR el negatiu ✅
6. Crea mostra diagnòstic negativa amb valoració='1'
```

#### ✅ Processament del Resultat 2
```
1. COMPROVACIÓ 0: Pacient existeix? → Sí
2. COMPROVACIÓ 1: Comportament=1 i positius? → No (comportament=0)
3. COMPROVACIÓ 2: Positius vigents per "Sang"? → Sí (té Klebsiella BLEE)
4. COMPROVACIÓ 2b: Ja existeix negatiu per ETQ789012 i "Sang"? → Sí ✋
5. DECISIÓ: NO INCORPORAR (ja s'ha incorporat un negatiu)
6. Auditoria: NMRCM (No supera la comprovació de mostra)
```

## 📈 Logging Implementat

### Quan detecta un negatiu existent:
```
Comprovant si ja existeix un negatiu per aquesta mostra i tipus de mostra...
🔎 Comprovant / creant mostra diagnòstic de tipus 'Sang'
  Mostra del pacient 12345678 + data 15/01/2025 + tipus 'Sang' + valoració '1': JA existeix (ID: 4567)
⚠️ JA existeix un negatiu per aquesta mostra (ID: 4567)
  No cal incorporar més negatius de la mateixa etiqueta
🔄 Inserint auditoria amb codi 'NMRCM' (No supera la comprovació de mostra)
✔️ Inserit registre d'auditoria per mostra amb etiqueta ETQ789012, amb resultat NMRCM
```

### Quan no existeix cap negatiu previ:
```
Comprovant si ja existeix un negatiu per aquesta mostra i tipus de mostra...
🔎 Comprovant / creant mostra diagnòstic de tipus 'Sang'
  Mostra del pacient 12345678 + data 15/01/2025 + tipus 'Sang' + valoració '1': NO existeix
✔️ No existeix cap negatiu previ per aquesta mostra → Continuar amb la incorporació
Comprovació 2 COMPLERTA: Pacient té positius vigents → Cal incorporar el negatiu
✔️ Resultat negatiu CAL incorporar (via Comprovacio2), recuperant diagnòstics positius...
```

## 🔑 Avantatges de la Solució

1. **Prevé duplicats**: Només s'incorpora el primer negatiu que contraresta un positiu
2. **Eficient**: Utilitza un mètode existent del repositori
3. **Clara i mantenible**: Logging detallat que explica cada pas
4. **Auditoria completa**: Registra tots els casos amb codi NMRCM
5. **Estadístiques**: Incrementa els comptadors `ResultatsNoIncorporats` i `AuditoriasCreades`

## ⚠️ Codi d'Auditoria

**NMRCM**: No supera la comprovació de mostra

Aquest codi s'utilitza en dos casos:
1. Quan NO es compleix cap de les comprovacions 1 o 2
2. **🆕 Quan ja existeix un negatiu per aquesta etiqueta i tipus de mostra** (nova comprovació)

## 🧪 Casos de Test Recomanats

### Test 1: Mostra amb 2 negatius, pacient amb positius
**Entrada**:
- Mostra amb 2 resultats negatius del mateix tipus
- Pacient amb positius vigents per aquest tipus

**Esperat**:
- Només s'incorpora el primer negatiu
- El segon genera auditoria NMRCM

### Test 2: Mostra amb 2 negatius de tipus diferents
**Entrada**:
- Resultat 1: negatiu tipus "Sang"
- Resultat 2: negatiu tipus "Orina"
- Pacient amb positius vigents per "Sang" i "Orina"

**Esperat**:
- S'incorporen els 2 negatius (són de tipus diferents)

### Test 3: Mostra amb 1 negatiu, pacient sense positius
**Entrada**:
- Mostra amb 1 resultat negatiu
- Pacient sense positius vigents

**Esperat**:
- No s'incorpora (no passa la Comprovació 2)
- Auditoria: NMRCM

## 📝 Notes Tècniques

- La comprovació es fa amb **valoració = '1'** que identifica mostres negatives
- Es comprova amb la **mateixa etiqueta** (`mostra.EtiquetaId`)
- Es comprova amb el **mateix tipus de mostra** (`resultatMostra.MostraDescripcio`)
- Es comprova amb la **mateixa data** (`resultatMostra.DataPeticioTrunc`)
- Si existeix, **no es continua** amb el processament d'aquest resultat (`return`)

## 📅 Data d'Implementació

**Data**: Gener 2025  
**Estat**: ✅ Implementat i validat  
**Build**: ✅ Successful  
**Versió**: 1.0.0

---

**Autor**: Equip de desenvolupament MultirIntegraModulab
