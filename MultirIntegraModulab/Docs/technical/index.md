---
title: Documentació Tècnica
description: Detall tècnic complet del sistema MultirIntegraModulab
weight: 20
---

# 🔧 Documentació Tècnica

Documentació tècnica completa del sistema, arquitectura, implementació i configuració.

---

## 📋 Contingut d'aquesta Secció

### [📚 Procés Captació de Dades](PROCES_CAPTACIO_DADES.md) ⭐⭐⭐
**Per a**: Desenvolupadors, Tècnics  
**Temps de lectura**: 45-60 minuts  
**Nivell**: Avançat

**Document principal tècnic** amb detall complet del processament.

**Contingut destacat**:
- ✅ **Fase 1**: Validació Inicial
- 🧪 **Fase 2**: Classificació de Mostra
- 🔎 **Fase 3**: Determinar Tipus Incorporació
- 🦠 **Fase 4**: Comprovació Microorganismes
- 🛡️ **Fase 5**: Comprovació Mecanismes
- ⚡ **Fase 6A**: Processar Mostra Positiva
- 🔍 **Fase 6B**: Processar Mostra Negativa
- 📝 **Fase 7**: Auditoria i Finalització

**Inclou**:
- Diagrames de flux ASCII
- Exemples de codi C#
- Queries SQL
- Casos d'ús complets
- Model de dades
- Configuració i paràmetres

[➡️ Llegir Documentació Completa](PROCES_CAPTACIO_DADES.md)

---

### [📊 Diagrames Flux Mermaid](DIAGRAMES_FLUX_MERMAID.md) ⭐⭐⭐
**Per a**: Tots els perfils tècnics  
**Temps de lectura**: 20-30 minuts  
**Nivell**: Intermedi

**10 diagrames interactius** en format Mermaid visualitzables a GitHub.

**Diagrames inclosos**:
1. **Flux Principal Complet** - Vista general del sistema
2. **Classificació de Mostra** - Lògica positius/negatius
3. **Comprovació Microorganismes** - Verificació i creació
4. **Comprovació Mecanismes** - Resistències i combinacions
5. **Processar Positiva** - Flux detallat positius
6. **Processar Negativa** - Comprovacions negatius
7. **Determinar Tipus Incorporació** - Nova, repetida, validada...
8. **Flux Dades entre Sistemes** - Oracle → .NET → MySQL
9. **Cicle de Vida Mostra** - Estats de la mostra
10. **Model de Dades (ER)** - Relacions entre taules

[➡️ Veure Diagrames](DIAGRAMES_FLUX_MERMAID.md)

---

### [🔌 API Reference](api-reference.md)
**Per a**: Desenvolupadors  
**Temps de lectura**: 30-40 minuts  
**Nivell**: Avançat

Referència completa de l'API del sistema.

**Contingut**:
- Interfaces públiques
- Use Cases
- Services
- Repositories
- DTOs i Models
- Exemples d'ús

[➡️ API Reference](api-reference.md)

---

### [📦 Model de Dades](data-model.md)
**Per a**: Desenvolupadors, DBAs  
**Temps de lectura**: 20-30 minuts  
**Nivell**: Intermedi

Detall del model de dades MySQL.

**Contingut**:
- Diagrama ER complet
- Descripció de taules
- Relacions i constraints
- Índexs i optimitzacions
- Scripts de creació

[➡️ Model de Dades](data-model.md)

---

### [⚙️ Configuració](configuration.md)
**Per a**: Desenvolupadors, Administradors  
**Temps de lectura**: 15-20 minuts  
**Nivell**: Intermedi

Guia completa de configuració del sistema.

**Contingut**:
- App.config detallat
- Connection strings
- Paràmetres d'execució
- Logging
- WebService configuració
- Vigència de positius

[➡️ Configuració](configuration.md)

---

## 🎯 Rutes de Lectura Recomanades

### 📘 Ruta: Desenvolupador Nou (2-3 hores)

```
1. [Procés Captació Dades](PROCES_CAPTACIO_DADES.md) (45-60 min)
   └─ Llegir totes les 7 fases
   
2. [Diagrames Flux](DIAGRAMES_FLUX_MERMAID.md) (20 min)
   └─ Visualitzar diagrames 1, 2, 5, 6
   
3. [API Reference](api-reference.md) (30 min)
   └─ Interfícies principals i Use Cases
   
4. [Model de Dades](data-model.md) (20 min)
   └─ Taules principals i relacions
   
5. [Configuració](configuration.md) (15 min)
   └─ Paràmetres clau
```

---

### 📗 Ruta: Manteniment Ràpid (1 hora)

