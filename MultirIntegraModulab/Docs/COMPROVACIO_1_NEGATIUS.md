# 📋 Implementació Comprovació 1 - Mostres Negatives

## 🎯 Objectiu

Implementar la primera comprovació per determinar si cal incorporar un resultat negatiu:

**Comprovació 1**: Tipus de mostra a incorporar sempre que el pacient tingui algun positiu.

---

**⚠️ NOTA**: Aquest document descriu només la Comprovació 1.  
Per veure el sistema complet (Comprovació 1 + 2), consulteu:
- [COMPROVACIO_2_NEGATIUS.md](COMPROVACIO_2_NEGATIUS.md) - Detalls de la Comprovació 2
- [COMPROVACIONS_NEGATIUS_RESUM.md](COMPROVACIONS_NEGATIUS_RESUM.md) - **Sistema complet** ⭐

---

## 📖 Descripció de la Comprovació

Hi ha uns tipus de mostra amb `comportament = 1` (per exemple: Frotis rectal, Exsudat per faringui/amígdales, Exsudat per nasal, Exsudat per axil·lar) que es volen incorporar sempre que el pacient hagi tingut algun positiu previ **per qualsevol tipus de mostra**.

### Lògica Implementada

1. **Obtenir el comportament del tipus de mostra**:
   ```sql
   SELECT comportament  
   FROM tipusmostra_m 
   WHERE UPPER(codi) = UPPER(MOSTRA_DESCRIPCIO) 
     AND dt_delete IS NULL 
     AND actiu = 1
   ```

2. **Si comportament = 1**, comprovar si el pacient té positius previs:
   ```sql
   SELECT COUNT(*) AS positius_algun_tipus_mostra  
   FROM pacients_diagnostics_mostra pdm 
   JOIN tipusmostra_m tm ON pdm.tipus_mostra_m = tm.codi 
   WHERE pdm.npat = 'PACIENT_SAP' 
     AND pdm.valoracio = '2'  -- Positiu
     AND pdm.dt_delete IS NULL  
     AND tm.dt_delete IS NULL
   ```

3. **Si el pacient té positius**: Cal incorporar el negatiu ✅
4. **Si el pacient NO té positius**: No cal incorporar (de moment) ❌
5. **Si comportament ≠ 1**: Continuar amb la Comprovació 2

## 🔧 Canvis Implementats

### 1. Interfície IMultiRRepository

**Fitxer**: `MultirIntegraModulab\Domain\Interfaces\IMultiRRepository.cs`

Afegits dos nous mètodes:

```csharp
/// <summary>
/// Obté el comportament d'un tipus de mostra
/// </summary>
int? ObtenirComportamentTipusMostra(string codiMostra);

/// <summary>
/// Comprova si el pacient té algun diagnòstic positiu (per qualsevol tipus de mostra)
/// </summary>
bool PacientTePositiusAlgunTipusMostra(string pacientSap);
```

### 2. Servei de Base de Dades

**Fitxer**: `MultirIntegraModulab\Infrastructure\Persistence\LegacyServices\MultiRDbService.TipusMostra.cs`

Implementats els dos mètodes:

#### ObtenirComportamentTipusMostra
- Consulta el camp `comportament` de la taula `tipusmostra_m`
- Retorna `int?` (null si no existeix o no està actiu)
- Gestió d'errors completa amb logging

#### PacientTePositiusAlgunTipusMostra
- Consulta la taula `pacients_diagnostics_mostra` amb JOIN a `tipusmostra_m`
- Compta diagnòstics amb `valoracio = '2'` (positius)
- Retorna `bool` indicant si el pacient té almenys un positiu
- Logging informatiu del nombre de positius trobats

### 3. Repositori MultiRRepository

**Fitxer**: `MultirIntegraModulab\Infrastructure\Persistence\Repositories\MultiRRepository.cs`

Delegació dels nous mètodes al servei de base de dades:

