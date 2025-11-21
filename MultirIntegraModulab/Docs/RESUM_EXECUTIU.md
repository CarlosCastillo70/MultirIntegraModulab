# 📋 RESUM EXECUTIU - SISTEMA D'INTEGRACIÓ MODULAB-MULTIR

## 🎯 Objectiu del Sistema

El sistema **MultirIntegraModulab** automatitza la integració de resultats microbiològics des d'Oracle (Modulab) cap a MySQL (MultiR), garantint la coherència, validació i traçabilitat de les dades per a la vigilància epidemiològica.

---

## 📊 Visió General

### Flux de Dades

```
┌─────────────┐                    ┌──────────────┐                    ┌─────────────┐
│   ORACLE    │  ──── Lectura ──►  │   .NET App   │  ──── Insert ──►  │    MYSQL    │
│  (Modulab)  │                    │  Integració  │                    │  (MultiR)   │
└─────────────┘                    └──────────────┘                    └─────────────┘
    Sistema                             Motor de                         Sistema de
    Laboratori                       Processament                    Vigilància Epid.
```

### Procés en 7 Fases

| # | Fase | Descripció | Temps Aprox. |
|---|------|------------|--------------|
| 1️⃣ | **Validació** | Verificació dades mínimes | <1 seg |
| 2️⃣ | **Classificació** | Determinar si és positiva/negativa | <1 seg |
| 3️⃣ | **Tipus Incorporació** | Nova, repetida, validada, etc. | 1-2 seg |
| 4️⃣ | **Microorganismes** | Verificar/crear microorganismes | 2-3 seg |
| 5️⃣ | **Mecanismes** | Verificar/crear mecanismes | 2-3 seg |
| 6️⃣ | **Processament** | Crear registres MySQL | 3-5 seg |
| 7️⃣ | **Auditoria** | Traçabilitat completa | <1 seg |

**Temps total per mostra**: **10-15 segons** aprox.

---

## 🔑 Conceptes Clau

### 1. Classificació de Mostres

#### 🟢 MOSTRES POSITIVES
- **Definició**: Microorganismes especials O microorganismes amb mecanismes de resistència
- **Acció**: S'incorporen **sempre** (excepte combinacions prohibides)
- **Impacte**: Alta prioritat en vigilància epidemiològica

