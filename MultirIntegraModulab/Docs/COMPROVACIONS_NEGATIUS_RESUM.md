# 📋 Sistema Complet de Comprovacions - Mostres Negatives

## 🎯 Visió General

Aquest document descriu el sistema complet implementat per determinar si cal incorporar resultats negatius segons dues comprovacions específiques.

## 📖 Regla de Negoci

**Les mostres negatives, en principi, NO s'incorporen**, excepte quan es compleix **alguna de les dues comprovacions següents**:

### ✅ Comprovació 1: Comportament Global
**Tipus de mostra a incorporar sempre que el pacient tingui algun positiu**

- Aplica a tipus de mostra amb `comportament = 1`
- Comprova si el pacient té **algun positiu per qualsevol tipus de mostra**
- Si es compleix → **Incorporar el negatiu**

**Exemples de tipus amb comportament 1**:
- Frotis rectal
- Exsudat per faringui/amígdales  
- Exsudat per nasal
- Exsudat per axil·lar

### ✅ Comprovació 2: Positius Vigents Específics
**Tipus de mostra a incorporar si el pacient té positius vigents per aquest tipus o equivalents**

- Aplica quan `comportament ≠ 1`
- Comprova si el pacient té **positius vigents per aquest tipus de mostra o equivalents**
- Un positiu és vigent si no ha superat `dies_vigencia_positiu`
- Si es compleix → **Incorporar el negatiu**

## 🔄 Flux de Decisió Complet

```
┌───────────────────────────────────────────────┐
│        Resultat Negatiu a Processar          │
└────────────────┬──────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────┐
│ 1. Obtenir comportament del tipus de mostra    │
└────────────────┬────────────────────────────────┘
                 │
                 ▼
         ┌───────────────┐
         │ Comportament  │
         │    == 1?      │
         └───┬───────┬───┘
            Sí      No
             │       │
             │       │
             ▼       │
    ┌────────────────────┐
    │  COMPROVACIÓ 1     │
    │                    │
    │ Pacient té positius│
    │ (qualsevol tipus)? │
    └────┬──────┬────────┘
        Sí     No
         │      │
         │      │
         │      ▼
         │   ┌──────────────────────┐
         │   │   COMPROVACIÓ 2      │
         │   │                      │
         │   │ Pacient té positius  │◄──────┐
         │   │ vigents per aquest   │       │
         │   │ tipus o equivalents? │       │
         │   └────┬──────┬──────────┘       │
         │       Sí     No                  │
         │        │      │                  │
         ▼        ▼      ▼                  │
    ┌─────┐  ┌─────┐  ┌────────────┐       │
    │INCOR│  │INCOR│  │NO INCORPOR │       │
    │Compr│  │Compr│  │  (NMRCM)   │       │
    │  1  │  │  2  │  └────────────┘       │
    └─────┘  └─────┘                       │
       │        │                           │
       └────┬───┘                           │
            │                               │
            ▼                               │
    ┌──────────────────┐                   │
    │ Processar mostra │                   │
    │ com a positiva   │                   │
    └──────────────────┘                   │
```

## 🗂️ Taules de la Base de Dades

### tipusmostra_m
```sql
CREATE TABLE tipusmostra_m (
    id INT PRIMARY KEY,
    codi VARCHAR(100),
    descripcio VARCHAR(255),
    comportament INT,               -- 0, 1, etc.
    dies_vigencia_positiu INT,      -- Dies que un positiu és vigent
    actiu TINYINT(1),
    dt_delete DATETIME
);
```

### tipusmostra_equivalents
```sql
CREATE TABLE tipusmostra_equivalents (
    id INT PRIMARY KEY,
    tipusmostra_id INT,                  -- FK a tipusmostra_m.id
    tipusmostra_id_equivalent INT,       -- FK a tipusmostra_m.id
    FOREIGN KEY (tipusmostra_id) REFERENCES tipusmostra_m(id),
    FOREIGN KEY (tipusmostra_id_equivalent) REFERENCES tipusmostra_m(id)
);
```

### pacients_diagnostics_mostra
```sql
CREATE TABLE pacients_diagnostics_mostra (
    id INT PRIMARY KEY,
    npat VARCHAR(50),                    -- Identificador pacient
    tipus_mostra_m VARCHAR(100),         -- FK a tipusmostra_m.codi
    data_mostra DATE,                    -- Per calcular vigència
    valoracio CHAR(1),                   -- '2' = Positiu
    dt_delete DATETIME
);
```

