# ?? Diagrama 9: Determinar Tipus Incorporació

```mermaid
flowchart TD
    Start([?? Determinar Tipus]) --> GetDatesOracle[?? Obtenir Dates Oracle<br/>DataResultat màx<br/>DataValidacio màx]
    
    GetDatesOracle --> QueryMySQL[?? Consultar MySQL<br/>mostra_microorganisme<br/>per etiqueta]
    
    QueryMySQL --> ExistsMySQL{Existeix a<br/>MySQL?}
    
    ExistsMySQL -->|NO| Nova[?? NOVA<br/>No existeix a destí]
    ExistsMySQL -->|SÍ| CompareDates[?? Comparar Dates]
    
    CompareDates --> SameDates{Dates<br/>Iguals?}
    
    SameDates -->|SÍ| Repetida[?? REPETIDA<br/>Mateix resultat]
    SameDates -->|NO| CheckValidation{Canvi en<br/>Validació?}
    
    CheckValidation -->|Abans NO, ara SÍ| Validada[? VALIDADA<br/>Primera validació]
    CheckValidation -->|Abans SÍ, ara NO| Desvalidada[?? DESVALIDADA<br/>S'ha desvalidat]
    CheckValidation -->|Ambdues SÍ, dates diferents| Revalidada[?? REVALIDADA<br/>Revalidació]
    CheckValidation -->|DataResultat anterior| Antiga[?? ANTIGA<br/>Data anterior a última]
    CheckValidation -->|DataResultat diferent| Canviada[?? CANVIADA<br/>Resultat modificat]
    
    Nova & Repetida & Validada & Desvalidada & Revalidada & Antiga & Canviada --> Result([? Tipus Determinat])
    
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

## Descripció

Aquest diagrama mostra el procés de **determinació del tipus d'incorporació** d'una mostra.

### Tipus d'Incorporació:

#### ?? **NOVA**
- La mostra **NO existeix** a MySQL
- És la **primera** vegada que es processa
- **Acció**: Processar normalment

#### ?? **REPETIDA**
- La mostra **existeix** amb les **mateixes dates**
- Mateix `data_resultat` i `data_validacio`
- **Acció**: **SKIP** (no processar)

#### ? **VALIDADA**
- La mostra existia **sense data validació**
- Ara **té data validació**
- És la **primera validació**
- **Acció**: Processar com a validada

#### ?? **DESVALIDADA**
- La mostra tenia **data validació**
- Ara **NO té data validació**
- S'ha **desvalidat** el resultat
- **Acció**: Processar com a desvalidada

#### ?? **REVALIDADA**
- La mostra tenia **data validació**
- Ara té **data validació diferent**
- **Acció**: Processar com a revalidada

#### ?? **ANTIGA**
- `data_resultat` és **anterior** a l'última registrada
- Resultat **fora d'ordre temporal**
- **Acció**: Registrar a auditoria, no processar

#### ?? **CANVIADA**
- `data_resultat` és **diferent** (no anterior)
- El resultat s'ha **modificat**
- **Acció**: Processar com a canviada

### Flux de Decisió:

1. **Consultar MySQL** per etiqueta
2. **Si NO existeix** ? **NOVA**
3. **Si existeix**:
   - Comparar dates `data_resultat` i `data_validacio`
   - **Dates iguals** ? **REPETIDA**
   - **Dates diferents** ? Analitzar canvi:
     - Canvi validació ? **VALIDADA / DESVALIDADA / REVALIDADA**
     - Canvi data resultat ? **ANTIGA / CANVIADA**

### Processament Posterior:

- **NOVA, VALIDADA, REVALIDADA** ? Processar completament
- **REPETIDA** ? Skip (no processar)
- **ANTIGA** ? Auditoria només
- **DESVALIDADA, CANVIADA** ? Processar segons lògica específica
