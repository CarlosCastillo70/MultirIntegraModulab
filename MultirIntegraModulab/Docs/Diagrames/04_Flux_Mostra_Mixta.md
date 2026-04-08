# ?? Diagrama 4: Flux Mostra Mixta MMR + VR (NOU)

```mermaid
flowchart TD
    Start([?? Mostra Mixta<br/>MMR + VR]) --> SeparateTypes[?? Separar per Tipus]
    
    SeparateTypes --> MMRResults[?? Resultats MMR]
    SeparateTypes --> VRResults[?? Resultats VR]
    
    MMRResults --> ClassifyMMR[?? Classificar MMR]
    VRResults --> ProcessVR[?? Processar VR]
    
    ClassifyMMR --> TypeMMR{Tipus<br/>Mostra MMR?}
    
    TypeMMR -->|Positiva| ProcessPosMMR[? Processar Positiva MMR]
    TypeMMR -->|Negativa| ProcessNegMMR[?? Processar Negativa MMR]
    TypeMMR -->|Mixta| ProcessMixtaMMR[??? Processar Mixta MMR]
    
    ProcessPosMMR --> ResultMMR[? MMR Processat]
    ProcessNegMMR --> ResultMMR
    ProcessMixtaMMR --> ResultMMR
    
    ProcessVR --> CheckProvaVR{Tipus Prova<br/>permet VR?}
    CheckProvaVR -->|NO| RejectVR[? VR Rebutjat]
    CheckProvaVR -->|SÍ| CheckCentreVR{Centre<br/>permet VR?}
    
    CheckCentreVR -->|NO| RejectVR
    CheckCentreVR -->|SÍ| CreateVR[?? Crear Registres VR]
    
    CreateVR --> ResultVR[? VR Processat]
    
    ResultMMR --> Combine[?? Combinar Resultats]
    ResultVR --> Combine
    RejectVR --> Combine
    
    Combine --> UpdateDates[?? Actualitzar Dates Globals]
    UpdateDates --> AuditMixt[?? Auditoria Mixta]
    
    AuditMixt --> Result([? Mostra Mixta Processada])
    
    style Start fill:#ffe4b3
    style Result fill:#c8e6c9
    style ProcessPosMMR fill:#d4edda
    style ProcessNegMMR fill:#cce5ff
    style ProcessVR fill:#e1d5f0
    style CreateVR fill:#d5e8f0
    style RejectVR fill:#ffcdd2
    style Combine fill:#fff9c4
```

## Descripció

Aquest diagrama mostra el flux per processar mostres **Mixtes** que contenen tant **MMR** com **VR**.

### Estratègia de Processament:

1. **Separació inicial** dels resultats per tipus de microorganisme
2. **Processament independent**:
   - Part **MMR**: Segueix el flux normal de classificació MMR
   - Part **VR**: Segueix el flux específic de VR amb les seves validacions
3. **Combinació de resultats** finals
4. **Actualització de dates** globals del pacient
5. **Auditoria mixta** que reflecteix ambdós processaments

### Possibles Escenaris:

- **MMR Positiva + VR Positiu** ? Tots processats
- **MMR Negativa + VR Positiu** ? Negativa pot no incorporar-se segons comprovacions
- **MMR Positiva + VR Rebutjat** ? Només MMR processada
- **MMR Mixta + VR Positiu** ? Processament complex amb múltiples registres

### Avantatges:

- **Processament independent** evita conflictes
- **Flexibilitat** per gestionar errors parcials
- **Auditoria completa** de tot el processament
- **Dates actualitzades** correctament per ambdós tipus
