# Afegit camp shortdescription1 a integracio_modulab

## ?? Resum del Canvi

S'ha afegit el camp `shortdescription1` a la taula `integracio_modulab` (taula d'auditoria) per guardar el valor del camp SHORTDESCRIPTION1 de Modulab en els registres d'auditoria.

---

## ?? Objectiu

Guardar el valor de `SHORTDESCRIPTION1` (camp que indica si un resultat és Positiu, Negatiu, etc.) a la taula d'auditoria `integracio_modulab` per tenir més informació contextual en els registres d'auditoria.

---

## ?? Canvis Implementats

### 1. Modificació del mètode `InserirRegistreAuditoria`

**Arxiu:** `MultirIntegraModulab\Infrastructure\Persistence\LegacyServices\MultiRDbServiceExtensions.cs`

**Abans:**
```sql
INSERT INTO integracio_modulab (
    etiqueta_id, 
    pacient_sap, 
    cip, 
    colegiat_id, 
    nom_metge, 
    centre_descripcio, 
    data_peticio_truc, 
    aillament_descripcio, 
    mecanisme_resistencia1_id, 
    mecanisme_resistencia_descrip, 
    servei_descripcio, 
    prova_descripcio, 
    mostra_descripcio, 
    dt_create, 
    data_resultat, 
    data_validacio, 
    resultat
) VALUES (...)
```

**Després:**
```sql
INSERT INTO integracio_modulab (
    etiqueta_id, 
    pacient_sap, 
    cip, 
    colegiat_id, 
    nom_metge, 
    centre_descripcio, 
    data_peticio_truc, 
    aillament_descripcio, 
    mecanisme_resistencia1_id, 
    mecanisme_resistencia_descrip, 
    servei_descripcio, 
    prova_descripcio, 
    mostra_descripcio, 
    shortdescription1,          ?? NOU CAMP
    dt_create, 
    data_resultat, 
    data_validacio, 
    resultat
) VALUES (...)
```

### 2. Afegit paràmetre al INSERT

**Abans:**
```csharp
cmd.Parameters.AddWithValue("@servei_descripcio", resultatUtilitzar.ServeiDescripcio ?? "");
cmd.Parameters.AddWithValue("@prova_descripcio", resultatUtilitzar.ProvaDescripcio ?? "");
cmd.Parameters.AddWithValue("@mostra_descripcio", resultatUtilitzar.MostraDescripcio ?? "");

cmd.Parameters.AddWithValue("@data_resultat", ...);
```

**Després:**
```csharp
cmd.Parameters.AddWithValue("@servei_descripcio", resultatUtilitzar.ServeiDescripcio ?? "");
cmd.Parameters.AddWithValue("@prova_descripcio", resultatUtilitzar.ProvaDescripcio ?? "");
cmd.Parameters.AddWithValue("@mostra_descripcio", resultatUtilitzar.MostraDescripcio ?? "");
cmd.Parameters.AddWithValue("@shortdescription1", resultatUtilitzar.ShortDescription1 ?? "");  ?? NOU

cmd.Parameters.AddWithValue("@data_resultat", ...);
```

---

## ??? Estructura de Base de Dades

### Taula: `integracio_modulab`

**Camp afegit:**
```sql
ALTER TABLE integracio_modulab 
ADD COLUMN shortdescription1 VARCHAR(10) DEFAULT NULL 
COMMENT 'Descripció curta del resultat (P=Positiu, N=Negatiu, etc.)';
```

**Valors possibles:**
- `'P'` - Positiu
- `'N'` - Negatiu
- Altres valors segons Modulab
- `NULL` - Si no està informat

---

## ?? Font de les Dades

El valor de `shortdescription1` s'obté de:

**Classe:** `ResultatMostra`  
**Propietat:** `ShortDescription1`  
**Origen:** Camp `SHORTDESCRIPTION1` de la base de dades Oracle de Modulab

```csharp
public class ResultatMostra
{
    /// <summary>
    /// Descripció curta adicional del detall de la prova (SHORTDESCRIPTION1)
    /// </summary>
    public string ShortDescription1 { get; set; }
    
    // ... altres propietats
}
```

---

## ?? Flux d'Informació

