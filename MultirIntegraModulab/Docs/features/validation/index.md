---
title: Sistema de Comprovacions per Negatius
description: Documentació completa del sistema de validació de mostres negatives
weight: 1
---

# ✅ Sistema de Comprovacions per Negatius

Sistema de validació per determinar si una mostra negativa s'ha d'incorporar o no a MultiR.

---

## 🎯 Visió General

Les **mostres negatives** (sense microorganisme o microorganisme NO especial sense mecanismes) passen per un sistema de **2 comprovacions seqüencials** per decidir si cal incorporar-les:

```
🔵 MOSTRA NEGATIVA
    │
    ▼
┌───────────────────────────┐
│  COMPROVACIÓ 1            │
│  ¿Comportament=1?         │
│  ¿Té positius generals?   │
└───┬───────────────────┬───┘
   SÍ                  NO
    │                   │
    ▼                   ▼
✅ INCORPORAR    ┌───────────────────────────┐
                 │  COMPROVACIÓ 2            │
                 │  ¿Té positius vigents     │
                 │   tipus equivalent?       │
                 └───┬───────────────────┬───┘
                    SÍ                  NO
                     │                   │
                     ▼                   ▼
                 ✅ INCORPORAR      ❌ NO INCORPORAR
                                        (NMRCM)
```

---

## 📋 Documents d'aquesta Secció

### [📊 Diagrames de Comprovacions](DIAGRAMES_COMPROVACIONS.md) ⭐
**Temps de lectura**: 20-30 minuts  
**Nivell**: Intermedi

Diagrama visual complet del sistema amb casos d'ús.

**Contingut**:
- Flux principal de decisió
- Diagrames visuals ASCII
- Taula de decisions amb colors
- Casos d'ús amb emojis
- Query flow
- Model de dades simplificat

[➡️ Veure Diagrames](DIAGRAMES_COMPROVACIONS.md)

---

### [1️⃣ Comprovació 1: Positius Generals](COMPROVACIO_1_NEGATIUS.md)
**Temps de lectura**: 15-20 minuts  
**Nivell**: Intermedi-Avançat

Detall de la primera comprovació.

**Contingut**:
- Què és el comportament del tipus de mostra
- Lògica de decisió
- Query SQL
- Exemples pràctics
- Casos extrems
- Implementació C#

**Criteri**: 
```sql
SI tipus_mostra.comportament = 1
   AND EXISTS (
       SELECT 1 FROM pacients_diagnostics_mostra
       WHERE npat = :npat
         AND valoracio = '2'
         AND dt_delete IS NULL
   )
   → ✅ INCORPORAR
```

[➡️ Llegir Comprovació 1](COMPROVACIO_1_NEGATIUS.md)

---

### [2️⃣ Comprovació 2: Positius Vigents Tipus Mostra](COMPROVACIO_2_NEGATIUS.md)
**Temps de lectura**: 20-25 minuts  
**Nivell**: Avançat

Detall de la segona comprovació (més complexa).

**Contingut**:
- Positius vigents del tipus de mostra
- Tipus equivalents
- Càlcul de vigència (90-365 dies)
- Query SQL complexa
- Exemples detallats
- Implementació C#

**Criteri**:
```sql
SI EXISTS (
    SELECT 1 FROM pacients_diagnostics_mostra pdm
    JOIN tipusmostra_m tm ON pdm.tipus_mostra_m = tm.descripcio
    WHERE pdm.npat = :npat
      AND tm.id IN (:tipus_id, :equivalents)
      AND pdm.valoracio = '2'
      AND pdm.data_mostra >= NOW() - tm.dies_vigencia_positiu
      AND pdm.dt_delete IS NULL
   )
   → ✅ INCORPORAR
SINÓ
   → ❌ NO INCORPORAR (NMRCM)
```

[➡️ Llegir Comprovació 2](COMPROVACIO_2_NEGATIUS.md)

---

### [📝 Resum de Comprovacions](COMPROVACIONS_NEGATIUS_RESUM.md)
**Temps de lectura**: 10-15 minuts  
**Nivell**: Intermedi

Resum consolidat de les dues comprovacions.

**Contingut**:
- Comparativa entre Comprovació 1 i 2
- Taula de decisions
- Exemples consolidats
- Estadístiques
- Matriu de decisions

[➡️ Llegir Resum](COMPROVACIONS_NEGATIUS_RESUM.md)

---

### [💻 Implementació Tècnica](IMPLEMENTACIO_COMPROVACIONS_RESUM.md)
**Temps de lectura**: 25-30 minuts  
**Nivell**: Avançat

Detall d'implementació en C#.

**Contingut**:
- Codi C# complet
- Use Cases
- Estructures de dades
- Mètodes i funcions
- Proves unitàries
- Optimitzacions

[➡️ Llegir Implementació](IMPLEMENTACIO_COMPROVACIONS_RESUM.md)

---

## 🎓 Ruta d'Aprenentatge Recomanada

### 📘 Per a Analistes / Usuaris Finals (45 min)

```
1. [Diagrames Comprovacions](DIAGRAMES_COMPROVACIONS.md) (20 min)
   └─ Entendre el flux visual
   
2. [Resum Comprovacions](COMPROVACIONS_NEGATIUS_RESUM.md) (10 min)
   └─ Taula de decisions
   
3. Exemples pràctics (15 min)
   └─ Revisar casos d'ús
```

---

