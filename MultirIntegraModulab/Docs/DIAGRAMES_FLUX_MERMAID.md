# 📊 DIAGRAMES DE FLUX - FORMAT MERMAID

Aquest document conté diagrames de flux en format Mermaid que poden ser visualitzats a GitHub, en editors compatibles o utilitzant https://mermaid.live/

---

## 🔄 Diagrama 1: Flux Principal Complet

```mermaid
flowchart TD
    Start([🏁 INICI: Lectura Mostres Oracle]) --> Validate{✅ Validació<br/>Mostra Vàlida?}
    
    Validate -->|NO| Reject[❌ REBUTJAR<br/>Log Warning]
    Validate -->|SÍ| Classify[🧪 Classificar Mostra]
    
    Classify --> ClassifyResult{Tipus de Mostra?}
    
    ClassifyResult -->|1 Positiu| Positive1[🟢 1 Positiu]
    ClassifyResult -->|N Positius| PositiveN[🟢🟢 N Positius]
    ClassifyResult -->|1 Negatiu| Negative1[🔵 1 Negatiu]
    ClassifyResult -->|N Negatius| NegativeN[🔵🔵 N Negatius]
    ClassifyResult -->|Mixta| Mixed[🟢🔵 Mixta]
    
    Positive1 & PositiveN & Mixed --> DetermineType[🔎 Determinar Tipus Incorporació]
    Negative1 & NegativeN --> DetermineType
    
    DetermineType --> TypeResult{Tipus?}
    TypeResult -->|NOVA| Nova[🆕 Nova]
    TypeResult -->|REPETIDA| Repetida[🔁 Repetida - Skip]
    TypeResult -->|VALIDADA| Validada[✅ Validada]
    TypeResult -->|REVALIDADA| Revalidada[🔄 Revalidada]
    TypeResult -->|ANTIGA| Antiga[🕐 Antiga - Auditoria]
    
    Nova & Validada & Revalidada --> CheckMicro[🦠 Comprovar Microorganismes]
    
    CheckMicro --> CheckMech[🛡️ Comprovar Mecanismes]
    
    CheckMech --> MechResult{Combinació<br/>Prohibida?}
    
    MechResult -->|SÍ| BlockCNI[❌ ATURAR<br/>Auditoria CNI]
    MechResult -->|NO| Branch{Bifurcació<br/>Tipus Mostra}
    
    Branch -->|POSITIVA| ProcessPos[⚡ Processar Positiva]
    Branch -->|NEGATIVA| ProcessNeg[🔍 Processar Negativa]
    Branch -->|MIXTA| ProcessBoth[⚡🔍 Processar Ambdues]
    
    ProcessPos --> CreatePos[💾 Crear Registres Positius]
    ProcessNeg --> CheckNeg1{Comprovació 1<br/>Comportament=1<br/>Té positius?}
    
    CheckNeg1 -->|SÍ| IncorporateC1[✅ Incorporar<br/>Comprovació 1]
    CheckNeg1 -->|NO| CheckNeg2{Comprovació 2<br/>Té positius vigents<br/>mateix tipus?}
    
    CheckNeg2 -->|SÍ| IncorporateC2[✅ Incorporar<br/>Comprovació 2]
    CheckNeg2 -->|NO| NoIncorporate[❌ No Incorporar<br/>Auditoria NMRCM]
    
    CreatePos --> Audit[📝 Auditoria OK]
    IncorporateC1 & IncorporateC2 --> CreateNeg[💾 Crear Registres Negatius]
    CreateNeg --> Audit
    NoIncorporate --> AuditNMRCM[📝 Auditoria NMRCM]
    
    ProcessBoth --> CreatePos & ProcessNeg
    
    Audit & AuditNMRCM & BlockCNI --> Result([📊 RESULTAT FINAL])
    Repetida & Antiga & Reject --> Result
    
    style Start fill:#e1f5e1
    style Result fill:#e1f5e1
    style Reject fill:#ffe1e1
    style BlockCNI fill:#ffe1e1
    style NoIncorporate fill:#fff3cd
    style Audit fill:#d1ecf1
    style AuditNMRCM fill:#fff3cd
    style ProcessPos fill:#d4edda
    style ProcessNeg fill:#cce5ff
```

---

## 🧪 Diagrama 2: Classificació de Mostra

