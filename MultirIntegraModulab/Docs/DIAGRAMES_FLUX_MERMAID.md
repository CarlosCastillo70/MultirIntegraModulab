# 📊 DIAGRAMES DE FLUX - FORMAT MERMAID

Aquest document conté diagrames de flux en format Mermaid que poden ser visualitzats a GitHub, en editors compatibles o utilitzant https://mermaid.live/

---

## 🔄 Diagrama 1: Flux Principal Complet (ACTUALITZAT amb VR)

```mermaid
flowchart TD
    Start([🏁 INICI: Lectura Mostres Oracle]) --> Validate{✅ Validació<br/>Mostra Vàlida?}
    
    Validate -->|NO| Reject[❌ REBUTJAR<br/>Log Warning]
    Validate -->|SÍ| DeterminarTipus[🎯 Determinar Tipus Incorporació]
    
    DeterminarTipus --> TypeResult{Tipus?}
    TypeResult -->|NOVA| Nova[🆕 Nova]
    TypeResult -->|REPETIDA| Repetida[🔁 Repetida - Skip]
    TypeResult -->|VALIDADA| Validada[✅ Validada]
    TypeResult -->|REVALIDADA| Revalidada[🔄 Revalidada]
    TypeResult -->|ANTIGA| Antiga[🕐 Antiga - Auditoria]
    
    Repetida & Antiga --> Result
    Nova & Validada & Revalidada --> CheckMicro[🦠 Comprovar Microorganismes]
    
    CheckMicro --> CheckMech[🛡️ Comprovar Mecanismes]
    
    CheckMech --> MechResult{Tots resultats<br/>tenen CNI?}
    
    MechResult -->|SÍ| BlockCNI[❌ ATURAR<br/>Auditoria CNI<br/>Mostra descartada]
    MechResult -->|NO| TipusMicro{Tipus<br/>Microorganisme?}
    
    TipusMicro -->|MMR| ClassifyMMR[🧪 Classificar Mostra MMR]
    TipusMicro -->|VR| ProcessVR[🦠 Processar Virus Respiratori]
    TipusMicro -->|MIXT| ProcessMixt[🔀 Processar Mixta MMR+VR]
    
    ClassifyMMR --> ClassifyResult{Tipus Mostra MMR?}
    
    ClassifyResult -->|1 Positiu| Positive1[🟢 1 Positiu]
    ClassifyResult -->|N Positius| PositiveN[🟢🟢 N Positius]
    ClassifyResult -->|1 Negatiu| Negative1[🔵 1 Negatiu]
    ClassifyResult -->|N Negatius| NegativeN[🔵🔵 N Negatius]
    ClassifyResult -->|Mixta| Mixed[🟢🔵 Mixta]
    
    Positive1 & PositiveN --> ProcessPos[⚡ Processar Positiva MMR]
    Negative1 & NegativeN --> ProcessNeg[🔍 Processar Negativa MMR]
    Mixed --> ProcessBoth[⚡🔍 Processar Mixta MMR]
    
    ProcessPos --> CreatePos[💾 Crear Registres Positius MMR]
    ProcessNeg --> CheckNeg1{Comprovació 1<br/>Comportament=1<br/>Té positius?}
    
    CheckNeg1 -->|SÍ| IncorporateC1[✅ Incorporar<br/>Comprovació 1]
    CheckNeg1 -->|NO| CheckNeg2{Comprovació 2<br/>Té positius vigents<br/>mateix tipus?}
    
    CheckNeg2 -->|SÍ| IncorporateC2[✅ Incorporar<br/>Comprovació 2]
    CheckNeg2 -->|NO| NoIncorporate[❌ No Incorporar<br/>Auditoria NMRCM]
    
    CreatePos --> AuditMMR[📝 Auditoria OK MMR]
    IncorporateC1 & IncorporateC2 --> CreateNeg[💾 Crear Registres Negatius]
    CreateNeg --> AuditMMR
    NoIncorporate --> AuditNMRCM[📝 Auditoria NMRCM]
    
    ProcessBoth --> CreatePos & ProcessNeg
    
    ProcessVR --> CheckProva{Tipus Prova<br/>permet VR?}
    CheckProva -->|NO| AuditTPNIVR[📝 Auditoria TPNIVR]
    CheckProva -->|SÍ| CheckCentre{Centre<br/>permet VR?}
    CheckCentre -->|NO| AuditCNIVR[📝 Auditoria CNIVR]
    CheckCentre -->|SÍ| CreateVR[💾 Crear Registres VR<br/>SEMPRE positius]
    
    CreateVR --> NotaVR[📝 Nota Curs Clínic VR]
    NotaVR --> AuditVR[📝 Auditoria OK VR]
    
    ProcessMixt --> ProcessMMRMixt[⚡ Processar part MMR]
    ProcessMixt --> ProcessVRMixt[🦠 Processar part VR]
    ProcessMMRMixt & ProcessVRMixt --> AuditMixt[📝 Auditoria Mixta]
    
    AuditMMR & AuditNMRCM & AuditVR & AuditMixt & AuditTPNIVR & AuditCNIVR & BlockCNI --> Result([📊 RESULTAT FINAL])
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

---

## 🧪 Diagrama 2: Classificació de Mostra MMR

```mermaid
flowchart TD
    Start([📥 ResultatMostra MMR]) --> HasMicro{Té<br/>Microorganisme?}
    
    HasMicro -->|NO| NegResult[🔵 1 NEGATIU<br/>Sense microorganisme]
    HasMicro -->|SÍ| CheckSpecial{És<br/>Especial?}
    
    CheckSpecial -->|SÍ| CountMechSpecial[Comptar<br/>Mecanismes]
    CheckSpecial -->|NO| CountMechNormal[Comptar<br/>Mecanismes]
    
    CountMechSpecial --> MechSpecialCount{Nombre<br/>Mecanismes?}
    MechSpecialCount -->|0| Special0[⚡ 1 POSITIU<br/>Especial sense mec.]
    MechSpecialCount -->|N| SpecialN[⚡⚡ N POSITIUS<br/>Especial amb N mec.]
    
    CountMechNormal --> MechNormalCount{Nombre<br/>Mecanismes?}
    MechNormalCount -->|0| Normal0[🔵 1 NEGATIU<br/>Normal sense mec.]
    MechNormalCount -->|N| NormalN[⚡ N POSITIUS<br/>Normal amb N mec.]
    
    NegResult & Special0 & SpecialN & Normal0 & NormalN --> Aggregate[📊 Agregar per Mostra]
    
    Aggregate --> FinalClass{Classificació<br/>Final}
    
    FinalClass -->|1P, 0N| Result1[🟢 1 Sol Positiu]
    FinalClass -->|NP, 0N| ResultN[🟢🟢 N Positius]
    FinalClass -->|0P, 1N| Result1N[🔵 1 Sol Negatiu]
    FinalClass -->|0P, NN| ResultNN[🔵🔵 N Negatius]
    FinalClass -->|NP, MN| ResultMix[🟢🔵 Mixta]
    
    Result1 & ResultN & Result1N & ResultNN & ResultMix --> End([✅ Fi])
    
    style Start fill:#e1f5e1
    style End fill:#e1f5e1
    style Special0 fill:#ffe4b3
    style SpecialN fill:#ffcc80
    style NormalN fill:#a5d6a7
    style NegResult fill:#b3e5fc
    style Normal0 fill:#b3e5fc
