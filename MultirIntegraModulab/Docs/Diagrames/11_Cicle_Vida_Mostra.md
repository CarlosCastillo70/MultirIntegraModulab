# ?? Diagrama 11: Cicle de Vida d'una Mostra (ACTUALITZAT amb VR)

```mermaid
stateDiagram-v2
    [*] --> Oracle: Resultat nou a Modulab
    
    Oracle --> Lectura: Query amb filtre dates
    
    Lectura --> Validacio: Col·lecció mostres
    
    Validacio --> DeterminarTipus: Mostra vàlida
    Validacio --> [*]: Mostra no vàlida
    
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
    
    DeterminarTipusMicro --> MMR: Només MMR
    DeterminarTipusMicro --> VR: Només VR
    DeterminarTipusMicro --> Mixta: MMR + VR
    
    MMR --> ClassificacioMMR: Classificar
    
    ClassificacioMMR --> Positiva: 1+ resultats positius
    ClassificacioMMR --> Negativa: 0 resultats positius
    ClassificacioMMR --> MixtaMMR: Positius i Negatius
    
    Positiva --> ProcessarPos: Processar
    Negativa --> ComprovacionsNeg: Check comprov. 1 i 2
    MixtaMMR --> ProcessarPos
    MixtaMMR --> ComprovacionsNeg
    
    ComprovacionsNeg --> Incorporar: Té positius
    ComprovacionsNeg --> NoIncorporar: No té positius
    
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
    MySQL_VR --> NotaVR: Crear nota curs clínic
    MySQL_Mixta --> AuditoriaMixta
    
    NotaVR --> AuditoriaOK
    
    AuditoriaOK --> [*]: Mostra processada
    AuditoriaNMRCM --> [*]: Negatiu no incorporat
    AuditoriaMixta --> [*]: Mixta processada
    AuditoriaTPNIVR --> [*]: VR tipus prova invalida
    AuditoriaCNIVR --> [*]: VR centre invalida
    
    note right of Oracle
        Sistema origen
        Resultats microbiològics
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
        - Nota curs clínic
    end note
    
    note right of AuditoriaNMRCM
        Negatiu sense
        positius vigents
        del pacient
    end note
```

## Descripció

Aquest diagrama mostra el **cicle de vida complet** d'una mostra des que arriba d'Oracle fins que es processa o es descarta.

### Estats Principals:

#### 1?? **Lectura i Validació**
- Lectura d'Oracle amb filtre de dates
- Validació de dades bàsiques
- Si no vàlida ? Descartada

#### 2?? **Determinació Tipus**
- **NOVA**: Primera vegada
- **REPETIDA**: Ja processada ? Skip
- **VALIDADA**: Primera validació ? Processar
- **REVALIDADA**: Revalidació ? Processar
- **ANTIGA**: Data anterior ? Auditoria només

#### 3?? **Comprovacions Inicials**
- Microorganismes (especials vs normals)
- Mecanismes (detecció CNI)
- Si tots CNI ? Mostra descartada

#### 4?? **Determinació Tipus Microorganisme**
- **MMR**: Només microorganismes amb mecanismes
- **VR**: Només virus respiratoris
- **Mixta**: Combinació MMR + VR

#### 5?? **Processament MMR**
- **Classificació**: Positiva / Negativa / Mixta
- **Positiva**: Sempre s'incorpora
- **Negativa**: Comprovacions 1 i 2
  - Si té positius vigents ? Incorporar
  - Si no té positius vigents ? NO incorporar (NMRCM)

#### 6?? **Processament VR**
- **Validació Tipus Prova**: Ha de permetre VR (TPNIVR)
- **Validació Centre**: Ha d'estar a VR_CENTRES (CNIVR)
- **Processament**: Sempre positiu, sense mecanismes
- **Nota Automàtica**: Creació al curs clínic

#### 7?? **Processament Mixta**
- Processament independent de part MMR i part VR
- Combinació de resultats finals
- Auditoria mixta

#### 8?? **Estats Finals**
- **Mostra processada** (OK)
- **Negatiu no incorporat** (NMRCM)
- **Mixta processada** (OK)
- **VR tipus prova invalida** (TPNIVR)
- **VR centre invalida** (CNIVR)
- **Mostra descartada** (CNI)

### Diferències MMR vs VR:

| Característica | MMR | VR |
|---|---|---|
| Mecanismes | Sí (1-5) | No (NULL) |
| Comprovacions Negatius | Sí (Comprov. 1 i 2) | No (sempre positius) |
| Validació Específica | No | Sí (Tipus Prova + Centre) |
| Nota Curs Clínic | No | Sí (automàtica) |
