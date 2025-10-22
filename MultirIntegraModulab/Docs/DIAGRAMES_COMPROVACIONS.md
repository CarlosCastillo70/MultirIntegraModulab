# 📊 Diagrama Visual - Sistema de Comprovacions per Mostres Negatives

## 🎯 Flux Principal de Decisió

```
┌─────────────────────────────────────────────────────────────────┐
│                    MOSTRA NEGATIVA A PROCESSAR                  │
│                                                                  │
│  • Etiqueta: ETQ123456                                          │
│  • Pacient: 12345678                                            │
│  • Tipus mostra: Frotis rectal / Sang / Orina / ...           │
│  • Resultat: NEGATIU                                            │
└────────────────────────────┬────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│               FASE 1: COMPROVACIONS                             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
         ╔════════════════════════════════════════╗
         ║   1. Obtenir comportament tipus mostra ║
         ║      SELECT comportament                ║
         ║      FROM tipusmostra_m                 ║
         ╚════════════════╤═══════════════════════╝
                          │
                          ▼
              ┌───────────────────────┐
              │  Comportament == 1?   │
              └─────┬─────────────┬───┘
                    │             │
                   SÍ            NO
                    │             │
                    │             │
    ╔═══════════════▼═══════════════════════╗
    ║       COMPROVACIÓ 1                   ║
    ║  (Positius generals del pacient)      ║
    ║                                        ║
    ║  SELECT COUNT(*)                      ║
    ║  FROM pacients_diagnostics_mostra     ║
    ║  WHERE valoracio = '2'                ║
    ╚══════════════╤════════════════════════╝
                   │
                   ▼
       ┌───────────────────────┐
       │ Pacient té positius?  │
       └─────┬─────────────┬───┘
             │             │
            SÍ            NO
             │             │
             │             │
    ╔════════▼══════╗     │
    ║ ✅ INCORPORAR ║     │
    ║               ║     │
    ║ Via:          ║     │
    ║ Comprovació 1 ║     │
    ╚═══════════════╝     │
             │             │
             │             └──────────────────┐
             │                                │
             │                                ▼
             │        ╔═══════════════════════════════════════════╗
             │        ║       COMPROVACIÓ 2                       ║
             │        ║  (Positius vigents tipus mostra equiv.)  ║
             │        ║                                            ║
             │        ║  SELECT COUNT(*)                          ║
             │        ║  FROM pacients_diagnostics_mostra pdm    ║
             │        ║  JOIN tipusmostra_m tm                    ║
             │        ║  WHERE (tm.descripcio = tipus_mostra      ║
             │        ║         OR tm.id IN equivalents)          ║
             │        ║    AND valoracio = '2'                    ║
             │        ║    AND vigència OK                        ║
             │        ╚══════════════╤════════════════════════════╝
             │                       │
             │                       ▼
             │           ┌───────────────────────────┐
             │           │ Pacient té positius       │
             │           │ vigents per aquest tipus? │
             │           └─────┬─────────────────┬───┘
             │                 │                 │
             │                SÍ                NO
             │                 │                 │
             │                 │                 │
             │        ╔════════▼═══════╗  ╔═════▼════════════╗
             │        ║ ✅ INCORPORAR  ║  ║ ❌ NO INCORPORAR ║
             │        ║                ║  ║                  ║
             │        ║ Via:           ║  ║ Codi:            ║
             │        ║ Comprovació 2  ║  ║ NMRCM            ║
             │        ╚════════╤═══════╝  ╚═════╤════════════╝
             │                 │                 │
             └─────────────────┴─────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│               FASE 2: PROCESSAMENT                              │
│                                                                  │
│  Si cal incorporar:                                             │
│    1. Crear/actualitzar pacients_diagnostics                   │
│    2. Crear/actualitzar pacients_diagnostics_mostra            │
│    3. Crear mostra_microorganisme                               │
│    4. Actualitzar dates                                         │
│    5. Crear tipus mostra/prova si no existeixen                │
│    6. Crear auditoria OK                                        │
│                                                                  │
│  Si NO cal incorporar:                                          │
│    1. Crear auditoria NMRCM                                     │
│    2. Incrementar ResultatsNoIncorporats                       │
└─────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                         RESULTAT                                │
│                                                                  │
│  ResultatProcessamentNegatiu {                                 │
│    Exitosa = true                                               │
│    ResultatsProcessats = 5                                      │
│    ResultatsNoIncorporats = 2                                   │
│    IncorporatsPerComprovacio1 = 2  ← Tracking                  │
│    IncorporatsPerComprovacio2 = 1  ← Tracking                  │
│    AuditoriasCreades = 5                                        │
│    ...                                                          │
│  }                                                              │
└─────────────────────────────────────────────────────────────────┘
```

