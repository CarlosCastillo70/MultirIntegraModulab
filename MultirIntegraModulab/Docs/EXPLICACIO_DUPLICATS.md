# ?? Explicació dels Duplicats d'Entitats

## ? **Pregunta de l'Usuari**

> "Veig classes, per exemple `ResultatProva`, que està al directori arrel i a `Domain/Entities`. Per quin motiu?"

---

## ?? **Resposta Directa**

Els duplicats existeixen perquè durant la **migració a Clean Architecture** es van crear versions noves de les entitats al namespace `MultirIntegraModulab.Domain.Entities`, **PERÒ no s'han eliminat les versions originals** del namespace arrel `MultirIntegraModulab`.

Això és una **situació temporal** durant el procés de migració per mantenir la compatibilitat amb el codi legacy.

---

## ?? **Duplicats Identificats**

S'han identificat **4 duplicats d'entitats**:

| Entitat | Versió Legacy (Arrel) | Versió Clean Architecture | Estat |
|---------|------------------------|---------------------------|-------|
| `ResultatProva.cs` | `MultirIntegraModulab/` | `Domain/Entities/` | ?? Marcat obsolet |
| `ResultatProvaRegistre.cs` | `MultirIntegraModulab/` | `Domain/Entities/` | ?? Marcat obsolet |
| `DiagnosticExistent.cs` | `MultirIntegraModulab/` | `Domain/Entities/` | ?? Marcat obsolet |
| `ModelClasses.cs` | `MultirIntegraModulab/` | `Domain/Enums/` | ?? Marcat obsolet |

**Nota**: Les entitats `Microorganisme` i `ColeccioResultatsMostres` **NO estan duplicades**, només existeixen a `Domain/Entities/`.

---

## ?? **Per Què Existeixen els Duplicats?**

### **Cronologia del Problema:**

1. **Codi Original (Legacy)**
   ```
   MultirIntegraModulab/
   ??? ResultatProva.cs              ? Namespace: MultirIntegraModulab
   ??? ResultatProvaRegistre.cs      ? Namespace: MultirIntegraModulab
   ??? DiagnosticExistent.cs         ? Namespace: MultirIntegraModulab
   ??? ModelClasses.cs               ? Namespace: MultirIntegraModulab
   ```

2. **Migració a Clean Architecture**
   ```
   MultirIntegraModulab/
   ??? Domain/
   ?   ??? Entities/
   ?   ?   ??? ResultatProva.cs              ? NOU: Namespace: Domain.Entities
   ?   ?   ??? ResultatProvaRegistre.cs      ? NOU: Namespace: Domain.Entities
   ?   ?   ??? DiagnosticExistent.cs         ? NOU: Namespace: Domain.Entities
   ?   ??? Enums/
   ?       ??? ModelClasses.cs               ? NOU: Namespace: Domain.Enums
   ```

3. **Resultat: Duplicació**
   - Les versions noves es creen a `Domain/`
   - **PERÒ**: Les versions legacy NO s'eliminen per compatibilitat
   - El codi legacy continua utilitzant les versions de l'arrel
   - El codi Clean Architecture utilitza les versions de `Domain/`

---

## ?? **Per Què NO S'Han Eliminat?**

### **Raons per Mantenir Ambdues Versions Temporalment:**

1. **Compatibilitat amb Codi Legacy** ??
   - `TractamentResultats.cs` (940 línies) necessita les versions arrel
   - `Processadors/*.cs` (10 fitxers) utilitzen el namespace arrel
   - `MultiRDbService.cs` compartit entre legacy i clean

2. **Migració Gradual** ??
   - Permet migrar pas a pas sense trencar funcionalitat
   - Reduce el risc de errors massius
   - Facilita testing progressiu

3. **Convivència Segura** ?
   - Codi legacy funciona sense canvis
   - Clean Architecture té la seva pròpia versió neta
   - Build exitós sense errors

---

## ?? **Com es Gestionen els Conflictes?**

Quan un fitxer necessita utilitzar ambdós namespaces, s'utilitza **using alias**:

### **Exemple de Conflicte:**

```csharp
// PROBLEMA: Ambigüitat
using MultirIntegraModulab;              // Legacy
using MultirIntegraModulab.Domain.Entities;  // Clean

var mostra = new ResultatProva();  // ? Quin ResultatProva?
```

### **Solució: Using Alias**

