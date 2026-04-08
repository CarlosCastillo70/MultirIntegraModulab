# ?? Diagrama 5: Comprovar Microorganismes

```mermaid
flowchart TD
    Start([?? Llista Microorganismes]) --> GetUnique[Obtenir<br/>Microorganismes Únics]
    
    GetUnique --> Loop{Per cada<br/>Microorganisme}
    
    Loop -->|Següent| CheckExists{Existeix a BD?}
    
    CheckExists -->|NO| Create[?? CREAR<br/>microorganisme<br/>especial=0]
    CheckExists -->|SÍ| Exists[? Ja existeix]
    
    Create --> CheckSpecialTable
    Exists --> CheckSpecialTable{Existeix a<br/>micro_especial?}
    
    CheckSpecialTable -->|SÍ| MarkSpecial[? Marcar com<br/>ESPECIAL=1]
    CheckSpecialTable -->|NO| MarkNormal[? Marcar com<br/>ESPECIAL=0]
    
    MarkSpecial & MarkNormal --> Store[?? Guardar a<br/>Diccionari Resultats]
    
    Store --> More{Més<br/>Microorganismes?}
    
    More -->|SÍ| Loop
    More -->|NO| Result([? Resultat:<br/>Dictionary microorg, especial])
    
    style Start fill:#e1f5e1
    style Result fill:#e1f5e1
    style Create fill:#c8e6c9
    style MarkSpecial fill:#ffe4b3
    style MarkNormal fill:#e3f2fd
```

## Descripció

Aquest diagrama mostra el procés de **comprovació i marcatge de microorganismes**.

### Flux de Comprovació:

1. **Obtenir microorganismes únics** de la llista
2. Per cada microorganisme:
   - **Comprovar si existeix** a la taula `microorganisme`
   - Si **NO existeix**: Crear amb `especial = 0`
   - Si **SÍ existeix**: Continuar
3. **Comprovar si és especial**:
   - Consultar taula `micro_especial`
   - Si existeix ? Marcar `especial = 1`
   - Si no existeix ? Marcar `especial = 0`
4. **Guardar en diccionari** de resultats
5. **Retornar** diccionari complet: `{ microorganisme: especial }`

### Taules Implicades:

- **`microorganisme`**: Taula principal de microorganismes
  - `id` (PK)
  - `descripcio` (UK)
  - `especial` (tinyint: 0/1)

- **`micro_especial`**: Llista de microorganismes especials
  - `id` (PK)
  - `microorganisme` (UK)

### Ús del Resultat:

El diccionari retornat s'utilitza posteriorment per:
- **Classificar** resultats com a positius o negatius
- **Determinar** si cal crear registres de diagnòstic
- **Aplicar lògica** específica segons el tipus de microorganisme