```
1. [Diagrames Flux](DIAGRAMES_FLUX_MERMAID.md) (20 min)
   └─ Refrescar fluxos principals
   
2. [API Reference](api-reference.md) (20 min)
   └─ Mètodes específics a modificar
   
3. [Configuració](configuration.md) (10 min)
   └─ Paràmetres afectats
   
4. [Procés Captació](PROCES_CAPTACIO_DADES.md) (10 min)
   └─ Només secció rellevant
```

---

### 📕 Ruta: DBA / Administrador (1 hora)

```
1. [Model de Dades](data-model.md) (30 min)
   └─ Totes les taules i índexs
   
2. [Configuració](configuration.md) (20 min)
   └─ Connection strings i paràmetres BD
   
3. [Procés Captació](PROCES_CAPTACIO_DADES.md) (10 min)
   └─ Secció "Taules Principals MySQL"
```

---

## 📊 Matriu de Documents per Tasca

| Tasca | Documents Necessaris | Temps |
|-------|---------------------|-------|
| **Implementar nova funcionalitat** | Procés Captació + API Reference + Model Dades | 2-3h |
| **Corregir bug** | Diagrames + Secció específica Procés | 30-60' |
| **Optimitzar rendiment** | Model Dades + Configuració | 1h |
| **Configurar nou entorn** | Configuració + Model Dades | 45' |
| **Entendre flux complet** | Diagrames + Procés Captació | 1.5h |
| **Modificar BD** | Model Dades + API Reference | 1h |

---

## 🔍 Cerca Ràpida per Tema

### Arquitectura
- [Procés Captació](PROCES_CAPTACIO_DADES.md#arquitectura-clean) - Secció "Arquitectura"
- [Diagrama 8](DIAGRAMES_FLUX_MERMAID.md#diagrama-8) - Flux de dades

### Fases de Processament
- [Fase 1: Validació](PROCES_CAPTACIO_DADES.md#fase-1)
- [Fase 2: Classificació](PROCES_CAPTACIO_DADES.md#fase-2)
- [Fase 3: Tipus Incorporació](PROCES_CAPTACIO_DADES.md#fase-3)
- [Fase 4: Microorganismes](PROCES_CAPTACIO_DADES.md#fase-4)
- [Fase 5: Mecanismes](PROCES_CAPTACIO_DADES.md#fase-5)
- [Fase 6: Processar](PROCES_CAPTACIO_DADES.md#fase-6)
- [Fase 7: Auditoria](PROCES_CAPTACIO_DADES.md#fase-7)

### Base de Dades
- [Model Dades Complet](data-model.md)
- [Diagrama ER](DIAGRAMES_FLUX_MERMAID.md#diagrama-10)
- [Taules Principals](PROCES_CAPTACIO_DADES.md#taules-principals)

### API
- [Use Cases](api-reference.md#use-cases)
- [Services](api-reference.md#services)
- [Repositories](api-reference.md#repositories)

---

## 🛠️ Eines i Recursos

### Visualització de Diagrames
- [Mermaid Live Editor](https://mermaid.live/) - Per editar/visualitzar diagrames
- [VS Code + Extensió Mermaid](https://marketplace.visualstudio.com/items?itemName=bierner.markdown-mermaid) - Preview local

### Desenvolupament
- [Visual Studio 2019+](https://visualstudio.microsoft.com/)
- [.NET Framework 4.8 SDK](https://dotnet.microsoft.com/download/dotnet-framework/net48)
- [MySQL Workbench](https://www.mysql.com/products/workbench/) - Gestió BD

### Testing
- [Postman](https://www.postman.com/) - Testing API (si aplicable)
- [NUnit](https://nunit.org/) - Framework de testing

---

## 📚 Documents Relacionats

### Funcionalitats
- [Classificació Mostres](../features/classification/index.md)
- [Microorganismes](../features/microorganisms/index.md)
- [Comprovacions Negatius](../features/validation/index.md)

### Guies
- [Guia Desenvolupador](../guides/developer-guide.md)
- [Guia Desplegament](../guides/deployment-guide.md)
- [Troubleshooting](../guides/troubleshooting.md)

### Referència
- [Glossari](../reference/glossary.md)
- [Codis Auditoria](../reference/audit-codes.md)

---

## 🆘 Suport Tècnic

Si trobes problemes o necessites aclariments:

1. **Consulta**: [Troubleshooting](../guides/troubleshooting.md)
2. **FAQ**: [Preguntes Freqüents](../guides/troubleshooting.md#faq)
3. **Issues**: [GitHub Issues](https://github.com/CarlosCastillo70/MultirIntegraModulab/issues)
4. **Email**: suport@multir.cat

---

**Següent secció**: [Funcionalitats →](../features/index.md)
