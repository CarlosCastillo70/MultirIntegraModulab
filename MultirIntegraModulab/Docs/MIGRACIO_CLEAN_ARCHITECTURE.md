# ?? Guia de Migració a Clean Architecture

## ?? Resum

Aquest document explica com migrar del codi legacy al nou sistema amb Clean Architecture.

---

## ?? Comparativa: Abans vs Després

### **ABANS (Legacy)**

```csharp
// Program.cs - Codi acoblat
var oracleService = new ModulabDbService(connectionString);
var mysqlService = new MultiRDbService(connectionString);

var resultats = oracleService.CarregarResultatsDeMostres(1, mysqlService, 50);

var tractament = new TractamentResultats(mysqlService);
tractament.ProcessarMostres(resultats);
```

**Problemes:**
- ? Connection strings hardcoded
- ? Acoblament fort entre capes
- ? Difícil de testar
- ? No segueix SOLID

### **DESPRÉS (Clean Architecture)**

```csharp
// ProgramCleanArchitecture.cs - Arquitectura neta
// 1. Configuració
var configService = new ConfigurationService();
var loggerService = new LoggerService();

// 2. Repositoris (capa d'adaptadors)
var modulabDbService = new ModulabDbService(configService.OracleConnectionString);
var modulabRepository = new ModulabRepository(modulabDbService, loggerService);

var multiRDbService = new MultiRDbService(configService.MySqlConnectionString);
var multiRRepository = new MultiRRepository(multiRDbService, loggerService);

// 3. Servei d'aplicació (Use Cases)
var processamentService = new ProcessamentMostresService(
    modulabRepository,
    multiRRepository,
    loggerService
);

// 4. Carregar i processar
var mostres = modulabRepository.CarregarResultats(configService.DiesEndarreraCarrega);
var resum = await processamentService.ProcessarMostresAsync(mostres);
```

**Avantatges:**
- ? Configuració externalitzada
- ? Dependències injectades
- ? Fàcil de testar (mocks)
- ? Segueix principis SOLID
- ? Capes ben separades

---

## ??? Arquitectura de Capes

```
???????????????????????????????????????????????????????????
?                    PRESENTATION                          ?
?              (ProgramCleanArchitecture.cs)               ?
?                                                           ?
?  • Configura Dependency Injection                        ?
?  • Coordina el flux d'execució                           ?
???????????????????????????????????????????????????????????
                       ?
                       ?
???????????????????????????????????????????????????????????
?                    APPLICATION                           ?
?    (Use Cases + Services + DTOs)                         ?
?                                                           ?
?  ProcessamentMostresService                              ?
?    ??? ProcessarMostresUseCase                           ?
?    ??? ValidarMostraUseCase                              ?
?    ??? ClassificarMostraUseCase                          ?
?                                                           ?
?  • Orquestra la lògica de negoci                         ?
?  • Sense coneixement d'infraestructura                   ?
???????????????????????????????????????????????????????????
                       ?
                       ?
???????????????????????????????????????????????????????????
?                     DOMAIN                               ?
?         (Entities + Interfaces)                          ?
?                                                           ?
?  Entities:                Interfaces (Ports):            ?
?  • ResultatProva          • IModulabRepository           ?
?  • ColeccioResultats      • IMultiRRepository            ?
?                           • ILoggerService               ?
?                                                           ?
?  • Regles de negoci pures                                ?
?  • Sense dependències externes                           ?
???????????????????????????????????????????????????????????
                       ?
                       ? (implementa)
???????????????????????????????????????????????????????????
?                 INFRASTRUCTURE                           ?
?                  (Adaptadors)                            ?
?                                                           ?
?  Repositories:          Services:                        ?
?  • ModulabRepository    • LoggerService                  ?
?  • MultiRRepository     • ConfigurationService           ?
?                                                           ?
?  • Implementa interfícies del Domain                     ?
?  • Detalls tècnics (BD, APIs)                            ?
???????????????????????????????????????????????????????????
```

---

## ?? Pla de Migració (Pas a Pas)

### **Fase 1: Preparació** ? (Completat)

- [x] Crear estructura de carpetes
- [x] Definir interfícies al Domain
- [x] Crear DTOs a Application
- [x] Implementar repositoris a Infrastructure

### **Fase 2: Migració de Use Cases** ? (Completada)

#### 2.1 Use Cases Creats ?

