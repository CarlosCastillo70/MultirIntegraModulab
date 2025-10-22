# Legacy Services

Aquesta carpeta conté els serveis de base de dades legacy que encara són utilitzats pels repositoris de Clean Architecture.

## ?? Important

Aquests arxius són **adaptadors legacy** que proporcionen accés a les bases de dades MySQL (MultiR) i Oracle (Modulab). Encara són necessaris perquè:

1. Els **Repositoris** (`MultiRRepository` i `ModulabRepository`) els utilitzen com a capa d'accés a dades
2. Contenen lògica específica de BD que encara no s'ha refactoritzat completament
3. Funcionen correctament i no hi ha raó urgent per reescriure'ls

## ?? Arxius

### Serveis de Base de Dades
- **`MultiRDbService.cs`** - Servei principal d'accés a MultiR (MySQL)
- **`MultiRDbServiceHistorial.cs`** - Extensions per gestió d'historial
- **`MultiRDbServiceExtensions.cs`** - Extensions generals del servei MultiR
- **`ModulabDbService.cs`** - Servei principal d'accés a Modulab (Oracle)
- **`IDbService.cs`** - Interfície base dels serveis de BD

### Models Legacy
- **`Microorganisme.cs`** - Model de microorganisme utilitzat pels serveis legacy

## ?? Estratègia de Migració

Aquests serveis seran **reemplaçats gradualment** per implementacions modernes:

### Fase 1 (Actual) ?
- Utilitzar els serveis legacy com a adaptadors dins dels repositoris
- Mantenir la funcionalitat existent

### Fase 2 (Futur) ??
- Crear `DbContext` amb Entity Framework o Dapper
- Implementar consultes tipades i segures
- Migrar lògica a repositoris

### Fase 3 (Futur) ??
- Eliminar completament els serveis legacy
- Utilitzar només abstraccions del Domain

## ?? Ús Actual

Els repositoris de Clean Architecture utilitzen aquests serveis:

```csharp
// MultiRRepository.cs
public class MultiRRepository : IMultiRRepository
{
    private readonly MultiRDbService _multiRDbService; // ? Servei legacy
    
    public MultiRRepository(MultiRDbService multiRDbService, ILoggerService logger)
    {
        _multiRDbService = multiRDbService;
    }
    
    // Mètodes del repositori que utilitzen el servei legacy...
}
```

## ? No utilitzar directament

**NO** utilitzar aquests serveis directament en nou codi. En el seu lloc:

? **Utilitzar:** `IMultiRRepository` o `IModulabRepository` (abstraccions del Domain)
? **NO utilitzar:** `MultiRDbService` o `ModulabDbService` directament

## ?? Referències

- **Carpeta `_Legacy/`** - Conté la còpia original dels arxius legacy
- **`Domain/Interfaces/IMultiRRepository.cs`** - Abstracció moderna
- **`Domain/Interfaces/IModulabRepository.cs`** - Abstracció moderna

---

**Estat:** En ús (temporalment fins a la refactorització completa)  
**Última actualització:** 2024
