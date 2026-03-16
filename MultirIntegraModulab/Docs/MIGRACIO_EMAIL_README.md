# ?? Migració Paràmetres d'Email: Remitent i Destinataris

## ?? Objectiu

Separar el paràmetre `EMAIL_NOTIFICACIONS` en dos paràmetres diferents per clarificar la seva funció:

| Paràmetre Antic | Nou Paràmetre | Funció |
|-----------------|---------------|--------|
| ~~EMAIL_NOTIFICACIONS~~ | **EMAIL_FROM** | Email **remitent** (From) |
| ~~EMAIL_NOTIFICACIONS~~ | **EMAIL_DESTINATARIS** | Emails **destinataris** (To) |

---

## ?? Problema Detectat

El paràmetre `EMAIL_NOTIFICACIONS` s'estava utilitzant incorrectament:

```csharp
// ConfigurationServiceHibrid.cs (ABANS - INCORRECTE)
public override string EmailFrom
{
    // ? Utilitzava EMAIL_NOTIFICACIONS per al remitent
    string valorBD = _parametresHelper.ObtenirString(
        "CONFIG_GENERAL", 
        "EMAIL_NOTIFICACIONS",  // ? INCORRECTE
        null);
}

public override List<string> EmailsDestinataris
{
    // ? No llegia de BD, sempre utilitzava App.config
    return base.EmailsDestinataris;
}
```

**Resultat**: Els emails sempre s'enviaven a l'adreça configurada a `App.config` (`carloscastillollucia@gmail.com`) i no a la de la BD (`ccastillo.ics@gencat.cat`).

---

## ? Solució Implementada

Ara hi ha **dos paràmetres separats** a la BD:

```csharp
// ConfigurationServiceHibrid.cs (DESPRÉS - CORRECTE)
public override string EmailFrom
{
    // ? Llegeix EMAIL_FROM de BD (remitent)
    string valorBD = _parametresHelper.ObtenirString(
        "CONFIG_GENERAL", 
        "EMAIL_FROM",  // ? CORRECTE
        null);
}

public override List<string> EmailsDestinataris
{
    // ? Llegeix EMAIL_DESTINATARIS de BD (destinataris)
    string valorBD = _parametresHelper.ObtenirString(
        "CONFIG_GENERAL", 
        "EMAIL_DESTINATARIS",  // ? CORRECTE
        null);
    
    // Divideix per ; per suportar múltiples destinataris
    return valorBD.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                  .Select(e => e.Trim())
                  .ToList();
}
```

---

## ?? Passos de Migració

### 1?? Executar Script SQL

Executar el fitxer: `Docs/SQL_MIGRACIO_EMAIL_NOTIFICACIONS.sql`

Aquest script:
- ? Crea el paràmetre `EMAIL_FROM` amb valor per defecte
- ? Crea el paràmetre `EMAIL_DESTINATARIS` copiant el valor d'`EMAIL_NOTIFICACIONS` (si existia)
- ?? Opcionalment elimina `EMAIL_NOTIFICACIONS` (comentat per seguretat)

```sql
-- Executar a la BD (marsa_test o marsa)
mysql -u marsa -p marsa_test < Docs/SQL_MIGRACIO_EMAIL_NOTIFICACIONS.sql
```

### 2?? Verificar Paràmetres Creats

```sql
SELECT categoria, clau, valor, descripcio
FROM parametres_aplicacio
WHERE categoria = 'CONFIG_GENERAL'
  AND clau IN ('EMAIL_FROM', 'EMAIL_DESTINATARIS')
  AND dt_delete IS NULL;
```

**Resultat esperat:**

| categoria | clau | valor | descripcio |
|-----------|------|-------|------------|
| CONFIG_GENERAL | EMAIL_FROM | ccastillo.ics@gencat.cat | Email remitent per notificacions |
| CONFIG_GENERAL | EMAIL_DESTINATARIS | admin@hospital.cat;epidemio@hospital.cat | Emails destinataris (separats per ;) |

### 3?? Desplegar Nova Versió

La nova versió del codi ja està preparada per llegir els dos paràmetres separats de BD.

### 4?? Validar Funcionament

Executar l'aplicació i verificar al resum de configuració:

```
=== PARÀMETRES DE BASE DE DADES ===
Dies vigència positius (BD):      365 dies
Email remitent (BD):               ccastillo.ics@gencat.cat
Emails destinataris (BD):          admin@hospital.cat; epidemio@hospital.cat
Habilitar emails (BD):             True
```

### 5?? (Opcional) Eliminar EMAIL_NOTIFICACIONS

Després de validar que tot funciona correctament, es pot eliminar el paràmetre antic:

```sql
UPDATE parametres_aplicacio
SET dt_delete = NOW(),
    usuari_modificacio = 'ADMIN'
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'EMAIL_NOTIFICACIONS'
  AND dt_delete IS NULL;
```

---

## ?? Gestió Post-Migració

### Canviar Email Remitent

```sql
UPDATE parametres_aplicacio
SET valor = 'notifications@hospital.cat',
    usuari_modificacio = 'nom_usuari'
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'EMAIL_FROM';
```

### Canviar Emails Destinataris

**Important**: Separar múltiples emails amb punt i coma (;)

```sql
-- Un sol destinatari
UPDATE parametres_aplicacio
SET valor = 'admin@hospital.cat',
    usuari_modificacio = 'nom_usuari'
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'EMAIL_DESTINATARIS';

-- Múltiples destinataris
UPDATE parametres_aplicacio
SET valor = 'admin@hospital.cat;epidemio@hospital.cat;ti@hospital.cat',
    usuari_modificacio = 'nom_usuari'
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'EMAIL_DESTINATARIS';
```

### Afegir Destinatari Addicional

```sql
-- Consultar valor actual
SELECT valor FROM parametres_aplicacio
WHERE categoria = 'CONFIG_GENERAL' AND clau = 'EMAIL_DESTINATARIS';

-- Afegir nou destinatari al final amb ;
UPDATE parametres_aplicacio
SET valor = CONCAT(valor, ';nou_destinatari@hospital.cat'),
    usuari_modificacio = 'nom_usuari'
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'EMAIL_DESTINATARIS';
```

---

## ?? Fallback a App.config

Si els paràmetres no existeixen a BD, l'aplicació utilitzarà automàticament els valors d'`App.config`:

```xml
<!-- App.config (Fallback) -->
<add key="EmailFrom" value="ccastillo.ics@gencat.cat" />
<add key="EmailsDestinataris" value="carloscastillollucia@gmail.com" />
```

---

## ? Checklist de Validació

- [ ] Script SQL executat correctament
- [ ] Paràmetres `EMAIL_FROM` i `EMAIL_DESTINATARIS` creats a BD
- [ ] Aplicació desplegada amb nova versió
- [ ] Resum de configuració mostra valors correctes de BD
- [ ] Email de prova enviat amb èxit als destinataris de BD
- [ ] (Opcional) Paràmetre `EMAIL_NOTIFICACIONS` eliminat

---

## ?? Referències

- **Fitxer SQL**: `Docs/SQL_MIGRACIO_EMAIL_NOTIFICACIONS.sql`
- **Documentació completa**: `Docs/MIGRACIO_PARAMETRES_BD_RESUM.md`
- **Codi modificat**: `Infrastructure/Configuration/ConfigurationServiceHibrid.cs`