- [x] `ValidarMostraUseCase` - Valida mostres
- [x] `ClassificarMostraUseCase` - Classifica per tipus
- [x] `ProcessarMostresUseCase` - Coordina el processament
- [x] `DeterminarTipusIncorporacioUseCase` - Determina tipus d'incorporació
- [x] `ComprovadorMicroorganismesUseCase` - Comprova/crea microorganismes
- [x] `ComprovadorMecanismesResistenciaUseCase` - Comprova mecanismes

#### 2.2 Use Cases Pendents (TOTS COMPLETATS!) ?

- [x] `ProcessarMostraPositivaUseCase` - Processar mostra amb resultats positius ?
- [x] `ProcessarMostraNegativaUseCase` - Processar mostra amb resultats negatius ?
- [x] `ProcessarMostresPositivesUseCase` - Múltiples resultats positius ?
- [x] `ProcessarMostresNegativesUseCase` - Múltiples resultats negatius ?
- [x] `ProcessarMostraMixtaUseCase` - Processar mostra mixta ?

### **Fase 3: Processament Específic de Mostra** ? (Completada)

Tots els processadors específics han estat migrats a Use Cases:

1. ? **ProcessarMostraPositivaUseCase**
   - Ubicació: `Application/UseCases/ProcessarMostres/ProcessarMostraPositivaUseCase.cs`
   - Funcionalitats:
     - Verificar/crear pacient (placeholder SAP)
     - Crear relacions MOSTRA_MICROORGANISME
     - Processar fins a 5 mecanismes de resistència
     - Crear integracions de resultats
   - Resultat tipat: `ResultatProcessamentPositiu`

2. ? **ProcessarMostraNegativaUseCase**
   - Ubicació: `Application/UseCases/ProcessarMostres/ProcessarMostraNegativaUseCase.cs`
   - Funcionalitats:
     - Audita mostra amb codi "MN"
     - No insereix a taules principals
   - Resultat tipat: `ResultatProcessamentNegatiu`

3. ? **ProcessarMostresPositivesUseCase**
   - Ubicació: `Application/UseCases/ProcessarMostres/ProcessarMostresMultiplesUseCase.cs`
   - Delega a ProcessarMostraPositivaUseCase

4. ? **ProcessarMostresNegativesUseCase**
   - Ubicació: `Application/UseCases/ProcessarMostres/ProcessarMostresMultiplesUseCase.cs`
   - Delega a ProcessarMostraNegativaUseCase

5. ? **ProcessarMostraMixtaUseCase**
   - Ubicació: `Application/UseCases/ProcessarMostres/ProcessarMostresMultiplesUseCase.cs`
   - Processa positius i audita negatius amb codi "MM"

### **Fase 4: Integració i Testing** ?? (Següent pas)

- [ ] Tests unitaris per cada Use Case
- [ ] Tests d'integració
- [ ] Coverage > 80%
- [ ] Implementar GestorPacientsUseCase (integració SAP)
- [ ] Actualitzar Program.cs principal per utilitzar Clean Architecture per defecte

### **Fase 5: Neteja de Duplicats** ?? (En planificació)

Durant la migració s'han creat entitats duplicades que cal gestionar:

#### **Duplicats Identificats:**

1. **ModelClasses.cs** (2 versions)
   - ?? Legacy: `MultirIntegraModulab/ModelClasses.cs` ?? Marcat com obsolet
   - ? Clean: `MultirIntegraModulab/Domain/Enums/ModelClasses.cs` (utilitzar aquesta)

#### **Estratègia de Neteja:**

- ?? **Estat actual:** Convivència temporal
  - Codi legacy utilitza `MultirIntegraModulab` namespace
  - Clean Architecture utilitza `MultirIntegraModulab.Domain.Enums`
  - Using alias quan calgui resoldre conflictes

- ?? **Pla d'acció:**
  1. ? Documentar duplicats ([PLA_NETEJA_DUPLICATS.md](PLA_NETEJA_DUPLICATS.md))
  2. ? Marcar legacy com `[Obsolete]`
  3. ? Migrar `Program.cs` a Clean Architecture
  4. ? Deprecar `TractamentResultats.cs` completament
  5. ? Eliminar codi legacy i duplicats

**?? Documentació completa:** [PLA_NETEJA_DUPLICATS.md](PLA_NETEJA_DUPLICATS.md)
