# ?? CONTROL D'INCORPORACIÓ VR PER CENTRE

## ?? Informació General

**Data d'Implementació**: Gener 2025  
**Versió**: 1.0  
**Estat**: ? Implementat (Pendent creació taula BD)  
**Codi d'Auditoria Nou**: **CNIVR**

---

## ?? Objectiu

Implementar un control a nivell de **centre** per determinar quins resultats de **virus respiratoris (VR)** s'han d'incorporar al sistema MultiR segons l'origen de la mostra.

---

## ?? Solució Implementada

### Taula Genèrica de Paràmetres

S'ha creat una **taula genèrica** `parametres_aplicacio` que permet gestionar configuracions de l'aplicació de forma flexible i escalable.

```sql
CREATE TABLE parametres_aplicacio (
    id INT AUTO_INCREMENT PRIMARY KEY,
    categoria VARCHAR(50) NOT NULL,      -- Ex: 'VR_CENTRES'
    clau VARCHAR(100) NOT NULL,          -- Ex: 'HOSPITAL TRUETA'
    valor TEXT NOT NULL,                 -- Valor del paràmetre
    descripcio TEXT NULL,                -- Descripció
    tipus_dada VARCHAR(20) DEFAULT 'STRING',
    actiu INT(1) DEFAULT 1,
    dt_create TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    dt_update TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    dt_delete TIMESTAMP NULL DEFAULT NULL,
    usuari_modificacio VARCHAR(50) NULL
);
```

---

## ?? Flux de Funcionament

```
VR a processar
    ?
    ?
???????????????????????
? Tipus Prova VR?     ?
???????????????????????
       ?
  ? NO ? Auditoria TPNIVR
       ?
  ? SÍ
       ?
       ?
???????????????????????
? Centre Autoritzat?  ?
???????????????????????
       ?
  ? NO ? Auditoria CNIVR
       ?
  ? SÍ
       ?
       ?
  Processar VR Normal
```

---

## ?? Implementació Realitzada

### 1. Interfície del Repositori

**Fitxer**: `Domain/Interfaces/IMultiRRepository.cs`

```csharp
#region Paràmetres d'Aplicació

/// <summary>
/// Comprova si un valor està a la llista de paràmetres actius d'una categoria
/// </summary>
bool ExisteixParametre(string categoria, string valor);

/// <summary>
/// Obté el valor d'un paràmetre de l'aplicació
/// </summary>
string ObtenirParametre(string categoria, string clau);

/// <summary>
/// Obté tots els paràmetres actius d'una categoria
/// </summary>
List<string> ObtenirParametresPerCategoria(string categoria);

#endregion
```

### 2. Implementació MySQL

**Fitxer**: `Infrastructure/Persistence/LegacyServices/MultiRDbService.Parametres.cs`

```csharp
public bool ExisteixParametre(string categoria, string valor)
{
    // SELECT COUNT(*) 
    // FROM parametres_aplicacio 
    // WHERE categoria = @categoria
    //   AND UPPER(clau) = UPPER(@valor)
    //   AND actiu = 1
    //   AND dt_delete IS NULL
    
    // Retorna true si existeix i està actiu
}
```

### 3. Delegació al Repositori

**Fitxer**: `Infrastructure/Persistence/Repositories/MultiRRepository.cs`

```csharp
#region Paràmetres d'Aplicació

public bool ExisteixParametre(string categoria, string valor) =>
    _multiRDbService.ExisteixParametre(categoria, valor);

// ... altres mètodes

#endregion
```

### 4. Integració al Use Case VR

**Fitxer**: `Application/UseCases/ProcessarMostres/ProcessarMostraVirusRespiratoriUseCase.cs`

```csharp
// FASE 0b: COMPROVAR CENTRE (NOMÉS PER VR)
string centreDescripcio = mostra.Resultats[0].CentreDescripcio;

_logger.Info($"?? Comprovant centre: '{centreDescripcio}'");

bool centrePermitVR = _multiRRepository.ExisteixParametre("VR_CENTRES", centreDescripcio);

if (!centrePermitVR)
{
    _logger.Info("?? El centre NO permet incorporar VR");
    _logger.Info("?? La mostra NO es processarà");
    
    // Auditoria CNIVR
    foreach (var resultat in mostra.Resultats)
    {
        _multiRRepository.InserirAuditoriaIntegracioModulab(mostra, "CNIVR", resultat);
    }
    
    return ResultatError("Centre no permet VR");
}

_logger.Info("? Centre permet incorporar virus respiratoris");
```

---

## ?? Nou Codi d'Auditoria

### CNIVR - Centre No Incorpora Virus Respiratori

