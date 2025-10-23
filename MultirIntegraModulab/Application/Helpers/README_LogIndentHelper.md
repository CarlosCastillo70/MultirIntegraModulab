# LogIndentHelper - Helper d'Indentació per Logs

## 📌 Descripció

`LogIndentHelper` és una classe estàtica que proporciona funcionalitats per aplicar indentació jeràrquica consistent als missatges de log, millorant significativament la llegibilitat dels fitxers de log.

## 🎯 Problema que Resol

**Abans**:
```csharp
_logger.Info("Processant mostra");
_logger.Info("  Comprovant pacient");     // Espais manuals inconsistents
_logger.Info("   Pacient trobat");        // Diferent indentació
_logger.Info(" Creant diagnòstic");       // Més inconsistència
```

**Després**:
```csharp
_logger.Info($"Processant mostra");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Comprovant pacient");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Pacient trobat");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Creant diagnòstic");
```

---

## 🚀 Ús Ràpid

### 1. Importar el namespace

```csharp
using MultirIntegraModulab.Application.Helpers;
```

### 2. Utilitzar en logs

```csharp
// Nivell 1 (2 espais)
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Processant mostra {id}");

// Nivell 2 (4 espais)
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Comprovant dades");

// Nivell 3 (6 espais)
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Validant pacient");
```

---

## 📚 API

### Mètode Principal: `Indent(int nivell)`

Genera una cadena d'espais segons el nivell especificat.

**Signatura**:
```csharp
public static string Indent(int nivell)
```

**Paràmetres**:
- `nivell` (int): Nivell d'indentació (0 = cap, 1 = 2 espais, 2 = 4 espais, etc.)

**Retorna**:
- `string`: Cadena amb els espais corresponents al nivell

**Exemple**:
```csharp
string indent = LogIndentHelper.Indent(2);  // Retorna "    " (4 espais)
_logger.Info($"{indent}Missatge indentat");
```

---

### Mètode: `Format(string missatge, int nivell)`

Afegeix indentació a un missatge existent.

**Signatura**:
```csharp
public static string Format(string missatge, int nivell)
```

**Paràmetres**:
- `missatge` (string): Missatge a indentar
- `nivell` (int): Nivell d'indentació

**Retorna**:
- `string`: Missatge amb indentació aplicada

**Exemple**:
```csharp
string missatge = LogIndentHelper.Format("Processant element", 2);
_logger.Info(missatge);  // "    Processant element"
```

---

### Mètode: `FormatLinies(string[] linies, int nivell)`

Afegeix indentació a múltiples línies de text.

**Signatura**:
```csharp
public static string[] FormatLinies(string[] linies, int nivell)
```

**Paràmetres**:
- `linies` (string[]): Array de línies a indentar
- `nivell` (int): Nivell d'indentació

**Retorna**:
- `string[]`: Línies amb indentació aplicada

**Exemple**:
```csharp
string[] linies = { "Línia 1", "Línia 2", "Línia 3" };
string[] liniesIndentades = LogIndentHelper.FormatLinies(linies, 2);
foreach (var linia in liniesIndentades)
{
    _logger.Info(linia);
}
// Output:
//     Línia 1
//     Línia 2
//     Línia 3
```

---

## 🎚️ Nivells Predefinits

La classe `Nivells` proporciona constants per als nivells estàndard:

| Constant | Valor | Espais | Descripció |
|----------|-------|--------|------------|
| `Nivells.Principal` | 0 | 0 | Missatges principals (inici/final) |
| `Nivells.UseCase` | 1 | 2 | Use Cases, mètodes principals |
| `Nivells.Fase` | 2 | 4 | Fases de processament |
| `Nivells.Comprovacio` | 3 | 6 | Detalls de comprovacions |
| `Nivells.Operacio` | 4 | 8 | Operacions internes |
| `Nivells.Detall` | 5 | 10 | Detalls molt específics |