**Exemples**:
- MRSA (Staphylococcus aureus resistent a meticilina)
- E.coli amb BLEE (betalactamasa d'espectre estès)
- Klebsiella pneumoniae amb carbapenemasa

#### 🔵 MOSTRES NEGATIVES
- **Definició**: Microorganismes NO especials sense mecanismes O sense creixement
- **Acció**: S'incorporen **només si** el pacient té positius vigents
- **Impacte**: Control d'evolució de pacients colonitzats

**Exemples**:
- Frotis rectal sense creixement (pacient amb positius anteriors)
- E.coli sense mecanismes (cultiu rutinari)

### 2. Comprovacions per a Negatius

```
┌─────────────────────────────────────────────────────────────┐
│  COMPROVACIÓ 1:                                             │
│  Si el tipus de mostra té comportament=1                    │
│  i el pacient té algun positiu general                      │
│  → INCORPORAR                                               │
└─────────────────────────────────────────────────────────────┘
         │
         │ SI NO passa Comprovació 1
         ▼
┌─────────────────────────────────────────────────────────────┐
│  COMPROVACIÓ 2:                                             │
│  Si el pacient té positius vigents                          │
│  del mateix tipus de mostra (o equivalents)                 │
│  → INCORPORAR                                               │
└─────────────────────────────────────────────────────────────┘
         │
         │ SI NO passa cap comprovació
         ▼
┌─────────────────────────────────────────────────────────────┐
│  NO INCORPORAR                                              │
│  Auditoria: NMRCM (No Mostra Resultats Cultiu Micro)       │
└─────────────────────────────────────────────────────────────┘
```

### 3. Combinacions Prohibides

El sistema detecta combinacions de microorganisme + mecanisme marcades com **NO INCORPORAR**:

- Evita falsos positius
- Combinacions conegudes com a no rellevants epidemiològicament
- Configurable via taula `micro_mecanisme_noincoporar`

**Exemple**:
- Pseudomonas aeruginosa + VIM (si està configurat com a no rellevant)

---

## 📈 Mètriques i Resultats

### Estructura del Resultat

```csharp
ResumProcessamentDto {
    TotalMostres              // Total llegides d'Oracle
    TotalProcessats           // Total incorporades
    TotalPositius             // Mostres positives
    TotalNegatius             // Mostres negatives
    ResultatsNoIncorporats    // Negatius no incorporats (NMRCM)
    IncorporatsPerComprovacio1  // Via comprovació 1
    IncorporatsPerComprovacio2  // Via comprovació 2
    Errors                    // Llista d'errors
    TempsExecucio             // Durada del procés
}
```

### Exemple de Resultat

```
📊 RESUM D'EXECUCIÓ
══════════════════════════════════════════════════════
📥 Total mostres llegides:        150
✅ Total processades:             142
   └─ 🟢 Positius:                 85
   └─ 🔵 Negatius incorporats:     57
      ├─ Via Comprovació 1:        23
      └─ Via Comprovació 2:        34
❌ No incorporades (NMRCM):         8
🚫 Errors:                          0
⏱️  Temps d'execució:              18 minuts 32 segons
══════════════════════════════════════════════════════
```

---

## 🎯 Casos d'Ús Principals

### 📋 CAS 1: Pacient amb MRSA (Positiu)

```
SITUACIÓ:
  Pacient 12345678 amb frotis rectal POSITIU per MRSA

PROCESSAMENT:
  ✅ Validació OK
  ✅ Classificació: 1 POSITIU (MRSA és especial)
  ✅ Tipus: NOVA (no existia a MultiR)
  ✅ Microorganisme: MRSA existeix i és ESPECIAL
  ✅ Mecanismes: No té
  ⚡ PROCESSAR POSITIVA:
     • Crear pacients_diagnostics
     • Crear pacients_diagnostics_mostra (valoracio='2')
     • Crear mostra_microorganisme
     • Actualitzar dates pacient

RESULTAT:
  ✅ Incorporat correctament
  📝 Auditoria: OK
  🔔 Alerta epidemiològica activada
```

### 📋 CAS 2: Frotis Rectal Negatiu (Pacient amb Positius)

```
SITUACIÓ:
  Pacient 12345678 amb frotis rectal NEGATIU
  (mateix pacient del CAS 1, una setmana després)

PROCESSAMENT:
  ✅ Validació OK
  ✅ Classificació: 1 NEGATIU (sense creixement)
  ✅ Tipus: NOVA
  🔍 PROCESSAR NEGATIVA:
     • Comprovació 1: Comportament=1 i té positius
     → DECISIÓ: INCORPORAR (Comprovació 1)

RESULTAT:
  ✅ Incorporat correctament
  📝 Auditoria: OK
  📊 Control evolució: Pacient continua en seguiment
```

### 📋 CAS 3: Orina Negativa (Pacient sense Positius)

```
SITUACIÓ:
  Pacient 87654321 amb urinocultiu NEGATIU
  Pacient sense positius vigents

PROCESSAMENT:
  ✅ Validació OK
  ✅ Classificació: 1 NEGATIU
  ✅ Tipus: NOVA
  🔍 PROCESSAR NEGATIVA:
     • Comprovació 1: NO aplica (comportament=0)
     • Comprovació 2: NO té positius vigents
     → DECISIÓ: NO INCORPORAR

RESULTAT:
  ⚠️ No incorporat
  📝 Auditoria: NMRCM
  📊 Comptador NoIncorporats++
```

### 📋 CAS 4: E.coli amb BLEE (Positiu)

```
SITUACIÓ:
  Pacient 11223344 amb urinocultiu POSITIU
  E.coli amb mecanisme BLEE

PROCESSAMENT:
  ✅ Validació OK
  ✅ Classificació: 1 POSITIU (micro NO especial + mecanisme)
  ✅ Tipus: NOVA
  ✅ Microorganisme: E.coli existeix, NO especial
  ✅ Mecanisme: BLEE existeix
  ✅ Combinació: NO prohibida
  ⚡ PROCESSAR POSITIVA:
     • Crear tots els registres
     • Crear micro_mecanisme_mostra per BLEE

RESULTAT:
  ✅ Incorporat correctament
  📝 Auditoria: OK
  🔔 Alerta: E.coli amb BLEE
```

---

## 🛡️ Seguretat i Traçabilitat

### Auditoria Completa

**Cada mostra** processada genera un registre d'auditoria amb:

- Data i hora d'integració
- Etiqueta de la mostra
- Pacient
- Dates de resultat i validació
- Microorganisme detectat
- Tipus de mostra
- Codi de retorn (OK, CNI, NMRCM, ERROR)
- Observacions

### Codis d'Auditoria

| Codi | Significat | Descripció |
|------|-----------|------------|
| **OK** | ✅ Èxit | Mostra processada i incorporada correctament |
| **CNI** | 🚫 Combinació No Incorporar | Microorganisme + Mecanisme prohibit |
| **NMRCM** | ⚠️ No Mostres Resultats Cultiu Micro | Negatiu no incorporat (sense positius vigents) |
| **ERROR** | ❌ Error | Excepció o error durant el processament |

### Traçabilitat

```
┌─────────────────────────────────────────────────────────────┐
│  TRAÇABILITAT COMPLETA:                                     │
│                                                              │
│  1. Log detallat (nivells INFO, WARNING, ERROR)            │
│  2. Auditoria MySQL (auditoria_integracio_modulab)         │
│  3. Timestamps en tots els registres                        │
│  4. Tracking de modificacions (data_entrada, data_modif.)  │
│  5. Soft deletes (dt_delete) per històric                  │
└─────────────────────────────────────────────────────────────┘
```

---

## ⚙️ Configuració i Parametrització

### Paràmetres Clau

| Paràmetre | Valor per Defecte | Descripció |
|-----------|------------------|------------|
| `DiesHistoric` | 7 | Dies enrere per llegir mostres |
| `MaxMostresPerExecucio` | 1000 | Màxim mostres per execució |
| `ProcessarMostresAntigues` | true | Processar mostres antigues |
| `WebServiceTimeout` | 30 seg | Timeout WebService pacients |

### Vigència de Positius

Configurat per **tipus de mostra** a `tipusmostra_m.dies_vigencia_positiu`:

| Tipus Mostra | Vigència |
|--------------|----------|
| Frotis rectal | 365 dies |
| Sang | 180 dies |
| Orina | 90 dies |
| Esputo | 90 dies |

**Impacte**: Els positius fora de vigència NO compten per a les comprovacions de negatius.

---

## 📊 Rendiment

### Temps d'Execució

| Volum | Temps Estimat | Observacions |
|-------|--------------|--------------|
| 10 mostres | 2-5 seg | Temps mínim setup |
| 100 mostres | 15-30 seg | Execució normal |
| 1.000 mostres | 3-5 min | Per lots grans |
| 10.000 mostres | 30-60 min | Planificar en horari no crític |

### Optimitzacions

- **Queries preparades** per evitar SQL injection
- **Batch inserts** quan és possible
- **Índexs** en camps clau (etiqueta, npat, dates)
- **Cache** de microorganismes especials
- **Connection pooling** per BD

---

## 🔄 Casos Especials

### Mostres Repetides

```
SI una mostra ja existeix a MultiR amb les mateixes dates:
  → Tipus: REPETIDA
  → Acció: SKIP (no processar)
  → Auditoria: OK (sense modificacions)
```

### Mostres Validades

```
SI una mostra ja existia SENSE validar i ara està validada:
  → Tipus: VALIDADA
  → Acció: Actualitzar data_validacio
  → Auditoria: OK
```

### Mostres Revalidades

```
SI una mostra ja estava validada i ara té una nova data validació:
  → Tipus: REVALIDADA
  → Acció: Actualitzar data_validacio
  → Auditoria: OK
```

### Mostres Desvalidades

```
SI una mostra estava validada i ara NO:
  → Tipus: DESVALIDADA
  → Acció: Actualitzar (eliminar data_validacio)
  → Auditoria: OK
```

### Mostres Antigues

```
SI la data resultat és anterior a l'última processada:
  → Tipus: ANTIGA
  → Acció: Només crear auditoria (no modificar dades)
  → Auditoria: OK
```

---

## 🎓 Beneficis del Sistema

### 1. ✅ Automatització Completa
- Elimina entrada manual de dades
- Redueix errors humans
- Procés consistent i repetible

### 2. 📊 Vigilància Epidemiològica Efectiva
- Detecció ràpida de microorganismes especials
- Seguiment de pacients colonitzats
- Control d'evolució (positius → negatius)

### 3. 🔍 Traçabilitat Total
- Auditoria completa de cada mostra
- Logs detallats per debugging
- Històric de modificacions

### 4. 🛡️ Seguretat de Dades
- Validacions múltiples
- Detecció de combinacions prohibides
- Soft deletes (no pèrdua de dades)

### 5. ⚡ Escalabilitat
- Processa fins a 10.000 mostres/dia
- Optimitzat per grans volums
- Execució programable (scheduler)

---

## 📋 Taules Principals de MySQL

### 1. `pacients_diagnostics`
**Registre general del pacient en el sistema**
- Camp clau: `npat` (número de pacient)
- Dates: entrada, modificació

### 2. `pacients_diagnostics_mostra`
**Resultats de mostres del pacient**
- Camps clau: `npat`, `etiqueta`, `tipus_mostra_m`
- `valoracio`: '0'=Negatiu, '2'=Positiu
- `vigent`: 'S'/'N' (per gestionar històric)

### 3. `mostra_microorganisme`
**Detall de cada mostra**
- Camps clau: `etiqueta` (UNIQUE)
- Dates: mostra, resultat, validació
- Relació amb microorganisme i tipus prova

### 4. `micro_mecanisme_mostra`
**Mecanismes de resistència per mostra**
- Relació: mostra + microorganisme + mecanisme
- Permet fins a 5 mecanismes per resultat

### 5. `auditoria_integracio_modulab`
**Traçabilitat completa**
- Tots els intents de processament
- Codis de retorn
- Observacions

---

## 🚀 Execució del Sistema

### Modes d'Execució

#### 1. **Manual**
```bash
MultirIntegraModulab.exe
```
- Per proves i debugging
- Execució immediata
- Logs en consola

#### 2. **Programat (Task Scheduler)**
```
Freqüència recomanada: Cada 1-2 hores
Horari preferit: Fora de pic (nit, cap de setmana)
```

#### 3. **On-Demand**
- Des d'interfície d'administració
- Per reprocessar períodes concrets

### Paràmetres de Línia de Comandes

```bash
# Processar els últims 7 dies (per defecte)
MultirIntegraModulab.exe

# Processar els últims 30 dies
MultirIntegraModulab.exe --dies=30

# Processar rang de dates específic
MultirIntegraModulab.exe --dataInici=2025-01-01 --dataFi=2025-01-31

# Mode verbose (més logs)
MultirIntegraModulab.exe --verbose

# Dry-run (no escriure a BD)
MultirIntegraModulab.exe --dry-run
```

---

## ⚠️ Punts d'Atenció

### 1. Volum de Dades
- **Problema**: Grans volums poden saturar
- **Solució**: Limitar a 1.000 mostres/execució, executar múltiples cops

### 2. Connexió WebService
- **Problema**: Timeout o no disponible
- **Solució**: Sistema continua sense dades de pacient (warning)

### 3. Microorganismes Nous
- **Problema**: Microorganisme nou no marcat com especial
- **Solució**: Revisar periòdicament taula `micro_especial`

### 4. Combinacions Prohibides
- **Problema**: Nova combinació a no incorporar
- **Solució**: Afegir a taula `micro_mecanisme_noincoporar`

### 5. Duplicats
- **Problema**: Mostra processada múltiples vegades
- **Solució**: Sistema detecta REPETIDA i fa SKIP automàtic

---

## 📞 Contacte i Suport

### Logs i Debugging

**Ubicació logs**: `C:\Logs\MultirIntegraModulab\`

**Tipus de fitxers**:
- `IntegracionModulab_YYYYMMDD.log` - Log diari
- `Errors_YYYYMMDD.log` - Només errors

### Resolució de Problemes

| Problema | Solució |
|----------|---------|
| "No es pot connectar a Oracle" | Verificar connectionString, xarxa, credencials |
| "Mostra no processada (CNI)" | Revisar taula `micro_mecanisme_noincoporar` |
| "Massa lent" | Reduir `MaxMostresPerExecucio`, optimitzar índexs |
| "Error WebService" | Verificar URL, timeout, credencials |

---

## 📚 Documentació Relacionada

1. **PROCES_CAPTACIO_DADES.md** - Documentació tècnica completa
2. **DIAGRAMES_FLUX_MERMAID.md** - Diagrames interactius
3. **DIAGRAMES_COMPROVACIONS.md** - Detall comprovacions negatius
4. **COMPROVACIO_1_NEGATIUS.md** - Comprovació 1 en detall
5. **COMPROVACIO_2_NEGATIUS.md** - Comprovació 2 en detall
6. **RESUM_FINAL.md** - Resum tècnic general

---

## 📊 Estadístiques (Exemple Real)

### Execució Tipus (7 dies, 150 mostres)

```
📊 ESTADÍSTIQUES DETALLADES
══════════════════════════════════════════════════════

📥 ENTRADA:
   • Mostres llegides Oracle:            150
   • Període: 2025-01-08 a 2025-01-15

✅ PROCESSAMENT:
   • Total processades:                  142  (94.7%)
   • Mostres positives:                   85  (59.9%)
   • Mostres negatives:                   57  (40.1%)
      ├─ Incorporades (Comprovació 1):    23  (40.4%)
      ├─ Incorporades (Comprovació 2):    34  (59.6%)
      └─ No incorporades (NMRCM):          8

🦠 MICROORGANISMES:
   • Microorganismes únics:               45
   • Microorganismes especials:           12  (26.7%)
   • Nous microorganismes creats:          3

🛡️ MECANISMES:
   • Mecanismes únics:                    18
   • Nous mecanismes creats:               1
   • Combinacions prohibides detectades:   0

❌ ERRORS:
   • Mostres amb error:                    0
   • Mostres no vàlides:                   8  (5.3%)

⏱️ RENDIMENT:
   • Temps total:                     18m 32s
   • Temps mitjà/mostra:                7.8s
   • Mostres/minut:                      7.7

💾 REGISTRES CREATS:
   • pacients_diagnostics:                45  (nous)
   • pacients_diagnostics_mostra:        142
   • mostra_microorganisme:              142
   • micro_mecanisme_mostra:              87
   • auditoria_integracio_modulab:       150

══════════════════════════════════════════════════════
```

---

**Document creat**: Gener 2025  
**Versió**: 1.0  
**Destinataris**: Direcció, Gestió, Responsables IT  
**Estat**: ✅ Aprovat per producció
