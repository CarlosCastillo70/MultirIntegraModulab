# Carpeta Legacy

Aquesta carpeta conté els arxius de l'antiga implementació abans de la migració a Clean Architecture.

## Arxius moguts:

### Serveis de Base de Dades Legacy
- `MultiRDbService.cs` - Servei principal de BD MultiR
- `MultiRDbServiceHistorial.cs` - Extensions per historial
- `MultiRDbServiceExtensions.cs` - Extensions generals
- `ModulabDbService.cs` - Servei de BD Modulab
- `IDbService.cs` - Interfície base dels serveis

### Serveis Externs Legacy
- `PacientWebService.cs` - Servei web de pacients
- `Logger.cs` - Sistema de logging antic

### Models Legacy
- `Microorganisme.cs` - Model de microorganisme (duplicat)

### Exemples i Tests Legacy
- `ExempleUsTractament.cs`
- `ExempleClassificacioEstats.cs`
- `ExempleEliminacioRegistres.cs`
- `ExempleUsHistorialMostres.cs`

## Nota Important

Aquests arxius s'han mantingut per referència històrica i compatibilitat temporal.
**NO utilitzar en noves funcionalitats**. Utilitzar les implementacions de Clean Architecture:

- **Repositoris**: `Infrastructure/Persistence/Repositories/`
- **Serveis**: `Application/Services/` i `Infrastructure/ExternalServices/`
- **Models**: `Domain/Entities/`
- **Casos d'ús**: `Application/UseCases/`

Data de migració: 2024
