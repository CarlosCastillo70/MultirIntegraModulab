# ??? Clean Architecture - MultirIntegraModulab

## ?? Resum Executiu

Aquest projecte segueix els principis de **Clean Architecture** per garantir:
- ? **Separació de preocupacions** clara entre capes
- ? **Testabilitat** elevada amb injecció de dependències
- ? **Mantenibilitat** amb responsabilitats ben definides
- ? **Escalabilitat** preparada per créixer
- ? **Independència** de frameworks i infraestructura

---

## ??? Estructura del Projecte

```
MultirIntegraModulab/
?
??? ?? Domain/                                    [Capa més interna - Regles de negoci]
?   ??? Entities/                                 Entitats del domini
?   ?   ??? ResultatProva.cs
?   ?   ??? ResultatProvaRegistre.cs
?   ?   ??? ColeccioResultatsMostres.cs
?   ?   ??? DiagnosticExistent.cs
?   ?   ??? Microorganisme.cs
?   ?
?   ??? Interfaces/                               Ports (interfícies)
?   ?   ??? IModulabRepository.cs                 Port per Modulab (Oracle)
?   ?   ??? IMultiRRepository.cs                  Port per MultiR (MySQL)
?   ?   ??? ILoggerService.cs                     Port per logging
?   ?   ??? IConfigurationService.cs              Port per configuració
?   ?   ??? IPacientWebService.cs                 Port per web service SAP
?   ?
?   ??? Enums/                                    Enumeracions del domini
?   ?   ??? ModelClasses.cs
?   ?
?   ??? ValueObjects/                             Value Objects (futurs)
?
??? ?? Application/                                [Capa d'aplicació - Use Cases]
?   ??? UseCases/                                  Casos d'ús del negoci
?   ?   ??? ProcessarMostres/
?   ?   ?   ??? ProcessarMostresUseCase.cs        Processa col·lecció de mostres
?   ?   ?   ??? ValidarMostraUseCase.cs           Valida una mostra
?   ?   ?
?   ?   ??? ClassificarMostres/
?   ?       ??? ClassificarMostraUseCase.cs       Classifica tipus de mostra
?   ?
?   ??? DTOs/                                      Data Transfer Objects
?   ?   ??? ResumProcessamentDto.cs               DTO per resums
?   ?   ??? MostraDto.cs                          DTO per mostres
?   ?
?   ??? Interfaces/                                Interfícies de serveis
?   ?   ??? IProcessamentMostresService.cs        Servei principal
?   ?
?   ??? Services/                                  Implementacions de serveis
?       ??? ProcessamentMostresService.cs         Coordina Use Cases
?
??? ?? Infrastructure/                             [Capa d'infraestructura - Adaptadors]
?   ??? Persistence/
?   ?   ??? Repositories/                          Implementacions de repositoris
?   ?   ?   ??? ModulabRepository.cs              Adapta ModulabDbService
?   ?   ?   ??? MultiRRepository.cs               Adapta MultiRDbService
?   ?   ?
?   ?   ??? Context/                              Contextos de BD (futurs)
?   ?
?   ??? ExternalServices/
?   ?   ??? Logger/
?   ?   ?   ??? LoggerService.cs                  Adapta Logger existent
?   ?   ?
?   ?   ??? WebServices/                          Web Services externs
?   ?       ??? PacientWebService.cs              (ja existent)
?   ?
?   ??? Configuration/
?       ??? ConfigurationService.cs               Adapta AppConfiguration
?
??? ?? Presentation/                               [Capa de presentació]
?   ??? Program.cs                                 Punt d'entrada
?
??? ?? Processadors/                               [Legacy - Processadors refactoritzats]
    ??? ValidadorMostres.cs
    ??? ClassificadorMostres.cs
    ??? DeterminadorTipusIncorporacio.cs
    ??? ... (altres processadors)
```

---

## ?? Flux de Dependències

```
???????????????????????????????????????????????????????????????
?                    Presentation Layer                        ?
?                      (Program.cs)                            ?
?                                                               ?
?  • Configura Dependency Injection                            ?
?  • Coordina el flux d'execució                               ?
???????????????????????????????????????????????????????????????
                       ?
                       ?
???????????????????????????????????????????????????????????????
?                   Application Layer                          ?
?              (Use Cases + Services)                          ?
?                                                               ?
?  ProcessamentMostresService                                  ?
?    ??? ProcessarMostresUseCase                               ?
?    ??? ValidarMostraUseCase                                  ?
?    ??? ClassificarMostraUseCase                              ?
?                                                               ?
?  • Orquestra la lògica de negoci                             ?
?  • No coneix detalls d'infraestructura                       ?
???????????????????????????????????????????????????????????????
                       ?
                       ?
???????????????????????????????????????????????????????????????
?                     Domain Layer                             ?
?              (Entities + Interfaces)                         ?
?                                                               ?
?  Entities:                    Interfaces (Ports):            ?
?  • ResultatProva              • IModulabRepository           ?
?  • ResultatProvaRegistre      • IMultiRRepository            ?
?  • ColeccioResultatsMostres   • ILoggerService               ?
?                                • IConfigurationService       ?
?                                                               ?
?  • Regles de negoci pures                                    ?
?  • Sense dependències externes                               ?
???????????????????????????????????????????????????????????????
                       ?
                       ?
                       ? (implementa)
                       ?
???????????????????????????????????????????????????????????????
?                 Infrastructure Layer                         ?
?                    (Adaptadors)                              ?
?                                                               ?
?  Repositories:              Services:                        ?
?  • ModulabRepository        • LoggerService                  ?
?  • MultiRRepository         • ConfigurationService           ?
?                             • PacientWebService              ?
?                                                               ?
?  • Implementa interfícies del Domain                         ?
?  • Coneix detalls tècnics (BD, APIs, etc.)                   ?
???????????????????????????????????????????????????????????????
```