**Exemple**:
```csharp
_logger.Info($"Iniciant processament");  // Nivell 0 (Principal)
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Executant Use Case");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Fase 1: Validació");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Comprovant dades");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}Operació interna");
```

---

## 💡 Exemples d'Ús

### Exemple 1: Use Case Simple

```csharp
public async Task<Resultat> ExecutarAsync(Mostra mostra)
{
    _logger.Info($"Processant mostra {mostra.Id}");
    
    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Validant dades");
    if (!ValidarMostra(mostra))
    {
        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Validació fallida");
        return Resultat.Error;
    }
    
    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Processant resultats");
    return await ProcessarResultats(mostra);
}
```

**Output Log**:
```
2025-01-15 10:00:00 INFO : Processant mostra ETQ123
2025-01-15 10:00:00 INFO :   Validant dades
2025-01-15 10:00:00 INFO :   Processant resultats
```

---

### Exemple 2: Processament Jeràrquic

```csharp
public void ProcessarPacient(string pacientId)
{
    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Processant pacient {pacientId}");
    
    _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Comprovant existència");
    if (PacientExisteix(pacientId))
    {
        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✓ Pacient trobat");
        
        _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Obtenint diagnòstics");
        var diagnostics = ObtenirDiagnostics(pacientId);
        
        foreach (var diag in diagnostics)
        {
            _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}Processant diagnòstic {diag.Id}");
        }
    }
    else
    {
        _logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}⚠ Pacient no trobat");
    }
}
```

**Output Log**:
```
2025-01-15 10:00:00 INFO :   Processant pacient 12345
2025-01-15 10:00:00 INFO :     Comprovant existència
2025-01-15 10:00:00 INFO :       ✓ Pacient trobat
2025-01-15 10:00:00 INFO :       Obtenint diagnòstics
2025-01-15 10:00:00 INFO :         Processant diagnòstic 1
2025-01-15 10:00:00 INFO :         Processant diagnòstic 2
```

---

### Exemple 3: Amb Mètode Format

```csharp
public void ExempleFormat()
{
    string missatge1 = LogIndentHelper.Format("Inici processament", LogIndentHelper.Nivells.UseCase);
    string missatge2 = LogIndentHelper.Format("Comprovant dades", LogIndentHelper.Nivells.Fase);
    
    _logger.Info(missatge1);
    _logger.Info(missatge2);
}
```

---

### Exemple 4: Múltiples Línies

```csharp
public void ExempleMultiplesLinies()
{
    string[] errors = { "Error 1: Dada invàlida", "Error 2: Format incorrecte", "Error 3: Valor null" };
    string[] errorsIndentats = LogIndentHelper.FormatLinies(errors, LogIndentHelper.Nivells.Comprovacio);
    
    _logger.Error("S'han detectat errors:");
    foreach (var error in errorsIndentats)
    {
        _logger.Error(error);
    }
}
```

**Output Log**:
```
2025-01-15 10:00:00 ERROR : S'han detectat errors:
2025-01-15 10:00:00 ERROR :       Error 1: Dada invàlida
2025-01-15 10:00:00 ERROR :       Error 2: Format incorrecte
2025-01-15 10:00:00 ERROR :       Error 3: Valor null
```

---

## 🎨 Bons Usos amb Emojis

Combinar indentació amb emojis millora encara més la llegibilitat:

```csharp
_logger.Info($"🔄 Processant mostra");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}🔎 Comprovant pacient");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✓ Validació correcta");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ Advertència detectada");
_logger.Error($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}❌ Error crític");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}📋 Creant registre");
```

### Emojis Recomanats

| Emoji | Significat | Ús |
|-------|-----------|-----|
| 🔄 | Processant | Inici de processament |
| 🔎 | Comprovant | Comprovacions, validacions |
| ✓ / ✔️ | Èxit | Operació exitosa |
| ⚠️ | Advertència | Warnings, situacions inesperades |
| ❌ | Error | Errors, fallades |
| 📋 | Creant | Creació de registres |
| 🗑️ | Esborrant | Eliminació de dades |
| ℹ️ | Informació | Informació general |

