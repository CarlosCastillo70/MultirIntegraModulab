# ?? Diagrama 1: Flux Principal Complet (ACTUALITZAT amb VR)

```mermaid
flowchart TD
    Start([?? INICI: Lectura Mostres Oracle]) --> Validate{? Validació<br/>Mostra Vàlida?}
    
    Validate -->|NO| Reject[? REBUTJAR<br/>Log Warning]
    Validate -->|SÍ| DeterminarTipus[?? Determinar Tipus Incorporació]
    
    DeterminarTipus --> TypeResult{Tipus?}
    TypeResult -->|NOVA| Nova[?? Nova]
    TypeResult -->|REPETIDA| Repetida[?? Repetida - Skip]
    TypeResult -->|VALIDADA| Validada[? Validada]
    TypeResult -->|REVALIDADA| Revalidada[?? Revalidada]
    TypeResult -->|ANTIGA| Antiga[?? Antiga - Auditoria]
    
    Repetida & Antiga --> Result
    Nova & Validada & Revalidada --> CheckMicro[?? Comprovar Microorganismes]
    
    CheckMicro --> CheckMech[??? Comprovar Mecanismes]
    
    CheckMech --> MechResult{Tots resultats<br/>tenen CNI?}
    
    MechResult -->|SÍ| BlockCNI[? ATURAR<br/>Auditoria CNI<br/>Mostra descartada]
    MechResult -->|NO| TipusMicro{Tipus<br/>Microorganisme?}
    
    TipusMicro -->|MMR| ClassifyMMR[?? Classificar Mostra MMR]
    TipusMicro -->|VR| ProcessVR[?? Processar Virus Respiratori]
    TipusMicro -->|MIXT| ProcessMixt[?? Processar Mixta MMR+VR]
    
    ClassifyMMR --> ClassifyResult{Tipus Mostra MMR?}
    
    ClassifyResult -->|1 Positiu| Positive1[?? 1 Positiu]
    ClassifyResult -->|N Positius| PositiveN[???? N Positius]
    ClassifyResult -->|1 Negatiu| Negative1[?? 1 Negatiu]
    ClassifyResult -->|N Negatius| NegativeN[???? N Negatius]
    ClassifyResult -->|Mixta| Mixed[???? Mixta]
    
    Positive1 & PositiveN --> ProcessPos[? Processar Positiva MMR]
    Negative1 & NegativeN --> ProcessNeg[?? Processar Negativa MMR]
    Mixed --> ProcessBoth[??? Processar Mixta MMR]
    
    ProcessPos --> CreatePos[?? Crear Registres Positius MMR]
    ProcessNeg --> CheckNeg1{Comprovació 1<br/>Comportament=1<br/>Té positius?}
    
    CheckNeg1 -->|SÍ| IncorporateC1[? Incorporar<br/>Comprovació 1]
    CheckNeg1 -->|NO| CheckNeg2{Comprovació 2<br/>Té positius vigents<br/>mateix tipus?}
    
    CheckNeg2 -->|SÍ| IncorporateC2[? Incorporar<br/>Comprovació 2]
    CheckNeg2 -->|NO| NoIncorporate[? No Incorporar<br/>Auditoria NMRCM]
    
    CreatePos --> AuditMMR[?? Auditoria OK MMR]
    IncorporateC1 & IncorporateC2 --> CreateNeg[?? Crear Registres Negatius]
    CreateNeg --> AuditMMR
    NoIncorporate --> AuditNMRCM[?? Auditoria NMRCM]
    
    ProcessBoth --> CreatePos & ProcessNeg
    
    ProcessVR --> CheckProva{Tipus Prova<br/>permet VR?}
    CheckProva -->|NO| AuditTPNIVR[?? Auditoria TPNIVR]
    CheckProva -->|SÍ| CheckCentre{Centre<br/>permet VR?}
    CheckCentre -->|NO| AuditCNIVR[?? Auditoria CNIVR]
    CheckCentre -->|SÍ| CreateVR[?? Crear Registres VR<br/>SEMPRE positius]
    
    CreateVR --> NotaVR[?? Nota Curs Clínic VR]
    NotaVR --> AuditVR[?? Auditoria OK VR]
    
    ProcessMixt --> ProcessMMRMixt[? Processar part MMR]
    ProcessMixt --> ProcessVRMixt[?? Processar part VR]
    ProcessMMRMixt & ProcessVRMixt --> AuditMixt[?? Auditoria Mixta]
    
    AuditMMR & AuditNMRCM & AuditVR & AuditMixt & AuditTPNIVR & AuditCNIVR & BlockCNI --> Result([?? RESULTAT FINAL])
    Reject --> Result
    
    style Start fill:#e1f5e1
    style Result fill:#e1f5e1
    style Reject fill:#ffe1e1
    style BlockCNI fill:#ffe1e1
    style NoIncorporate fill:#fff3cd
    style ProcessVR fill:#e1d5f0
    style CreateVR fill:#d5e8f0
    style NotaVR fill:#d5e8f0
    style ProcessMixt fill:#ffe4b3
    style AuditMMR fill:#d1ecf1
    style AuditVR fill:#e8d5f0
    style AuditNMRCM fill:#fff3cd
    style ProcessPos fill:#d4edda
    style ProcessNeg fill:#cce5ff
```

## Descripció

Aquest és el diagrama principal que mostra el flux complet de processament de mostres, incloent:

- **Validació inicial** de mostres
- **Determinació del tipus** d'incorporació (Nova, Repetida, Validada, Revalidada, Antiga)
- **Comprovació de microorganismes** i mecanismes de resistència
- **Bifurcació segons tipus** de microorganisme:
  - **MMR** (Microorganismes amb Mecanismes de Resistència)
  - **VR** (Virus Respiratoris)
  - **MIXT** (Combinació de MMR + VR)
- **Processament específic** per cada tipus amb les seves validacions
- **Auditoria final** amb codis específics (OK, NPWS, CNI, NMRCM, TPNIVR, CNIVR)
