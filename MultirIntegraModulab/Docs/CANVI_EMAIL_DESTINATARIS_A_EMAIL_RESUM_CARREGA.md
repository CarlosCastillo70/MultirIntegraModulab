# Canvi de Clau: EMAIL_DESTINATARIS ? EMAIL_RESUM_CARREGA

## ?? Resum del Canvi

S'ha renombrat la clau del paràmetre `EMAIL_DESTINATARIS` a `EMAIL_RESUM_CARREGA` per fer més descriptiu i clar el seu propòsit: contenir els emails que reben el resum de la càrrega diària de Modulab.

---

## ?? Objectiu

Millorar la claredat i descriptivitat dels noms dels paràmetres, diferenciant clarament entre:
- **EMAIL_RESUM_CARREGA**: Emails que reben el resum diari del processament
- **EMAIL_MDO**: Emails que reben alertes específiques de MDO

---

## ?? Canvis Realitzats al Codi

### 1. `ConfigurationServiceHibrid.cs`

#### Canvi 1: Comentari de documentació (línia 14)

**ABANS:**
```csharp
/// PARÀMETRES A BD (CONFIG_GENERAL):
/// - DIES_VIGENCIA_POSITIUS_DEFAULT
/// - EMAIL_FROM
/// - EMAIL_DESTINATARIS
/// - HABILITAR_NOTIFICACIONS_EMAIL
```

**DESPRÉS:**
```csharp
/// PARÀMETRES A BD (CONFIG_GENERAL):
/// - DIES_VIGENCIA_POSITIUS_DEFAULT
/// - EMAIL_FROM
/// - EMAIL_RESUM_CARREGA
/// - HABILITAR_NOTIFICACIONS_EMAIL
```

#### Canvi 2: Propietat EmailsDestinataris (línia 115 i 129-132)

**ABANS:**
```csharp
/// <summary>
/// MIGRAT A BD: Emails destinataris per notificacions del sistema
/// Pot variar segons organització/departament
/// Format a BD: emails separats per punt i coma (;)
/// </summary>
public override List<string> EmailsDestinataris
{
    get
    {
        // ...
        
        // Llegir de BD primer amb el paràmetre EMAIL_DESTINATARIS
        string valorBD = _parametresHelper.ObtenirString(
            "CONFIG_GENERAL", 
            "EMAIL_DESTINATARIS", 
            null);
```

**DESPRÉS:**
```csharp
/// <summary>
/// MIGRAT A BD: Emails destinataris per notificacions de resum de càrrega
/// Pot variar segons organització/departament
/// Format a BD: emails separats per punt i coma (;)
/// </summary>
public override List<string> EmailsDestinataris
{
    get
    {
        // ...
        
        // Llegir de BD primer amb el paràmetre EMAIL_RESUM_CARREGA
        string valorBD = _parametresHelper.ObtenirString(
            "CONFIG_GENERAL", 
            "EMAIL_RESUM_CARREGA", 
            null);
```

---

## ??? Canvis a la Base de Dades

### Script SQL per Renombrar:

```sql
-- 1. Verificar configuració actual
SELECT * FROM parametres_aplicacio
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'EMAIL_DESTINATARIS';

-- 2. Renombrar la clau
UPDATE parametres_aplicacio 
SET clau = 'EMAIL_RESUM_CARREGA',
    dt_update = NOW()
WHERE categoria = 'CONFIG_GENERAL' 
  AND clau = 'EMAIL_DESTINATARIS';

-- 3. Verificar el canvi
SELECT * FROM parametres_aplicacio
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'EMAIL_RESUM_CARREGA';

-- 4. Verificar que no quedi cap registre amb la clau antiga
SELECT COUNT(*) FROM parametres_aplicacio WHERE clau = 'EMAIL_DESTINATARIS';
-- Hauria de retornar 0
```

---

## ?? Estructura Abans i Després

### ABANS:

```
categoria       | clau               | valor
----------------|--------------------|---------------------------------
CONFIG_GENERAL  | EMAIL_DESTINATARIS | admin1@hospital.cat;admin2@hospital.cat
```

### DESPRÉS:

```
categoria       | clau                | valor
----------------|---------------------|---------------------------------
CONFIG_GENERAL  | EMAIL_RESUM_CARREGA | admin1@hospital.cat;admin2@hospital.cat
```

**IMPORTANT:** El **valor** NO canvia, només la **clau** del paràmetre.

---

## ?? Comparació de Paràmetres d'Email