## 📊 Consultes SQL Implementades

### Comprovació 1: Comportament del tipus de mostra
```sql
SELECT comportament  
FROM tipusmostra_m 
WHERE UPPER(codi) = UPPER(@codiMostra) 
  AND dt_delete IS NULL 
  AND actiu = 1;
```

### Comprovació 1: Positius generals del pacient
```sql
SELECT COUNT(*) AS positius_algun_tipus_mostra  
FROM pacients_diagnostics_mostra pdm 
JOIN tipusmostra_m tm ON pdm.tipus_mostra_m = tm.codi 
WHERE pdm.npat = @pacientSap 
  AND pdm.valoracio = '2'
  AND pdm.dt_delete IS NULL  
  AND tm.dt_delete IS NULL;
```

### Comprovació 2: Positius vigents per tipus + equivalents
```sql
SELECT COUNT(*) AS positius_vigents_tipus_mostra_i_equivalents 
FROM pacients_diagnostics_mostra pdm		 
JOIN tipusmostra_m tm ON pdm.tipus_mostra_m = tm.descripcio 		 
WHERE pdm.npat = @pacientSap
  AND ( 
    -- Mateix tipus de mostra
    UPPER(tm.descripcio) = UPPER(@tipusMostra) 
    OR 
    -- O tipus equivalent
    tm.id IN ( 
        SELECT tipusmostra_id_equivalent 
        FROM tipusmostra_equivalents 
        WHERE tipusmostra_id = ( 
            SELECT id  
            FROM tipusmostra_m tmm  
            WHERE UPPER(tmm.descripcio) = UPPER(@tipusMostra) 
        ) 
    ) 
  ) 
  AND pdm.valoracio = '2'  -- Positiu
  AND ( 
    -- Sense límit de vigència o dins del període vigent
    tm.dies_vigencia_positiu IS NULL 
    OR pdm.data_mostra >= DATE_SUB(CURRENT_DATE, INTERVAL tm.dies_vigencia_positiu DAY) 
  ) 
  AND pdm.dt_delete IS NULL 
  AND tm.dt_delete IS NULL;
```

## 🎯 Matriu de Decisions Completa

| Comportament | Positius<br/>generals | Positius vigents<br/>tipus/equiv | **Decisió** | **Via** | **Codi<br/>Audit** |
|:------------:|:---------------------:|:--------------------------------:|:-----------:|:-------:|:------------------:|
| **1** | ✅ Sí | - | ✅ **Incorporar** | Comprovació 1 | **OK** |
| **1** | ❌ No | ✅ Sí | ✅ **Incorporar** | Comprovació 2 | **OK** |
| **1** | ❌ No | ❌ No | ❌ No incorporar | - | **NMRCM** |
| **0** | - | ✅ Sí | ✅ **Incorporar** | Comprovació 2 | **OK** |
| **0** | - | ❌ No | ❌ No incorporar | - | **NMRCM** |
| **null** | - | ✅ Sí | ✅ **Incorporar** | Comprovació 2 | **OK** |
| **null** | - | ❌ No | ❌ No incorporar | - | **NMRCM** |

## 💾 Estructura de Dades

### Enum TipusComprovacioNegatiu
```csharp
public enum TipusComprovacioNegatiu
{
    /// <summary>
    /// No cal incorporar el negatiu
    /// </summary>
    Cap = 0,
    
    /// <summary>
    /// Comprovació 1: Tipus de mostra amb comportament 1 i pacient amb positius
    /// </summary>
    Comprovacio1 = 1,
    
    /// <summary>
    /// Comprovació 2: Pacient amb positius vigents per aquest tipus de mostra o equivalents
    /// </summary>
    Comprovacio2 = 2
}
```