---

## ?? Principis Aplicats

### 1. **Dependency Inversion Principle (DIP)**
- Les capes externes depenen de les internes
- Les abstraccions (interfícies) estan al Domain
- Les implementacions estan a Infrastructure

### 2. **Single Responsibility Principle (SRP)**
- Cada Use Case té una responsabilitat única
- Cada servei gestiona un aspecte específic

### 3. **Open/Closed Principle (OCP)**
- Els Use Cases són oberts a extensió
- Les interfícies permeten afegir nous adaptadors sense modificar el domini

### 4. **Separation of Concerns**
- **Domain**: Què es fa (regles de negoci)
- **Application**: Com es coordina
- **Infrastructure**: Amb què es fa (tecnologies)
- **Presentation**: Com es presenta

---

## ?? Dependency Injection

### Configuració bàsica (Program.cs)

```csharp
// 1. Configurar serveis d'infraestructura
var configService = new ConfigurationService();
var logger = new LoggerService(configService.LogPath);

// 2. Configurar connexions BD
var modulabDbService = new ModulabDbService(configService.ModulabConnectionString);
var multiRDbService = new MultiRDbService(configService.MultiRConnectionString);

// 3. Configurar repositoris
var modulabRepository = new ModulabRepository(modulabDbService, logger);
var multiRRepository = new MultiRRepository(multiRDbService, logger);

// 4. Configurar servei d'aplicació
var processamentService = new ProcessamentMostresService(
    modulabRepository,
    multiRRepository,
    logger
);

// 5. Utilitzar el servei
var mostres = await modulabRepository.CarregarMostresAsync(diesEndarrera: 1);
var resum = await processamentService.ProcessarMostresAsync(mostres);
```

---

## ?? Testabilitat

### Avantatges per Testing

```csharp
// Test d'un Use Case sense dependències externes
[Test]
public void ValidarMostra_MostraValida_RetornaTrue()
{
    // Arrange
    var mockLogger = new Mock<ILoggerService>();
    var useCase = new ValidarMostraUseCase(mockLogger.Object);
    
    var mostra = new ResultatProva 
    { 
        EtiquetaId = "TEST123",
        PacientSap = "PAC001",
        Registres = new List<ResultatProvaRegistre> 
        { 
            new ResultatProvaRegistre { DataResultat = DateTime.Now }
        }
    };
    
    // Act
    var resultat = useCase.Executar(mostra);
    
    // Assert
    Assert.IsTrue(resultat);
}

// Test d'un servei amb mocks de repositoris
[Test]
public async Task ProcessarMostra_MostraValida_ProcessaCorrectament()
{
    // Arrange
    var mockModulabRepo = new Mock<IModulabRepository>();
    var mockMultiRRepo = new Mock<IMultiRRepository>();
    var mockLogger = new Mock<ILoggerService>();
    
    var service = new ProcessamentMostresService(
        mockModulabRepo.Object,
        mockMultiRRepo.Object,
        mockLogger.Object
    );
    
    // Act & Assert
    // ...
}
```

---

## ?? Migració des del Codi Legacy

### Estat Actual

- ? Domain Layer completat
- ? Application Layer creat (base)
- ? Infrastructure Layer creat (adaptadors)
- ? Integració amb Processadors existents
- ? Migració completa de Use Cases

### Pròxims Passos

1. **Completar Use Cases**
   - DeterminarTipusIncorporacioUseCase
   - ProcessarMostraPositivaUseCase
   - ProcessarMostraNegativaUseCase
   - etc.

2. **Integrar Processadors**
   - Wrapping dels processadors existents com a Use Cases
   - Refactorització progressiva

3. **Actualitzar Program.cs**
   - Configurar DI completa
   - Utilitzar els nous serveis

4. **Tests Unitaris**
   - Crear tests per cada Use Case
   - Tests d'integració entre capes

---

## ?? Referències

- [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Hexagonal Architecture - Alistair Cockburn](https://alistair.cockburn.us/hexagonal-architecture/)
- [Domain-Driven Design - Eric Evans](https://www.domainlanguage.com/ddd/)
- [SOLID Principles](https://en.wikipedia.org/wiki/SOLID)

---

## ? Checklist d'Implementació

### Domain Layer
- [x] Entities
- [x] Interfaces (Ports)
- [x] Enums
- [ ] Value Objects (futurs)
- [ ] Domain Services (si cal)

### Application Layer
- [x] Use Cases base
- [x] DTOs
- [x] Service interfaces
- [x] Service implementations
- [ ] Use Cases complets
- [ ] Validators
- [ ] Mappers

### Infrastructure Layer
- [x] Repositories
- [x] Logger Service
- [x] Configuration Service
- [ ] Database Context
- [ ] External Services complets

### Presentation Layer
- [ ] Program.cs actualitzat amb DI
- [ ] Error Handling
- [ ] Logging configurat

### Testing
- [ ] Unit Tests per Use Cases
- [ ] Integration Tests
- [ ] Repository Tests

---

**Última actualització:** Gener 2025  
**Estat:** ?? En desenvolupament - Estructura base creada
