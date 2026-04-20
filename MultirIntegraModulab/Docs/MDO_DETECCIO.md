# Detecció de MDO (Malalties de Declaració Obligatòria)

## ?? Resum de la Implementació

S'ha implementat la detecció automàtica de MDO (Malalties de Declaració Obligatòria) durant el processament de mostres de Modulab.

---

## ?? Objectiu

Detectar mostres que corresponen a MDO per poder gestionar-les adequadament, ja que són poc freqüents però molt importants des del punt de vista epidemiològic.

---

## ?? Criteris de Detecció

Una mostra és considerada **MDO** si compleix alguna d'aquestes condicions:

### Cas 1: `incorpora_mdo = 1`
- El camp `incorpora_mdo` de la taula `tipusprova` és `1`
- **I** el resultat és **positiu** (`SHORTDESCRIPTION1 = 'P'`)

### Cas 2: `incorpora_mdo = 2`
- El camp `incorpora_mdo` de la taula `tipusprova` és `2`
- **Independentment** del resultat (positiu o negatiu)

### Cas 3: `incorpora_mdo = 0`
- **NO** és MDO (comportament per defecte)

---

## ?? Canvis Implementats

### 1. Nou Mètode a `MultiRDbService.TipusProva.cs`

```csharp
/// <summary>
/// Comprova si un tipus de prova és MDO (Malaltia de Declaració Obligatòria)
/// </summary>
/// <param name="codiProva">Codi de la prova (PROVA_DESCRIPCIO de Modulab)</param>
/// <param name="shortDescription1">Valor de SHORTDESCRIPTION1 del resultat ('P' = Positiu)</param>
/// <returns>True si és MDO, False en cas contrari</returns>
public bool TipusProvaEsMDO(string codiProva, string shortDescription1)
```

**Lògica implementada:**
1. Consulta el camp `incorpora_mdo` de la taula `tipusprova`
2. Si `incorpora_mdo = 0` ? NO és MDO
3. Si `incorpora_mdo = 2` ? SEMPRE és MDO
4. Si `incorpora_mdo = 1` ? És MDO només si el resultat és positiu

---

### 2. Nou Mètode a `ProcessarMostresUseCase.cs`

```csharp
/// <summary>
/// Detecta si una mostra és MDO (Malaltia de Declaració Obligatòria)
/// </summary>
private bool DetectarMostraMDO(Mostra mostra)
```

**Funcionalitat:**
- Itera per tots els resultats de la mostra
- Comprova si algun resultat té un tipus de prova MDO
- Genera logs detallats amb informació del MDO detectat
- Retorna `true` si es detecta almenys un resultat MDO

---

### 3. Integració al Flux de Processament

S'ha afegit una nova **FASE 5.1** després de la comprovació de mecanismes de resistència:

```csharp
// FASE 5.1: Detectar si és MDO (Malaltia de Declaració Obligatòria)
bool esMDO = DetectarMostraMDO(mostra);
```

**Posició al flux:**
- **ABANS**: FASE 5 (Comprovar mecanismes de resistència)
- **NOVA FASE**: FASE 5.1 (Detectar si és MDO)
- **DESPRÉS**: FASE 6 (Determinar tipus de microorganisme)

---

### 4. Actualització d'Interfícies i Repositoris

**`IMultiRRepository.cs`:**
```csharp
bool TipusProvaEsMDO(string codiProva, string shortDescription1);
```

**`MultiRRepository.cs`:**
```csharp
public bool TipusProvaEsMDO(string codiProva, string shortDescription1) =>
    _multiRDbService.TipusProvaEsMDO(codiProva, shortDescription1);
```

---

## ?? Logs Generats

### Quan es detecta una MDO:

```
?? Comprovant si la mostra és MDO (Malaltia de Declaració Obligatòria)...
   ?? MDO detectat!
      Tipus prova: [NOM_PROVA]
      Microorganisme: [NOM_MICROORGANISME]
      Estat resultat: POSITIU
? MOSTRA MDO confirmada - 1 resultat(s) MDO detectat(s)
?? Aquesta mostra requereix gestió especial per MDO
```

### Quan NO es detecta MDO:

```
?? Comprovant si la mostra és MDO (Malaltia de Declaració Obligatòria)...
?? La mostra NO és MDO - processament normal
```

---

## ??? Estructura de Base de Dades

### Taula: `tipusprova`

**Camp afegit:**
```sql
ALTER TABLE tipusprova 
ADD COLUMN incorpora_mdo INT(1) DEFAULT 0 
COMMENT 'Indica si aquest tipus de prova és MDO (0=NO, 1=SÍ si positiu, 2=SEMPRE)';
```

**Valors possibles:**
- `0` - NO és MDO (valor per defecte)
- `1` - És MDO només si el resultat és positiu
- `2` - SEMPRE és MDO (independentment del resultat)