## 🎭 Casos d'Ús amb Emojis

### Cas 1: 🟢 Incorporar via Comprovació 1
```
📥 Mostra: Frotis rectal NEGATIU
🔍 Comportament = 1 → ✅ Aplica Comprovació 1
👤 Pacient: Té 3 positius
✅ RESULTAT: INCORPORAR (Comprovació 1)
📊 Comptador: IncorporatsPerComprovacio1++
```

### Cas 2: 🟢 Incorporar via Comprovació 2
```
📥 Mostra: Sang NEGATIU
🔍 Comportament = 0 → ❌ No aplica Comprovació 1
🔍 Aplicar Comprovació 2
👤 Pacient: Té 2 positius vigents per 'Sang'
✅ RESULTAT: INCORPORAR (Comprovació 2)
📊 Comptador: IncorporatsPerComprovacio2++
```

### Cas 3: 🔴 No incorporar
```
📥 Mostra: Orina NEGATIU
🔍 Comportament = 0 → ❌ No aplica Comprovació 1
🔍 Aplicar Comprovació 2
👤 Pacient: NO té positius vigents per 'Orina'
❌ RESULTAT: NO INCORPORAR
🏷️  Auditoria: NMRCM
📊 Comptador: ResultatsNoIncorporats++
```

## 📊 Taula de Decisions amb Colors

```
╔═══════════════╦══════════════╦══════════════════════╦═══════════════╦═════════════╗
║ Comportament  ║   Positius   ║   Positius vigents   ║   DECISIÓ     ║     VIA     ║
║               ║   generals   ║   tipus/equivalents  ║               ║             ║
╠═══════════════╬══════════════╬══════════════════════╬═══════════════╬═════════════╣
║      1        ║     ✅ Sí    ║         -            ║ 🟢 Incorporar ║ Compr. 1    ║
╠═══════════════╬══════════════╬══════════════════════╬═══════════════╬═════════════╣
║      1        ║     ❌ No    ║       ✅ Sí         ║ 🟢 Incorporar ║ Compr. 2    ║
╠═══════════════╬══════════════╬══════════════════════╬═══════════════╬═════════════╣
║      1        ║     ❌ No    ║       ❌ No         ║ 🔴 No incorp. ║     -       ║
╠═══════════════╬══════════════╬══════════════════════╬═══════════════╬═════════════╣
║      0        ║      -       ║       ✅ Sí         ║ 🟢 Incorporar ║ Compr. 2    ║
╠═══════════════╬══════════════╬══════════════════════╬═══════════════╬═════════════╣
║      0        ║      -       ║       ❌ No         ║ 🔴 No incorp. ║     -       ║
╠═══════════════╬══════════════╬══════════════════════╬═══════════════╬═════════════╣
║     null      ║      -       ║       ✅ Sí         ║ 🟢 Incorporar ║ Compr. 2    ║
╠═══════════════╬══════════════╬══════════════════════╬═══════════════╬═════════════╣
║     null      ║      -       ║       ❌ No         ║ 🔴 No incorp. ║     -       ║
╚═══════════════╩══════════════╩══════════════════════╩═══════════════╩═════════════╝
```

## 🗄️ Model de Dades Simplificat