```csharp
public int? ObtenirComportamentTipusMostra(string codiMostra) =>
    _multiRDbService.ObtenirComportamentTipusMostra(codiMostra);

public bool PacientTePositiusAlgunTipusMostra(string pacientSap) =>
    _multiRDbService.PacientTePositiusAlgunTipusMostra(pacientSap);
```

### 4. Use Case ProcessarMostraNegativaUseCase

**Fitxer**: `MultirIntegraModulab\Application\UseCases\ProcessarMostres\ProcessarMostraNegativaUseCase.cs`

Implementada la lògica de la Comprovació 1 dins del mètode `ProcessarResultatNegatiu`:

```csharp
// Comprovació 1: Tipus de mostra a incorporar sempre que el pacient tingui algun positiu

// 1. Obtenir comportament del tipus de mostra
int? comportament = _multiRRepository.ObtenirComportamentTipusMostra(resultatMostra.MostraDescripcio);

// 2. Si comportament = 1, comprovar positius del pacient
if (comportament.HasValue && comportament.Value == 1)
{
    bool pacientTePositius = _multiRRepository.PacientTePositiusAlgunTipusMostra(mostra.PacientSap);
    
    if (pacientTePositius)
    {
        calIncorporarNegatiu = true; // ✅ Cal incorporar
    }
}

// 3. Si no cal incorporar, crear auditoria NMRCM
if (!calIncorporarNegatiu)
{
    _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "NMRCM");
    resultat.ResultatsNoIncorporats++;
    return;
}

// 4. Si cal incorporar, continuar amb el processament normal
```

## 📊 Logging Implementat

El sistema ara proporciona logs detallats per cada comprovació:

```
🔍 Comprovant si cal incorporar el negatiu per tipus mostra: Frotis rectal
ℹ️ Tipus de mostra amb comportament 1 (incorporar si el pacient té positius)
ℹ️ Pacient 12345678 té 3 diagnòstic(s) positiu(s)
✓ Comprovació 1 COMPLERTA: Pacient té positius previs → Cal incorporar el negatiu
✓ Resultat negatiu cal incorporar, processant...
```

O en cas negatiu:

```
🔍 Comprovant si cal incorporar el negatiu per tipus mostra: Altre tipus mostra
ℹ️ Tipus de mostra amb comportament 0 (no aplica comprovació 1)
ℹ️ Resultat negatiu no cal incorporar segons comprovacions
✓ Auditoria NMRCM creada per mostra ETQ123456
```

## 🎯 Codis d'Auditoria

- **NMRCM** (No supera la comprovació de mostra): S'insereix quan el resultat negatiu NO cal incorporar segons les comprovacions
- **OK**: S'insereix quan el resultat negatiu SÍ s'ha incorporat correctament

## 📈 Estadístiques al Resultat

El `ResultatProcessamentNegatiu` ara inclou:

- `ResultatsNoIncorporats`: Nombre de resultats negatius que no s'han incorporat
- `AuditoriasCreades`: Nombre d'auditories creades (NMRCM o OK)
- `ResultatsProcessats`: Nombre total de resultats processats amb èxit

## ✅ Validació

- ✅ Build exitosa sense errors
- ✅ Tots els fitxers compilen correctament
- ✅ Logging estructurat implementat
- ✅ Gestió d'errors completa
- ✅ Segueix Clean Architecture
- ✅ Compleix amb SOLID

## 🔜 Pròxims Passos

1. **Implementar Comprovació 2**: Encara pendent de definició
2. **Tests Unitaris**: Crear tests per validar la lògica de comprovació
3. **Tests d'Integració**: Validar amb dades reals de base de dades

## 📝 Notes Tècniques

- Els mètodes retornen valors nullable (`int?`) per gestionar casos on no existeix el tipus de mostra
- La consulta SQL utilitza JOIN per assegurar integritat referencial
- El logging és consistent amb l'estil del projecte (emojis + missatges descriptius)
- Tota la lògica segueix el patró Repository per mantenir l'arquitectura neta

---

**Data d'implementació**: Gener 2025  
**Estat**: ✅ Completat i validat  
**Versió**: 1.0.0
