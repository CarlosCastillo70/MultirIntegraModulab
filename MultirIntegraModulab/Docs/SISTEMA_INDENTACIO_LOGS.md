# 📋 Sistema d'Indentació Jeràrquica per Logs

## 🎯 Objectiu

Millorar la llegibilitat dels fitxers de log mitjançant un sistema d'indentació jeràrquica consistent que reflecteix l'estructura d'execució del codi.

## ✅ Implementació Completada

**Data**: Gener 2025  
**Estat**: ✅ Completada i validada  
**Build**: ✅ Exitosa  

---

## 📊 Estructura d'Indentació

### Nivells Definits

| Nivell | Indentació | Descripció | Ús |
|--------|-----------|------------|-----|
| **0 - Principal** | 0 espais | Missatges principals | Inici/final execució, separadors |
| **1 - UseCase** | 2 espais | Use Cases principals | Mètodes ExecutarAsync principals |
| **2 - Fase** | 4 espais | Fases de processament | Comprovacions, fases principals |
| **3 - Comprovacio** | 6 espais | Detalls de comprovacions | Operacions específiques |
| **4 - Operacio** | 8 espais | Operacions internes | Detalls tècnics |
| **5 - Detall** | 10 espais | Detalls molt específics | Rarament utilitzat |

### Constants Disponibles

```csharp
LogIndentHelper.Nivells.Principal      // 0
LogIndentHelper.Nivells.UseCase        // 1
LogIndentHelper.Nivells.Fase           // 2
LogIndentHelper.Nivells.Comprovacio    // 3
LogIndentHelper.Nivells.Operacio       // 4
LogIndentHelper.Nivells.Detall         // 5
```

---

## 🔧 Component Creat

### `LogIndentHelper.cs`

**Ubicació**: `MultirIntegraModulab/Application/Helpers/LogIndentHelper.cs`

**Funcionalitats**:
- `Indent(int nivell)`: Genera cadena d'espais segons nivell
- `Format(string missatge, int nivell)`: Afegeix indentació a un missatge
- `FormatLinies(string[] linies, int nivell)`: Indenta múltiples línies
- Classe estàtica `Nivells`: Constants per als nivells estàndard

---

## 📝 Fitxers Modificats

### 1. ProcessarMostresMultiplesUseCase.cs

**Canvis aplicats**:
- ✅ Afegit `using MultirIntegraModulab.Application.Helpers;`
- ✅ Eliminats espais manuals dels logs
- ✅ Aplicada indentació consistent amb `LogIndentHelper`

**Exemple abans**:
```csharp
_logger.Info($"  Total registres positius: {classificacio.ResultatsPositius}");
_logger.Info($"  Auditant {classificacio.ResultatsNegatius} registres negatius");
_logger.Info($"  ? Auditoria creada per registres negatius de mostra mixta");
```

**Exemple després**:
```csharp
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Total registres positius: {classificacio.ResultatsPositius}");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Auditant {classificacio.ResultatsNegatius} registres negatius");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✓ Auditoria creada per registres negatius de mostra mixta");
```

### 2. ProcessarMostraNegativaUseCase.cs

**Canvis aplicats**:
- ✅ Afegit `using MultirIntegraModulab.Application.Helpers;`
- ✅ Aplicada indentació jeràrquica a tots els nivells:
  - Nivell UseCase: Processament resultat negatiu
  - Nivell Fase: Comprovacions principals
  - Nivell Comprovacio: Detalls de comprovacions 0, 1, 2
  - Nivell Operacio: Incorporació de resultats negatius

**Exemple abans**:
```csharp
_logger.Info($"  Processant resultat negatiu: {microorganisme}");
_logger.Info($"  🔍 Comprovant si cal incorporar el negatiu per tipus mostra: {resultatMostra.MostraDescripcio}");
_logger.Info($"   Aplicant Comprovació 0: Verificant existència del pacient {mostra.PacientSap}");
_logger.Info($"   ✔️ Pacient {mostra.PacientSap} existeix a la taula de pacients");
_logger.Info($"  🔄 Incorporant el resultat negatiu al diagnòstic {diagnosticId}");
```