### ResultatProcessamentNegatiu
```csharp
public class ResultatProcessamentNegatiu
{
    // Indicadors generals
    public bool Exitosa { get; set; }
    public string Missatge { get; set; }
    
    // Comptadors de processament
    public int DiagnosticsCreats { get; set; }
    public int DiagnosticsExistents { get; set; }
    public int MostresDiagnosticCreades { get; set; }
    public int MostresDiagnosticExistents { get; set; }
    public int RelacionsCreades { get; set; }
    public int RelacionsDuplicades { get; set; }
    public int ResultatsProcessats { get; set; }
    public int IntegracionsCreades { get; set; }
    public int AuditoriasCreades { get; set; }
    
    // Comptadors específics de negatius
    public int ResultatsNoIncorporats { get; set; }
    public int IncorporatsPerComprovacio1 { get; set; }  // ← NOU
    public int IncorporatsPerComprovacio2 { get; set; }  // ← NOU
}
```

## 📊 Exemples de Logging

### Cas 1: Incorporar via Comprovació 1
```
  Processant resultat negatiu: sense microorganisme
  🔍 Comprovant si cal incorporar el negatiu per tipus mostra: Frotis rectal
  ℹ️ Tipus de mostra amb comportament 1 (incorporar si el pacient té positius)
  Pacient 12345678 té 3 diagnòstic(s) positiu(s)
  ✓ Comprovació 1 COMPLERTA: Pacient té positius previs → Cal incorporar el negatiu
  ✓ Resultat negatiu CAL incorporar (via Comprovacio1), processant...
```

### Cas 2: Incorporar via Comprovació 2
```
  Processant resultat negatiu: sense microorganisme
  🔍 Comprovant si cal incorporar el negatiu per tipus mostra: Sang
  ℹ️ Tipus de mostra amb comportament 0 (no aplica comprovació 1)
  🔍 Aplicant Comprovació 2: Positius vigents per aquest tipus de mostra o equivalents
  Pacient 12345678 té 2 positiu(s) vigent(s) per tipus mostra 'Sang' o equivalents
  ✓ Comprovació 2 COMPLERTA: Pacient té positius vigents → Cal incorporar el negatiu
  ✓ Resultat negatiu CAL incorporar (via Comprovacio2), processant...
```

### Cas 3: No incorporar (cap comprovació passada)
```
  Processant resultat negatiu: sense microorganisme
  🔍 Comprovant si cal incorporar el negatiu per tipus mostra: Orina
  ℹ️ Tipus de mostra amb comportament 0 (no aplica comprovació 1)
  🔍 Aplicant Comprovació 2: Positius vigents per aquest tipus de mostra o equivalents
  Pacient 12345678 NO té positius vigents per tipus mostra 'Orina' o equivalents
  ℹ️ Resultat negatiu NO cal incorporar segons comprovacions
  ✓ Auditoria NMRCM creada per mostra ETQ123456
```

### Resum final
```
Mostra negativa ETQ123456 processada correctament: 
  3 diagnòstics creats, 2 diagnòstics existents, 
  4 mostres creades, 1 mostres existents, 
  5 relacions creades, 1 duplicades, 
  5 resultats processats, 2 no incorporats, 
  2 incorporats per comprovació 1, 
  1 incorporats per comprovació 2, 
  8 auditories
```

## 🏷️ Codis d'Auditoria

| Codi | Descripció | Quan s'utilitza |
|------|------------|-----------------|
| **NMRCM** | No supera la comprovació de mostra | Cap de les dues comprovacions ha passat |
| **OK** | Processament correcte | Almenys una comprovació ha passat i s'ha incorporat |
| **DMM** | Duplicat Mostra Microorganisme | Ja existia la relació mostra-microorganisme |

## 📈 Mètriques i Anàlisi

El sistema proporciona mètriques detallades per analitzar l'eficàcia de cada comprovació:

```csharp
// Exemple d'anàlisi
var totalIncorporats = resultat.IncorporatsPerComprovacio1 + 
                      resultat.IncorporatsPerComprovacio2;

var percentatgeCompr1 = (resultat.IncorporatsPerComprovacio1 * 100.0) / totalIncorporats;
var percentatgeCompr2 = (resultat.IncorporatsPerComprovacio2 * 100.0) / totalIncorporats;

Console.WriteLine($"Comprovació 1: {percentatgeCompr1:F1}%");
Console.WriteLine($"Comprovació 2: {percentatgeCompr2:F1}%");
```

## 🔧 API del Repositori

### Mètodes Implementats