```
Modulab (Oracle)
    ?
SHORTDESCRIPTION1
    ?
ResultatMostra.ShortDescription1
    ?
InserirAuditoriaIntegracioModulab()
    ?
InserirRegistreAuditoria()
    ?
integracio_modulab.shortdescription1
```

---

## ?? Casos d'Ús

### Exemple 1: Auditoria d'una mostra MDO positiva

```
Mostra:
  - Etiqueta: "MDO001234"
  - ShortDescription1: "P" (Positiu)
  
Registre a integracio_modulab:
  - etiqueta_id: "MDO001234"
  - shortdescription1: "P"
  - resultat: "MDO"
  - ...
```

### Exemple 2: Auditoria d'una mostra descartada

```
Mostra:
  - Etiqueta: "ABC123456"
  - ShortDescription1: "N" (Negatiu)
  
Registre a integracio_modulab:
  - etiqueta_id: "ABC123456"
  - shortdescription1: "N"
  - resultat: "EMCR"
  - ...
```

---

## ?? Consultes d'Exemple

### Veure totes les auditories amb resultat positiu:
```sql
SELECT * 
FROM integracio_modulab 
WHERE shortdescription1 = 'P'
ORDER BY dt_create DESC;
```

### Comptar auditories per tipus de resultat:
```sql
SELECT 
    shortdescription1,
    COUNT(*) as total
FROM integracio_modulab
WHERE dt_create >= DATE_SUB(NOW(), INTERVAL 30 DAY)
GROUP BY shortdescription1;
```

### Veure MDO positives:
```sql
SELECT 
    etiqueta_id,
    pacient_sap,
    prova_descripcio,
    shortdescription1,
    resultat,
    dt_create
FROM integracio_modulab 
WHERE resultat = 'MDO'
  AND shortdescription1 = 'P'
ORDER BY dt_create DESC;
```

---

## ?? Beneficis

1. **Més context en auditoria**: Es pot veure directament si un resultat era positiu o negatiu sense haver de consultar altres taules

2. **Filtratge més fàcil**: Es poden filtrar auditories per tipus de resultat (P/N) de manera directa

3. **Estadístiques més precises**: Es poden generar estadístiques sobre resultats positius/negatius sense joins addicionals

4. **Diagnòstic MDO**: Especialment útil per les MDO, on és important saber si el resultat era positiu o negatiu

5. **Historial complet**: El registre d'auditoria conté tota la informació rellevant de la mostra

---

## ? Validació

Per verificar que el canvi funciona correctament:

1. Processar una mostra amb `ShortDescription1` informat
2. Comprovar que s'ha creat un registre d'auditoria
3. Verificar que el camp `shortdescription1` conté el valor esperat:

```sql
SELECT 
    etiqueta_id,
    shortdescription1,
    resultat,
    dt_create
FROM integracio_modulab 
ORDER BY dt_create DESC 
LIMIT 10;
```

---

## ?? Estat de la Implementació

? **Completat:**
- Modificació del SQL INSERT per incloure `shortdescription1`
- Afegit paràmetre amb el valor de `ShortDescription1`
- Compilació exitosa
- Documentació creada

---

## ?? Data d'Implementació

**Data:** 2025-01-XX  
**Versió:** 1.0  
**Context:** Millora de la funcionalitat d'auditoria per suportar detecció de MDO

---

## ?? Referències

- **Codi Font:**
  - `MultirIntegraModulab\Infrastructure\Persistence\LegacyServices\MultiRDbServiceExtensions.cs`
  - `MultirIntegraModulab\Domain\Entities\ResultatMostra.cs`

- **Documentació relacionada:**
  - `MDO_DETECCIO.md` (context del camp shortdescription1 per MDO)

---

## ?? Relació amb MDO

Aquest canvi està directament relacionat amb la funcionalitat de **detecció de MDO**, ja que:

- Les MDO es detecten en part pel valor de `SHORTDESCRIPTION1 = 'P'` (positiu)
- Ara aquest valor es guarda a l'auditoria per facilitar el seguiment
- Es pot identificar fàcilment quines auditories corresponen a MDO positives

**Nota important:** Per detectar si una mostra és MDO, cal consultar també el camp `incorpora_mdo` de la taula `tipusprova`. El camp `shortdescription1` per si sol NO és suficient per determinar si és MDO.