**Exemple després**:
```csharp
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Processant resultat negatiu: {microorganisme}");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}🔍 Comprovant si cal incorporar el negatiu per tipus mostra: {resultatMostra.MostraDescripcio}");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}Aplicant Comprovació 0: Verificant existència del pacient {mostra.PacientSap}");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Comprovacio)}✔️ Pacient {mostra.PacientSap} existeix a la taula de pacients");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Operacio)}🔄 Incorporant el resultat negatiu al diagnòstic {diagnosticId}");
```

### 3. ProcessarMostraPositivaUseCase.cs

**Canvis aplicats**:
- ✅ Afegit `using MultirIntegraModulab.Application.Helpers;`
- ✅ Aplicada indentació a ProcessarPacientAsync:
  - Nivell UseCase: Comprovant/creant pacient
  - Nivell Fase: Operacions principals
  - Nivell Comprovacio: Detalls
  - Nivell Operacio: Auditories
- ✅ Aplicada indentació a ProcessarResultatPositiu:
  - Nivell UseCase: Processant resultat
  - Nivell Fase: Operacions amb diagnòstics positius
  - Nivell Comprovacio: Creació mostres negatives
  - Nivell Operacio: Detalls per cada diagnòstic

**Exemple abans**:
```csharp
_logger.Info($"  🔎 Comprovant/creant pacient: {mostra.PacientSap}");
_logger.Warning($" ⚠️ Mostra {mostra.EtiquetaId} sense identificador de pacient");
_logger.Info($"  ✓ Pacient {mostra.PacientSap} ja existeix a MultiR");
_logger.Info($" ✔️ No hi ha altres diagnòstics positius per aquest pacient i tipus de mostra");
```

**Exemple després**:
```csharp
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}🔎 Comprovant/creant pacient: {mostra.PacientSap}");
_logger.Warning($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}⚠️ Mostra {mostra.EtiquetaId} sense identificador de pacient");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✓ Pacient {mostra.PacientSap} ja existeix a MultiR");
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.Fase)}✔️ No hi ha altres diagnòstics positius per aquest pacient i tipus de mostra");
```

---

## 📖 Exemple de Log Resultant

### ABANS (amb espais inconsistents):
```
2025-01-15 10:30:00 INFO : 🔄 Processant mostra amb múltiples resultats negatius: ETQ123456
2025-01-15 10:30:00 INFO :   Total registres negatius: 3
2025-01-15 10:30:00 INFO :   Processant resultat negatiu: E. coli
2025-01-15 10:30:00 INFO :   🔍 Comprovant si cal incorporar el negatiu per tipus mostra: Frotis rectal
2025-01-15 10:30:00 INFO :    Aplicant Comprovació 0: Verificant existència del pacient 12345678
2025-01-15 10:30:00 INFO :    ✔️ Pacient 12345678 existeix a la taula de pacients
2025-01-15 10:30:00 INFO :    Aplicant Comprovació 1: Positius vigents per qualsevol tipus de mostra
2025-01-15 10:30:00 INFO :    ✔️ Resultat negatiu CAL incorporar (via Comprovacio1)
2025-01-15 10:30:00 INFO :    Trobats 3 diagnòstics positius a neutralitzar
2025-01-15 10:30:00 INFO :   🔄 Incorporant el resultat negatiu al diagnòstic 42: E. coli + BLEE
```

### DESPRÉS (amb indentació jeràrquica):
```
2025-01-15 10:30:00 INFO : 🔄 Processant mostra amb múltiples resultats negatius: ETQ123456
2025-01-15 10:30:00 INFO :   Total registres negatius: 3
2025-01-15 10:30:00 INFO :   Processant resultat negatiu: E. coli
2025-01-15 10:30:00 INFO :     🔍 Comprovant si cal incorporar el negatiu per tipus mostra: Frotis rectal
2025-01-15 10:30:00 INFO :       Aplicant Comprovació 0: Verificant existència del pacient 12345678
2025-01-15 10:30:00 INFO :       ✔️ Pacient 12345678 existeix a la taula de pacients
2025-01-15 10:30:00 INFO :       Aplicant Comprovació 1: Positius vigents per qualsevol tipus de mostra
2025-01-15 10:30:00 INFO :       ✔️ Resultat negatiu CAL incorporar (via Comprovacio1)
2025-01-15 10:30:00 INFO :       Trobats 3 diagnòstics positius a neutralitzar
2025-01-15 10:30:00 INFO :         🔄 Incorporant el resultat negatiu al diagnòstic 42: E. coli + BLEE
```