```csharp
// Obtenir comportament d'un tipus de mostra
int? ObtenirComportamentTipusMostra(string codiMostra);

// Comprovar positius generals del pacient (Comprovació 1)
bool PacientTePositiusAlgunTipusMostra(string pacientSap);

// Comprovar positius vigents per tipus + equivalents (Comprovació 2)
bool PacientTePositiusVigentsTipusMostraIEquivalents(
    string pacientSap, 
    string tipusMostra);
```

## 🎓 Casos d'Ús Pràctics

### Exemple 1: Vigilància de bacteris resistents
- Pacient amb MRSA (Staphylococcus aureus resistent) detectat fa 3 mesos
- Nova mostra de frotis nasal **negativa**
- **Comprovació 1**: Frotis nasal té `comportament = 1` i pacient té MRSA → ✅ **Incorporar**
- **Motiu**: Cal fer seguiment de la descolonització

### Exemple 2: Seguiment específic per tipus
- Pacient amb infecció urinària per E. coli (fa 2 mesos, vigent)
- Nova mostra d'orina **negativa**
- **Comprovació 1**: Orina té `comportament = 0` → No aplica
- **Comprovació 2**: Té positiu vigent per 'Orina' → ✅ **Incorporar**
- **Motiu**: Cal documentar la resolució de la infecció

### Exemple 3: No incorporar
- Pacient sense cap positiu previ
- Nova mostra de sang **negativa**
- **Comprovació 1**: Sang té `comportament = 0` → No aplica
- **Comprovació 2**: No té positius vigents per 'Sang' → ❌ **No incorporar**
- **Motiu**: No té interès clínic registrar aquest negatiu

## ✅ Avantatges del Sistema

1. **📊 Traçabilitat total**: Cada decisió queda registrada amb el seu motiu
2. **📈 Mètriques detallades**: Permet analitzar l'eficàcia de cada comprovació
3. **🔍 Logging complet**: Cada pas és visible per a debugging i auditoria
4. **⚡ Optimització**: Comprovació 1 té prioritat (més ràpida)
5. **🎯 Flexibilitat**: Tipus equivalents permeten agrupar mostres relacionades
6. **🔐 Integritat**: Respecta soft deletes i filtra registres no actius
7. **📝 Mantenibilitat**: Codi net, estructurat i documentat

## 🔜 Possibles Extensions Futures

1. **Comprovació 3**: Altres criteris clínics
2. **Configuració dinàmica**: Comportaments configurables des de BD
3. **Alertes automàtiques**: Notificar quan un negatiu s'incorpora
4. **Dashboard**: Visualització de mètriques en temps real
5. **Export de dades**: Generació d'informes per a epidemiologia

## 📚 Documentació Relacionada

- [COMPROVACIO_1_NEGATIUS.md](COMPROVACIO_1_NEGATIUS.md) - Detalls de la Comprovació 1
- [COMPROVACIO_2_NEGATIUS.md](COMPROVACIO_2_NEGATIUS.md) - Detalls de la Comprovació 2
- [RESUM_FINAL_CLEAN_ARCHITECTURE.md](RESUM_FINAL_CLEAN_ARCHITECTURE.md) - Arquitectura general

## 🧪 Tests Recomanats

### Tests Unitaris

```csharp
[Test]
public void Comprovacio1_ComportamentIs1_PacientAmbPositius_Incorpora()
{
    // Arrange: Tipus mostra comportament=1, pacient amb positius
    // Act: Processar resultat negatiu
    // Assert: calIncorporarNegatiu == true, tipusComprovacio == Comprovacio1
}

[Test]
public void Comprovacio2_PositiusVigents_Incorpora()
{
    // Arrange: Tipus mostra comportament=0, pacient amb positius vigents
    // Act: Processar resultat negatiu
    // Assert: calIncorporarNegatiu == true, tipusComprovacio == Comprovacio2
}

[Test]
public void CapComprovacio_NoIncorpora()
{
    // Arrange: Cap condició es compleix
    // Act: Processar resultat negatiu
    // Assert: calIncorporarNegatiu == false, codi auditoria == NMRCM
}
```

---

**Data d'implementació**: Gener 2025  
**Estat**: ✅ Completat i validat  
**Versió**: 1.0.0  
**Autors**: Equip de desenvolupament MultirIntegraModulab