```

---

## 🦠 Diagrama 3: Flux Específic Virus Respiratoris (NOU)

```mermaid
flowchart TD
    Start([🦠 Mostra amb VR]) --> CheckProva{Tipus Prova<br/>existeix i permet VR?}
    
    CheckProva -->|NO| RejectProva[❌ REBUTJAR<br/>Auditoria TPNIVR]
    CheckProva -->|SÍ| CheckCentre{Centre a llista<br/>VR_CENTRES?}
    
    CheckCentre -->|NO| RejectCentre[❌ REBUTJAR<br/>Auditoria CNIVR]
    CheckCentre -->|SÍ| CheckPacient{Pacient<br/>existeix?}
    
    CheckPacient -->|SÍ| PacientOK[✓ Pacient existent]
    CheckPacient -->|NO| WebService[🌐 Consultar WebService]
    
    WebService --> WSResult{Trobat?}
    WSResult -->|NO| RejectWS[❌ REBUTJAR<br/>Auditoria NPWS]
    WSResult -->|SÍ| CreatePacient[💾 Crear Pacient]
    
    CreatePacient --> LoopVR
    PacientOK --> LoopVR[📋 Per cada Virus Respiratori]
    
    LoopVR --> CheckMicroVR{Microorganisme VR<br/>existeix?}
    CheckMicroVR -->|NO| CreateMicroVR[💾 Crear Microorganisme VR]
    CheckMicroVR -->|SÍ| ExistsMicroVR[✓ Ja existeix]
    
    CreateMicroVR --> CreateDiagVR
    ExistsMicroVR --> CreateDiagVR[💾 Crear/Obtenir<br/>Diagnòstic VR<br/>mecanisme=NULL]
    
    CreateDiagVR --> CreateMostraVR[💾 Crear/Obtenir<br/>Mostra Diagnòstic VR<br/>valoracio='2']
    
    CreateMostraVR --> CreateRelacioVR[💾 Crear Relació<br/>mostra_microorganisme VR]
    
    CreateRelacioVR --> UpdateDatesVR[📅 Actualitzar dates]
    
    UpdateDatesVR --> MoreVR{Més VR<br/>a processar?}
    MoreVR -->|SÍ| LoopVR
    MoreVR -->|NO| NotaClinica{Cal nota<br/>curs clínic?}
    
    NotaClinica -->|SÍ| CreateNota[📝 Crear Nota VR<br/>curs clínic]
    NotaClinica -->|NO| SkipNota[⏩ Saltar nota]
    
    CreateNota --> AuditOK
    SkipNota --> AuditOK[📝 Auditoria OK]
    
    AuditOK --> Success([✅ VR Processats])
    RejectProva & RejectCentre & RejectWS --> Fail([❌ VR Rebutjats])
    
    style Start fill:#e1f5e1
    style Success fill:#d5e8f0
    style Fail fill:#ffcdd2
    style CreateMicroVR fill:#c8e6c9
    style CreateNota fill:#bbdefb
    style RejectProva fill:#ffcdd2
    style RejectCentre fill:#ffcdd2
    style RejectWS fill:#ffcdd2