| Paràmetre | Propòsit | Format | Exemple |
|-----------|----------|--------|---------|
| **EMAIL_RESUM_CARREGA** | Emails que reben el resum diari | Un registre amb múltiples emails separats per `;` | `admin1@hospital.cat;admin2@hospital.cat` |
| **EMAIL_MDO** | Emails que reben alertes MDO | Múltiples registres, un email per registre | Diversos registres amb `valor = 'mdo@hospital.cat'`, `'urgencies@hospital.cat'`, etc. |

---

## ?? Impacte del Canvi

### ? El que NO canvia:

- La funcionalitat es manté idèntica
- Els emails destinataris són els mateixos
- El format del valor (múltiples emails separats per `;`) no canvia
- El comportament de l'aplicació no canvia

### ? El que canvia:

- El nom de la clau és més descriptiu
- Queda més clar que aquests emails són per al resum de càrrega
- Es diferencia millor de `EMAIL_MDO` (alertes MDO)

---

## ?? Procediment de Desplegament

### 1. Backup de la Base de Dades

```sql
-- Fer backup del registre abans de canviar-lo
CREATE TABLE parametres_aplicacio_backup_email AS
SELECT * FROM parametres_aplicacio 
WHERE categoria = 'CONFIG_GENERAL' 
  AND clau = 'EMAIL_DESTINATARIS';
```

### 2. Executar el Canvi a la BD

```sql
UPDATE parametres_aplicacio 
SET clau = 'EMAIL_RESUM_CARREGA',
    dt_update = NOW()
WHERE categoria = 'CONFIG_GENERAL' 
  AND clau = 'EMAIL_DESTINATARIS';
```

### 3. Desplegar el Codi Nou

- El codi ja està actualitzat i compilat
- Desplegar l'executable nou
- Reiniciar el servei/aplicació

### 4. Verificar el Funcionament

```sql
-- Verificar que l'aplicació llegeix el paràmetre
SELECT * FROM parametres_aplicacio 
WHERE clau = 'EMAIL_RESUM_CARREGA';

-- Comprovar els logs de l'aplicació
-- Hauria de mostrar: "Carregats X valors per la clau 'EMAIL_RESUM_CARREGA'"
```

---

## ?? Compatibilitat Cap Enrere

L'aplicació té **fallback automàtic** a `App.config` si no troba el paràmetre a la base de dades:

```csharp
if (!string.IsNullOrEmpty(valorBD))
{
    return valorBD.Split(new[] { ';' }, ...)...;
}

// Fallback a App.config
return base.EmailsDestinataris;
```

**Això significa:**
- Si el paràmetre no existeix a BD ? llegeix de `App.config`
- Si hi ha error llegint de BD ? llegeix de `App.config`
- L'aplicació continua funcionant encara que el canvi falli

---

## ? Validació Post-Desplegament

### Test 1: Verificar càrrega del paràmetre

Executar l'aplicació i comprovar al log:
```
Carregats 1 paràmetres de la categoria 'CONFIG_GENERAL'
```

O similar, indicant que s'ha llegit correctament el paràmetre.

### Test 2: Verificar enviament d'email

Si `EnviarEmailLog = true`, verificar que:
- Es genera un email de resum
- S'envia als destinataris correctes
- El log mostra: `? Email enviat a: admin1@hospital.cat, admin2@hospital.cat`

### Test 3: Verificar fallback

Temporalment esborrar/desactivar el paràmetre a BD i verificar que:
- L'aplicació llegeix de `App.config`
- No es genera cap error
- Els emails continuen enviant-se

---

## ?? Data d'Implementació

**Data:** 2025-01-XX  
**Versió:** 1.0  
**Context:** Millora de nomenclatura de paràmetres per més claredat

---

## ?? Referències

- **Codi Font:**
  - `MultirIntegraModulab\Infrastructure\Configuration\ConfigurationServiceHibrid.cs`

- **Scripts SQL:**
  - `SQL_RENAME_EMAIL_DESTINATARIS_TO_EMAIL_RESUM_CARREGA.sql`

- **Documentació relacionada:**
  - `EMAIL_ALERTA_MDO.md`
  - `NOU_METODE_OBTENIR_VALORS_PER_CLAU.md`
  - `SQL_CONFIG_EMAIL_MDO.sql`

---

## ?? Checklist de Desplegament

- [ ] Fer backup de `parametres_aplicacio`
- [ ] Executar l'UPDATE per canviar la clau
- [ ] Verificar que s'ha canviat correctament
- [ ] Verificar que no queden registres amb la clau antiga
- [ ] Desplegar el codi nou
- [ ] Reiniciar el servei/aplicació
- [ ] Comprovar els logs per verificar la càrrega del paràmetre
- [ ] Provar l'enviament d'un email de resum
- [ ] Verificar que els emails arriben als destinataris correctes
- [ ] Documentar el canvi al registre de canvis del projecte
