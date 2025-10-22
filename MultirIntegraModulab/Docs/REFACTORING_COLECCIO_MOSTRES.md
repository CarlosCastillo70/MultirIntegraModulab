# ?? Refactoring de Nomenclatura: ColeccioResultatsMostres ? ColeccioMostres

## ?? Objectiu del Refactoring

Clarificar la nomenclatura del projecte per reflectir correctament la jerarquia d'entitats:
- **Mostra**: Contenidor principal amb `ETIQUETA_ID`
- **ResultatMostra**: Registre individual dins d'una mostra

---

## ?? Canvis Realitzats

### **Classe Principal: `ColeccioMostres`**

#### **Abans:**
```csharp
public class ColeccioResultatsMostres
{
    private readonly Dictionary<string, Mostra> _resultats;
    
    public Mostra ObtenirResultat(string etiquetaId) { }
    public List<Mostra> ObtenirTotsElsResultats() { }
    public int NombreTotalResultats { get; }
}
```

#### **Després:**
```csharp
public class ColeccioMostres
{
    private readonly Dictionary<string, Mostra> _mostres;
    
    public Mostra ObtenirMostra(string etiquetaId) { }
    public List<Mostra> ObtenirTotesLesMostres() { }
    public int NombreTotalMostres { get; }
}
```

---

## ? Mètodes Actualitzats

| Mètode Antic (Obsolet) | Mètode Nou |
|------------------------|------------|
| `ObtenirResultat()` | `ObtenirMostra()` |
| `ObtenirTotsElsResultats()` | `ObtenirTotesLesMostres()` |
| `ObtenirResultatsPerPacient()` | `ObtenirMostresPerPacient()` |
| `ObtenirResultatsPerCip()` | `ObtenirMostresPerCip()` |
| `ObtenirResultatsPerMetge()` | `ObtenirMostresPerMetge()` |
| `ObtenirResultatsPerCentre()` | `ObtenirMostresPerCentre()` |
| `ObtenirResultatsPerServei()` | `ObtenirMostresPerServei()` |
| `ObtenirResultatsPerDataResultat()` | `ObtenirMostresPerDataResultat()` |
| `ObtenirResultatsPerDataPeticio()` | `ObtenirMostresPerDataPeticio()` |
| `ObtenirResultatsPerMicroorganisme()` | `ObtenirMostresPerMicroorganisme()` |
| `ObtenirResultatsPerMecanismeResistencia()` | `ObtenirMostresPerMecanismeResistencia()` |
| `ObtenirResultatsPerDescripcioMecanismeResistencia()` | `ObtenirMostresPerDescripcioMecanismeResistencia()` |

### **Propietats Actualitzades:**

| Propietat Antiga (Obsoleta) | Propietat Nova |
|------------------------------|----------------|
| `NombreTotalResultats` | `NombreTotalMostres` |

---

## ?? Compatibilitat amb Codi Existent

Per **mantenir compatibilitat** amb codi existent, s'han creat:

1. **Alies de classe:**
```csharp
[Obsolete("Utilitzeu ColeccioMostres en lloc d'aquesta classe")]
public class ColeccioResultatsMostres : ColeccioMostres
{
    // Hereda tots els mètodes de ColeccioMostres
}
```

2. **Mètodes marcats com `[Obsolete]`:**
```csharp
[Obsolete("Utilitzeu ObtenirMostra() en lloc d'aquest mètode")]
public Mostra ObtenirResultat(string etiquetaId)
{
    return ObtenirMostra(etiquetaId);
}
```

### **Warnings de compilació:**
- El compilador mostrarà **warnings** quan s'utilitzin mètodes obsolets
- Els warnings indiquen quin mètode nou utilitzar
- El codi antic **continua funcionant** sense errors

---

## ?? Fitxers Afectats

### **Fitxer principal:**
- ? `MultirIntegraModulab\Domain\Entities\ColeccioMostres.cs` (NOU)

### **Fitxers que utilitzen la classe:**
Els següents fitxers poden mostrar warnings però continuen funcionant:
- `MultirIntegraModulab\Application\Services\ProcessamentMostresService.cs`
- `MultirIntegraModulab\Application\Interfaces\IProcessamentMostresService.cs`
- `MultirIntegraModulab\Application\UseCases\ProcessarMostres\ProcessarMostresUseCase.cs`
- `MultirIntegraModulab\Infrastructure\Persistence\LegacyServices\ModulabDbService.cs`
- `MultirIntegraModulab\Infrastructure\Persistence\Repositories\ModulabRepository.cs`
- `MultirIntegraModulab\Domain\Interfaces\IModulabRepository.cs`
- `MultirIntegraModulab\Program.cs`

---

## ?? Guia de Migració

### **Pas 1: Actualitzar imports**
```csharp
// Abans
using MultirIntegraModulab.Domain.Entities;
var colleccio = new ColeccioResultatsMostres(); // Warnings

// Després
using MultirIntegraModulab.Domain.Entities;
var colleccio = new ColeccioMostres(); // Sense warnings
```

### **Pas 2: Actualitzar crides de mètodes**
```csharp
// Abans
var mostra = colleccio.ObtenirResultat("ETIQ123");
var mostres = colleccio.ObtenirTotsElsResultats();
int total = colleccio.NombreTotalResultats;

// Després
var mostra = colleccio.ObtenirMostra("ETIQ123");
var mostres = colleccio.ObtenirTotesLesMostres();
int total = colleccio.NombreTotalMostres;
```

### **Pas 3: Actualitzar signatures de mètodes**
```csharp
// Abans
public async Task<ResumProcessamentDto> ProcessarMostresAsync(ColeccioResultatsMostres mostres)

// Després
public async Task<ResumProcessamentDto> ProcessarMostresAsync(ColeccioMostres mostres)
```

---

## ?? Resum de la Jerarquia

```
ColeccioMostres
    ??? Mostra (contenidor amb ETIQUETA_ID)
            ??? ResultatMostra[] (registres individuals)
                    ??? Microorganismes
                    ??? Mecanismes de resistència
                    ??? Data resultat
                    ??? Data validació
                    ??? Altres dades
```

---

## ? Beneficis del Refactoring

1. **Nomenclatura clara**: `ColeccioMostres` és més descriptiu que `ColeccioResultatsMostres`
2. **Consistència**: Tots els mètodes utilitzen "Mostres" en lloc de "Resultats"
3. **Mantenibilitat**: Més fàcil d'entendre per nous desenvolupadors
4. **Compatibilitat**: El codi antic continua funcionant
5. **Guia de migració**: Els warnings del compilador indiquen com migrar

---

## ?? Comprovacions

- ? Build exitós
- ? Mètodes obsolets marquen amb `[Obsolete]`
- ? Alies `ColeccioResultatsMostres` funciona
- ? Documentació actualitzada
- ? Variables internes actualitzades (`_mostres` en lloc de `_resultats`)

---

## ?? Historial de Canvis

| Data | Versió | Canvi |
|------|--------|-------|
| 2024-01-XX | 1.0 | Refactoring complet de `ColeccioResultatsMostres` a `ColeccioMostres` |

---

## ?? Notes Finals

- Els mètodes obsolets es poden eliminar en futures versions després de migrar tot el codi
- Es recomana actualitzar els fitxers progressivament per eliminar els warnings
- La classe `ColeccioResultatsMostres` pot ser eliminada en una futura versió major

**Autor**: Refactoring automàtic amb GitHub Copilot  
**Data**: Gener 2024