```

---

## 🔀 Diagrama 4: Flux Mostra Mixta MMR + VR (NOU)

```mermaid
flowchart TD
    Start([🔀 Mostra Mixta<br/>MMR + VR]) --> SeparateTypes[🔀 Separar per Tipus]
    
    SeparateTypes --> MMRResults[📊 Resultats MMR]
    SeparateTypes --> VRResults[🦠 Resultats VR]
    
    MMRResults --> ClassifyMMR[🧪 Classificar MMR]
    VRResults --> ProcessVR[🦠 Processar VR]
    
    ClassifyMMR --> TypeMMR{Tipus<br/>Mostra MMR?}
    
    TypeMMR -->|Positiva| ProcessPosMMR[⚡ Processar Positiva MMR]
    TypeMMR -->|Negativa| ProcessNegMMR[🔍 Processar Negativa MMR]
    TypeMMR -->|Mixta| ProcessMixtaMMR[⚡🔍 Processar Mixta MMR]
    
    ProcessPosMMR --> ResultMMR[✅ MMR Processat]
    ProcessNegMMR --> ResultMMR
    ProcessMixtaMMR --> ResultMMR
    
    ProcessVR --> CheckProvaVR{Tipus Prova<br/>permet VR?}
    CheckProvaVR -->|NO| RejectVR[❌ VR Rebutjat]
    CheckProvaVR -->|SÍ| CheckCentreVR{Centre<br/>permet VR?}
    
    CheckCentreVR -->|NO| RejectVR
    CheckCentreVR -->|SÍ| CreateVR[💾 Crear Registres VR]
    
    CreateVR --> ResultVR[✅ VR Processat]
    
    ResultMMR --> Combine[🔗 Combinar Resultats]
    ResultVR --> Combine
    RejectVR --> Combine
    
    Combine --> UpdateDates[📅 Actualitzar Dates Globals]
    UpdateDates --> AuditMixt[📝 Auditoria Mixta]
    
    AuditMixt --> Result([✅ Mostra Mixta Processada])
    
    style Start fill:#ffe4b3
    style Result fill:#c8e6c9
    style ProcessPosMMR fill:#d4edda
    style ProcessNegMMR fill:#cce5ff
    style ProcessVR fill:#e1d5f0
    style CreateVR fill:#d5e8f0
    style RejectVR fill:#ffcdd2
    style Combine fill:#fff9c4
```

---

## 🦠 Diagrama 5: Comprovar Microorganismes

```mermaid
flowchart TD
    Start([🦠 Llista Microorganismes]) --> GetUnique[Obtenir<br/>Microorganismes Únics]
    
    GetUnique --> Loop{Per cada<br/>Microorganisme}
    
    Loop -->|Següent| CheckExists{Existeix a BD?}
    
    CheckExists -->|NO| Create[💾 CREAR<br/>microorganisme<br/>especial=0]
    CheckExists -->|SÍ| Exists[✓ Ja existeix]
    
    Create --> CheckSpecialTable
    Exists --> CheckSpecialTable{Existeix a<br/>micro_especial?}
    
    CheckSpecialTable -->|SÍ| MarkSpecial[⚡ Marcar com<br/>ESPECIAL=1]
    CheckSpecialTable -->|NO| MarkNormal[○ Marcar com<br/>ESPECIAL=0]
    
    MarkSpecial & MarkNormal --> Store[📝 Guardar a<br/>Diccionari Resultats]
    
    Store --> More{Més<br/>Microorganismes?}
    
    More -->|SÍ| Loop
    More -->|NO| Result([✅ Resultat:<br/>Dictionary microorg, especial])
    
    style Start fill:#e1f5e1
    style Result fill:#e1f5e1
    style Create fill:#c8e6c9
    style MarkSpecial fill:#ffe4b3
    style MarkNormal fill:#e3f2fd
