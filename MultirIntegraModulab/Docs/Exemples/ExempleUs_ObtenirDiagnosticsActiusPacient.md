# 📋 Exemple d'ús: ObtenirDiagnosticsActiusPacient

## ✅ Exemple Bàsic

```csharp
using MultirIntegraModulab;
using MultirIntegraModulab.Domain.Interfaces;
using MultirIntegraModulab.Infrastructure.Persistence.Repositories;

// Crear el servei
var connectionString = ConfigurationManager.ConnectionStrings["MultiRMySQL"].ConnectionString;
var logger = new LoggerService();
var multiRDbService = new MultiRDbService(connectionString);
var multiRRepository = new MultiRRepository(multiRDbService, logger);

// Obtenir diagnòstics actius d'un pacient
string pacientSap = "12345678";
var diagnosticsActius = multiRRepository.ObtenirDiagnosticsActiusPacient(pacientSap);

Console.WriteLine($"\n📊 Diagnòstics actius del pacient {pacientSap}:");
Console.WriteLine($"═══════════════════════════════════════════════════════\n");

foreach (var diagnostic in diagnosticsActius)
{
    Console.WriteLine($"🦠 Diagnòstic {diagnostic.DiagnosticId}:");
    Console.WriteLine($"   Microorganisme: {diagnostic.Microorganisme}");
    
    if (!string.IsNullOrWhiteSpace(diagnostic.Mecanisme))
    {
        Console.WriteLine($"   Mecanisme: {diagnostic.Mecanisme}");
    }
    
    Console.WriteLine($"   Darrer positiu: {diagnostic.DataDarrerPositiu:dd/MM/yyyy}");
    Console.WriteLine($"   Tipus mostra: {diagnostic.DescripcioTipusMostra ?? diagnostic.TipusMostra}");
    
    // Comprovar si requereix nota al curs clínic
    bool requereixNota = diagnostic.MecanismeNotaCursClinic == true || 
                        diagnostic.MicroorganismeNotaCursClinic == true;
    
    if (requereixNota)
    {
        Console.WriteLine($"   ⚠️ Requereix nota al curs clínic:");
        if (diagnostic.MecanismeNotaCursClinic == true)
            Console.WriteLine($"      - Mecanisme {diagnostic.Mecanisme}");
        if (diagnostic.MicroorganismeNotaCursClinic == true)
            Console.WriteLine($"      - Microorganisme {diagnostic.Microorganisme}");
    }
    
    Console.WriteLine();
}

Console.WriteLine($"═══════════════════════════════════════════════════════");
Console.WriteLine($"Total: {diagnosticsActius.Count} diagnòstic(s) actiu(s)\n");
```

## 📤 Sortida Esperada

```
🔎 Obtenint diagnòstics actius del pacient 12345678
Trobats 3 diagnòstic(s) actiu(s) per al pacient 12345678
  • Diagnòstic 101: Staphylococcus aureus resistente a meticilina (Darrer positiu: 15/01/2025, Tipus: Frotis rectal)
  • Diagnòstic 102: Escherichia coli + BLEE (Darrer positiu: 10/01/2025, Tipus: Orina)
  • Diagnòstic 103: Klebsiella pneumoniae + KPC (Darrer positiu: 05/01/2025, Tipus: Sang)

📊 Diagnòstics actius del pacient 12345678:
═══════════════════════════════════════════════════════

🦠 Diagnòstic 101:
   Microorganisme: Staphylococcus aureus resistente a meticilina
   Darrer positiu: 15/01/2025
   Tipus mostra: Frotis rectal
   ⚠️ Requereix nota al curs clínic:
      - Microorganisme Staphylococcus aureus resistente a meticilina

🦠 Diagnòstic 102:
   Microorganisme: Escherichia coli
   Mecanisme: BLEE
   Darrer positiu: 10/01/2025
   Tipus mostra: Orina
   ⚠️ Requereix nota al curs clínic:
      - Mecanisme BLEE

🦠 Diagnòstic 103:
   Microorganisme: Klebsiella pneumoniae
   Mecanisme: KPC
   Darrer positiu: 05/01/2025
   Tipus mostra: Sang
   ⚠️ Requereix nota al curs clínic:
      - Mecanisme KPC

═══════════════════════════════════════════════════════
Total: 3 diagnòstic(s) actiu(s)
```

## 🎯 Cas d'ús: Vigilància Epidemiològica

```csharp
// Filtrar diagnòstics que requereixen nota clínica
var diagnosticsAmbNota = diagnosticsActius
    .Where(d => d.MecanismeNotaCursClinic == true || 
                d.MicroorganismeNotaCursClinic == true)
    .ToList();

if (diagnosticsAmbNota.Any())
{
    Console.WriteLine($"\n⚠️ ALERTA: {diagnosticsAmbNota.Count} diagnòstic(s) requereixen nota al curs clínic\n");
    
    foreach (var diagnostic in diagnosticsAmbNota)
    {
        Console.WriteLine($"  • {diagnostic.Microorganisme}");
        
        if (!string.IsNullOrWhiteSpace(diagnostic.Mecanisme))
        {
            Console.WriteLine($"    + {diagnostic.Mecanisme}");
        }
        
        var diesDesdeDarrerPositiu = (DateTime.Now - diagnostic.DataDarrerPositiu.Value).Days;
        Console.WriteLine($"    Darrer positiu fa {diesDesdeDarrerPositiu} dies\n");
    }
}
```

## 📅 Cas d'ús: Control de Vigència

```csharp
// Ordenar per data més recent i comprovar vigència
var diagnosticsOrdenats = diagnosticsActius
    .Where(d => d.DataDarrerPositiu.HasValue)
    .OrderByDescending(d => d.DataDarrerPositiu.Value)
    .ToList();

Console.WriteLine("\n📅 Control de vigència:\n");

foreach (var diagnostic in diagnosticsOrdenats)
{
    var diesDesdeDarrerPositiu = (DateTime.Now - diagnostic.DataDarrerPositiu.Value).Days;
    
    // Suposem vigència de 90 dies (això hauria de venir de la BD)
    int diesVigencia = 90;
    int diesRestants = diesVigencia - diesDesdeDarrerPositiu;
    
    Console.WriteLine($"  • {diagnostic.Microorganisme}");
    
    if (diesRestants > 0)
    {
        Console.WriteLine($"    ✅ Vigent - {diesRestants} dies restants");
    }
    else
    {
        Console.WriteLine($"    ⚠️ Caducat fa {Math.Abs(diesRestants)} dies");
        Console.WriteLine($"       → Suggerir nova mostra de control");
    }
    
    Console.WriteLine();
}
```

---

**Document creat**: Gener 2025  
**Versió**: 1.0  
**Fitxer**: ExempleUs_ObtenirDiagnosticsActiusPacient.md
