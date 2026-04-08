# ?? Diagrama 2: Classificació de Mostra MMR

```mermaid
flowchart TD
    Start([?? ResultatMostra MMR]) --> HasMicro{Té<br/>Microorganisme?}
    
    HasMicro -->|NO| NegResult[?? 1 NEGATIU<br/>Sense microorganisme]
    HasMicro -->|SÍ| CheckSpecial{És<br/>Especial?}
    
    CheckSpecial -->|SÍ| CountMechSpecial[Comptar<br/>Mecanismes]
    CheckSpecial -->|NO| CountMechNormal[Comptar<br/>Mecanismes]
    
    CountMechSpecial --> MechSpecialCount{Nombre<br/>Mecanismes?}
    MechSpecialCount -->|0| Special0[? 1 POSITIU<br/>Especial sense mec.]
    MechSpecialCount -->|N| SpecialN[?? N POSITIUS<br/>Especial amb N mec.]
    
    CountMechNormal --> MechNormalCount{Nombre<br/>Mecanismes?}
    MechNormalCount -->|0| Normal0[?? 1 NEGATIU<br/>Normal sense mec.]
    MechNormalCount -->|N| NormalN[? N POSITIUS<br/>Normal amb N mec.]
    
    NegResult & Special0 & SpecialN & Normal0 & NormalN --> Aggregate[?? Agregar per Mostra]
    
    Aggregate --> FinalClass{Classificació<br/>Final}
    
    FinalClass -->|1P, 0N| Result1[?? 1 Sol Positiu]
    FinalClass -->|NP, 0N| ResultN[???? N Positius]
    FinalClass -->|0P, 1N| Result1N[?? 1 Sol Negatiu]
    FinalClass -->|0P, NN| ResultNN[???? N Negatius]
    FinalClass -->|NP, MN| ResultMix[???? Mixta]
    
    Result1 & ResultN & Result1N & ResultNN & ResultMix --> End([? Fi])
    
    style Start fill:#e1f5e1
    style End fill:#e1f5e1
    style Special0 fill:#ffe4b3
    style SpecialN fill:#ffcc80
    style NormalN fill:#a5d6a7
    style NegResult fill:#b3e5fc
    style Normal0 fill:#b3e5fc
```

## Descripció

Aquest diagrama mostra com es classifiquen les mostres MMR (Microorganismes amb Mecanismes de Resistència):

### Criteris de Classificació:

1. **Microorganisme Especial amb mecanismes** ? POSITIU
2. **Microorganisme Especial sense mecanismes** ? POSITIU
3. **Microorganisme Normal amb mecanismes** ? POSITIU
4. **Microorganisme Normal sense mecanismes** ? NEGATIU
5. **Sense microorganisme** ? NEGATIU

### Agregació Final:

- **1 Sol Positiu**: 1 positiu, 0 negatius
- **N Positius**: N positius, 0 negatius
- **1 Sol Negatiu**: 0 positius, 1 negatiu
- **N Negatius**: 0 positius, N negatius
- **Mixta**: N positius, M negatius