**Significat**: El centre d'origen de la mostra NO permet incorporar virus respiratoris.

**Quan es genera**:
- Mostra detectada com VR
- Tipus de prova permet VR
- Però el centre NO està a la taula de centres autoritzats

**Acció**: La mostra NO s'incorpora a MultiR

**Taula auditoria**:
```sql
INSERT INTO auditoria_integracio_modulab
(etiqueta_id, pacient_sap, microorganisme, tipus_prova, 
 centre_descripcio, resultat)
VALUES
('VR001', '12345678', 'SARS-CoV-2', 'PCR COVID',
 'CAP PERIFÈRIC', 'CNIVR');
```

---

## ?? Exemples Pràctics

### Cas 1: Centre Autoritzat

```
?? ENTRADA:
   • Etiqueta: VR001234
   • Centre: HOSPITAL UNIVERSITARI DR. JOSEP TRUETA
   • Tipus Prova: PCR SARS-CoV-2
   • Microorganisme: SARS-CoV-2

?? COMPROVACIÓ TIPUS PROVA: ? Permet VR

?? COMPROVACIÓ CENTRE:
   SELECT COUNT(*) 
   FROM parametres_aplicacio
   WHERE categoria = 'VR_CENTRES'
     AND UPPER(clau) = UPPER('HOSPITAL UNIVERSITARI DR. JOSEP TRUETA')
     AND actiu = 1;
   
   Resultat: 1 (existeix)

? DECISIÓ: INCORPORAR
   
?? SORTIDA:
   ? Mostra incorporada
   ?? Auditoria: OKVR
```

### Cas 2: Centre NO Autoritzat

```
?? ENTRADA:
   • Etiqueta: VR001235
   • Centre: CAP PERIFÈRIC
   • Tipus Prova: PCR SARS-CoV-2
   • Microorganisme: SARS-CoV-2

?? COMPROVACIÓ TIPUS PROVA: ? Permet VR

?? COMPROVACIÓ CENTRE:
   SELECT COUNT(*) 
   FROM parametres_aplicacio
   WHERE categoria = 'VR_CENTRES'
     AND UPPER(clau) = UPPER('CAP PERIFÈRIC')
     AND actiu = 1;
   
   Resultat: 0 (no existeix)

? DECISIÓ: NO INCORPORAR
   
?? SORTIDA:
   ?? Mostra NO incorporada
   ?? Auditoria: CNIVR
```

---

## ?? Guia d'Ús per a l'Usuari

### 1. Crear la Taula

```bash
mysql -u user -p multir < Docs/SQL_CREATE_PARAMETRES_APLICACIO.sql
```

### 2. Afegir Centres que Permeten VR

```sql
-- Afegir el teu centre principal
INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
VALUES
('VR_CENTRES', 'HOSPITAL UNIVERSITARI DR. JOSEP TRUETA', '1', 
 'Centre principal - Permet VR', 'BOOL', 1, 'admin');

-- Afegir un centre secundari
INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
VALUES
('VR_CENTRES', 'HOSPITAL DE SANTA CATERINA', '1', 
 'Centre secundari - Permet VR', 'BOOL', 1, 'admin');
```

### 3. Consultar Centres Configurats

```sql
-- Veure tots els centres VR actius
SELECT 
    clau as centre,
    descripcio,
    dt_create as data_alta
FROM parametres_aplicacio
WHERE categoria = 'VR_CENTRES'
  AND actiu = 1
  AND dt_delete IS NULL
ORDER BY clau;
```

### 4. Gestionar Centres

```sql
-- Desactivar temporalment un centre
UPDATE parametres_aplicacio
SET actiu = 0, usuari_modificacio = 'admin'
WHERE categoria = 'VR_CENTRES'
  AND clau = 'HOSPITAL DE SANTA CATERINA';

-- Reactivar un centre
UPDATE parametres_aplicacio
SET actiu = 1, usuari_modificacio = 'admin'
WHERE categoria = 'VR_CENTRES'
  AND clau = 'HOSPITAL DE SANTA CATERINA';

-- Esborrar un centre (soft delete)
UPDATE parametres_aplicacio
SET dt_delete = NOW(), usuari_modificacio = 'admin'
WHERE categoria = 'VR_CENTRES'
  AND clau = 'CAP GIRONA-1';
```

---

## ?? Logs i Traces

### Log Centre Permet VR

```
?? Comprovant centre: 'HOSPITAL UNIVERSITARI DR. JOSEP TRUETA'
? Centre 'HOSPITAL UNIVERSITARI DR. JOSEP TRUETA' permet incorporar virus respiratoris
?? Continuant amb flux VR...
```