### 📗 Per a Desenvolupadors (90 min)

```
1. [Diagrames](DIAGRAMES_COMPROVACIONS.md) (15 min)
   └─ Vista general
   
2. [Comprovació 1](COMPROVACIO_1_NEGATIUS.md) (20 min)
   └─ Lògica i queries
   
3. [Comprovació 2](COMPROVACIO_2_NEGATIUS.md) (25 min)
   └─ Lògica complexa i vigència
   
4. [Implementació](IMPLEMENTACIO_COMPROVACIONS_RESUM.md) (30 min)
   └─ Codi C# i optimitzacions
```

---

## 📊 Taula Resum de Decisions

| Comportament | Positius Generals | Positius Vigents Tipus | Decisió | Via |
|:------------:|:-----------------:|:----------------------:|:-------:|:---:|
| 1 | ✅ | - | ✅ Incorporar | Comp. 1 |
| 1 | ❌ | ✅ | ✅ Incorporar | Comp. 2 |
| 1 | ❌ | ❌ | ❌ No incorporar | NMRCM |
| 0 | - | ✅ | ✅ Incorporar | Comp. 2 |
| 0 | - | ❌ | ❌ No incorporar | NMRCM |
| null | - | ✅ | ✅ Incorporar | Comp. 2 |
| null | - | ❌ | ❌ No incorporar | NMRCM |

---

## 💡 Exemples Ràpids

### Exemple 1: Incorporar via Comprovació 1
```
📥 Mostra: Frotis rectal NEGATIU
🔍 Tipus mostra: comportament = 1
👤 Pacient: Té 2 positius anteriors (MRSA)
✅ RESULTAT: INCORPORAR per Comprovació 1
```

### Exemple 2: Incorporar via Comprovació 2
```
📥 Mostra: Sang NEGATIU
🔍 Tipus mostra: comportament = 0
👤 Pacient: Té 1 positiu vigent de "Sang venosa" (equivalent)
✅ RESULTAT: INCORPORAR per Comprovació 2
```

### Exemple 3: No incorporar (NMRCM)
```
📥 Mostra: Orina NEGATIU
🔍 Tipus mostra: comportament = 0
👤 Pacient: NO té positius vigents de "Orina"
❌ RESULTAT: NO INCORPORAR
📝 Auditoria: NMRCM
```

---

## 🔍 Conceptes Clau

### Comportament del Tipus de Mostra
- **Valor**: 0 o 1 (a la taula `tipusmostra_m`)
- **Significat**: 
  - `1` = Tipus amb alta vigilància (ex: Frotis rectal)
  - `0` = Tipus estàndard

### Positius Generals
- Qualsevol resultat positiu del pacient
- Sense restricció de tipus de mostra
- Sense restricció temporal

### Positius Vigents
- Resultats positius dins del període de vigència
- Específics del tipus de mostra (o equivalents)
- Vigència: 90-365 dies segons tipus

### Tipus Equivalents
- Tipus de mostra similars (ex: Sang venosa ≈ Sang arterial)
- Configurat a `tipusmostra_equivalents`

### NMRCM
- **No Mostra Resultats Cultiu Micro**
- Codi d'auditoria per negatius no incorporats
- Indica que no cal seguiment del pacient per aquest tipus

---

## 📈 Estadístiques Típiques

En una execució de 150 mostres:

```
🔵 Mostres Negatives: 57 (38%)
   ├─ ✅ Incorporades: 49 (86%)
   │  ├─ Via Comprovació 1: 23 (47%)
   │  └─ Via Comprovació 2: 26 (53%)
   └─ ❌ No incorporades (NMRCM): 8 (14%)
```

---

## 🔗 Documents Relacionats

### Documentació Tècnica
- [Procés Captació - Fase 6B](../../technical/PROCES_CAPTACIO_DADES.md#fase-6b)
- [Diagrama 6](../../technical/DIAGRAMES_FLUX_MERMAID.md#diagrama-6)

### Altres Funcionalitats
- [Classificació Mostres](../classification/negative-samples.md)
- [Tipus Incorporació](../incorporation/index.md)
- [Auditoria](../audit/codes.md)

### Referència
- [Glossari](../../reference/glossary.md)
- [Base de Dades](../../reference/database-schema.md)

---

## 🆘 Preguntes Freqüents

<details>
<summary><strong>Per què alguns negatius NO s'incorporen?</strong></summary>

Perquè el pacient NO té positius vigents que justifiquin el seguiment amb aquest tipus de mostra. Criteri: No cal vigilància si no hi ha colonització activa coneguda.

</details>

<details>
<summary><strong>Què és el "comportament" del tipus de mostra?</strong></summary>

És un atribut (0 o 1) que indica si aquest tipus de mostra requereix alta vigilància. Per exemple, els frotis rectals de vigilància tenen comportament=1.

</details>

<details>
<summary><strong>Com es calcula la vigència d'un positiu?</strong></summary>

Cada tipus de mostra té un atribut `dies_vigencia_positiu` (90-365 dies). Un positiu és vigent si `data_mostra >= AVUI - dies_vigencia`.

</details>

<details>
<summary><strong>Què són els tipus equivalents?</strong></summary>

Tipus de mostra similars. Per exemple, "Sang venosa" i "Sang arterial" són equivalents. Es configura a la taula `tipusmostra_equivalents`.

</details>

---

**Següent**: [Classificació de Mostres →](../classification/index.md)
