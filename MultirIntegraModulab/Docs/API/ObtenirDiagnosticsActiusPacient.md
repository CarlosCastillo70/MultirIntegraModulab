# 📋 Funció ObtenirDiagnosticsActiusPacient

## 🎯 Objectiu

Obté tots els diagnòstics actius (vigents) d'un pacient amb informació del darrer positiu associat.

## 📖 Descripció

Aquesta funció retorna una llista dels diagnòstics actius d'un pacient, incloent:
- Informació del diagnòstic (microorganisme i mecanisme)
- Data del darrer positiu
- Tipus de mostra del darrer positiu
- Camps `nota_curs_clinic` dels mecanismes i microorganismes

## 🔧 Signatura

```csharp
List<DiagnosticActiuPacient> ObtenirDiagnosticsActiusPacient(string pacientSap)
```

### Paràmetres

| Paràmetre | Tipus | Descripció |
|-----------|-------|------------|
| `pacientSap` | string | Identificador del pacient (npat) |

### Retorna

Llista de `DiagnosticActiuPacient` amb els següents camps:

| Camp | Tipus | Descripció |
|------|-------|------------|
| `DiagnosticId` | int | ID del diagnòstic |
| `PacientSap` | string | Número de pacient |
| `Microorganisme` | string | Codi del microorganisme |
| `Mecanisme` | string | Codi del mecanisme (pot ser null) |
| `TipusMecanisme` | string | Tipus/descripció del mecanisme |
| `DataDiagnostic` | DateTime? | Data del diagnòstic |
| `DataDarrerPositiu` | DateTime? | Data del darrer positiu |
| `TipusMostra` | string | Codi del tipus de mostra |
| `DescripcioTipusMostra` | string | Descripció del tipus de mostra |
| `MecanismeNotaCursClinic` | bool? | Si el mecanisme requereix nota clínica |
| `MicroorganismeNotaCursClinic` | bool? | Si el microorganisme requereix nota clínica |

## 💻 Exemples d'Ús

### Exemple 1: Obtenir diagnòstics actius d'un pacient

```csharp
var diagnosticsActius = _multiRRepository.ObtenirDiagnosticsActiusPacient("12345678");

foreach (var diagnostic in diagnosticsActius)
{
    Console.WriteLine($"Diagnòstic {diagnostic.DiagnosticId}:");
    Console.WriteLine($"  Microorganisme: {diagnostic.Microorganisme}");
    
    if (!string.IsNullOrWhiteSpace(diagnostic.Mecanisme))
    {
        Console.WriteLine($"  Mecanisme: {diagnostic.Mecanisme}");
    }
    
    Console.WriteLine($"  Darrer positiu: {diagnostic.DataDarrerPositiu:dd/MM/yyyy}");
    Console.WriteLine($"  Tipus mostra: {diagnostic.DescripcioTipusMostra}");
    
    // Comprovar si requereix nota al curs clínic
    if (diagnostic.MecanismeNotaCursClinic == true || 
        diagnostic.MicroorganismeNotaCursClinic == true)
    {
        Console.WriteLine($"  ⚠️ Requereix nota al curs clínic");
    }
}
```

### Exemple 2: Filtrar diagnòstics que requereixen nota clínica

```csharp
var diagnosticsActius = _multiRRepository.ObtenirDiagnosticsActiusPacient("12345678");

var diagnosticsAmbNota = diagnosticsActius
    .Where(d => d.MecanismeNotaCursClinic == true || 
                d.MicroorganismeNotaCursClinic == true)
    .ToList();

Console.WriteLine($"Diagnòstics que requereixen nota clínica: {diagnosticsAmbNota.Count}");

foreach (var diagnostic in diagnosticsAmbNota)
{
    Console.WriteLine($"  • {diagnostic.Microorganisme}");
    
    if (diagnostic.MecanismeNotaCursClinic == true)
    {
        Console.WriteLine($"    → Mecanisme {diagnostic.Mecanisme} requereix nota");
    }
    
    if (diagnostic.MicroorganismeNotaCursClinic == true)
    {
        Console.WriteLine($"    → Microorganisme requereix nota");
    }
}
```

### Exemple 3: Ordenar per data del darrer positiu

