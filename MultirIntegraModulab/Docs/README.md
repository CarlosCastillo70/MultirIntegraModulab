# MultirIntegraModulab

> Sistema d'integració entre MultiR (MySQL) i Modulab (Oracle) seguint els principis de **Clean Architecture**

---

## ?? Descripció

Aplicació .NET Framework 4.8 que integra dades de resultats de proves entre dues bases de dades:
- **MultiR** (MySQL) - Sistema de microorganismes i resistències
- **Modulab** (Oracle) - Sistema de laboratori

---

## ??? Arquitectura

Aquest projecte segueix els principis de **Clean Architecture** amb les següents capes:

```
MultirIntegraModulab/
?
??? Domain/                    # Lògica de negoci i regles del domini
?   ??? Entities/              # Entitats del domini
?   ??? Interfaces/            # Ports (abstraccions)
?   ??? Enums/                 # Enumeracions
?
??? Application/               # Casos d'ús i orquestració
?   ??? UseCases/              # Casos d'ús del negoci
?   ??? Services/              # Serveis d'aplicació
?   ??? DTOs/                  # Data Transfer Objects
?   ??? Interfaces/            # Abstraccions de serveis
?
??? Infrastructure/            # Implementacions tècniques
?   ??? Persistence/           # Repositoris i BD
?   ??? ExternalServices/      # Serveis externs
?   ??? Configuration/         # Configuració
?
??? Configuration/             # Arxius de configuració
?
??? ProgramCleanArchitecture.cs  # Punt d'entrada principal
?
??? _Legacy/                   # ?? Codi antic (no utilitzar)
    ??? README.md              # Documentació dels arxius legacy
```

### ?? Documentació Detallada

- **[CLEAN_ARCHITECTURE_README.md](MultirIntegraModulab/CLEAN_ARCHITECTURE_README.md)** - Guia completa de l'arquitectura
- **[_Legacy/README.md](_Legacy/README.md)** - Informació sobre arxius legacy

---

## ?? Punt d'Entrada

El projecte utilitza **Clean Architecture** com a punt d'entrada principal:

```xml
<StartupObject>MultirIntegraModulab.ProgramCleanArchitecture</StartupObject>
```

### Execució

```bash
# Compilar el projecte
dotnet build

# Executar el projecte
dotnet run
```

---

## ?? Dependències

- **.NET Framework 4.8**
- **Oracle.ManagedDataAccess** (19.23.0) - Connexió a Modulab (Oracle)
- **MySql.Data** (9.4.0) - Connexió a MultiR (MySQL)
- **System.Net.Http** (4.3.4) - Comunicació amb serveis web
- **System.Web** - Utilities (HttpUtility)

---

## ?? Configuració

El projecte utilitza `AppConfiguration.xml` per configurar:
- Connexions a bases de dades (MultiR i Modulab)
- Rutes de logs
- Configuració de serveis externs

Exemple:
```xml
<?xml version="1.0" encoding="utf-8"?>
<Configuration>
  <ModulabConnectionString>Data Source=...</ModulabConnectionString>
  <MultiRConnectionString>Server=...</MultiRConnectionString>
  <LogPath>C:\Logs\MultirIntegra\</LogPath>
</Configuration>
```

---

## ?? Testing

El projecte està dissenyat per ser altament testable gràcies a:
- **Inversió de dependències** (DIP)
- **Injecció de dependències** explícita
- **Abstraccions** (interfaces) al Domain

```csharp
// Exemple de test amb mocks
[Test]
public async Task ProcessarMostres_MostresValides_ProcessaCorrectament()
{
    // Arrange
    var mockRepo = new Mock<IModulabRepository>();
    var mockLogger = new Mock<ILoggerService>();
    var service = new ProcessamentMostresService(mockRepo.Object, mockLogger.Object);
    
    // Act
    var resultat = await service.ProcessarMostresAsync(mostres);
    
    // Assert
    Assert.IsTrue(resultat.EsExit);
}
```

---

## ?? Estructura de Capes

### ?? Domain (Capa més interna)
- **Zero dependències externes**
- Conté les regles de negoci pures
- Defineix les abstraccions (interfaces)

### ?? Application
- Depèn només del Domain
- Implementa els casos d'ús del negoci
- Orquestra el flux de dades

### ?? Infrastructure
- Implementa les abstraccions del Domain
- Conté els detalls tècnics (BD, APIs, etc.)
- Adaptadors per sistemes externs

### ?? Presentation
- Punt d'entrada de l'aplicació
- Configura la injecció de dependències
- Coordina l'execució

---

## ?? Carpeta Legacy

La carpeta `_Legacy/` conté el codi de l'antiga implementació abans de la migració a Clean Architecture.

**Important:**
- ? **NO utilitzar** en noves funcionalitats
- ? Mantingut només per referència històrica
- ?? Consultar `_Legacy/README.md` per més detalls

---

## ?? Casos d'Ús Principals

1. **Processar Mostres** - Carrega i processa mostres de Modulab
2. **Validar Mostra** - Valida que una mostra compleix els requisits
3. **Classificar Mostra** - Determina el tipus de mostra (positiva, negativa, etc.)
4. **Determinar Tipus Incorporació** - Decideix com incorporar els resultats a MultiR
5. **Processar Mostra Positiva** - Incorpora resultats positius
6. **Processar Mostra Negativa** - Incorpora resultats negatius

---

## ?? Principis Aplicats

- ? **SOLID Principles**
- ? **Clean Architecture**
- ? **Dependency Inversion**
- ? **Separation of Concerns**
- ? **Single Responsibility**
- ? **Open/Closed Principle**

---

## ?? Evolució del Projecte

### ? Completat
- [x] Estructura Clean Architecture definida
- [x] Domain Layer implementat
- [x] Infrastructure Layer (repositoris i serveis)
- [x] Application Layer (base)
- [x] Migració d'arxius legacy a `_Legacy/`

### ?? En Progrés
- [ ] Completar tots els Use Cases
- [ ] Tests unitaris complets
- [ ] Tests d'integració

### ?? Pendent
- [ ] Documentació d'API
- [ ] Optimitzacions de rendiment
- [ ] Monitoratge i alertes

---

## ?? Desenvolupament

Aquest projecte segueix les millors pràctiques de Clean Code i Clean Architecture per garantir:
- ?? Alta testabilitat
- ?? Fàcil manteniment
- ?? Escalabilitat
- ?? Flexibilitat per canvis

---

## ?? Llicència

[Definir llicència del projecte]

---

## ?? Contacte

[Definir informació de contacte]

---

**Última actualització:** 2024  
**Versió:** 2.0 (Clean Architecture)  
**Framework:** .NET Framework 4.8
