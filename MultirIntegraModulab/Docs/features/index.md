---
title: Funcionalitats
description: Detall de les funcionalitats principals del sistema
weight: 30
---

# ⚙️ Funcionalitats

Documentació detallada de les funcionalitats principals de MultirIntegraModulab.

---

## 📋 Contingut d'aquesta Secció

### 🧪 [Classificació de Mostres](classification/index.md)
Detall del sistema de classificació automàtica de mostres.

**Inclou**:
- Lògica de classificació positius/negatius
- Criteris de microorganismes especials
- Comptatge de mecanismes
- Casos extrems i exemples

**Documents**:
- [Classificació General](classification/index.md)
- [Mostres Positives](classification/positive-samples.md)
- [Mostres Negatives](classification/negative-samples.md)

[➡️ Veure Classificació](classification/index.md)

---

### 🦠 [Microorganismes](microorganisms/index.md)
Gestió i verificació de microorganismes.

**Inclou**:
- Microorganismes especials
- Verificació i creació automàtica
- Taula micro_especial
- Criteris epidemiològics

**Documents**:
- [Visió General](microorganisms/index.md)
- [Microorganismes Especials](microorganisms/special-microorganisms.md)
- [Gestió Microorganismes](microorganisms/management.md)

[➡️ Veure Microorganismes](microorganisms/index.md)

---

### 🛡️ [Mecanismes de Resistència](resistance/index.md)
Gestió de mecanismes de resistència antimicrobiana.

**Inclou**:
- Tipus de mecanismes (BLEE, KPC, VIM, etc.)
- Verificació i creació
- Combinacions prohibides
- Impacte epidemiològic

**Documents**:
- [Visió General](resistance/index.md)
- [Tipus de Mecanismes](resistance/mechanisms-types.md)
- [Combinacions Prohibides](resistance/forbidden-combinations.md)

[➡️ Veure Mecanismes](resistance/index.md)

---

### ✅ [Sistema de Comprovacions per Negatius](validation/index.md) ⭐⭐⭐
Sistema de validació per a mostres negatives.

**Inclou**:
- **Comprovació 1**: Positius generals del pacient
- **Comprovació 2**: Positius vigents del tipus de mostra
- Criteris de vigència
- Tipus equivalents
- Lògica de decisió

**Documents**:
- [Diagrames Comprovacions](validation/DIAGRAMES_COMPROVACIONS.md)
- [Comprovació 1](validation/COMPROVACIO_1_NEGATIUS.md)
- [Comprovació 2](validation/COMPROVACIO_2_NEGATIUS.md)
- [Resum Comprovacions](validation/COMPROVACIONS_NEGATIUS_RESUM.md)
- [Implementació](validation/IMPLEMENTACIO_COMPROVACIONS_RESUM.md)

[➡️ Veure Comprovacions](validation/index.md)

---

### 📊 [Tipus d'Incorporació](incorporation/index.md)
Sistema de detecció del tipus d'incorporació de mostres.

**Inclou**:
- Nova, Repetida, Validada, Revalidada, Desvalidada, Antiga
- Comparació Oracle vs MySQL
- Gestió de dates
- Criteris de decisió

**Documents**:
- [Visió General](incorporation/index.md)
- [Tipus d'Incorporació](incorporation/types.md)
- [Comparació Mostres](incorporation/comparison.md)

[➡️ Veure Tipus Incorporació](incorporation/index.md)

---

### 📝 [Auditoria i Traçabilitat](audit/index.md)
Sistema complet d'auditoria i traçabilitat.

**Inclou**:
- Codis d'auditoria (OK, CNI, NMRCM, ERROR)
- Taula auditoria_integracio_modulab
- Logging detallat
- Traçabilitat completa

**Documents**:
- [Visió General](audit/index.md)
- [Codis Auditoria](audit/codes.md)
- [Sistema de Logs](audit/logging.md)

[➡️ Veure Auditoria](audit/index.md)

---

## 🎯 Mapa de Funcionalitats

```
⚙️ FUNCIONALITATS
    │
    ├── 🧪 Classificació
    │   ├── Positives
    │   └── Negatives
    │
    ├── 🦠 Microorganismes
    │   ├── Especials
    │   └── Gestió
    │
    ├── 🛡️ Mecanismes
    │   ├── Tipus
    │   └── Prohibides
    │
    ├── ✅ Comprovacions ⭐
    │   ├── Comprovació 1
    │   └── Comprovació 2
    │
    ├── 📊 Incorporació
    │   ├── Tipus
    │   └── Comparació
    │
    └── 📝 Auditoria
        ├── Codis
        └── Logging
```

---

## 📊 Matriu de Funcionalitats per Perfil

| Funcionalitat | Dev | Analista | Usuari | Prioritat |
|---------------|:---:|:--------:|:------:|:---------:|
| Classificació | ⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐ | Alta |
| Microorganismes | ⭐⭐⭐ | ⭐⭐ | ⭐ | Alta |
| Mecanismes | ⭐⭐⭐ | ⭐⭐ | ⭐ | Alta |
| **Comprovacions** | ⭐⭐⭐ | ⭐⭐⭐ | ⭐ | **Crítica** |
| Incorporació | ⭐⭐⭐ | ⭐⭐ | - | Mitjana |
| Auditoria | ⭐⭐ | ⭐⭐ | ⭐⭐ | Alta |

---

## 🔍 Cerca per Cas d'Ús

### Vull entendre...

**Com es classifica una mostra**
→ [Classificació de Mostres](classification/index.md)

**Per què una mostra és positiva**
→ [Mostres Positives](classification/positive-samples.md)

**Per què un negatiu NO s'incorpora**
→ [Comprovacions Negatius](validation/index.md)

**Què és un microorganisme especial**
→ [Microorganismes Especials](microorganisms/special-microorganisms.md)

**Què és una combinació prohibida**
→ [Combinacions Prohibides](resistance/forbidden-combinations.md)

**Com funciona la vigència de positius**
→ [Comprovació 2](validation/COMPROVACIO_2_NEGATIUS.md#vigència)

**Què és una mostra repetida**
→ [Tipus Incorporació](incorporation/types.md#repetida)

**Què signifiquen els codis d'auditoria**
→ [Codis Auditoria](audit/codes.md)

---

## 🎓 Tutorials Relacionats

- [Processar Mostres](../tutorials/processing-samples.md)
- [Gestionar Negatius](../tutorials/handling-negatives.md)
- [Configurar Microorganismes Especials](../tutorials/configure-special-microorganisms.md)
- [Afegir Combinacions Prohibides](../tutorials/add-forbidden-combinations.md)

---

## 📚 Documents Tècnics Relacionats

- [Procés Captació Dades](../technical/PROCES_CAPTACIO_DADES.md)
- [Diagrames Flux](../technical/DIAGRAMES_FLUX_MERMAID.md)
- [Model de Dades](../technical/data-model.md)
- [API Reference](../technical/api-reference.md)

---

## 🆘 Suport

Per a preguntes sobre funcionalitats específiques:

1. Consulta la documentació de la funcionalitat
2. Revisa els [exemples](../examples/use-cases.md)
3. Consulta [Troubleshooting](../guides/troubleshooting.md)
4. Contacta suport tècnic

---

**Següent secció**: [Guies Pràctiques →](../guides/index.md)
