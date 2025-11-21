# 📋 PROCÉS DE CAPTACIÓ DE DADES - DOCUMENTACIÓ COMPLETA

## 📑 Índex
1. [Visió General del Sistema](#visió-general-del-sistema)
2. [Flux Principal de Captació](#flux-principal-de-captació)
3. [Detall de Cada Fase](#detall-de-cada-fase)
4. [Diagrames de Flux](#diagrames-de-flux)
5. [Casos d'Ús](#casos-dús)
6. [Resultats Possibles](#resultats-possibles)

---

## 🎯 Visió General del Sistema

### Objectiu
Integrar mostres microbiològiques des d'Oracle (Modulab) cap a MySQL (MultiR), garantint la coherència, validació i traçabilitat de les dades.

### Components Principals
- **Font de Dades**: Oracle (Modulab) - Sistema origen amb resultats microbiològics
- **Destí**: MySQL (MultiR) - Sistema de gestió de vigilància epidemiològica
- **Motor d'Integració**: MultirIntegraModulab - Aplicació .NET Framework 4.8

### Arquitectura
```
┌─────────────────────────────────────────────────────────────────────┐
│                        ARQUITECTURA CLEAN                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌───────────────┐          ┌───────────────┐                      │
│  │  ORACLE       │          │    MYSQL      │                      │
│  │  (Modulab)    │          │   (MultiR)    │                      │
│  └───────┬───────┘          └───────┬───────┘                      │
│          │                          │                               │
│  ┌───────▼──────────────────────────▼───────┐                      │
│  │      INFRASTRUCTURE LAYER                │                      │
│  │  • ModulabRepository                     │                      │
│  │  • MultiRRepository                      │                      │
│  │  • MultiRDbService                       │                      │
│  └──────────────────┬───────────────────────┘                      │
│                     │                                               │
│  ┌──────────────────▼───────────────────────┐                      │
│  │      APPLICATION LAYER                   │                      │
│  │  • ProcessamentMostresService            │                      │
│  │  • Use Cases:                            │                      │
│  │    - ValidarMostraUseCase                │                      │
│  │    - ClassificarMostraUseCase            │                      │
│  │    - DeterminarTipusIncorporacioUseCase  │                      │
│  │    - ComprovadorMicroorganismesUseCase   │                      │
│  │    - ComprovadorMecanismesUseCase        │                      │
│  │    - ProcessarMostraPositivaUseCase      │                      │
│  │    - ProcessarMostraNegativaUseCase      │                      │
│  └──────────────────┬───────────────────────┘                      │
│                     │                                               │
│  ┌──────────────────▼───────────────────────┐                      │
│  │         DOMAIN LAYER                     │                      │
│  │  • Entities (Mostra, ResultatMostra)     │                      │
│  │  • Enums (TipusMostra, TipusIncorporacio)│                      │
│  │  • Interfaces (IMultiRRepository, ...)   │                      │
│  └──────────────────────────────────────────┘                      │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Flux Principal de Captació

### Diagrama de Flux Complet

```
┌────────────────────────────────────────────────────────────────────┐
│                      INICI DEL PROCÉS                               │
│                                                                     │
│  • Sistema llegeix mostres d'Oracle (Modulab)                      │
│  • Filtre: Mostres amb DataResultat recent                         │
│  • Agrupació: Per EtiquetaId (múltiples resultats per mostra)     │
└───────────────────────────┬────────────────────────────────────────┘
                            │
                            ▼
        ╔═══════════════════════════════════════╗
        ║   FASE 1: VALIDACIÓ INICIAL           ║
        ║   (ValidarMostraUseCase)              ║
        ╚═══════════════════╤═══════════════════╝
                            │
                ┌───────────▼───────────┐
                │ ¿Mostra vàlida?       │
                │ • EtiquetaId not null  │
                │ • Té resultats        │
                │ • NPacient vàlid      │
                └───┬───────────────┬───┘
                   NO              SÍ
                    │               │
        ╔═══════════▼═════════╗    │
        ║ ❌ REBUTJAR        ║    │
        ║ • No processar     ║    │
        ║ • Log warning      ║    │
        ╚════════════════════╝    │
                                   │
        ╔══════════════════════════▼═══════════════════════╗
        ║   FASE 2: CLASSIFICACIÓ                          ║
        ║   (ClassificarMostraUseCase)                     ║
        ╚═══════════════════╤══════════════════════════════╝
                            │
                            │  Analitza cada ResultatMostra:
                            │  • Té microorganisme?
                            │  • Microorganisme especial?
                            │  • Nombre de mecanismes?
                            │
                ┌───────────▼───────────────────────┐
                │  COMPTATGE POSITIUS/NEGATIUS      │
                │                                    │
                │  Per cada resultat:               │
                │  ┌─────────────────────────────┐  │
                │  │ SI té microorganisme:       │  │
                │  │   SI és especial:           │  │
                │  │     SI mecanismes = 0       │  │
                │  │       → 1 POSITIU           │  │
                │  │     SINÓ                    │  │
                │  │       → N POSITIUS          │  │
                │  │   SINÓ (no especial):       │  │
                │  │     SI mecanismes = 0       │  │
                │  │       → 1 NEGATIU           │  │
                │  │     SINÓ                    │  │
                │  │       → N POSITIUS          │  │
                │  │ SINÓ (no té microorganisme) │  │
                │  │   → 1 NEGATIU               │  │
                │  └─────────────────────────────┘  │
                └────────────┬──────────────────────┘
                             │
                ┌────────────▼───────────────┐
                │  CLASSIFICACIÓ FINAL       │
                └────────┬───────────────────┘
                         │
         ┌───────────────┼───────────────────┬──────────────┐
         │               │                   │              │
    ┌────▼────┐    ┌────▼────┐        ┌────▼────┐   ┌────▼─────┐
    │1 Positiu│    │N Positiu│        │1 Negatiu│   │N Negatius│
    └────┬────┘    └────┬────┘        └────┬────┘   └────┬─────┘
         │              │                   │             │
         └──────────────┴───────────────────┴─────────────┘
                                │
        ╔═══════════════════════▼═══════════════════════╗
        ║   FASE 3: DETERMINAR TIPUS INCORPORACIÓ       ║
        ║   (DeterminarTipusIncorporacioUseCase)        ║
        ╚═══════════════════╤═══════════════════════════╝
                            │
                            │  • Obtenir dates Oracle
                            │  • Comparar amb MySQL
                            │  • Classificar estat
                            │
                ┌───────────▼────────────┐
                │  TIPUS INCORPORACIÓ:   │
                │                        │
                │  • NOVA               │
                │  • REPETIDA           │
                │  • VALIDADA           │
                │  • REVALIDADA         │
                │  • DESVALIDADA        │
                │  • ANTIGA             │
                └───────────┬────────────┘
                            │
        ╔═══════════════════▼═══════════════════════╗
        ║   FASE 4: COMPROVACIÓ MICROORGANISMES     ║
        ║   (ComprovadorMicroorganismesUseCase)     ║
        ╚═══════════════════╤═══════════════════════╝
                            │
                            │  Per cada microorganisme únic:
                            │
                ┌───────────▼────────────┐
                │ ¿Existeix a MySQL?     │
                └───┬────────────────┬───┘
                   NO               SÍ
                    │                │
            ╔═══════▼══════╗        │
            ║ CREAR NOU    ║        │
            ║ • Inserir BD ║        │
            ║ • especial=0 ║        │
            ╚═══════╤══════╝        │
                    │                │
                    └────────┬───────┘
                             │
                ┌────────────▼─────────────┐
                │ ¿És microorganisme       │
                │  especial?               │
                │  (taula micro_especial)  │
                └───┬──────────────────┬───┘
                   SÍ                 NO
                    │                  │
            ╔═══════▼══════╗   ╔══════▼══════╗
            ║ ESPECIAL=1   ║   ║ ESPECIAL=0  ║
            ╚══════════════╝   ╚═════════════╝
                    │                  │
                    └────────┬─────────┘
                             │
        ╔════════════════════▼═══════════════════════╗
        ║   FASE 5: COMPROVACIÓ MECANISMES          ║
        ║   (ComprovadorMecanismesUseCase)          ║
        ╚════════════════════╤═══════════════════════╝
                             │
                             │  Per cada mecanisme:
                             │
                ┌────────────▼─────────────┐
                │ ¿Existeix a MySQL?       │
                └───┬──────────────────┬───┘
                   NO                 SÍ
                    │                  │
            ╔═══════▼══════╗          │
            ║ CREAR NOU    ║          │
            ║ • Inserir BD ║          │
            ╚═══════╤══════╝          │
                    │                  │
                    └────────┬─────────┘
                             │
                ┌────────────▼──────────────────┐
                │ ¿Combinació                   │
                │  Microorganisme + Mecanisme   │
                │  marcada com NO INCORPORAR?   │
                └───┬───────────────────────┬───┘
                   SÍ                      NO
                    │                       │
        ╔═══════════▼═════════╗            │
        ║ ❌ ATURAR PROCÉS    ║            │
        ║ • Auditoria CNI     ║            │
        ║ • No incorporar     ║            │
        ╚═════════════════════╝            │
                                            │
        ╔═══════════════════════════════════▼══════════════════════╗
        ║   FASE 6: BIFURCACIÓ SEGONS TIPUS MOSTRA                 ║
        ╚═══════════════════════╤══════════════════════════════════╝
                                │
                    ┌───────────▼────────────┐
                    │ ¿Tipus de mostra?      │
                    └───┬────────────────┬───┘
                       │                 │
                  POSITIVA            NEGATIVA
                       │                 │
        ╔══════════════▼══════════════╗ ╔▼═══════════════════════════╗
        ║ PROCESSAR MOSTRA POSITIVA   ║ ║ PROCESSAR MOSTRA NEGATIVA  ║
        ║                             ║ ║                            ║
        ║ (Veure diagrama detallat)   ║ ║ (Veure diagrama detallat)  ║
        ╚═════════════════════════════╝ ╚════════════════════════════╝
                       │                 │
                       └────────┬────────┘
                                │
        ╔═══════════════════════▼══════════════════════╗
        ║   FASE 7: FINALITZACIÓ                       ║
        ║                                               ║
        ║  • Actualitzar comptadors                    ║
        ║  • Crear auditoria                           ║
        ║  • Retornar resultat                         ║
        ╚═══════════════════════════════════════════════╝
                                │
                                ▼
┌────────────────────────────────────────────────────────────────────┐
│                        RESULTAT FINAL                               │
│                                                                     │
│  ResumProcessamentDto {                                            │
│    TotalMostres = N                                                │
│    TotalProcessats = X                                             │
│    TotalPositius = Y                                               │
│    TotalNegatius = Z                                               │
│    Errors = [ ... ]                                                │
│    TempsExecucio = T ms                                            │
│  }                                                                 │
└────────────────────────────────────────────────────────────────────┘
```

---

## 📊 Detall de Cada Fase

### FASE 1: Validació Inicial

**Objectiu**: Verificar que la mostra té les dades mínimes necessàries

**Use Case**: `ValidarMostraUseCase`

**Comprovacions**:
```
✓ EtiquetaId no és null ni buit
✓ Col·lecció de Resultats no és null
✓ Té almenys 1 resultat
✓ NPacient no és null ni buit
✓ Format NPacient és vàlid (8 dígits numèrics)
```

**Resultat**:
- ✅ **VÀLIDA**: Continua processament
- ❌ **NO VÀLIDA**: Rebutja i registra warning

**Codi d'execució**:
```csharp
bool esValida = _validarMostraUseCase.Executar(mostra);
if (!esValida) {
    _logger.Warning($"Mostra {mostra.EtiquetaId} no vàlida");
    return false;
}
```

---

### FASE 2: Classificació de la Mostra

**Objectiu**: Determinar si la mostra conté resultats positius, negatius o ambdós

**Use Case**: `ClassificarMostraUseCase`

**Lògica de Classificació**:

```
┌─────────────────────────────────────────────────────────────┐
│  Per cada ResultatMostra:                                   │
│                                                              │
│  1. ¿Té microorganisme? (AillamentDescripcio)              │
│     └─ NO → NEGATIU (sense microorganisme)                 │
│     └─ SÍ → Continua                                        │
│                                                              │
│  2. ¿És microorganisme especial?                           │
│     └─ Consulta taula: micro_especial                      │
│                                                              │
│  3. Comptar mecanismes resistència (1-5)                   │
│                                                              │
│  4. APLICAR REGLES:                                         │
│                                                              │
│     SI té microorganisme:                                   │
│       SI és especial:                                       │
│         SI mecanismes = 0:                                  │
│           → 1 POSITIU ⚡                                    │
│           Exemple: MRSA sense mecanismes                   │
│         SINÓ:                                               │
│           → N POSITIUS ⚡⚡                                 │
│           Exemple: MRSA amb 2 mecanismes = 2 positius     │
│                                                              │
│       SI NO és especial:                                    │
│         SI mecanismes = 0:                                  │
│           → 1 NEGATIU 🔵                                   │
│           Exemple: E.coli sense mecanismes                 │
│         SINÓ:                                               │
│           → N POSITIUS ⚡                                  │
│           Exemple: E.coli amb BLEE = 1 positiu            │
│                                                              │
│     SI NO té microorganisme:                                │
│       → 1 NEGATIU 🔵                                       │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

**Tipus de Mostra Finals**:

| Codi | Tipus | Descripció |
|------|-------|------------|
| `UnSolResultatPositiu` | 🟢 | 1 positiu, 0 negatius |
| `MultiplesResultatsTotsPositius` | 🟢🟢 | N positius, 0 negatius |
| `UnSolResultatNegatiu` | 🔵 | 0 positius, 1 negatiu |
| `MultiplesResultatsTotsNegatius` | 🔵🔵 | 0 positius, N negatius |
| `MultiplesResultatsPositiusINegatius` | 🟢🔵 | N positius, M negatius |

**Exemples Reals**:

```
📋 EXEMPLE 1: MRSA (Microorganisme especial sense mecanismes)
   • Microorganisme: "Staphylococcus aureus resistente a meticilina"
   • És especial: SÍ
   • Mecanismes: 0
   → Classificació: 1 POSITIU ⚡

📋 EXEMPLE 2: E.coli amb BLEE
   • Microorganisme: "Escherichia coli"
   • És especial: NO
   • Mecanismes: 1 (BLEE)
   → Classificació: 1 POSITIU ⚡

📋 EXEMPLE 3: E.coli sense mecanismes
   • Microorganisme: "Escherichia coli"
   • És especial: NO
   • Mecanismes: 0
   → Classificació: 1 NEGATIU 🔵

📋 EXEMPLE 4: Sense creixement
   • Microorganisme: null/buit
   → Classificació: 1 NEGATIU 🔵

📋 EXEMPLE 5: Mostra mixta
   Resultat 1:
     • Microorganisme: "MRSA"
     • Especial: SÍ
     • Mecanismes: 0
     → 1 POSITIU
   Resultat 2:
     • Microorganisme: "E.coli"
     • Especial: NO
     • Mecanismes: 0
     → 1 NEGATIU
   → Classificació FINAL: MÚLTIPLES POSITIUS I NEGATIUS (1P + 1N)
```

---

### FASE 3: Determinar Tipus d'Incorporació

**Objectiu**: Comparar l'estat de la mostra a Oracle amb MySQL per decidir l'acció

**Use Case**: `DeterminarTipusIncorporacioUseCase`

**Procés**:

```
┌──────────────────────────────────────────────────────────┐
│  1. Obtenir dates d'Oracle (Modulab):                    │
│     • DataResultat (màxima de tots els resultats)       │
│     • DataValidacio (màxima si algun està validat)      │
│                                                           │
│  2. Consultar MySQL (MultiR):                            │
│     SELECT * FROM mostra_microorganisme                  │
│     WHERE etiqueta = 'ETQ123456'                         │
│                                                           │
│  3. Comparar estats:                                     │
│                                                           │
│     ┌─────────────────────────────────────────────┐     │
│     │ SI no existeix a MySQL:                     │     │
│     │   → NOVA 🆕                                 │     │
│     └─────────────────────────────────────────────┘     │
│                                                           │
│     ┌─────────────────────────────────────────────┐     │
│     │ SI existeix:                                │     │
│     │   SI dates iguals:                          │     │
│     │     → REPETIDA 🔁                          │     │
│     │   SI DataResultat diferent:                 │     │
│     │     → CANVIADA/REVALIDADA 🔄              │     │
│     │   SI abans validada, ara NO:                │     │
│     │     → DESVALIDADA ⬇️                       │     │
│     │   SI abans NO validada, ara SÍ:            │     │
│     │     → VALIDADA ✅                          │     │
│     │   SI ambdues validades amb dates diferents: │     │
│     │     → REVALIDADA 🔄                        │     │
│     └─────────────────────────────────────────────┘     │
└──────────────────────────────────────────────────────────┘
```

**Tipus d'Incorporació**:

| Tipus | Emoji | Descripció | Acció |
|-------|-------|------------|-------|
| **NOVA** | 🆕 | No existeix a MySQL | Crear registres nous |
| **REPETIDA** | 🔁 | Mateix resultat, mateixes dates | Ignorar (skip) |
| **VALIDADA** | ✅ | S'ha validat per primera vegada | Actualitzar dates validació |
| **REVALIDADA** | 🔄 | Validada abans i ara també (dates diferents) | Actualitzar dates validació |
| **DESVALIDADA** | ⬇️ | Abans validada, ara ja no | Actualitzar a no validada |
| **ANTIGA** | 🕐 | Data resultat anterior a últim processament | Només crear auditoria |

---

### FASE 4: Comprovació de Microorganismes

**Objectiu**: Garantir que tots els microorganismes existeixen a la BD i marcar si són especials

**Use Case**: `ComprovadorMicroorganismesUseCase`

**Procés**:

```
┌──────────────────────────────────────────────────────────────┐
│  1. Obtenir llista única de microorganismes de la mostra     │
│                                                               │
│  2. Per cada microorganisme:                                 │
│                                                               │
│     A) Comprovar existència:                                 │
│        ┌──────────────────────────────────────────────┐     │
│        │ SELECT COUNT(*)                              │     │
│        │ FROM microorganisme                          │     │
│        │ WHERE descripcio = 'E.coli'                 │     │
│        │   AND dt_delete IS NULL                      │     │
│        └──────────────────────────────────────────────┘     │
│                                                               │
│        SI no existeix:                                       │
│        ┌──────────────────────────────────────────────┐     │
│        │ INSERT INTO microorganisme                   │     │
│        │ (descripcio, especial, data_entrada)        │     │
│        │ VALUES ('E.coli', 0, NOW())                 │     │
│        └──────────────────────────────────────────────┘     │
│                                                               │
│     B) Comprovar si és especial:                            │
│        ┌──────────────────────────────────────────────┐     │
│        │ SELECT COUNT(*)                              │     │
│        │ FROM micro_especial                          │     │
│        │ WHERE microorganisme = 'MRSA'               │     │
│        │   AND dt_delete IS NULL                      │     │
│        └──────────────────────────────────────────────┘     │
│                                                               │
│        SI existeix → ESPECIAL = TRUE ⚡                      │
│        SINÓ        → ESPECIAL = FALSE                        │
│                                                               │
│  3. Retornar:                                                │
│     • Dictionary<microorganisme, esEspecial>                │
│     • Llista de microorganismes creats                      │
└──────────────────────────────────────────────────────────────┘
```

**Taules Implicades**:

```sql
-- Taula principal de microorganismes
CREATE TABLE microorganisme (
    id INT AUTO_INCREMENT PRIMARY KEY,
    descripcio VARCHAR(255) NOT NULL,
    especial TINYINT(1) DEFAULT 0,
    data_entrada DATETIME,
    dt_delete DATETIME NULL,
    UNIQUE KEY (descripcio)
);

-- Taula de microorganismes especials
CREATE TABLE micro_especial (
    id INT AUTO_INCREMENT PRIMARY KEY,
    microorganisme VARCHAR(255) NOT NULL,
    data_entrada DATETIME,
    dt_delete DATETIME NULL,
    UNIQUE KEY (microorganisme)
);
```

**Exemples**:

```
✅ Escherichia coli:
   • Existeix: SÍ
   • És especial: NO
   • Acció: Cap (ja existeix)

✅ Staphylococcus aureus resistente a meticilina:
   • Existeix: SÍ
   • És especial: SÍ (taula micro_especial)
   • Acció: Cap (ja existeix i marcat com especial)

🆕 Klebsiella pneumoniae XDR:
   • Existeix: NO
   • Acció: CREAR microorganisme amb especial=0
   • Nota: Caldria afegir-lo manualment a micro_especial si és especial
```

---

### FASE 5: Comprovació de Mecanismes de Resistència

**Objectiu**: Garantir que tots els mecanismes existeixen i detectar combinacions prohibides

**Use Case**: `ComprovadorMecanismesResistenciaUseCase`

**Procés**:

```
┌──────────────────────────────────────────────────────────────────┐
│  Per cada ResultatMostra:                                         │
│                                                                    │
│    1. Obtenir mecanismes (fins a 5 per resultat)                 │
│                                                                    │
│    2. Per cada mecanisme:                                         │
│                                                                    │
│       A) Comprovar existència:                                   │
│          ┌────────────────────────────────────────────────┐      │
│          │ SELECT COUNT(*)                                │      │
│          │ FROM mecanisme_resistencia                     │      │
│          │ WHERE codi = 'BLEE'                           │      │
│          │   AND dt_delete IS NULL                        │      │
│          └────────────────────────────────────────────────┘      │
│                                                                    │
│          SI no existeix:                                          │
│          ┌────────────────────────────────────────────────┐      │
│          │ INSERT INTO mecanisme_resistencia              │      │
│          │ (codi, descripcio, data_entrada)              │      │
│          │ VALUES ('BLEE', 'Betalactamasa...', NOW())    │      │
│          └────────────────────────────────────────────────┘      │
│                                                                    │
│       B) Comprovar combinació NO INCORPORAR:                     │
│          ┌────────────────────────────────────────────────┐      │
│          │ SELECT COUNT(*)                                │      │
│          │ FROM micro_mecanisme_noincoporar               │      │
│          │ WHERE microorganisme = 'E.coli'               │      │
│          │   AND mecanisme_resistencia = 'BLEE'         │      │
│          │   AND dt_delete IS NULL                        │      │
│          └────────────────────────────────────────────────┘      │
│                                                                    │
│          SI existeix:                                             │
│            ❌ ATURAR PROCESSAMENT                                │
│            • Crear auditoria CNI                                 │
│            • Retornar ContinuarProcessament = FALSE              │
│                                                                    │
│          SINÓ:                                                    │
│            ✅ CONTINUAR                                          │
│                                                                    │
└──────────────────────────────────────────────────────────────────┘
```

**Taules Implicades**:

```sql
-- Mecanismes de resistència
CREATE TABLE mecanisme_resistencia (
    id INT AUTO_INCREMENT PRIMARY KEY,
    codi VARCHAR(50) NOT NULL,
    descripcio VARCHAR(255),
    data_entrada DATETIME,
    dt_delete DATETIME NULL,
    UNIQUE KEY (codi)
);

-- Combinacions prohibides
CREATE TABLE micro_mecanisme_noincoporar (
    id INT AUTO_INCREMENT PRIMARY KEY,
    microorganisme VARCHAR(255) NOT NULL,
    mecanisme_resistencia VARCHAR(50) NOT NULL,
    observacions TEXT,
    data_entrada DATETIME,
    dt_delete DATETIME NULL,
    UNIQUE KEY (microorganisme, mecanisme_resistencia)
);
```

**Exemples**:

```
✅ E.coli + BLEE:
   • Mecanisme existeix: SÍ
   • Combinació prohibida: NO
   • Acció: Continuar

❌ Pseudomonas aeruginosa + VIM:
   • Mecanisme existeix: SÍ
   • Combinació prohibida: SÍ (a la taula micro_mecanisme_noincoporar)
   • Acció: ATURAR PROCESSAMENT
   • Auditoria: CNI (Combinació No Incorporar)

🆕 E.coli + NDM-1:
   • Mecanisme existeix: NO
   • Acció: CREAR mecanisme
   • Combinació prohibida: NO
   • Acció: Continuar
```

---

### FASE 6A: Processar Mostra POSITIVA

**Use Case**: `ProcessarMostraPositivaUseCase`

**Diagrama de Flux Detallat**:

```
┌────────────────────────────────────────────────────────────────┐
│            PROCESSAR MOSTRA POSITIVA                            │
└────────────────────────────┬───────────────────────────────────┘
                             │
                             ▼
        ╔════════════════════════════════════╗
        ║  1. Obtenir pacient de WebService ║
        ╚════════════════╤═══════════════════╝
                         │
            ┌────────────▼─────────────┐
            │ ¿Pacient trobat?         │
            └────┬─────────────────┬───┘
                NO               SÍ
                 │                │
      ╔══════════▼════════╗      │
      ║ ⚠️ WARNING        ║      │
      ║ Pacient no trobat ║      │
      ║ Continuar igualment║      │
      ╚═══════════════════╝      │
                 │                │
                 └────────┬───────┘
                          │
        ╔═════════════════▼═══════════════════════╗
        ║  2. Preparar inserció pacient_diagnostic║
        ╚═════════════════╤═══════════════════════╝
                          │
            ┌─────────────▼──────────────┐
            │ ¿Pacient ja existeix       │
            │  a pacients_diagnostics?   │
            └────┬──────────────────┬────┘
                SÍ                 NO
                 │                  │
                 │         ╔════════▼══════════╗
                 │         ║ INSERT INTO       ║
                 │         ║ pacients_diagnostics║
                 │         ╚════════╤══════════╝
                 │                  │
                 └──────────┬───────┘
                            │
        ╔═══════════════════▼══════════════════════════════╗
        ║  3. Per cada resultat POSITIU:                   ║
        ╚═══════════════════╤══════════════════════════════╝
                            │
                            ▼
            ┌───────────────────────────────────┐
            │ A) Crear/Actualitzar              │
            │    pacients_diagnostics_mostra    │
            │                                    │
            │ • npat                            │
            │ • data_mostra                     │
            │ • tipus_mostra_m                  │
            │ • etiqueta                        │
            │ • valoracio = '2' (POSITIU)       │
            │ • vigent = 'S'                    │
            │ • data_entrada / modificacio      │
            └─────────────┬─────────────────────┘
                          │
            ┌─────────────▼─────────────────────┐
            │ B) Crear mostra_microorganisme    │
            │                                    │
            │ • npat                            │
            │ • etiqueta                        │
            │ • data_mostra                     │
            │ • data_resultat                   │
            │ • data_validacio (si escau)       │
            │ • microorganisme                  │
            │ • mostra                          │
            │ • id_prova                        │
            │ • estat_validacio                 │
            └─────────────┬─────────────────────┘
                          │
            ┌─────────────▼─────────────────────┐
            │ C) Crear micro_mecanisme_mostra   │
            │    (per cada mecanisme 1-5)       │
            │                                    │
            │ • npat                            │
            │ • etiqueta                        │
            │ • data_mostra                     │
            │ • microorganisme                  │
            │ • mecanisme_resistencia           │
            │ • data_entrada                    │
            └─────────────┬─────────────────────┘
                          │
            ┌─────────────▼─────────────────────┐
            │ D) Crear/Actualitzar tipus_mostra │
            │    (si no existeix)               │
            └─────────────┬─────────────────────┘
                          │
            ┌─────────────▼─────────────────────┐
            │ E) Crear/Actualitzar tipus_prova  │
            │    (si no existeix)               │
            └─────────────┬─────────────────────┘
                          │
            ┌─────────────▼─────────────────────┐
            │ F) Actualitzar dates pacient:     │
            │    • Última data d'inclusió       │
            │    • Última mostra positiva       │
            └─────────────┬─────────────────────┘
                          │
        ╔═════════════════▼══════════════════════╗
        ║  4. Crear auditoria integració        ║
        ║     • Codi: OK                         ║
        ║     • Detalls: Dates, microorganismes ║
        ╚════════════════════════════════════════╝
                          │
                          ▼
        ╔═════════════════════════════════════════╗
        ║  RESULTAT: ProcessamentPositiu          ║
        ║  • Exitosa = true                       ║
        ║  • ResultatsProcessats++                ║
        ║  • MicroorganismesCreats = [ ... ]     ║
        ╚═════════════════════════════════════════╝
```

**Taules Actualitzades**:

1. **pacients_diagnostics** - Registre general del pacient
2. **pacients_diagnostics_mostra** - Registre del resultat positiu
3. **mostra_microorganisme** - Detall de la mostra amb microorganisme
4. **micro_mecanisme_mostra** - Relació microorganisme-mecanisme
5. **tipusmostra_m** - Tipus de mostra (si nou)
6. **tipusprova_m** - Tipus de prova (si nou)
7. **auditoria_integracio_modulab** - Traçabilitat

---

### FASE 6B: Processar Mostra NEGATIVA

**Use Case**: `ProcessarMostraNegativaUseCase`

**Diagrama de Flux Detallat** (ja documentat a `DIAGRAMES_COMPROVACIONS.md`):

Veure document `DIAGRAMES_COMPROVACIONS.md` per al flux complet de comprovacions de negatius.

**Resum**:

```
┌────────────────────────────────────────────────────────────────┐
│            PROCESSAR MOSTRA NEGATIVA                            │
└────────────────────────┬───────────────────────────────────────┘
                         │
        ╔════════════════▼════════════════╗
        ║  COMPROVACIÓ 1:                ║
        ║  ¿Pacient té positius generals?║
        ╚════════════╤═══════════════════╝
                     │
         ┌───────────▼──────────┐
         │ SI comportament = 1: │
         │   i té positius      │
         └───┬──────────────┬───┘
            SÍ            NO
             │             │
    ╔════════▼═══════╗    │
    ║ ✅ INCORPORAR  ║    │
    ║ (Comprovació 1)║    │
    ╚════════════════╝    │
             │             │
             │             ▼
             │   ╔═════════════════════════════╗
             │   ║  COMPROVACIÓ 2:             ║
             │   ║  ¿Pacient té positius       ║
             │   ║   vigents del mateix tipus? ║
             │   ╚══════════╤══════════════════╝
             │              │
             │    ┌─────────▼──────────┐
             │    │ Té positius vigents│
             │    │ tipus equivalent?  │
             │    └────┬──────────┬────┘
             │        SÍ        NO
             │         │         │
             │  ╔══════▼═══╗  ╔═▼══════════╗
             │  ║✅INCORP. ║  ║❌NO INCORP.║
             │  ║(Compr.2) ║  ║   (NMRCM)  ║
             │  ╚══════╤═══╝  ╚═╤══════════╝
             │         │         │
             └─────────┴─────────┘
                       │
        ╔══════════════▼══════════════╗
        ║  SI INCORPORAR:             ║
        ║  • Crear pacients_diag.     ║
        ║  • Crear pacients_diag_most.║
        ║  • Crear mostra_micro.      ║
        ║  • Auditoria OK             ║
        ║                             ║
        ║  SI NO INCORPORAR:          ║
        ║  • Auditoria NMRCM          ║
        ║  • Increment contador       ║
        ╚═════════════════════════════╝
```

---

## 📈 Casos d'Ús Complets

### CAS 1: Mostra Nova Positiva amb MRSA

```
📥 ENTRADA:
   • EtiquetaId: ETQ001234
   • NPacient: 12345678
   • DataResultat: 2025-01-15 10:30
   • DataValidacio: 2025-01-15 14:00
   • Microorganisme: "Staphylococcus aureus resistente a meticilina"
   • Mecanismes: Cap
   • TipusMostra: "Frotis rectal"
   • TipusProva: "Cultivo bacteriológico"

📋 PROCESSAMENT:
   ✅ Fase 1: Validació → VÀLIDA
   ✅ Fase 2: Classificació → 1 POSITIU (micro especial sense mecanismes)
   ✅ Fase 3: Tipus Incorporació → NOVA (no existeix a MySQL)
   ✅ Fase 4: Microorganismes → MRSA ja existeix i és ESPECIAL
   ✅ Fase 5: Mecanismes → No té mecanismes
   ✅ Fase 6A: Processar Positiva
      • Crear pacients_diagnostics
      • Crear pacients_diagnostics_mostra (valoracio='2')
      • Crear mostra_microorganisme
      • Crear tipus_mostra (si no existeix)
      • Crear tipus_prova (si no existeix)
      • Actualitzar dates pacient
      • Crear auditoria OK

📤 SORTIDA:
   • Exitosa: TRUE
   • ResultatsProcessats: 1
   • MicroorganismesCreats: []
   • AuditoriaCodi: OK
```

### CAS 2: Mostra Positiva amb E.coli i BLEE

```
📥 ENTRADA:
   • EtiquetaId: ETQ001235
   • NPacient: 87654321
   • DataResultat: 2025-01-15 11:00
   • Microorganisme: "Escherichia coli"
   • Mecanismes: ["BLEE"]
   • TipusMostra: "Orina"

📋 PROCESSAMENT:
   ✅ Fase 1: Validació → VÀLIDA
   ✅ Fase 2: Classificació → 1 POSITIU (micro NO especial amb 1 mecanisme)
   ✅ Fase 3: Tipus Incorporació → NOVA
   ✅ Fase 4: Microorganismes → E.coli existeix i NO és especial
   ✅ Fase 5: Mecanismes → BLEE existeix, combinació NO prohibida
   ✅ Fase 6A: Processar Positiva
      • Crear tots els registres
      • Crear micro_mecanisme_mostra per BLEE

📤 SORTIDA:
   • Exitosa: TRUE
   • ResultatsProcessats: 1
```

### CAS 3: Mostra Negativa amb Pacient amb Positius

```
📥 ENTRADA:
   • EtiquetaId: ETQ001236
   • NPacient: 11223344
   • Microorganisme: null (sense creixement)
   • TipusMostra: "Frotis rectal"
   • Comportament tipus mostra: 1

📋 PROCESSAMENT:
   ✅ Fase 1: Validació → VÀLIDA
   ✅ Fase 2: Classificació → 1 NEGATIU (sense microorganisme)
   ✅ Fase 3: Tipus Incorporació → NOVA
   ✅ Fase 6B: Processar Negativa
      • Comprovació 1: Pacient TÉ positius generals
      → DECISIÓ: INCORPORAR (per Comprovació 1)
      • Crear pacients_diagnostics_mostra (valoracio='0')
      • Crear mostra_microorganisme (sense micro)
      • Auditoria OK

📤 SORTIDA:
   • Exitosa: TRUE
   • ResultatsProcessats: 1
   • IncorporatsPerComprovacio1: 1
```

### CAS 4: Mostra Negativa No Incorporada

```
📥 ENTRADA:
   • EtiquetaId: ETQ001237
   • NPacient: 55667788
   • Microorganisme: null
   • TipusMostra: "Sang"
   • Comportament tipus mostra: 0

📋 PROCESSAMENT:
   ✅ Fase 1: Validació → VÀLIDA
   ✅ Fase 2: Classificació → 1 NEGATIU
   ✅ Fase 3: Tipus Incorporació → NOVA
   ✅ Fase 6B: Processar Negativa
      • Comprovació 1: NO aplica (comportament=0)
      • Comprovació 2: Pacient NO té positius vigents de "Sang"
      → DECISIÓ: NO INCORPORAR
      • Auditoria NMRCM

📤 SORTIDA:
   • Exitosa: TRUE
   • ResultatsNoIncorporats: 1
   • CodiAuditoria: NMRCM
```

### CAS 5: Mostra amb Combinació Prohibida

```
📥 ENTRADA:
   • EtiquetaId: ETQ001238
   • NPacient: 99887766
   • Microorganisme: "Pseudomonas aeruginosa"
   • Mecanismes: ["VIM"]
   • TipusMostra: "Esputo"

📋 PROCESSAMENT:
   ✅ Fase 1: Validació → VÀLIDA
   ✅ Fase 2: Classificació → 1 POSITIU
   ✅ Fase 3: Tipus Incorporació → NOVA
   ✅ Fase 4: Microorganismes → P.aeruginosa existeix
   ❌ Fase 5: Mecanismes → VIM existeix
      • Combinació P.aeruginosa + VIM està a micro_mecanisme_noincoporar
      → DECISIÓ: ATURAR PROCESSAMENT
      • Auditoria CNI (Combinació No Incorporar)

📤 SORTIDA:
   • Exitosa: FALSE
   • ContinuarProcessament: FALSE
   • Missatge: "Combinació P.aeruginosa + VIM marcada com NO INCORPORAR"
   • CodiAuditoria: CNI
```

### CAS 6: Mostra Mixta (Positius i Negatius)

```
📥 ENTRADA:
   • EtiquetaId: ETQ001239
   • NPacient: 44556677
   • Resultat 1:
     - Microorganisme: "MRSA"
     - Mecanismes: []
   • Resultat 2:
     - Microorganisme: "Escherichia coli"
     - Mecanismes: []
   • TipusMostra: "Frotis rectal"

📋 PROCESSAMENT:
   ✅ Fase 1: Validació → VÀLIDA
   ✅ Fase 2: Classificació → MÚLTIPLES POSITIUS I NEGATIUS
      • Resultat 1: MRSA especial sense mecanismes → 1 POSITIU
      • Resultat 2: E.coli NO especial sense mecanismes → 1 NEGATIU
      → TOTAL: 1 Positiu + 1 Negatiu
   ✅ Fase 3: Tipus Incorporació → NOVA
   ✅ Fase 4-5: Comprovacions → OK
   ✅ Fase 6A: Processar Positiva (per MRSA)
   ✅ Fase 6B: Processar Negativa (per E.coli)

📤 SORTIDA:
   • Exitosa: TRUE
   • ResultatsProcessats: 2
   • TotalPositius: 1
   • TotalNegatius: 1
```

---

## 📊 Resultats Possibles

### Estructura del Resultat Final

```csharp
public class ResumProcessamentDto
{
    // Comptadors generals
    public int TotalMostres { get; set; }
    public int TotalProcessats { get; set; }
    public int TotalPositius { get; set; }
    public int TotalNegatius { get; set; }
    
    // Comptadors específics negatius
    public int ResultatsNoIncorporats { get; set; }
    public int IncorporatsPerComprovacio1 { get; set; }
    public int IncorporatsPerComprovacio2 { get; set; }
    
    // Errors i warnings
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
    
    // Rendiment
    public TimeSpan TempsExecucio { get; set; }
    public DateTime DataInici { get; set; }
    public DateTime DataFi { get; set; }
    
    // Detalls
    public Dictionary<string, int> MicroorganismesCreats { get; set; }
    public Dictionary<string, int> MecanismesCreats { get; set; }
}
```

### Codis d'Auditoria

| Codi | Descripció | Significat |
|------|------------|------------|
| **OK** | Processament correcte | Mostra incorporada correctament |
| **CNI** | Combinació No Incorporar | Microorganisme + Mecanisme prohibit |
| **NMRCM** | No Mostra Resultats Cultiu Micro | Negatiu no incorporat (sense positius vigents) |
| **ERROR** | Error en processament | Exception o error general |

### Estats de Validació

| Estat | Valor | Descripció |
|-------|-------|------------|
| **Pendent** | 0 | Resultat no validat |
| **Validat** | 1 | Resultat validat |
| **Desvalidat** | 2 | Resultat desvalidat |

### Valoracions de Mostra

| Valoració | Valor | Descripció |
|-----------|-------|------------|
| **Negatiu** | '0' | Sense microorganisme o micro NO especial sense mecanismes |
| **Positiu** | '2' | Microorganisme especial o amb mecanismes |

---

## 🔧 Configuració i Parameters

### App.config

```xml
<appSettings>
    <!-- Oracle (Modulab) -->
    <add key="OracleConnectionString" value="..." />
    
    <!-- MySQL (MultiR) -->
    <add key="MySqlConnectionString" value="..." />
    
    <!-- WebService Pacients -->
    <add key="PacientWebServiceUrl" value="https://..." />
    <add key="WebServiceTimeout" value="30" />
    
    <!-- Processament -->
    <add key="DiesHistoric" value="7" />
    <add key="MaxMostresPerExecucio" value="1000" />
    <add key="ProcessarMostresAntigues" value="true" />
</appSettings>
```

### Paràmetres de Vigència

Els positius tenen vigència segons el tipus de mostra:

```
Frotis rectal: 365 dies
Sang: 180 dies
Orina: 90 dies
Esputo: 90 dies
...
```

Configurat a la taula `tipusmostra_m.dies_vigencia_positiu`

---

## 📝 Logging i Traçabilitat

### Nivells de Log

```
[INFO]    Flux normal de processament
[WARNING] Situacions anòmales però controlades
[ERROR]   Errors que impedeixen processar una mostra
[FATAL]   Errors crítics del sistema
```

### Indentació de Logs

El sistema utilitza `LogIndentHelper` per estructurar els logs:

```
🏁 Processar mostres [Inici]
  📦 Processant mostra ETQ001234
    🧪 Mostra es classifica com 'POSITIU'
      🔎 Comprovant microorganismes
        ⚡ Microorganisme MRSA: 'ESPECIAL'
      ✅ Mostra processada correctament
  📦 Processant mostra ETQ001235
    ...
```

### Auditoria MySQL

Totes les mostres processades es registren a `auditoria_integracio_modulab`:

```sql
CREATE TABLE auditoria_integracio_modulab (
    id INT AUTO_INCREMENT PRIMARY KEY,
    data_integracio DATETIME NOT NULL,
    etiqueta VARCHAR(50) NOT NULL,
    npat VARCHAR(20),
    data_mostra DATETIME,
    data_resultat DATETIME,
    data_validacio DATETIME,
    microorganisme VARCHAR(255),
    tipus_mostra VARCHAR(100),
    codi_retorn VARCHAR(10),
    observacions TEXT,
    INDEX idx_etiqueta (etiqueta),
    INDEX idx_npat (npat),
    INDEX idx_data_integracio (data_integracio)
);
```

---

## 🎯 Resum Executiu

### Flux Complet en 10 Passos

1. **📥 LECTURA** - Obtenir mostres d'Oracle amb filtre de dates
2. **✅ VALIDACIÓ** - Verificar dades mínimes (Etiqueta, NPacient, Resultats)
3. **🧪 CLASSIFICACIÓ** - Determinar si és positiva, negativa o mixta
4. **🔎 TIPUS** - Comparar amb MySQL per determinar si és nova, repetida, etc.
5. **🦠 MICROORGANISMES** - Comprovar/crear microorganismes i marcar especials
6. **🛡️ MECANISMES** - Comprovar/crear mecanismes i detectar combinacions prohibides
7. **🔀 BIFURCACIÓ** - Processar com a positiva o negativa segons classificació
8. **💾 PERSISTÈNCIA** - Crear/actualitzar registres a MySQL
9. **📝 AUDITORIA** - Registrar traçabilitat (OK, CNI, NMRCM)
10. **📊 RESULTAT** - Retornar resum amb estadístiques

### Criteris Clau

#### Per a POSITIUS:
- **Sempre s'incorporen** (excepte si combinació prohibida)
- Creen registres a `pacients_diagnostics_mostra` amb `valoracio='2'`
- Generen entrada a `mostra_microorganisme`
- Si tenen mecanismes, creen `micro_mecanisme_mostra`

#### Per a NEGATIUS:
- **Incorporació condicionada** a 2 comprovacions
- **Comprovació 1**: Si comportament=1 i pacient té positius generals
- **Comprovació 2**: Si pacient té positius vigents del mateix tipus de mostra
- Si NO s'incorporen: auditoria NMRCM

### Temps d'Execució Estimats

| Mostres | Temps Aprox. | Observacions |
|---------|-------------|--------------|
| 10 | 2-5 seg | Processament ràpid |
| 100 | 15-30 seg | Normal |
| 1.000 | 3-5 min | Per lots grans |
| 10.000 | 30-60 min | Executar fora d'hores |

---

**Documentació creada**: Gener 2025  
**Versió**: 1.0  
**Autor**: Sistema MultirIntegraModulab  
**Estat**: ✅ Completa i actualitzada
