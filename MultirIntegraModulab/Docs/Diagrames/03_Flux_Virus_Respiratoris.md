# ?? Diagrama 3: Flux Específic Virus Respiratoris (NOU)

```mermaid
flowchart TD
    Start([?? Mostra amb VR]) --> CheckProva{Tipus Prova<br/>existeix i permet VR?}
    
    CheckProva -->|NO| RejectProva[? REBUTJAR<br/>Auditoria TPNIVR]
    CheckProva -->|SÍ| CheckCentre{Centre a llista<br/>VR_CENTRES?}
    
    CheckCentre -->|NO| RejectCentre[? REBUTJAR<br/>Auditoria CNIVR]
    CheckCentre -->|SÍ| CheckPacient{Pacient<br/>existeix?}
    
    CheckPacient -->|SÍ| PacientOK[? Pacient existent]
    CheckPacient -->|NO| WebService[?? Consultar WebService]
    
    WebService --> WSResult{Trobat?}
    WSResult -->|NO| RejectWS[? REBUTJAR<br/>Auditoria NPWS]
    WSResult -->|SÍ| CreatePacient[?? Crear Pacient]
    
    CreatePacient --> LoopVR
    PacientOK --> LoopVR[?? Per cada Virus Respiratori]
    
    LoopVR --> CheckMicroVR{Microorganisme VR<br/>existeix?}
    CheckMicroVR -->|NO| CreateMicroVR[?? Crear Microorganisme VR]
    CheckMicroVR -->|SÍ| ExistsMicroVR[? Ja existeix]
    
    CreateMicroVR --> CreateDiagVR
    ExistsMicroVR --> CreateDiagVR[?? Crear/Obtenir<br/>Diagnòstic VR<br/>mecanisme=NULL]
    
    CreateDiagVR --> CreateMostraVR[?? Crear/Obtenir<br/>Mostra Diagnòstic VR<br/>valoracio='2']
    
    CreateMostraVR --> CreateRelacioVR[?? Crear Relació<br/>mostra_microorganisme VR]
    
    CreateRelacioVR --> UpdateDatesVR[?? Actualitzar dates]
    
    UpdateDatesVR --> MoreVR{Més VR<br/>a processar?}
    MoreVR -->|SÍ| LoopVR
    MoreVR -->|NO| NotaClinica{Cal nota<br/>curs clínic?}
    
    NotaClinica -->|SÍ| CreateNota[?? Crear Nota VR<br/>curs clínic]
    NotaClinica -->|NO| SkipNota[? Saltar nota]
    
    CreateNota --> AuditOK
    SkipNota --> AuditOK[?? Auditoria OK]
    
    AuditOK --> Success([? VR Processats])
    RejectProva & RejectCentre & RejectWS --> Fail([? VR Rebutjats])
    
    style Start fill:#e1f5e1
    style Success fill:#d5e8f0
    style Fail fill:#ffcdd2
    style CreateMicroVR fill:#c8e6c9
    style CreateNota fill:#bbdefb
    style RejectProva fill:#ffcdd2
    style RejectCentre fill:#ffcdd2
    style RejectWS fill:#ffcdd2
```

## Descripció

Aquest diagrama mostra el flux específic per processar **Virus Respiratoris (VR)**.

### Característiques específiques dels VR:

- **SEMPRE són positius** (no hi ha VR negatius)
- **NO tenen mecanismes de resistència** (sempre NULL)
- **SEMPRE s'incorporen** (sense comprovacions de comportament com MMR)
- **Processament simplificat** vs MMR

### Validacions VR:

1. **Tipus de Prova**: Ha d'existir a `tipusprova_m` i tenir `permet_vr = 1`
2. **Centre**: Ha d'estar a la llista de paràmetres `VR_CENTRES`
3. **Pacient**: Ha d'existir o poder-se crear des del WebService

### Registres creats:

- `pacients_diagnostics` (si no existeix)
- `pacients_diagnostics_mostra` amb `valoracio='2'` (positiu)
- `mostra_microorganisme` amb `mecanisme = NULL`
- **Nota automàtica** al curs clínic del pacient

### Codis d'auditoria:

- **OK**: Processament correcte
- **TPNIVR**: Tipus de Prova No vàlida per Incorporar VR
- **CNIVR**: Centre No vàlid per Incorporar VR
- **NPWS**: Pacient No trobat al WebService