```mermaid
flowchart TD
    Start([📥 ResultatMostra]) --> HasMicro{Té<br/>Microorganisme?}
    
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

## 🔀 Diagrama 3: Flux Mostra Mixta MMR + VR (ACTUALITZAT)

```mermaid
flowchart TD
    Start([📥 ResultatMostra Mixta]) --> GetMR{Té Mecanisme<br/>Resistència?}
    
    GetMR -->|NO| CheckVR{Té Validació?<br/>Data > última mostra + 30 dies?}
    GetMR -->|SÍ| CheckMM[🔄 Reutilitzar últim MMR]
    
    CheckVR -->|SÍ| CreateVR[💾 Crear Registre de Validació]
    CheckVR -->|NO| IgnoreVR[🔍 Ignorar Validació]
    
    CreateMR --> CreateMM[💾 Crear/Actualitzar<br/>mostra_microorganisme]
    
    CheckMM --> HasMech{Té<br/>Mecanismes?}
    
    HasMech -->|SÍ| CreateMech[💾 CREATE<br/>micro_mecanisme_mostra<br/>per cada mecanisme 1-5]
    HasMech -->|NO| CheckTypes
    
    CreateMech --> CheckTypes{Tipus Mostra/Prova<br/>existeixen?}
    
    CheckTypes -->|NO| CreateTypes[💾 CREATE<br/>tipusmostra_m<br/>tipusprova_m]
    CheckTypes -->|SÍ| UpdateDates
    
    CreateTypes --> UpdateDates[📅 Actualitzar Dates Pacient<br/>última inclusió<br/>última mostra positiva]
    
    UpdateDates --> End([✅ Fi - Mostra Mixta Processada])
    
    style Start fill:#e1f5e1
    style End fill:#e1f5e1
    style CreateVR fill:#c8e6c9
    style IgnoreVR fill:#fff3cd
    style CreateMech fill:#c8e6c9
    style CreateTypes fill:#c8e6c9
```

---

## 🦠 Diagrama 5: Comprovar Microorganismes (sense canvis)

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
    Start([🛡️ Llista Mecanismes]) --> Loop{Per cada<br/>Mecanisme}
    
    Loop -->|Següent| CheckExists{Existeix a BD?}
    
    CheckExists -->|NO| Create[💾 CREAR<br/>mecanisme_resistencia]
    CheckExists -->|SÍ| Exists[✓ Ja existeix]
    
    Create --> CheckCombo
    Exists --> CheckCombo{Combinació<br/>Micro+Mec<br/>a NO incorporar?}
    
    CheckCombo -->|SÍ| Block[❌ ATURAR<br/>ContinuarProc=FALSE<br/>Auditoria CNI]
    CheckCombo -->|NO| OK[✅ Combinació OK]
    
    OK --> More{Més<br/>Mecanismes?}
    
    More -->|SÍ| Loop
    More -->|NO| Success([✅ Tots OK<br/>ContinuarProc=TRUE])
    
    Block --> Fail([❌ Processament Aturat])
    
    style Start fill:#e1f5e1
    style Success fill:#c8e6c9
    style Fail fill:#ffcdd2
    style Block fill:#ffcdd2
    style Create fill:#fff9c4
    style OK fill:#c8e6c9
```

---

## ⚡ Diagrama 7: Processar Mostra Positiva

```mermaid
flowchart TD
    Start([⚡ Mostra Positiva]) --> GetPatient[🌐 Obtenir Pacient<br/>WebService]
    
    GetPatient --> PatientFound{Pacient<br/>Trobat?}
    
    PatientFound -->|NO| Warning[⚠️ Warning<br/>Pacient no trobat<br/>Continuar igualment]
    PatientFound -->|SÍ| CheckPD
    
    Warning --> CheckPD{Pacient existeix a<br/>pacients_diagnostics?}
    
    CheckPD -->|NO| CreatePD[💾 INSERT<br/>pacients_diagnostics]
    CheckPD -->|SÍ| ExistsPD[✓ Ja existeix]
    
    CreatePD --> LoopResults
    ExistsPD --> LoopResults[📋 Per cada resultat POSITIU]
    
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

## 🔍 Diagrama 8: Processar Mostra Negativa (Comprovacions) - sense canvis

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

## 🎯 Diagrama 8: Determinar Tipus Incorporació

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
    CheckValidation -->|Abans SÍ, ara NO| Desvalidada[⬇️ DESVALIDA<br/>Segona validació]
    CheckValidation -->|Igual| Antiga[🕐 Antiga - Sense Canvis]
    
    Nova --> InsertNova[💾 INSERTAR<br/>composicio_mostra]
    
    InsertNova --> End([✅ Fi - Tipus Determinat i Nova Insertada])
    
    style Start fill:#e1f5e1
    style End fill:#e1f5e1
    style Nova fill:#c8e6c9
    style Repetida fill:#fff3cd
    style Validada fill:#c8e6c9
    style Desvalidada fill:#ffcdd2
    style Antiga fill:#e3f2fd
```

---

## 🎯 Diagrama 9: Determinar Tipus Incorporació (sense canvis)

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
    CheckValidation -->|Abans SÍ, ara NO| Desvalidada[⬇️ DESVALIDA<br/>Segona validació]
    CheckValidation -->|Igual| Antiga[🕐 Antiga - Sense Canvis]
    
    Nova --> InsertNova[💾 INSERTAR<br/>composicio_mostra]
    
    InsertNova --> End([✅ Fi - Tipus Determinat i Nova Insertada])
    
    style Start fill:#e1f5e1
    style End fill:#e1f5e1
    style Nova fill:#c8e6c9
    style Repetida fill:#fff3cd
    style Validada fill:#c8e6c9
    style Desvalida