```
┌─────────────────────────┐
│   tipusmostra_m         │
├─────────────────────────┤
│ id (PK)                 │◄───────┐
│ codi                    │        │
│ descripcio              │        │
│ comportament            │        │ FK
│ dies_vigencia_positiu   │        │
│ actiu                   │        │
│ dt_delete               │        │
└─────────────────────────┘        │
           △                        │
           │                        │
           │ FK                     │
           │                        │
┌──────────┴──────────────┐        │
│ tipusmostra_equivalents │        │
├─────────────────────────┤        │
│ id (PK)                 │        │
│ tipusmostra_id          │────────┘
│ tipusmostra_id_equiv    │────────┐
└─────────────────────────┘        │
                                    │
                                    │
┌────────────────────────────┐     │
│ pacients_diagnostics_mostra│     │
├────────────────────────────┤     │
│ id (PK)                    │     │
│ npat                       │     │ FK
│ tipus_mostra_m             │─────┘
│ data_mostra                │ 
│ valoracio                  │  ('2' = Positiu)
│ dt_delete                  │
└────────────────────────────┘
```

## 🔍 Query Flow - Comprovació 2

```
PACIENT: 12345678
TIPUS MOSTRA: "Sang"

STEP 1: Buscar ID del tipus mostra
┌─────────────────────────────────┐
│ SELECT id FROM tipusmostra_m    │
│ WHERE descripcio = 'Sang'       │
│                                  │
│ RESULTAT: id = 5                │
└─────────────────────────────────┘
                │
                ▼
STEP 2: Buscar tipus equivalents
┌──────────────────────────────────────┐
│ SELECT tipusmostra_id_equivalent     │
│ FROM tipusmostra_equivalents         │
│ WHERE tipusmostra_id = 5             │
│                                       │
│ RESULTAT: [7, 12] (Sang venosa, etc)│
└──────────────────────────────────────┘
                │
                ▼
STEP 3: Comptar positius vigents
┌───────────────────────────────────────────────────┐
│ SELECT COUNT(*)                                   │
│ FROM pacients_diagnostics_mostra pdm             │
│ JOIN tipusmostra_m tm                             │
│   ON pdm.tipus_mostra_m = tm.descripcio          │
│ WHERE pdm.npat = '12345678'                      │
│   AND tm.id IN (5, 7, 12)  ← Tipus + equivalents│
│   AND pdm.valoracio = '2'  ← Positiu             │
│   AND vigència OK          ← Segons dies_vigencia│
│                                                   │
│ RESULTAT: COUNT = 2                              │
└───────────────────────────────────────────────────┘
                │
                ▼
        ┌───────────────┐
        │ COUNT > 0?    │
        └───┬───────┬───┘
           SÍ     NO
            │      │
            ▼      ▼
        ✅ INCORP  ❌ NO
```

## 📈 Gràfic de Fluxos de Dades

```
[ORACLE: Modulab]                    [MYSQL: MultiR]
       │                                    │
       │ Resultats negatius                │
       ▼                                    │
┌──────────────┐                           │
│ ResultatMostra│                          │
└───────┬──────┘                           │
        │                                   │
        │ ──────────────────────────────►  │
        │   ProcessarMostraNegativaUseCase │
        │                                   │
        │                            ┌──────▼────────┐
        │                            │ tipusmostra_m │
        │                            │ comportament? │
        │                            └──────┬────────┘
        │                                   │
        │                            ┌──────▼──────────────┐
        │                            │pacients_diagnostics │
        │                            │     _mostra         │
        │                            │  positius vigents?  │
        │                            └──────┬──────────────┘
        │                                   │
        │                            ┌──────▼──────────────┐
        │                            │  Decisió: Incorporar│
        │                            │  o No incorporar?   │
        │                            └──────┬──────────────┘
        │                                   │
        │                            ┌──────▼──────────────┐
        │                            │ auditoria_integracio│
        │                            │   OK / NMRCM        │
        │                            └─────────────────────┘
```

## 🎯 Llegenda d'Icones

| Icona | Significat |
|-------|-----------|
| 🟢 | Incorporar el negatiu |
| 🔴 | No incorporar el negatiu |
| ✅ | Condició complerta |
| ❌ | Condició NO complerta |
| 🔍 | Comprovació en curs |
| 👤 | Dades del pacient |
| 📥 | Input |
| 📤 | Output |
| 📊 | Mètriques/comptadors |
| 🏷️ | Auditoria |
| ⚡ | Procés ràpid |
| 🐌 | Procés lent |

---

**Data**: Gener 2025  
**Versió del diagrama**: 1.0  
**Estat**: ✅ Actualitzat