---

## 💡 Guia d'Ús per Desenvolupadors

### 1. Importar el Helper

```csharp
using MultirIntegraModulab.Application.Helpers;
```

### 2. Utilitzar en Logs

**Forma bàsica**:
```csharp
_logger.Info($"{LogIndentHelper.Indent(LogIndentHelper.Nivells.UseCase)}Missatge de log");
```

**Amb variables**:
```csharp
int nivell = LogIndentHelper.Nivells.Fase;
_logger.Info($"{LogIndentHelper.Indent(nivell)}Processant {count} elements");
```

**Mètode alternatiu**:
```csharp
string missatge = LogIndentHelper.Format("Missatge de log", LogIndentHelper.Nivells.UseCase);
_logger.Info(missatge);
```

### 3. Escollir el Nivell Correcte

| Si estàs en... | Utilitza nivell... |
|----------------|-------------------|
| Mètode principal ExecutarAsync | `Nivells.Principal` o `Nivells.UseCase` |
| Mètode auxiliar (ProcessarPacient, etc.) | `Nivells.UseCase` |
| Fase de processament (Comprovacions) | `Nivells.Fase` |
| Detall de comprovació | `Nivells.Comprovacio` |
| Operació interna específica | `Nivells.Operacio` |
| Detall tècnic molt específic | `Nivells.Detall` |

---

## ✅ Avantatges

1. **Llegibilitat Millorada**: Jerarquia visual clara del flux d'execució
2. **Debugging Més Fàcil**: Identificar ràpidament on es produeix cada operació
3. **Consistència**: Tots els logs segueixen el mateix patró
4. **Mantenibilitat**: Fàcil ajustar nivells d'indentació si cal
5. **Escalabilitat**: Fàcil afegir nous nivells si es necessita més granularitat

---

## 🔄 Futura Evolució (Opcional)

### Opció Avançada: Context Automàtic

Si en el futur es vol automatitzar encara més, es pot modificar `ILoggerService` per tracking automàtic:

```csharp
public interface ILoggerService
{
    void Info(string missatge, int nivellIndentacio = 0);
    void AugmentarNivell();
    void ReducirNivell();
    int NivellActual { get; }
}
```

**Ús amb context automàtic**:
```csharp
_logger.AugmentarNivell();
_logger.Info("Processant resultat"); // Automàticament indentat
_logger.AugmentarNivell();
_logger.Info("Detall de processament"); // Més indentat
_logger.ReducirNivell();
_logger.ReducirNivell();
```

**Nota**: Aquesta funcionalitat no està implementada actualment per mantenir la simplicitat i evitar modificacions a la interfície de logging existent.

---

## 📊 Resum de la Implementació

| Aspecte | Detall |
|---------|--------|
| **Fitxers creats** | 1 (LogIndentHelper.cs) |
| **Fitxers modificats** | 3 (ProcessarMostres*UseCase.cs) |
| **Línies modificades** | ~50 logs actualitzats |
| **Build** | ✅ Exitosa |
| **Errors** | 0 |
| **Warnings** | 0 |
| **Temps implementació** | ~30 minuts |
| **Compatibilitat** | .NET Framework 4.8 ✅ |
| **Breaking changes** | Cap |

---

## 🎓 Best Practices

### ✅ Fer

- Utilitzar les constants predefinides (`Nivells.UseCase`, etc.)
- Mantenir consistència dins d'un mateix fitxer/mètode
- Incrementar la indentació quan s'entra en submètodes
- Utilitzar emojis per facilitar la identificació visual (🔄, ✔️, ⚠️, etc.)

### ❌ Evitar

- Crear nivells d'indentació personalitzats ad-hoc
- Saltar-se nivells (passar de 0 a 4 directament)
- Indentació excessiva (més de 10 espais)
- Espais manuals (`"  Missatge"`)

---

## 📞 Contacte

Per dubtes o suggeriments sobre el sistema d'indentació de logs:
- Revisar aquest document
- Consultar el codi font de `LogIndentHelper.cs`
- Revisar exemples en els fitxers ProcessarMostres*UseCase.cs

---

**Data creació**: Gener 2025  
**Última actualització**: Gener 2025  
**Versió**: 1.0.0  
**Estat**: ✅ Actiu i en ús  

🎉 **Sistema d'Indentació Implementat amb Èxit** 🎉