---

## ?? Flux de Processament Complet

```
FASE 1: Validar mostra
   ?
FASE 2: Determinar tipus d'incorporació
   ?
FASE 3: Tractament específic segons tipus d'incorporació
   ?
FASE 4: Comprovar microorganismes
   ?
FASE 5: Comprovar mecanismes de resistència
   ?
FASE 5.1: Detectar si és MDO ? NOVA
   ?
FASE 6: Determinar tipus de microorganisme (MR vs VR vs MIXT)
   ?
Processar segons tipus
```

---

## ?? Casos d'Ús

### Exemple 1: Tuberculosi Positiva (incorpora_mdo = 1)

```
Mostra:
  - Tipus prova: "CULTIU MYCOBACTERIUM"
  - SHORTDESCRIPTION1: "P" (Positiu)
  - incorpora_mdo: 1

Resultat: ? És MDO (perquè incorpora_mdo=1 i resultat és positiu)
```

### Exemple 2: Tuberculosi Negativa (incorpora_mdo = 1)

```
Mostra:
  - Tipus prova: "CULTIU MYCOBACTERIUM"
  - SHORTDESCRIPTION1: "N" (Negatiu)
  - incorpora_mdo: 1

Resultat: ? NO és MDO (perquè incorpora_mdo=1 però resultat NO és positiu)
```

### Exemple 3: Prova Sempre MDO (incorpora_mdo = 2)

```
Mostra:
  - Tipus prova: "DETECCIÓ EBOLA"
  - SHORTDESCRIPTION1: "N" (Negatiu)
  - incorpora_mdo: 2

Resultat: ? És MDO (perquè incorpora_mdo=2, independentment del resultat)
```

### Exemple 4: Prova NO MDO (incorpora_mdo = 0)

```
Mostra:
  - Tipus prova: "HEMOCULTIU STANDARD"
  - SHORTDESCRIPTION1: "P" (Positiu)
  - incorpora_mdo: 0

Resultat: ? NO és MDO (perquè incorpora_mdo=0)
```

---

## ?? Configuració

Per marcar un tipus de prova com a MDO, cal actualitzar el camp `incorpora_mdo` a la taula `tipusprova`:

```sql
-- Marcar una prova com a MDO només si és positiva
UPDATE tipusprova 
SET incorpora_mdo = 1
WHERE codi = 'CULTIU MYCOBACTERIUM';

-- Marcar una prova com a MDO sempre
UPDATE tipusprova 
SET incorpora_mdo = 2
WHERE codi = 'DETECCIÓ EBOLA';

-- Desmarcar una prova com a MDO
UPDATE tipusprova 
SET incorpora_mdo = 0
WHERE codi = 'HEMOCULTIU STANDARD';
```

---

## ?? Validació

Per verificar que la detecció funciona correctament:

1. Processar una mostra amb un tipus de prova marcat com a MDO
2. Revisar els logs generats durant el processament
3. Comprovar que apareix el missatge `?? MDO detectat!`
4. Verificar que la variable `esMDO` té el valor correcte

---

## ?? Estadístiques

La variable `esMDO` es pot utilitzar per:
- Generar informes d'MDO detectades
- Aplicar fluxos de treball específics per MDO
- Enviar notificacions automàtiques
- Actualitzar sistemes de vigilància epidemiològica

---

## ?? Estat de la Implementació

? **Completat:**
- Mètode de detecció a `MultiRDbService.TipusProva.cs`
- Mètode auxiliar a `ProcessarMostresUseCase.cs`
- Integració al flux de processament (FASE 5.1)
- Logs detallats de detecció
- Actualització d'interfícies i repositoris
- Compilació exitosa

?? **Pendents (fase futura):**
- Processament específic per MDO (si requerit)
- Integració amb sistemes de vigilància epidemiològica
- Notificacions automàtiques per MDO detectades
- Informes específics per MDO

---

## ?? Data d'Implementació

**Data:** 2025-01-XX  
**Versió:** 1.0  
**Autor:** Sistema de Integració Modulab

---

## ?? Referències

- **Codi Font:**
  - `MultirIntegraModulab\Infrastructure\Persistence\LegacyServices\MultiRDbService.TipusProva.cs`
  - `MultirIntegraModulab\Application\UseCases\ProcessarMostres\ProcessarMostresUseCase.cs`
  - `MultirIntegraModulab\Domain\Interfaces\IMultiRRepository.cs`
  - `MultirIntegraModulab\Infrastructure\Persistence\Repositories\MultiRRepository.cs`

- **Documentació relacionada:**
  - `VIRUS_RESPIRATORIS_VALIDACIO.md` (flux similar)
  - `SQL_ALTER_TIPUSPROVA_VR.sql` (estructura similar)