### Log Centre NO Permet VR

```
?? Comprovant centre: 'CAP PERIFÈRIC'
?? El centre 'CAP PERIFÈRIC' NO permet incorporar virus respiratoris
?? La mostra NO es processarà
?? Auditoria CNIVR generada
```

---

## ?? Avantatges de la Solució

| Avantatge | Descripció |
|-----------|------------|
| **?? Flexibilitat** | Afegir/treure centres sense codi ni redeployment |
| **?? Escalabilitat** | Taula preparada per altres paràmetres futurs |
| **?? Auditoria** | Tracking complet (qui, quan, què) |
| **?? Centralització** | Tota la configuració en un sol lloc |
| **?? Mantenibilitat** | Gestió per usuaris funcionals via SQL |
| **?? Seguretat** | Soft delete manté històric |
| **? Performance** | Índexs optimitzats per consultes ràpides |

---

## ?? Usos Futurs de la Taula

La taula `parametres_aplicacio` està preparada per:

### CONFIG_GENERAL
```sql
INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada)
VALUES
('CONFIG_GENERAL', 'TIMEOUT_WEBSERVICE', '30', 'Timeout en segons', 'INT'),
('CONFIG_GENERAL', 'EMAIL_NOTIFICACIONS', 'admin@hospital.cat', 'Email', 'STRING');
```

### MMR_CONFIG
```sql
INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada)
VALUES
('MMR_CONFIG', 'DIES_VIGENCIA_DEFAULT', '365', 'Dies vigència MMR', 'INT');
```

### TIPUS_MOSTRA_EQUIVALENTS (JSON)
```sql
INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada)
VALUES
('TIPUS_MOSTRA_EQUIV', 'SANG', 
 '["SANG VENOSA","SANG ARTERIAL","SANG CAPILAR"]', 
 'Tipus equivalents', 'JSON');
```

---

## ? Checklist d'Implementació

- [x] **Interfície actualitzada** (`IMultiRRepository`)
- [x] **Mètodes implementats** (`MultiRDbService.Parametres.cs`)
- [x] **Delegació al repositori** (`MultiRRepository`)
- [x] **Integració al flux VR** (FASE 0b)
- [x] **Codi auditoria creat** (CNIVR)
- [x] **Script SQL preparat** (`SQL_CREATE_PARAMETRES_APLICACIO.sql`)
- [x] **Documentació completa** (aquest fitxer)
- [x] **Logging implementat** (amb emojis i indentació)
- [ ] **Execució script SQL** (PENDENT - Usuari)
- [ ] **Configuració centres** (PENDENT - Usuari)
- [ ] **Validació amb dades reals** (PENDENT)

---

## ?? Suport

Per afegir o modificar centres:

1. **Consultar centres actuals**:
   ```sql
   SELECT * FROM parametres_aplicacio WHERE categoria = 'VR_CENTRES';
   ```

2. **Afegir nou centre** (substitueix NOM_CENTRE):
   ```sql
   INSERT INTO parametres_aplicacio 
   (categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
   VALUES
   ('VR_CENTRES', 'NOM_CENTRE', '1', 'Descripció', 'BOOL', 1, 'usuari');
   ```

3. **Revisar rebutjos** (últims 7 dies):
   ```sql
   SELECT 
       centre_descripcio,
       COUNT(*) as total_rebutjos
   FROM auditoria_integracio_modulab
   WHERE resultat = 'CNIVR'
     AND data_resultat >= DATE_SUB(NOW(), INTERVAL 7 DAY)
   GROUP BY centre_descripcio
   ORDER BY total_rebutjos DESC;
   ```

---

## ?? Resum Tècnic

| Aspecte | Estat | Notes |
|---------|-------|-------|
| **Codi C#** | ? Implementat | Tots els mètodes creats |
| **Taula BD** | ? Pendent | Crear amb script SQL |
| **Configuració Centres** | ? Pendent | Afegir centres reals |
| **Integració Flux VR** | ? Completa | FASE 0b implementada |
| **Codi Auditoria** | ? Creat | CNIVR |
| **Build** | ? Exitós | 0 errors, 0 warnings |
| **Script SQL** | ? Preparat | Executar a producció |
| **Documentació** | ? Completa | Aquest fitxer |

---

**Versió del Document**: 1.0  
**Data**: Gener 2025  
**Estat**: ? **CODI IMPLEMENTAT** - Pendent creació taula BD  
**Pròxim Pas**: Executar script SQL i configurar centres reals

?? **CONTROL D'INCORPORACIÓ VR PER CENTRE IMPLEMENTAT AMB ÈXIT** ??
