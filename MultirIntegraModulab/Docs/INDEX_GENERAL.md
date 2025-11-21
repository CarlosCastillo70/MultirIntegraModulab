# 📚 ÍNDEX GENERAL DE DOCUMENTACIÓ - MultirIntegraModulab

Aquest document serveix com a porta d'entrada a tota la documentació del sistema **MultirIntegraModulab**.

---

## 🎯 Per On Començar?

### Si ets...

#### 👨‍💼 **Direcció / Gestió**
📄 Comença per: **[RESUM_EXECUTIU.md](RESUM_EXECUTIU.md)**
- Visió general del sistema
- Beneficis i objectius
- Casos d'ús principals
- Mètriques i resultats

#### 👨‍💻 **Desenvolupador / Tècnic**
📄 Comença per: **[PROCES_CAPTACIO_DADES.md](PROCES_CAPTACIO_DADES.md)**
- Documentació tècnica completa
- Detall de cada fase
- Diagrames de flux ASCII
- Exemples de codi

#### 🎨 **Analista / Dissenyador**
📄 Comença per: **[DIAGRAMES_FLUX_MERMAID.md](DIAGRAMES_FLUX_MERMAID.md)**
- Diagrames visuals interactius
- Model de dades
- Flux de dades entre sistemes

#### 🔬 **Usuari Final / Vigilància Epidemiològica**
📄 Comença per: **[CASOS_DUS.md](#)** (si existeix) o **[RESUM_EXECUTIU.md](RESUM_EXECUTIU.md)**
- Casos d'ús pràctics
- Interpretació de resultats

---

## 📋 Documentació per Categories

### 🎯 **DOCUMENTS GENERALS**

#### 1. 📄 [RESUM_EXECUTIU.md](RESUM_EXECUTIU.md)
**Per a: Direcció, Gestió, Responsables IT**

Contingut:
- Objectiu del sistema
- Visió general
- Conceptes clau
- Mètriques i resultats
- Casos d'ús principals
- Beneficis del sistema
- Estadístiques reals

**Temps de lectura: 15-20 minuts**

---

#### 2. 📄 [RESUM_FINAL.md](RESUM_FINAL.md)
**Per a: Tots els perfils**

Contingut:
- Resum tècnic general
- Arquitectura del sistema
- Components principals
- Flux de treball
- Resultats esperats

**Temps de lectura: 10-15 minuts**

---

### 🔧 **DOCUMENTS TÈCNICS**

#### 3. 📄 [PROCES_CAPTACIO_DADES.md](PROCES_CAPTACIO_DADES.md) ⭐
**Per a: Desenvolupadors, Tècnics**

Contingut:
- **Documentació tècnica completa** del procés de captació
- Arquitectura Clean Architecture
- Detall de les 7 fases del processament
- Diagrames de flux en format ASCII
- Casos d'ús amb exemples
- Codis d'auditoria
- Configuració i paràmetres
- Taules de MySQL implicades
- Resum executiu tècnic

**Temps de lectura: 45-60 minuts**

**Contingut destacat**:
- ✅ Fase 1: Validació Inicial
- 🧪 Fase 2: Classificació de Mostra
- 🔎 Fase 3: Determinar Tipus Incorporació
- 🦠 Fase 4: Comprovació Microorganismes
- 🛡️ Fase 5: Comprovació Mecanismes
- ⚡ Fase 6A: Processar Mostra Positiva
- 🔍 Fase 6B: Processar Mostra Negativa
- 📝 Fase 7: Auditoria i Finalització

---

#### 4. 📄 [DIAGRAMES_FLUX_MERMAID.md](DIAGRAMES_FLUX_MERMAID.md) ⭐
**Per a: Tots els perfils tècnics**

Contingut:
- **10 diagrames interactius en format Mermaid**
- Flux principal complet
- Classificació de mostra
- Comprovació microorganismes
- Comprovació mecanismes
- Processar positiva
- Processar negativa
- Determinar tipus incorporació
- Flux de dades entre sistemes
- Cicle de vida d'una mostra
- Model de dades (ER)

**Visualització**: GitHub, VS Code, Mermaid Live Editor

---

### 🔍 **COMPROVACIONS DE NEGATIUS**

#### 5. 📄 [DIAGRAMES_COMPROVACIONS.md](DIAGRAMES_COMPROVACIONS.md)
**Per a: Desenvolupadors, Analistes**

Contingut:
- Diagrama visual del sistema de comprovacions
- Flux de decisió per a mostres negatives
- Comprovació 1 i Comprovació 2
- Taula de decisions amb colors
- Model de dades simplificat
- Query flow
- Gràfic de flux de dades
- Casos d'ús amb emojis

**Temps de lectura: 20-30 minuts**

---

#### 6. 📄 [COMPROVACIO_1_NEGATIUS.md](COMPROVACIO_1_NEGATIUS.md)
**Per a: Desenvolupadors**

Contingut:
- Detall de la **Comprovació 1**
- Comportament del tipus de mostra
- Positius generals del pacient
- Lògica de decisió
- Exemples pràctics
- Queries SQL
- Casos extrems

**Temps de lectura: 15-20 minuts**

---

#### 7. 📄 [COMPROVACIO_2_NEGATIUS.md](COMPROVACIO_2_NEGATIUS.md)
**Per a: Desenvolupadors**

Contingut:
- Detall de la **Comprovació 2**
- Positius vigents del tipus de mostra
- Tipus equivalents
- Càlcul de vigència
- Lògica de decisió
- Exemples pràctics
- Queries SQL
- Casos extrems

**Temps de lectura: 20-25 minuts**

---

#### 8. 📄 [COMPROVACIONS_NEGATIUS_RESUM.md](COMPROVACIONS_NEGATIUS_RESUM.md)
**Per a: Desenvolupadors, Analistes**

Contingut:
- Resum de les comprovacions 1 i 2
- Comparativa
- Taula de decisions
- Exemples consolidats
- Estadístiques

**Temps de lectura: 10-15 minuts**

---

#### 9. 📄 [IMPLEMENTACIO_COMPROVACIONS_RESUM.md](IMPLEMENTACIO_COMPROVACIONS_RESUM.md)
**Per a: Desenvolupadors**

Contingut:
- Detall d'implementació de les comprovacions
- Codi C#
- Estructures de dades
- Mètodes i funcions
- Proves i testing

**Temps de lectura: 25-30 minuts**

---

### 🎓 **DOCUMENTS AUXILIARS**

#### 10. 📄 [README_LogIndentHelper.md](README_LogIndentHelper.md)
**Per a: Desenvolupadors**

Contingut:
- Utilitat per indentació de logs
- Nivells d'indentació
- Exemples d'ús
- Configuració

**Temps de lectura: 5-10 minuts**

---

#### 11. 📄 [TRACTAMENT_MOSTRES_ANTIGUES.md](TRACTAMENT_MOSTRES_ANTIGUES.md)
**Per a: Desenvolupadors, Analistes**

Contingut:
- Com es tracten les mostres antigues
- Criteri de "mostra antiga"
- Decisió d'incorporació
- Auditoria
- Casos especials

**Temps de lectura: 10-15 minuts**

---

## 🗺️ Mapa de Navegació

```
                    📚 ÍNDEX GENERAL
                           │
           ┌───────────────┼───────────────┐
           │               │               │
      🎯 GENERALS     🔧 TÈCNICS    🔍 COMPROVACIONS
           │               │               │
           ├─ RESUM_       ├─ PROCES_      ├─ DIAGRAMES_
           │  EXECUTIU     │  CAPTACIO_    │  COMPROVACIONS
           │               │  DADES ⭐      │
           ├─ RESUM_       │               ├─ COMPROVACIO_1
           │  FINAL        ├─ DIAGRAMES_   │  _NEGATIUS
           │               │  FLUX_        │
           └───────────    │  MERMAID ⭐    ├─ COMPROVACIO_2
                           │               │  _NEGATIUS
                           └───────────    │
                                           ├─ COMPROVACIONS_
                                           │  NEGATIUS_RESUM
                                           │
                                           └─ IMPLEMENTACIO_
                                              COMPROVACIONS
```

---

## 📊 Matriu de Documents per Perfil

| Document | Direcció | Tècnic | Dev | Analista | Usuari |
|----------|:--------:|:------:|:---:|:--------:|:------:|
| RESUM_EXECUTIU | ⭐⭐⭐ | ⭐⭐ | ⭐ | ⭐⭐ | ⭐⭐ |
| RESUM_FINAL | ⭐⭐ | ⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐ | ⭐ |
| PROCES_CAPTACIO_DADES | ⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ | - |
| DIAGRAMES_FLUX_MERMAID | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | ⭐ |
| DIAGRAMES_COMPROVACIONS | ⭐ | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | - |
| COMPROVACIO_1_NEGATIUS | - | ⭐⭐ | ⭐⭐⭐ | ⭐⭐ | - |
| COMPROVACIO_2_NEGATIUS | - | ⭐⭐ | ⭐⭐⭐ | ⭐⭐ | - |
| COMPROVACIONS_RESUM | - | ⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ | - |
| IMPLEMENTACIO_COMPROVACIONS | - | ⭐⭐ | ⭐⭐⭐ | ⭐ | - |
| README_LogIndentHelper | - | ⭐ | ⭐⭐ | - | - |
| TRACTAMENT_MOSTRES_ANTIGUES | - | ⭐⭐ | ⭐⭐⭐ | ⭐⭐ | - |

**Llegenda**: ⭐⭐⭐ Essencial | ⭐⭐ Recomanat | ⭐ Opcional | - No aplicable

---

## 🔍 Cerca per Tema

### Arquitectura
- 📄 [PROCES_CAPTACIO_DADES.md](PROCES_CAPTACIO_DADES.md) - Secció "Arquitectura"
- 📄 [RESUM_FINAL.md](RESUM_FINAL.md) - Secció "Arquitectura del Sistema"

### Classificació de Mostres
- 📄 [PROCES_CAPTACIO_DADES.md](PROCES_CAPTACIO_DADES.md) - Fase 2
- 📄 [DIAGRAMES_FLUX_MERMAID.md](DIAGRAMES_FLUX_MERMAID.md) - Diagrama 2
- 📄 [RESUM_EXECUTIU.md](RESUM_EXECUTIU.md) - Secció "Conceptes Clau"

### Comprovacions Negatius
- 📄 [DIAGRAMES_COMPROVACIONS.md](DIAGRAMES_COMPROVACIONS.md) ⭐
- 📄 [COMPROVACIO_1_NEGATIUS.md](COMPROVACIO_1_NEGATIUS.md)
- 📄 [COMPROVACIO_2_NEGATIUS.md](COMPROVACIO_2_NEGATIUS.md)
- 📄 [COMPROVACIONS_NEGATIUS_RESUM.md](COMPROVACIONS_NEGATIUS_RESUM.md)

### Microorganismes i Mecanismes
- 📄 [PROCES_CAPTACIO_DADES.md](PROCES_CAPTACIO_DADES.md) - Fases 4 i 5
- 📄 [DIAGRAMES_FLUX_MERMAID.md](DIAGRAMES_FLUX_MERMAID.md) - Diagrames 3 i 4

### Auditoria i Traçabilitat
- 📄 [PROCES_CAPTACIO_DADES.md](PROCES_CAPTACIO_DADES.md) - Secció "Logging i Traçabilitat"
- 📄 [RESUM_EXECUTIU.md](RESUM_EXECUTIU.md) - Secció "Seguretat i Traçabilitat"

### Base de Dades
- 📄 [PROCES_CAPTACIO_DADES.md](PROCES_CAPTACIO_DADES.md) - Secció "Model de Dades"
- 📄 [DIAGRAMES_FLUX_MERMAID.md](DIAGRAMES_FLUX_MERMAID.md) - Diagrama 10 (ER)

### Configuració
- 📄 [PROCES_CAPTACIO_DADES.md](PROCES_CAPTACIO_DADES.md) - Secció "Configuració i Parameters"
- 📄 [RESUM_EXECUTIU.md](RESUM_EXECUTIU.md) - Secció "Configuració i Parametrització"

### Casos d'Ús
- 📄 [RESUM_EXECUTIU.md](RESUM_EXECUTIU.md) - Secció "Casos d'Ús Principals" ⭐
- 📄 [PROCES_CAPTACIO_DADES.md](PROCES_CAPTACIO_DADES.md) - Secció "Casos d'Ús Complets"

### Rendiment
- 📄 [RESUM_EXECUTIU.md](RESUM_EXECUTIU.md) - Secció "Rendiment"
- 📄 [PROCES_CAPTACIO_DADES.md](PROCES_CAPTACIO_DADES.md) - Secció "Temps d'Execució Estimats"

---

## 🎯 Rutes de Lectura Recomanades

### 📘 Ruta 1: QUICK START (30 minuts)
```
1. RESUM_EXECUTIU.md (15 min)
   └─ Objectiu, conceptes clau, casos d'ús

2. DIAGRAMES_FLUX_MERMAID.md (15 min)
   └─ Visualitzar diagrames 1, 2 i 6
```

**Perfil**: Nou al projecte, necessita visió general ràpida

---

### 📗 Ruta 2: DESENVOLUPADOR (90 minuts)
```
1. RESUM_FINAL.md (10 min)
   └─ Context general

2. PROCES_CAPTACIO_DADES.md (45 min) ⭐
   └─ Documentació tècnica completa

3. DIAGRAMES_FLUX_MERMAID.md (20 min)
   └─ Tots els diagrames

4. COMPROVACIONS_NEGATIUS_RESUM.md (15 min)
   └─ Resum comprovacions
```

**Perfil**: Desenvolupador que necessita implementar/mantenir

---

### 📕 Ruta 3: ANALISTA (60 minuts)
```
1. RESUM_EXECUTIU.md (15 min)
   └─ Context i objectius

2. DIAGRAMES_COMPROVACIONS.md (20 min)
   └─ Sistema de comprovacions

3. DIAGRAMES_FLUX_MERMAID.md (20 min)
   └─ Diagrames visuals

4. COMPROVACIONS_NEGATIUS_RESUM.md (5 min)
   └─ Resum decisions
```

**Perfil**: Analista de negoci, disseny funcional

---

### 📙 Ruta 4: DEEP DIVE COMPROVACIONS (75 minuts)
```
1. DIAGRAMES_COMPROVACIONS.md (20 min)
   └─ Visió general

2. COMPROVACIO_1_NEGATIUS.md (15 min)
   └─ Detall comprovació 1

3. COMPROVACIO_2_NEGATIUS.md (20 min)
   └─ Detall comprovació 2

4. IMPLEMENTACIO_COMPROVACIONS_RESUM.md (20 min)
   └─ Implementació tècnica
```

**Perfil**: Desenvolupador especialitzant-se en comprovacions

---

## 📁 Estructura de Carpetes

```
MultirIntegraModulab/
├── Docs/
│   ├── 📚 INDEX_GENERAL.md                      (aquest document)
│   ├── 📄 RESUM_EXECUTIU.md                     ⭐ Per a direcció
│   ├── 📄 RESUM_FINAL.md                        Resum tècnic
│   ├── 📄 PROCES_CAPTACIO_DADES.md              ⭐ Doc. tècnica completa
│   ├── 📄 DIAGRAMES_FLUX_MERMAID.md             ⭐ Diagrames interactius
│   ├── 📄 DIAGRAMES_COMPROVACIONS.md            Sistema comprovacions
│   ├── 📄 COMPROVACIO_1_NEGATIUS.md             Comprovació 1 detall
│   ├── 📄 COMPROVACIO_2_NEGATIUS.md             Comprovació 2 detall
│   ├── 📄 COMPROVACIONS_NEGATIUS_RESUM.md       Resum comprovacions
│   ├── 📄 IMPLEMENTACIO_COMPROVACIONS_RESUM.md  Implementació
│   ├── 📄 README_LogIndentHelper.md             Logs indentats
│   └── 📄 TRACTAMENT_MOSTRES_ANTIGUES.md        Mostres antigues
│
├── Application/
│   ├── Services/
│   ├── UseCases/
│   └── DTOs/
│
├── Domain/
│   ├── Entities/
│   ├── Enums/
│   └── Interfaces/
│
└── Infrastructure/
    └── Persistence/
```

---

## 🔖 Glossari de Termes

| Terme | Definició |
|-------|-----------|
| **MOSTRA POSITIVA** | Microorganisme especial O amb mecanismes de resistència |
| **MOSTRA NEGATIVA** | Microorganisme NO especial sense mecanismes O sense creixement |
| **COMPROVACIÓ 1** | Verificar si pacient té positius generals (comportament=1) |
| **COMPROVACIÓ 2** | Verificar si pacient té positius vigents del mateix tipus |
| **NMRCM** | No Mostra Resultats Cultiu Micro - Negatiu no incorporat |
| **CNI** | Combinació No Incorporar - Micro+Mecanisme prohibit |
| **VIGÈNCIA** | Període en què un positiu es considera actiu (90-365 dies) |
| **TIPUS EQUIVALENT** | Tipus de mostra similars (ex: Sang venosa ≈ Sang arterial) |
| **MICROORGANISME ESPECIAL** | Micro amb rellevància epidemiològica alta (MRSA, VRE, etc.) |
| **MECANISME DE RESISTÈNCIA** | Gen o proteïna que confereix resistència (BLEE, KPC, etc.) |

---

## 📞 Informació del Projecte

### Versions

| Component | Versió |
|-----------|--------|
| MultirIntegraModulab | 1.0.0 |
| .NET Framework | 4.8 |
| Documentació | 1.0 (Gener 2025) |

### Autors

- **Desenvolupament**: Equip Desenvolupament
- **Documentació**: Sistema Automatitzat
- **Revisió**: Equip Tècnic

### Contacte

- **Suport tècnic**: [suport@...]
- **Issues**: GitHub Issues
- **Wiki**: GitHub Wiki

---

## 📅 Historial de Versions de la Documentació

| Versió | Data | Canvis |
|--------|------|--------|
| 1.0 | Gener 2025 | Creació inicial de tota la documentació |
| | | • RESUM_EXECUTIU.md |
| | | • PROCES_CAPTACIO_DADES.md |
| | | • DIAGRAMES_FLUX_MERMAID.md |
| | | • INDEX_GENERAL.md |

---

## ✅ Checklist per a Nous Membres de l'Equip

### Dia 1: Visió General
- [ ] Llegir **RESUM_EXECUTIU.md**
- [ ] Revisar diagrames principals a **DIAGRAMES_FLUX_MERMAID.md**
- [ ] Entendre conceptes clau (positius/negatius)

### Dia 2-3: Aprofundiment Tècnic
- [ ] Estudiar **PROCES_CAPTACIO_DADES.md**
- [ ] Revisar codi de Use Cases principals
- [ ] Entendre arquitectura Clean Architecture

### Dia 4-5: Comprovacions
- [ ] Llegir **DIAGRAMES_COMPROVACIONS.md**
- [ ] Estudiar **COMPROVACIO_1_NEGATIUS.md**
- [ ] Estudiar **COMPROVACIO_2_NEGATIUS.md**
- [ ] Revisar implementació

### Setmana 2: Pràctica
- [ ] Executar aplicació en mode test
- [ ] Analitzar logs generats
- [ ] Revisar dades a MySQL
- [ ] Fer proves amb diferents tipus de mostres

### Setmana 3: Contribució
- [ ] Proposar millores
- [ ] Actualitzar documentació si cal
- [ ] Resoldre primers bugs/issues

---

## 🎓 Recursos Externs

### Conceptes Microbiològics
- 📚 CDC Guidelines on Antimicrobial Resistance
- 📚 WHO Priority Pathogens List
- 📚 EUCAST Breakpoints

### Tecnologia
- 📚 .NET Framework 4.8 Documentation
- 📚 Clean Architecture by Robert C. Martin
- 📚 MySQL 8.0 Reference Manual
- 📚 Oracle Database Documentation

### Diagrames
- 🔧 Mermaid.js Documentation: https://mermaid.js.org/
- 🔧 Mermaid Live Editor: https://mermaid.live/

---

## 🚀 Començar Ara

### Per a lectura ràpida (15 min):
👉 **[RESUM_EXECUTIU.md](RESUM_EXECUTIU.md)**

### Per a desenvolupadors (45 min):
👉 **[PROCES_CAPTACIO_DADES.md](PROCES_CAPTACIO_DADES.md)**

### Per a visualitzar fluxos:
👉 **[DIAGRAMES_FLUX_MERMAID.md](DIAGRAMES_FLUX_MERMAID.md)**

---

**Document creat**: Gener 2025  
**Última actualització**: Gener 2025  
**Mantenidor**: Equip de Desenvolupament  
**Versió**: 1.0