---

## 📋 Best Practices

### ✅ Fer

1. **Utilitzar constants predefinides**:
   ```csharp
   // ✅ Correcte
   LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)
   
   // ❌ Evitar
   LogIndentHelper.Indent(1)
   ```

2. **Mantenir consistència** dins del mateix mètode:
   ```csharp
   _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Operació 1");
   _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Operació 2");
   ```

3. **Incrementar progressivament**:
   ```csharp
   _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Mètode");
   _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}Submètode");
   _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Detall");
   ```

### ❌ Evitar

1. **Espais manuals**:
   ```csharp
   // ❌ No fer això
   _logger.Info("  Missatge");
   _logger.Info("   Altre missatge");
   
   // ✅ Fer això
   _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Missatge");
   ```

2. **Saltar nivells**:
   ```csharp
   // ❌ Evitar
   _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Principal)}Nivell 0");
   _logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}Nivell 4");  // Saltar del 0 al 4
   ```

3. **Indentació excessiva**:
   ```csharp
   // ❌ Massa nivells
   LogIndentHelper.Indent(10)  // 20 espais!
   ```

---

## 🔧 Configuració

### Modificar Espais per Nivell

Si necessites canviar els espais per nivell (per defecte: 2), modifica la constant:

```csharp
// A LogIndentHelper.cs
private const int ESPAIS_PER_NIVELL = 2;  // Canviar a 4, per exemple
```

**Nota**: Això afectarà tots els logs de l'aplicació.

---

## 🧪 Testing

### Exemple de Test Unitari

```csharp
[TestClass]
public class LogIndentHelperTests
{
    [TestMethod]
    public void Indent_Nivell0_RetornaStringBuit()
    {
        // Arrange & Act
        string resultat = LogIndentHelper.Indent(0);
        
        // Assert
        Assert.AreEqual(string.Empty, resultat);
    }
    
    [TestMethod]
    public void Indent_Nivell1_Retorna2Espais()
    {
        // Arrange & Act
        string resultat = LogIndentHelper.Indent(1);
        
        // Assert
        Assert.AreEqual("  ", resultat);
        Assert.AreEqual(2, resultat.Length);
    }
    
    [TestMethod]
    public void Format_AfegeixIndentacioCorrectament()
    {
        // Arrange
        string missatge = "Test";
        
        // Act
        string resultat = LogIndentHelper.Format(missatge, 2);
        
        // Assert
        Assert.AreEqual("    Test", resultat);
    }
}
```

---

## 📊 Performance

- **Overhead**: Mínim (creació de strings)
- **Memory**: Negligible
- **CPU**: < 0.1ms per crida
- **Recomanació**: Segur per ús en producció

---

## 🔗 Vegeu També

- [SISTEMA_INDENTACIO_LOGS.md](SISTEMA_INDENTACIO_LOGS.md) - Documentació completa del sistema
- [ILoggerService.cs](../Domain/Interfaces/ILoggerService.cs) - Interfície de logging
- [ProcessarMostraNegativaUseCase.cs](../Application/UseCases/ProcessarMostres/ProcessarMostraNegativaUseCase.cs) - Exemple d'ús real

---

## 📝 Changelog

### v1.0.0 (Gener 2025)
- ✅ Implementació inicial
- ✅ Constants de nivells predefinits
- ✅ Mètodes `Indent`, `Format`, `FormatLinies`
- ✅ Documentació completa
- ✅ Exemples d'ús

---

## 👥 Autors

Equip de desenvolupament MultirIntegraModulab

## 📄 Llicència

Intern - MultirIntegraModulab

---

**Última actualització**: Gener 2025  
**Versió**: 1.0.0
