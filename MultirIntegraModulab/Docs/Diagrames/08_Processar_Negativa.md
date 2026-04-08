# ?? Diagrama 8: Processar Mostra Negativa (Comprovacions)

```mermaid
flowchart TD
    Start([?? Mostra Negativa]) --> GetComportament[?? Obtenir Comportament<br/>del Tipus Mostra]
    
    GetComportament --> CheckComportament{Comportament<br/>= 1?}
    
    CheckComportament -->|SÍ| Comp1[?? COMPROVACIÓ 1<br/>Pacient té positius generals?]
    CheckComportament -->|NO| Comp2Direct[? Saltar a<br/>Comprovació 2]
    
    Comp1 --> HasGeneral{Té positius<br/>generals?}
    
    HasGeneral -->|SÍ| IncorpC1[? INCORPORAR<br/>Via Comprovació 1]
    HasGeneral -->|NO| Comp2[?? COMPROVACIÓ 2<br/>Pacient té positius vigents<br/>d'aquest tipus?]
    
    Comp2Direct --> Comp2
    
    Comp2 --> GetTipusId[Obtenir ID tipus mostra]
    GetTipusId --> GetEquiv[Obtenir tipus equivalents]
    GetEquiv --> QueryVigents[Query: Positius vigents<br/>tipus + equivalents]
    
    QueryVigents --> HasVigent{Té positius<br/>vigents?}
    
    HasVigent -->|SÍ| IncorpC2[? INCORPORAR<br/>Via Comprovació 2]
    HasVigent -->|NO| NoIncorp[? NO INCORPORAR<br/>Auditoria NMRCM]
    
    IncorpC1 --> CreateRecords[?? Crear Registres<br/>pacients_diagnostics<br/>pacients_diagnostics_mostra<br/>mostra_microorganisme]
    IncorpC2 --> CreateRecords
    
    CreateRecords --> AuditOK[?? Auditoria OK]
    NoIncorp --> AuditNMRCM[?? Auditoria NMRCM<br/>Increment NoIncorporats]
    
    AuditOK --> ResultOK([? Incorporat])
    AuditNMRCM --> ResultNo([?? No Incorporat])
    
    style Start fill:#e1f5e1
    style ResultOK fill:#c8e6c9
    style ResultNo fill:#fff3cd
    style IncorpC1 fill:#c8e6c9
    style IncorpC2 fill:#c8e6c9
    style NoIncorp fill:#ffcdd2
    style Comp1 fill:#e3f2fd
    style Comp2 fill:#e3f2fd
```

## Descripció

Aquest diagrama mostra el procés de **processament de mostres negatives MMR** amb les seves **comprovacions** per determinar si s'incorporen.

### Comprovacions:

Les mostres negatives **NO sempre s'incorporen**. Cal comprovar:

#### **COMPROVACIÓ 1** (només si `comportament = 1`):
- **Pregunta**: El pacient té positius generals (especials amb mecanismes)?
- **Si SÍ** ? Incorporar la mostra negativa
- **Si NO** ? Passar a Comprovació 2

#### **COMPROVACIÓ 2** (sempre):
- **Pregunta**: El pacient té positius vigents d'aquest tipus de mostra (o equivalents)?
- **Passos**:
  1. Obtenir ID del tipus de mostra
  2. Obtenir tipus equivalents (taula `tipusmostra_equivalents`)
  3. Query: Buscar positius vigents del tipus o equivalents
- **Si SÍ** ? Incorporar la mostra negativa
- **Si NO** ? **NO incorporar** (Auditoria **NMRCM**)

### Comportament Tipus Mostra:

- **`comportament = 1`**: Fa Comprovació 1 + Comprovació 2
- **`comportament ? 1`**: Només fa Comprovació 2

### Registres Creats (si s'incorpora):

- `pacients_diagnostics` (si no existeix)
- `pacients_diagnostics_mostra` amb `valoracio='0'` (negatiu)
- `mostra_microorganisme` sense mecanismes
- `auditoria_integracio_modulab` amb codi **OK**

### Codi Auditoria:

- **OK**: Negatiu incorporat (passa alguna comprovació)
- **NMRCM**: Negatiu sense Microorganisme amb Resistència al Carbapenem vigent (no incorporat)