```

---

## 🛡️ Diagrama 6: Comprovació Mecanismes (ACTUALITZAT)

```mermaid
flowchart TD
    Start([🛡️ Per cada Resultat]) --> HasMech{Té<br/>Mecanismes?}
    
    HasMech -->|NO| SkipMech[⏩ Sense mecanismes<br/>OK per VR]
    HasMech -->|SÍ| LoopMech[Per cada<br/>Mecanisme]
    
    LoopMech --> CheckExists{Mecanisme<br/>existeix a BD?}
    
    CheckExists -->|NO| Create[💾 CREAR<br/>mecanisme_resistencia]
    CheckExists -->|SÍ| Exists[✓ Ja existeix]
    
    Create --> CheckCombo
    Exists --> CheckCombo{Combinació<br/>Micro+Mec<br/>a NO incorporar?}
    
    CheckCombo -->|SÍ| RemoveResult[❌ ELIMINAR resultat<br/>de la llista<br/>Auditoria CNI]
    CheckCombo -->|NO| OK[✅ Combinació OK]
    
    RemoveResult --> MoreMech{Més<br/>Mecanismes?}
    OK --> MoreMech
    
    MoreMech -->|SÍ| LoopMech
    MoreMech -->|NO| CheckEmpty{Queden<br/>resultats?}
    
    SkipMech --> Success
    CheckEmpty -->|SÍ| Success([✅ Continuar<br/>Processament])
    CheckEmpty -->|NO| Fail([❌ Tots eliminats<br/>Mostra descartada])
    
    style Start fill:#e1f5e1
    style Success fill:#c8e6c9
    style Fail fill:#ffcdd2
    style RemoveResult fill:#ffcdd2
    style Create fill:#fff9c4
    style OK fill:#c8e6c9
```

---

## ⚡ Diagrama 7: Processar Mostra Positiva MMR

```mermaid
flowchart TD
    Start([⚡ Mostra Positiva MMR]) --> GetPatient[🌐 Obtenir Pacient<br/>WebService]
    
    GetPatient --> PatientFound{Pacient<br/>Trobat?}
    
    PatientFound -->|NO| Warning[⚠️ Warning<br/>Pacient no trobat<br/>Continuar igualment]
    PatientFound -->|SÍ| CheckPD
    
    Warning --> CheckPD{Pacient existeix a<br/>pacients_diagnostics?}
    
    CheckPD -->|NO| CreatePD[💾 INSERT<br/>pacients_diagnostics]
    CheckPD -->|SÍ| ExistsPD[✓ Ja existeix]
    
    CreatePD --> LoopResults
    ExistsPD --> LoopResults[📋 Per cada resultat POSITIU MMR]
    
    LoopResults --> CreatePDM[💾 CREATE/UPDATE<br/>pacients_diagnostics_mostra<br/>valoracio='2']
    
    CreatePDM --> CreateMM[💾 CREATE<br/>mostra_microorganisme]
    
    CreateMM --> HasMech{Té<br/>Mecanismes?}
    
    HasMech -->|SÍ| CreateMech[💾 CREATE<br/>micro_mecanisme_mostra<br/>per cada mecanisme 1-5]
    HasMech -->|NO| CheckTypes
    
    CreateMech --> CheckTypes{Tipus Mostra/Prova<br/>existeixen?}
    
    CheckTypes -->|NO| CreateTypes[💾 CREATE<br/>tipusmostra_m<br/>tipusprova_m]
    CheckTypes -->|SÍ| UpdateDates
    
    CreateTypes --> UpdateDates[📅 Actualitzar Dates Pacient<br/>última inclusió<br/>última mostra positiva]
    
    UpdateDates --> MoreResults{Més resultats<br/>positius?}
    
    MoreResults -->|SÍ| LoopResults
    MoreResults -->|NO| Audit[📝 Auditoria<br/>Codi: OK]
    
    Audit --> Result([✅ Resultat OK])
    
    style Start fill:#e1f5e1
    style Result fill:#c8e6c9
    style Warning fill:#fff3cd
    style Audit fill:#d1ecf1
