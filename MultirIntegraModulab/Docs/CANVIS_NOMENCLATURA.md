# ?? CANVIS DE NOMENCLATURA IMPLEMENTATS

## ?? Canvi Conceptual

### **ABANS:**
- **Resultat de prova** = Contenidor amb ETIQUETA_ID
- **Registre** = Element individual d'Oracle

### **DESPRÉS:**
- **Mostra** = Contenidor amb ETIQUETA_ID (una mostra pot tenir un o més resultats)
- **Resultat** = Element individual d'Oracle (els diferents registres que recuperem d'Oracle)

## ?? Fitxers Actualitzats

### 1. **TractamentResultats.cs** - Canvis principals:
- `ProcessarResultats()` ? `ProcessarMostres()`
- `ProcessarResultatIndividual()` ? `ProcessarMostraIndividual()`
- `ClassificarResultatExistent()` ? `ClassificarMostraExistent()`
- `ProcessarResultatAntic()` ? `ProcessarMostraAntica()`
- `ProcessarResultatRepetit()` ? `ProcessarMostraRepetida()`
- `ProcessarResultatDesvalidat()` ? `ProcessarMostraDesvalidada()`
- `ProcessarResultatValidat()` ? `ProcessarMostraValidada()`
- `ProcessarResultatRevalidat()` ? `ProcessarMostraRevalidada()`
- `ValidarResultatGlobal()` ? `ValidarMostraGlobal()`
- `ExecutarComprovacionsGlobalsResultat()` ? `ExecutarComprovacionsGlobalsMostra()`
- `ExecutarComprovacionsRegistres()` ? `ExecutarComprovacionsResultats()`

### 2. **ContextProcessament** - Propietats actualitzades:
- `Resultat` ? `Mostra`
- Comentaris actualitzats per reflectir "mostra individual"

### 3. **Interfícies actualitzades:**
- `IComprovacioRegistre` ? `IComprovacioResultat`
- Mètodes actualitzats per treballar amb "resultats individuals"

### 4. **ResumTractament** - Propietats actualitzades:
- `NovaIncorporacio` ? `NovesIncorporacions`
- `ResultatsAntics` ? `MostresAntiques`
- `ResultatsRepetits` ? `MostresRepetides`
- `ResultatsDesvalidats` ? `MostresDesvalidades`
- `ResultatsValidats` ? `MostresValidades`
- `ResultatsRevalidats` ? `MostresRevalidades`
- `ResultatsInvalids` ? `MostresInvalides`
- `RegistresInvalids` ? `ResultatsInvalids`
- `ResultatsAmbError` ? `MostresAmbError`

### 5. **Comentaris i logging actualitzats:**
- Tots els missatges de consola actualitzats
- Documentació XML actualitzada
- Descripció dels enums actualitzada

## ?? Exemples Actualitzats

### **ExempleNouSistemaTractament.cs:**
- `ProcessarResultats()` ? `ProcessarMostres()`
- Missatges actualitzats per parlar de "mostres" i "resultats"
- Estadístiques actualitzades

### **ExempleUsTractament.cs:**
- API actualitzada per usar `ProcessarMostres()`
- Comentaris actualitzats

### **ExempleUsTractamentAntics.cs:**
- API i propietats de resum actualitzades
- Missatges explicatius actualitzats

### **Program.cs:**
- Crida principal actualitzada: `ProcessarMostres()`

## ?? Impacte dels Canvis

### **Conceptualment més clar:**
- Una **mostra** (ETIQUETA_ID) pot contenir múltiples **resultats** d'Oracle
- La terminologia és més intuïtiva: "processar mostres" en lloc de "processar resultats"
- Els comentaris i documentació reflecteixen millor la realitat del domini

### **API més coherent:**
- `ProcessarMostres(ColeccioResultatsMostres)` ? més clar que processem mostres
- `ExecutarComprovacionsResultats()` ? clarifica que les comprovacions són per resultats individuals
- Les interfícies tenen noms més descriptius

### **Estadístiques més precises:**
- `MostresAntiques`, `MostresRepetides`, etc. ? més clar el que comptem
- `ResultatsInvalids` ? clarifica que són resultats individuals invàlids
- Missatges de log més precisos

## ?? Compatibilitat

### **Classes no modificades:**
- `ResultatProva` ? Manté el nom original (representa una mostra)
- `ResultatProvaRegistre` ? Manté el nom original (representa un resultat)
- `ColeccioResultatsMostres` ? Manté el nom original
- `EstatResultat` ? Manté el nom original

### **Per què no s'han canviat:**
1. Són classes core que podrien estar referenciades en altres parts del codi
2. El canvi de nomenclatura és principalment a nivell de processament
3. Les classes de dades mantenen la seva semàntica original

## ? Verificació

- ? Build successful
- ? Tots els exemples actualitzats
- ? API coherent i consistent
- ? Documentació actualitzada
- ? Logging clar i descriptiu

La nomenclatura ara reflecteix correctament que:
- Processem **mostres** (contenidors amb ETIQUETA_ID)
- Cada mostra té un o més **resultats** (registres d'Oracle)
- Les comprovacions es fan a nivell de mostra i a nivell de resultat individual