```csharp
var diagnosticsActius = _multiRRepository.ObtenirDiagnosticsActiusPacient("12345678");

// Ordenar per data més recent primer
var diagnosticsOrdenats = diagnosticsActius
    .Where(d => d.DataDarrerPositiu.HasValue)
    .OrderByDescending(d => d.DataDarrerPositiu.Value)
    .ToList();

Console.WriteLine($"Diagnòstics ordenats per data més recent:");

foreach (var diagnostic in diagnosticsOrdenats)
{
    var diesDesdeDarrerPositiu = (DateTime.Now - diagnostic.DataDarrerPositiu.Value).Days;
    
    Console.WriteLine($"  • {diagnostic.Microorganisme}");
    Console.WriteLine($"    Darrer positiu fa {diesDesdeDarrerPositiu} dies");
}
```

### Exemple 4: Generar informe per pacient

```csharp
public class InformeDiagnosticsPacient
{
    public string PacientSap { get; set; }
    public DateTime DataInforme { get; set; }
    public int TotalDiagnosticsActius { get; set; }
    public int DiagnosticsAmbNotaCursClinic { get; set; }
    public List<DiagnosticActiuPacient> Diagnostics { get; set; }
}

public InformeDiagnosticsPacient GenerarInformePacient(string pacientSap)
{
    var diagnosticsActius = _multiRRepository.ObtenirDiagnosticsActiusPacient(pacientSap);
    
    var informe = new InformeDiagnosticsPacient
    {
        PacientSap = pacientSap,
        DataInforme = DateTime.Now,
        TotalDiagnosticsActius = diagnosticsActius.Count,
        DiagnosticsAmbNotaCursClinic = diagnosticsActius
            .Count(d => d.MecanismeNotaCursClinic == true || 
                       d.MicroorganismeNotaCursClinic == true),
        Diagnostics = diagnosticsActius
    };
    
    return informe;
}
```

## 📊 Query SQL Utilitzada

La funció executa la següent query SQL:

```sql
SELECT DISTINCT
    pd.id AS diagnostic_id,
    pd.npat,
    pd.microorganisme,
    pd.mecanisme,
    pd.tipus_mecanisme,
    pd.data_diagnostic,
    -- Darrer positiu associat
    MAX(pdm.data_mostra) AS data_darrer_positiu,
    pdm_darrer.tipus_mostra_m AS tipus_mostra,
    tm.descripcio AS descripcio_tipus_mostra,
    -- Camps nota_curs_clinic
    mec.nota_curs_clinic AS mecanisme_nota_curs_clinic,
    micro.nota_curs_clinic AS microorganisme_nota_curs_clinic
FROM pacients_diagnostics pd
    INNER JOIN mostra_microorganisme mm ON pd.id = mm.pacient_diagnostic_id
    INNER JOIN pacients_diagnostics_mostra pdm ON mm.pacient_diagnostic_mostra_id = pdm.id
    LEFT JOIN pacients_diagnostics_mostra pdm_darrer ON pdm_darrer.id = (
        SELECT pdm_sub.id
        FROM mostra_microorganisme mm_sub
            INNER JOIN pacients_diagnostics_mostra pdm_sub ON mm_sub.pacient_diagnostic_mostra_id = pdm_sub.id
        WHERE mm_sub.pacient_diagnostic_id = pd.id
          AND pdm_sub.valoracio = '2'
          AND pdm_sub.dt_delete IS NULL
        ORDER BY pdm_sub.data_mostra DESC
        LIMIT 1
    )
    LEFT JOIN tipusmostra_m tm ON pdm_darrer.tipus_mostra_m = tm.codi
    LEFT JOIN mecanismes mec ON pd.mecanisme = mec.codi AND mec.dt_delete IS NULL
    LEFT JOIN microorganismes micro ON pd.microorganisme = micro.codi AND micro.dt_delete IS NULL
WHERE pd.npat = @pacientSap
  AND pd.vigent = 'S'
  AND pd.dt_delete IS NULL
  AND pdm.valoracio = '2'
  AND pdm.dt_delete IS NULL
GROUP BY pd.id, pd.npat, pd.microorganisme, pd.mecanisme, pd.tipus_mecanisme, 
         pd.data_diagnostic, pdm_darrer.tipus_mostra_m, tm.descripcio,
         mec.nota_curs_clinic, micro.nota_curs_clinic
ORDER BY MAX(pdm.data_mostra) DESC
```