```

---

## 🔍 Diagrama 8: Processar Mostra Negativa (Comprovacions)

```mermaid
flowchart TD
    Start([🔍 Mostra Negativa]) --> GetComportament[📊 Obtenir Comportament<br/>del Tipus Mostra]
    
    GetComportament --> CheckComportament{Comportament<br/>= 1?}
    
    CheckComportament -->|SÍ| Comp1[🔎 COMPROVACIÓ 1<br/>Pacient té positius generals?]
    CheckComportament -->|NO| Comp2Direct[⏩ Saltar a<br/>Comprovació 2]
    
    Comp1 --> HasGeneral{Té positius<br/>generals?}
    
    HasGeneral -->|SÍ| IncorpC1[✅ INCORPORAR<br/>Via Comprovació 1]
    HasGeneral -->|NO| Comp2[🔎 COMPROVACIÓ 2<br/>Pacient té positius vigents<br/>d'aquest tipus?]
    
    Comp2Direct --> Comp2
    
    Comp2 --> GetTipusId[Obtenir ID tipus mostra]
    GetTipusId --> GetEquiv[Obtenir tipus equivalents]
    GetEquiv --> QueryVigents[Query: Positius vigents<br/>tipus + equivalents]
    
    QueryVigents --> HasVigent{Té positius<br/>vigents?}
    
    HasVigent -->|SÍ| IncorpC2[✅ INCORPORAR<br/>Via Comprovació 2]
    HasVigent -->|NO| NoIncorp[❌ NO INCORPORAR<br/>Auditoria NMRCM]
    
    IncorpC1 --> CreateRecords[💾 Crear Registres<br/>pacients_diagnostics<br/>pacients_diagnostics_mostra<br/>mostra_microorganisme]
    IncorpC2 --> CreateRecords
    
    CreateRecords --> AuditOK[📝 Auditoria OK]
    NoIncorp --> AuditNMRCM[📝 Auditoria NMRCM<br/>Increment NoIncorporats]
    
    AuditOK --> ResultOK([✅ Incorporat])
    AuditNMRCM --> ResultNo([⚠️ No Incorporat])
    
    style Start fill:#e1f5e1
    style ResultOK fill:#c8e6c9
    style ResultNo fill:#fff3cd
    style IncorpC1 fill:#c8e6c9
    style IncorpC2 fill:#c8e6c9
    style NoIncorp fill:#ffcdd2
    style Comp1 fill:#e3f2fd
    style Comp2 fill:#e3f2fd
```

---

## 🎯 Diagrama 9: Determinar Tipus Incorporació

```mermaid
flowchart TD
    Start([🎯 Determinar Tipus]) --> GetDatesOracle[📅 Obtenir Dates Oracle<br/>DataResultat màx<br/>DataValidacio màx]
    
    GetDatesOracle --> QueryMySQL[🔍 Consultar MySQL<br/>mostra_microorganisme<br/>per etiqueta]
    
    QueryMySQL --> ExistsMySQL{Existeix a<br/>MySQL?}
    
    ExistsMySQL -->|NO| Nova[🆕 NOVA<br/>No existeix a destí]
    ExistsMySQL -->|SÍ| CompareDates[📊 Comparar Dates]
    
    CompareDates --> SameDates{Dates<br/>Iguals?}
    
    SameDates -->|SÍ| Repetida[🔁 REPETIDA<br/>Mateix resultat]
    SameDates -->|NO| CheckValidation{Canvi en<br/>Validació?}
    
    CheckValidation -->|Abans NO, ara SÍ| Validada[✅ VALIDADA<br/>Primera validació]
    CheckValidation -->|Abans SÍ, ara NO| Desvalidada[⬇️ DESVALIDADA<br/>S'ha desvalidat]
    CheckValidation -->|Ambdues SÍ, dates diferents| Revalidada[🔄 REVALIDADA<br/>Revalidació]
    CheckValidation -->|DataResultat anterior| Antiga[🕐 ANTIGA<br/>Data anterior a última]
    CheckValidation -->|DataResultat diferent| Canviada[🔄 CANVIADA<br/>Resultat modificat]
    
    Nova & Repetida & Validada & Desvalidada & Revalidada & Antiga & Canviada --> Result([✅ Tipus Determinat])
    
    style Start fill:#e1f5e1
    style Result fill:#e1f5e1
    style Nova fill:#c8e6c9
    style Repetida fill:#fff3cd
    style Validada fill:#bbdefb
    style Revalidada fill:#b39ddb
    style Desvalidada fill:#ffccbc
    style Antiga fill:#d7ccc8
    style Canviada fill:#ffcc80
```
    
    style Start fill:#e1f5e1
    style End fill:#e1f5e1
    style Nova fill:#c8e6c9
    style Repetida fill:#fff3cd
    style Validada fill:#c8e6c9
    style Desvalidada fill:#ffcdd2
    style Antiga fill:#e3f2fd
```

---
## ?? Diagrama 10: Flux de Dades entre Sistemes (ACTUALITZAT)

```mermaid
graph LR
    subgraph Oracle["?? ORACLE (Modulab)"]
        M[Mostres]
        R[Resultats MMR]
        RVR[Resultats VR]
        P[Pacients]
    end
    
    subgraph App["?? MultirIntegraModulab"]
        V[Validaci�]
        DT[Determinar Tipus]
        C[Classificaci� MMR]
        CM[Compr. Microorganismes]
        CR[Compr. Mecanismes]
        PP[Proc. Positiva MMR]
        PN[Proc. Negativa MMR]
        PVR[Proc. Virus Respiratori]
        PMIXT[Proc. Mixta MMR+VR]
    end
    
    subgraph MySQL["??? MYSQL (MultiR)"]
        PD[pacients_diagnostics]
        PDM[pacients_diagnostics_mostra]
        MM[mostra_microorganisme]
        MR[mecanisme_resistencia]
        MO[microorganisme]
        ME[micro_especial]
        MMM[micro_mecanisme_mostra]
        A[auditoria_integracio_modulab]
        TP[tipusprova_m]
        PARAM[parametres VR_CENTRES]
    end
    
    subgraph WS["?? WebService"]
        WP[Dades Pacient]
    end
    
    M --> V
    R --> V
    RVR --> V
    P --> V
    
    V --> DT
    DT --> CM
    CM --> ME
    CM --> MO
    CM --> CR
    CR --> MR
    CR --> C
    CR --> PVR
    CR --> PMIXT
    
    C --> PP
    C --> PN
    
    WP -.->|Opcional| PP
    WP -.->|Opcional| PN
    WP -.->|Opcional| PVR
    
    PP --> PD
    PP --> PDM
    PP --> MM
    PP --> MMM
    PP --> A
    
    PN --> PD
    PN --> PDM
    PN --> MM
    PN --> A
    
    PVR --> TP
    PVR --> PARAM
    PVR --> PD
    PVR --> PDM
    PVR --> MM
    PVR --> A
    
    PMIXT --> PP
    PMIXT --> PN
    PMIXT --> PVR
    
    style Oracle fill:#e8f5e9
    style MySQL fill:#e3f2fd
    style App fill:#fff3e0
    style WS fill:#f3e5f5
    style PVR fill:#e1d5f0
    style PMIXT fill:#ffe4b3
    style RVR fill:#e1d5f0
```

---

## ?? Diagrama 11: Cicle de Vida d'una Mostra (ACTUALITZAT amb VR)

```mermaid
stateDiagram-v2
    [*] --> Oracle: Resultat nou a Modulab
    
    Oracle --> Lectura: Query amb filtre dates
    
    Lectura --> Validacio: Col�lecci� mostres
    
    Validacio --> DeterminarTipus: Mostra v�lida
    Validacio --> [*]: Mostra no v�lida
    
    DeterminarTipus --> TipusIncorporacio: Tipus determinat
    
    TipusIncorporacio --> Repetida: REPETIDA
    TipusIncorporacio --> Antiga: ANTIGA
    TipusIncorporacio --> Processar: NOVA/VALIDADA/REVALIDADA
    
    Repetida --> [*]: Skip
    Antiga --> AuditoriaAntiga: Registrar
    AuditoriaAntiga --> [*]
    
    Processar --> ComprovacionsInicials: Check microorg + mec
    
    ComprovacionsInicials --> TotsCNI: Tots resultats CNI
    ComprovacionsInicials --> DeterminarTipusMicro: Alguns resultats OK
    
    TotsCNI --> AuditoriaCNI
    AuditoriaCNI --> [*]: Mostra descartada
    
    DeterminarTipusMicro --> MMR: Nom�s MMR
    DeterminarTipusMicro --> VR: Nom�s VR
    DeterminarTipusMicro --> Mixta: MMR + VR
    
    MMR --> ClassificacioMMR: Classificar
    
    ClassificacioMMR --> Positiva: 1+ resultats positius
    ClassificacioMMR --> Negativa: 0 resultats positius
    ClassificacioMMR --> MixtaMMR: Positius i Negatius
    
    Positiva --> ProcessarPos: Processar
    Negativa --> ComprovacionsNeg: Check comprov. 1 i 2
    MixtaMMR --> ProcessarPos
    MixtaMMR --> ComprovacionsNeg
    
    ComprovacionsNeg --> Incorporar: T� positius
    ComprovacionsNeg --> NoIncorporar: No t� positius
    
    ProcessarPos --> MySQL_Positiu
    Incorporar --> MySQL_Negatiu
    NoIncorporar --> AuditoriaNMRCM
    
    VR --> CheckProva: Comprovar tipus prova
    CheckProva --> CheckCentre: Tipus prova OK
    CheckProva --> AuditoriaTPNIVR: Tipus prova NO OK
    
    CheckCentre --> ProcessarVR: Centre OK
    CheckCentre --> AuditoriaCNIVR: Centre NO OK
    
    ProcessarVR --> MySQL_VR
    
    Mixta --> ProcessarMixtaPart1: Processar MMR
    Mixta --> ProcessarMixtaPart2: Processar VR
    
    ProcessarMixtaPart1 --> MySQL_Mixta
    ProcessarMixtaPart2 --> MySQL_Mixta
    
    MySQL_Positiu --> AuditoriaOK
    MySQL_Negatiu --> AuditoriaOK
    MySQL_VR --> NotaVR: Crear nota curs cl�nic
    MySQL_Mixta --> AuditoriaMixta
    
    NotaVR --> AuditoriaOK
    
    AuditoriaOK --> [*]: Mostra processada
    AuditoriaNMRCM --> [*]: Negatiu no incorporat
    AuditoriaMixta --> [*]: Mixta processada
    AuditoriaTPNIVR --> [*]: VR tipus prova invalida
    AuditoriaCNIVR --> [*]: VR centre invalida
    
    note right of Oracle
        Sistema origen
        Resultats microbiol�gics
        MMR i VR
    end note
    
    note right of MySQL_Positiu
        Registres MMR creats:
        - pacients_diagnostics
        - pacients_diagnostics_mostra
        - mostra_microorganisme
        - micro_mecanisme_mostra
    end note
    
    note right of MySQL_VR
        Registres VR creats:
        - pacients_diagnostics
        - pacients_diagnostics_mostra
        - mostra_microorganisme
        - Sense mecanismes
        - Nota curs cl�nic
    end note
    
    note right of AuditoriaNMRCM
        Negatiu sense
        positius vigents
        del pacient
    end note
```

---

## ?? Diagrama 12: Model de Dades Simplificat (ACTUALITZAT)

```mermaid
erDiagram
    PACIENTS_DIAGNOSTICS ||--o{ PACIENTS_DIAGNOSTICS_MOSTRA : "t�"
    PACIENTS_DIAGNOSTICS_MOSTRA ||--|| MOSTRA_MICROORGANISME : "genera"
    MOSTRA_MICROORGANISME ||--o{ MICRO_MECANISME_MOSTRA : "t� MMR"
    
    MICROORGANISME ||--o{ MOSTRA_MICROORGANISME : "�s"
    MICROORGANISME ||--o| MICRO_ESPECIAL : "pot ser"
    
    MECANISME_RESISTENCIA ||--o{ MICRO_MECANISME_MOSTRA : "�s"
    MICROORGANISME ||--o{ MICRO_MECANISME_NOINCOPORAR : "t�"
    MECANISME_RESISTENCIA ||--o{ MICRO_MECANISME_NOINCOPORAR : "forma"
    
    TIPUSMOSTRA_M ||--o{ PACIENTS_DIAGNOSTICS_MOSTRA : "classifica"
    TIPUSMOSTRA_M ||--o{ TIPUSMOSTRA_EQUIVALENTS : "pot tenir"
    
    TIPUSPROVA_M ||--o{ MOSTRA_MICROORGANISME : "identifica"
    TIPUSPROVA_M ||--o| PARAMETRES : "configura VR"
    
    MOSTRA_MICROORGANISME ||--|| AUDITORIA_INTEGRACIO_MODULAB : "registra"
    
    PACIENTS_DIAGNOSTICS {
        int id PK
        string npat
        datetime data_entrada
        datetime data_modificacio
    }
    
    PACIENTS_DIAGNOSTICS_MOSTRA {
        int id PK
        string npat FK
        datetime data_mostra
        string tipus_mostra_m
        string etiqueta
        char valoracio
        char vigent
    }
    
    MOSTRA_MICROORGANISME {
        int id PK
        string npat
        string etiqueta UK
        datetime data_mostra
        datetime data_resultat
        datetime data_validacio
        string microorganisme FK
        int id_prova FK
    }
    
    MICRO_MECANISME_MOSTRA {
        int id PK
        string npat
        string etiqueta
        datetime data_mostra
        string microorganisme FK
        string mecanisme_resistencia FK
        comment "NULL per VR"
    }
    
    MICROORGANISME {
        int id PK
        string descripcio UK
        tinyint especial
        comment "Inclou MMR i VR"
    }
    
    MICRO_ESPECIAL {
        int id PK
        string microorganisme UK
        comment "Nom�s MMR"
    }
    
    MECANISME_RESISTENCIA {
        int id PK
        string codi UK
        string descripcio
        comment "Nom�s per MMR"
    }
    
    MICRO_MECANISME_NOINCOPORAR {
        int id PK
        string microorganisme FK
        string mecanisme_resistencia FK
        comment "CNI - Nom�s MMR"
    }
    
    TIPUSMOSTRA_M {
        int id PK
        string descripcio UK
        int comportament
        int dies_vigencia_positiu
    }
    
    TIPUSPROVA_M {
        int id PK
        string descripcio UK
        tinyint permet_vr
        comment "Nou camp VR"
    }
    
    PARAMETRES {
        int id PK
        string clau
        string valor
        comment "VR_CENTRES: llista centres VR"
    }
    
    AUDITORIA_INTEGRACIO_MODULAB {
        int id PK
        string etiqueta
        string npat
        string codi_retorn
        datetime data_integracio
        comment "Codis: OK, NPWS, CNI, NMRCM, TPNIVR, CNIVR"
    }
```

---

## ?? Llegenda de Colors i S�mbols

### Colors dels Nodes

- ?? **Verd clar** (#e1f5e1): Inici/Fi de flux
- ?? **Verd** (#c8e6c9): Acci� exitosa, incorporaci�
- ?? **Blau** (#e3f2fd): Comprovacions, queries
- ?? **Groc** (#fff3cd): Warnings, situacions especials
- ?? **Taronja** (#ffe4b3): Microorganismes especials / Mostres mixtes
- ?? **Lila** (#e1d5f0): Virus Respiratoris
- ?? **Blau clar** (#d5e8f0): Registres VR
- ?? **Vermell clar** (#ffcdd2): Errors, rebutjos
- ? **Gris** (#f5f5f5): Processos neutrals

### S�mbols Utilitzats

- ?? Inici de proc�s
- ? Validaci�/Comprovaci� OK
- ? Error/Rebuig
- ?? Cerca/Query
- ?? Crear/Actualitzar BD
- ?? Auditoria
- ?? Classificaci� MMR
- ?? Microorganismes / Virus Respiratoris
- ??? Mecanismes
- ? Positiu MMR
- ?? Negatiu MMR
- ?? WebService
- ?? Resultat/Estad�stica
- ?? Actualitzaci�/Revalidaci�
- ?? Warning
- ?? Mostra mixta MMR+VR
- ?? Determinar tipus

### Codis d'Auditoria

- **OK**: Processament correcte
- **NPWS**: Pacient No trobat al Web Service
- **CNI**: Combinaci� No Incorporable (microorganisme + mecanisme)
- **NMRCM**: Negatiu sense Microorganisme amb Resist�ncia al Carbapenem vigent
- **TPNIVR**: Tipus de Prova No v�lida per Incorporar Virus Respiratoris
- **CNIVR**: Centre No v�lid per Incorporar Virus Respiratoris

---

## ?? Notes d'�s

### Visualitzar Diagrames

1. **GitHub**: Els diagrames Mermaid es renderitzen autom�ticament
2. **VS Code**: Instal�lar extensi� "Markdown Preview Mermaid Support"
3. **Online**: Copiar codi a https://mermaid.live/
4. **Exportar**: Des de mermaid.live es pot exportar a PNG, SVG, PDF

### Modificar Diagrames

Els diagrames s�n text pla i es poden editar f�cilment:

```mermaid
flowchart TD
    A[Inici] --> B{Decisi�}
    B -->|Opci� 1| C[Acci� 1]
    B -->|Opci� 2| D[Acci� 2]
```

### Sintaxi B�sica

- `flowchart TD`: Diagrama de flux de dalt a baix (Top-Down)
- `flowchart LR`: Diagrama de flux d'esquerra a dreta (Left-Right)
- `-->`: Fletxa simple
- `-.->`: Fletxa puntejada
- `==>`: Fletxa gruixuda
- `[ ]`: Node rectangular
- `{ }`: Node de decisi� (rombe)
- `(( ))`: Node circular
- `[( )]`: Node cil�ndric (BD)

---

## ?? Novetats Versi� 2.0 (Actualitzaci� Gener 2025)

### Funcionalitats Afegides:

1. **Virus Respiratoris (VR)**:
   - Processament espec�fic per VR (sempre positius, sense mecanismes)
   - Validaci� tipus de prova (`tipusprova_m.permet_vr`)
   - Validaci� centre (`parametres.VR_CENTRES`)
   - Generaci� autom�tica de nota curs cl�nic
   - Codis auditoria espec�fics: `TPNIVR`, `CNIVR`

2. **Mostres Mixtes MMR + VR**:
   - Processament separat i coordinat de MMR i VR
   - Bifurcaci� de flux segons tipus de microorganisme
   - Combinaci� de resultats finals

3. **Millores en Comprovaci� Mecanismes**:
   - Eliminaci� de resultats individuals amb CNI (no tota la mostra)
   - Descart de mostra nom�s si tots els resultats tenen CNI
   - Suport per VR (sense mecanismes)

### Diagrames Nous/Actualitzats:

- **Diagrama 1**: Flux principal amb bifurcaci� MMR/VR/Mixt ?
- **Diagrama 2**: Classificaci� MMR (sense canvis) ?
- **Diagrama 3**: **NOVELL** - Flux espec�fic Virus Respiratoris ?
- **Diagrama 4**: **NOVELL** - Flux Mostra Mixta MMR + VR ?
- **Diagrama 5**: Comprovar Microorganismes (renumerat) ?
- **Diagrama 6**: Actualitzat amb l�gica eliminaci� individual CNI ?
- **Diagrama 7**: Processar Positiva MMR (renumerat) ?
- **Diagrama 8**: Processar Negativa (renumerat) ?
- **Diagrama 9**: Determinar Tipus (renumerat) ?
- **Diagrama 10**: Actualitzat amb taules i processos VR ?
- **Diagrama 11**: Cicle de vida amb estats VR i Mixta ?
- **Diagrama 12**: Model dades amb camps VR ?

---

**Documentaci� actualitzada**: Gener 2025  
**Versi�**: 2.0  
**Format**: Mermaid.js  
**Compatibilitat**: GitHub, GitLab, VS Code, Mermaid Live Editor  
**Canvis principals**: Suport complet Virus Respiratoris (VR) i Mostres Mixtes
