# ?? Diagrama 12: Model de Dades Simplificat (ACTUALITZAT)

```mermaid
erDiagram
    PACIENTS_DIAGNOSTICS ||--o{ PACIENTS_DIAGNOSTICS_MOSTRA : "té"
    PACIENTS_DIAGNOSTICS_MOSTRA ||--|| MOSTRA_MICROORGANISME : "genera"
    MOSTRA_MICROORGANISME ||--o{ MICRO_MECANISME_MOSTRA : "té MMR"
    
    MICROORGANISME ||--o{ MOSTRA_MICROORGANISME : "és"
    MICROORGANISME ||--o| MICRO_ESPECIAL : "pot ser"
    
    MECANISME_RESISTENCIA ||--o{ MICRO_MECANISME_MOSTRA : "és"
    MICROORGANISME ||--o{ MICRO_MECANISME_NOINCOPORAR : "té"
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
        comment "Només MMR"
    }
    
    MECANISME_RESISTENCIA {
        int id PK
        string codi UK
        string descripcio
        comment "Només per MMR"
    }
    
    MICRO_MECANISME_NOINCOPORAR {
        int id PK
        string microorganisme FK
        string mecanisme_resistencia FK
        comment "CNI - Només MMR"
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

## Descripció

Aquest diagrama mostra el **model de dades simplificat** de la base de dades **MySQL (MultiR)**.

### Taules Principals:

#### ?? **PACIENTS_DIAGNOSTICS**
- Pacients amb diagnòstics
- `npat`: Número de pacient (clau)
- `data_entrada`: Data primera inclusió
- `data_modificacio`: Data última modificació

#### ?? **PACIENTS_DIAGNOSTICS_MOSTRA**
- Mostres per pacient
- `etiqueta`: Identificador únic de la mostra
- `valoracio`: '0' (negatiu) o '2' (positiu)
- `vigent`: 'S' (vigent) o 'N' (no vigent)
- `tipus_mostra_m`: FK a TIPUSMOSTRA_M

#### ?? **MOSTRA_MICROORGANISME**
- Relació mostra-microorganisme
- **UK**: `etiqueta` (clau única)
- `data_resultat`: Data del resultat
- `data_validacio`: Data validació (nullable)
- `microorganisme`: FK a MICROORGANISME
- `id_prova`: FK a TIPUSPROVA_M

#### ??? **MICRO_MECANISME_MOSTRA**
- Mecanismes de resistència per mostra **MMR**
- `mecanisme_resistencia`: FK a MECANISME_RESISTENCIA
- **NULL per VR** (virus no tenen mecanismes)

### Taules de Referència:

#### ?? **MICROORGANISME**
- Catàleg de microorganismes (MMR + VR)
- `descripcio`: Nom del microorganisme (UK)
- `especial`: 0 (normal) o 1 (especial)

#### ? **MICRO_ESPECIAL**
- Llista de microorganismes especials (només MMR)
- Determina si un microorganisme és "especial"

#### ?? **MECANISME_RESISTENCIA**
- Catàleg de mecanismes de resistència (només MMR)
- `codi`: Codi del mecanisme (UK)

#### ? **MICRO_MECANISME_NOINCOPORAR (CNI)**
- Combinacions prohibides microorganisme + mecanisme
- Només per MMR
- Si es detecta ? Resultat eliminat

### Taules de Configuració:

#### ?? **TIPUSMOSTRA_M**
- Tipus de mostra
- `comportament`: 0/1 (determina comprovacions negatius)
- `dies_vigencia_positiu`: Dies de vigència per positius

#### ?? **TIPUSPROVA_M**
- Tipus de prova
- **`permet_vr`**: **NOU CAMP** - 0/1 (permet incorporar VR?)

#### ?? **PARAMETRES**
- Configuració general
- **`VR_CENTRES`**: **NOU** - Llista centres que permeten VR

### Taula d'Auditoria:

#### ?? **AUDITORIA_INTEGRACIO_MODULAB**
- Registre de totes les integracions
- `codi_retorn`: **OK**, **NPWS**, **CNI**, **NMRCM**, **TPNIVR**, **CNIVR**
- `data_integracio`: Timestamp del processament

### Novetats VR:

1. **TIPUSPROVA_M.permet_vr**: Nou camp per validar VR
2. **PARAMETRES.VR_CENTRES**: Nova configuració per centres VR
3. **MICRO_MECANISME_MOSTRA**: `mecanisme_resistencia = NULL` per VR
4. **AUDITORIA**: Nous codis **TPNIVR** i **CNIVR**

### Relacions Clau:

- Un **pacient** té múltiples **mostres**
- Una **mostra** té un **microorganisme**
- Un **microorganisme MMR** pot tenir múltiples **mecanismes** (1-5)
- Un **microorganisme VR** **NO té mecanismes** (NULL)