### Taules implicades

| Taula | Descripció |
|-------|------------|
| `pacients_diagnostics` | Diagnòstics dels pacients |
| `mostra_microorganisme` | Relació entre mostres i diagnòstics |
| `pacients_diagnostics_mostra` | Mostres diagnòstiques |
| `tipusmostra_m` | Tipus de mostres |
| `mecanismes` | Mecanismes de resistència |
| `microorganismes` | Microorganismes |

## 🔍 Detalls d'Implementació

### Filtres aplicats

- `pd.vigent = 'S'` - Només diagnòstics vigents
- `pd.dt_delete IS NULL` - No esborrats
- `pdm.valoracio = '2'` - Només mostres positives
- `pdm.dt_delete IS NULL` - Mostres no esborrades

### Ordenació

Els resultats es retornen ordenats per **data del darrer positiu més recent** (DESC).

### Gestió de NULLs

- `Mecanisme` pot ser NULL (microorganisme especial sense mecanisme)
- `MecanismeNotaCursClinic` pot ser NULL (no definit)
- `MicroorganismeNotaCursClinic` pot ser NULL (no definit)

## 📝 Logging

La funció genera els següents missatges de log:

```
🔎 Obtenint diagnòstics actius del pacient 12345678
Trobats 3 diagnòstic(s) actiu(s) per al pacient 12345678
  • Diagnòstic 123: Staphylococcus aureus resistente a meticilina (Darrer positiu: 15/01/2025, Tipus: Frotis rectal)
  • Diagnòstic 124: Escherichia coli + BLEE (Darrer positiu: 10/01/2025, Tipus: Orina)
  • Diagnòstic 125: Klebsiella pneumoniae + KPC (Darrer positiu: 05/01/2025, Tipus: Sang)
```

## ⚠️ Consideracions

1. **Rendiment**: La query utilitza subqueries i LEFT JOINS. Per a pacients amb molts diagnòstics, pot tenir un impacte en el rendiment.

2. **Vigència**: Només retorna diagnòstics amb `vigent = 'S'`. Els diagnòstics marcats com a no vigents no es retornen.

3. **Darrer positiu**: Es calcula com la mostra positiva (`valoracio = '2'`) més recent per cada diagnòstic.

4. **Dades incompletes**: Si un diagnòstic no té cap mostra positiva (situació anòmala), no es retornarà.

## 🎓 Casos d'Ús

### Vigilància Epidemiològica

```csharp
// Obtenir pacients amb microorganismes resistents
var diagnostics = _multiRRepository.ObtenirDiagnosticsActiusPacient(pacientSap);

var resistentsPreocupants = diagnostics
    .Where(d => d.MecanismeNotaCursClinic == true || 
                d.MicroorganismeNotaCursClinic == true)
    .ToList();

if (resistentsPreocupants.Any())
{
    // Enviar alerta a vigilància epidemiològica
    AlertarVigilanciaEpidemiologica(pacientSap, resistentsPreocupants);
}
```

### Seguiment Clínic

```csharp
// Generar informe per metge
var diagnostics = _multiRRepository.ObtenirDiagnosticsActiusPacient(pacientSap);

foreach (var diagnostic in diagnostics)
{
    var dies = (DateTime.Now - diagnostic.DataDarrerPositiu.Value).Days;
    
    if (dies > 30)
    {
        // Suggerir nova mostra de control
        Console.WriteLine($"⚠️ Sense mostres de control fa {dies} dies per {diagnostic.Microorganisme}");
    }
}
```

## 🔗 Funcions Relacionades

- `ObtenirDiagnosticsPositiusPacientPerTipusMostra` - Obté IDs de diagnòstics per tipus de mostra
- `ObtenirInformDiagnostic` - Obté informació detallada d'un diagnòstic concret
- `MarcarDiagnosticNoVigent` - Marca un diagnòstic com a no vigent
- `ReactivarDiagnostic` - Reactiva un diagnòstic

---

**Document creat**: Gener 2025  
**Versió**: 1.0  
**Autor**: Sistema MultirIntegraModulab