```csharp
using MultirIntegraModulab;
using ResultatProva = MultirIntegraModulab.Domain.Entities.ResultatProva;

var mostra = new ResultatProva();  // ? Utilitza Domain.Entities
```

---

## ?? **Advertències Afegides als Fitxers Legacy**

Tots els fitxers duplicats legacy han estat **marcats amb advertències** al principi:

```csharp
// ?????? ADVERTÈNCIA: AQUEST FITXER ÉS LEGACY ??????
//
// Aquest fitxer es mantindrà temporalment per compatibilitat amb el codi legacy
// (TractamentResultats.cs, Processadors/, etc.)
//
// ? PER NOU CODI, UTILITZAR:
//    MultirIntegraModulab.Domain.Entities.ResultatProva
//
// ?? Aquest fitxer s'eliminarà després de completar la migració a Clean Architecture
// ?? Més informació: PLA_NETEJA_DUPLICATS.md
```

---

## ?? **Pla d'Eliminació Futur**

Els duplicats s'eliminaran en **3 fases**:

### **Fase 1: Validació** (1-3 mesos)
- [ ] Migrar `Program.cs` a Clean Architecture
- [ ] Tests exhaustius del sistema
- [ ] Validar funcionalitat completa

### **Fase 2: Deprecació** (3-6 mesos)
- [ ] Afegir atribut `[Obsolete]` a classes legacy
- [ ] Warnings de compilació per ús de legacy
- [ ] Actualitzar `MultiRDbService` per utilitzar Domain

### **Fase 3: Eliminació** (6+ mesos)
- [ ] Eliminar fitxers legacy de l'arrel
- [ ] Eliminar `TractamentResultats.cs`
- [ ] Eliminar `Processadors/` complet
- [ ] Build net només amb Clean Architecture

---

## ? **Què S'Ha Fet?**

1. ? **Identificat** tots els duplicats (4 fitxers)
2. ? **Marcat** els fitxers legacy com obsolets
3. ? **Documentat** el pla de neteja ([PLA_NETEJA_DUPLICATS.md](PLA_NETEJA_DUPLICATS.md))
4. ? **Creat** resum executiu ([RESUM_NETEJA_DUPLICATS.md](RESUM_NETEJA_DUPLICATS.md))
5. ? **Verificat** que el build compila sense errors

---

## ?? **Lliçons Apreses**

### **Per Què és Acceptable la Duplicació Temporal?**

- ? **Menys risc** que una migració forçada
- ? **Codi legacy funcional** sense canvis
- ? **Clean Architecture independent** i net
- ? **Migració gradual** ben documentada
- ? **Build exitós** sense errors

### **Bones Pràctiques Aplicades:**

1. **Marcar clarament** el codi legacy com obsolet
2. **Documentar** per què existeixen duplicats
3. **Planificar** l'eliminació futura
4. **Using alias** per resoldre conflictes
5. **Convivència temporal** controlada i documentada

---

## ?? **Documentació Relacionada**

- [PLA_NETEJA_DUPLICATS.md](PLA_NETEJA_DUPLICATS.md) - Pla detallat de neteja
- [RESUM_NETEJA_DUPLICATS.md](RESUM_NETEJA_DUPLICATS.md) - Resum executiu
- [MIGRACIO_CLEAN_ARCHITECTURE.md](MIGRACIO_CLEAN_ARCHITECTURE.md) - Guia de migració
- [CLEAN_ARCHITECTURE_README.md](CLEAN_ARCHITECTURE_README.md) - Arquitectura Clean

---

## ?? **Conclusió**

**Els duplicats existeixen per convivència temporal** durant la migració a Clean Architecture:

- ?? **Legacy** (arrel): Utilitzat per codi antic (`TractamentResultats`, `Processadors`)
- ?? **Clean** (Domain): Utilitzat per codi nou (Use Cases, Application, Infrastructure)

Aquesta **duplicació temporal** està:
- ? Documentada
- ? Marcada com obsoleta
- ? Planificada per eliminació
- ? Gestionada amb using alias

És una **estratègia acceptable** per permetre una migració gradual i segura cap a Clean Architecture.

---

**?? Per més detalls, consulta:**
- [PLA_NETEJA_DUPLICATS.md](PLA_NETEJA_DUPLICATS.md) - Anàlisi complet
- [RESUM_NETEJA_DUPLICATS.md](RESUM_NETEJA_DUPLICATS.md) - Resum tècnic

**Última actualització:** Gener 2025  
**Estat:** ?? Duplicats identificats, marcats i documentats
