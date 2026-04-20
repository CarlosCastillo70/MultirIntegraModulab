# Enviament d'Email d'Alerta per MDO

## ?? Resum de la Implementació

S'ha implementat l'enviament automàtic d'emails d'alerta quan es detecta una mostra MDO (Malaltia de Declaració Obligatòria) durant el processament de mostres de Modulab.

---

## ?? Objectiu

Notificar de manera immediata i automàtica quan es detecta una mostra MDO, enviant un correu electrònic amb tota la informació rellevant als responsables sanitaris configurats.

---

## ?? Canvis Implementats

### 1. Nou Mètode a `EmailService.cs`

**Arxiu:** `MultirIntegraModulab\Infrastructure\ExternalServices\Email\EmailService.cs`

#### Mètode Principal: `EnviarEmailMDO()`

```csharp
/// <summary>
/// Envia un email d'alerta de MDO (Malaltia de Declaració Obligatòria)
/// </summary>
/// <param name="mostra">Mostra MDO detectada</param>
/// <param name="emailsDestinatarisMDO">Llista d'emails destinataris per MDO</param>
/// <returns>True si s'ha enviat correctament</returns>
public bool EnviarEmailMDO(Domain.Entities.Mostra mostra, List<string> emailsDestinatarisMDO)
```

**Característiques:**
- **Prioritat ALTA**: Els emails MDO s'envien amb `MailPriority.High`
- **Validació**: Comprova que la mostra i els destinataris no siguin null
- **Format**: Text pla amb format estructurat i clar
- **Emoji d'alerta**: Utilitza ?? per identificar ràpidament els emails MDO

#### Mètode Auxiliar: `GenerarCosEmailMDO()`

Genera el contingut de l'email amb:
- Capçalera d'alerta visible
- Data i hora de detecció
- Informació de la mostra (etiqueta, pacient, data resultat)
- Detalls de cada resultat MDO:
  - Tipus de prova
  - Microorganisme
  - Resultat (POSITIU ?? / altres)
  - Centre i servei
  - Metge sol·licitant
  - Data de validació
- Missatge d'atenció amb instruccions

---

### 2. Modificacions a `ProcessarMostresUseCase.cs`

**Arxiu:** `MultirIntegraModulab\Application\UseCases\ProcessarMostres\ProcessarMostresUseCase.cs`

#### 2.1 Afegit Camp Private per EmailService

```csharp
private readonly Infrastructure.ExternalServices.Email.EmailService _emailService;
```

#### 2.2 Modificat Constructor

```csharp
public ProcessarMostresUseCase(
    IModulabRepository modulabRepository,
    IMultiRRepository multiRRepository,
    IPacientWebService pacientWebService,
    ILoggerService logger,
    IConfigurationService configurationService,
    ValidarMostraUseCase validarMostraUseCase,
    Infrastructure.ExternalServices.Email.EmailService emailService = null)  // ?? NOU PARÀMETRE
```

**Nota:** El paràmetre `emailService` és opcional (pot ser null) per mantenir compatibilitat.

#### 2.3 Modificat Mètode `DetectarMostraMDO()`

Ara, quan es detecta una MDO, crida automàticament a:
```csharp
EnviarEmailAlertaMDO(mostra);
```

#### 2.4 Nou Mètode `EnviarEmailAlertaMDO()`

```csharp
/// <summary>
/// Envia un email d'alerta quan es detecta una mostra MDO
/// </summary>
/// <param name="mostra">Mostra MDO detectada</param>
private void EnviarEmailAlertaMDO(Mostra mostra)
```

**Funcionalitat:**
1. Comprova si el servei d'email està disponible
2. Obté els destinataris des de `parametres_aplicacio` amb categoria `EMAIL_MDO`
3. Valida que hi hagi destinataris configurats
4. Envia l'email utilitzant `_emailService.EnviarEmailMDO()`
5. Registra el resultat al log (èxit o error)

**Logs generats:**
```
?? Enviant email d'alerta MDO a 2 destinatari(s)...
? Email d'alerta MDO enviat correctament a: mdo@hospital.cat, urgencies@hospital.cat
```

---

### 3. Modificacions a `ProcessamentMostresService.cs`

**Arxiu:** `MultirIntegraModulab\Application\Services\ProcessamentMostresService.cs`

#### Modificat Constructor

Ara accepta el servei d'email com a paràmetre opcional:

```csharp
public ProcessamentMostresService(
    IModulabRepository modulabRepository,
    IMultiRRepository multiRRepository,
    IPacientWebService pacientWebService,
    ILoggerService logger,
    IConfigurationService configurationService,
    Infrastructure.ExternalServices.Email.EmailService emailService = null)  // ?? NOU
{
    // ...
    _processarMostresUseCase = new ProcessarMostresUseCase(
        _modulabRepository,
        _multiRRepository,
        _pacientWebService,
        _logger,
        _configurationService,
        _validarMostraUseCase,
        emailService);  // Passar el servei d'email
}
```

---

### 4. Modificacions a `Program.cs`

**Arxiu:** `MultirIntegraModulab\Program.cs`

#### Configuració del Servei d'Email per MDO

S'ha afegit una nova secció (FASE 1.6) per configurar el servei d'email específicament per MDO:

```csharp
// 1.6 Configurar servei d'email per MDO (utilitzant la mateixa configuració)
EmailService emailServiceMDO = null;
if (configService.EnviarEmailLog)
{
    try
    {
        emailServiceMDO = new EmailService(
            configService.SmtpServer,
            configService.SmtpPort,
            configService.SmtpUsuari,
            configService.SmtpPassword,
            configService.SmtpUsarSSL,
            configService.EmailFrom,
            configService.EmailsDestinataris,
            loggerService
        );
        loggerService.Info("? Servei d'email per MDO configurat");
    }
    catch (Exception exEmail)
    {
        loggerService.Warning($"?? No s'ha pogut configurar el servei d'email per MDO: {exEmail.Message}");
    }
}
else
{
    loggerService.Info("?? Servei d'email desactivat - no s'enviaran alertes MDO");
}

// 1.7 Configurar servei d'aplicació
var processamentService = new ProcessamentMostresService(
    modulabRepository,
    multiRRepository,
    pacientWebService,
    loggerService,
    configService,
    emailServiceMDO  // Passar el servei d'email
);
```

**Lògica:**
- Si `EnviarEmailLog` està activat ? configura el servei d'email per MDO
- Si hi ha error en la configuració ? es registra un warning però l'aplicació continua
- Si `EnviarEmailLog` està desactivat ? no es configura el servei (no s'enviaran alertes MDO)

---

## ??? Configuració de Base de Dades

### Taula: `parametres_aplicacio`

Per configurar els destinataris dels emails de MDO, cal afegir registres a la taula `parametres_aplicacio`:

```sql
-- Exemple: Afegir destinataris per emails de MDO
INSERT INTO parametres_aplicacio (categoria, clau, valor, dt_create, dt_update, actiu)
VALUES 
('EMAIL_MDO', 'mdo@hospital.cat', 'Email principal MDO', NOW(), NOW(), 1),
('EMAIL_MDO', 'urgencies@hospital.cat', 'Email urgències', NOW(), NOW(), 1),
('EMAIL_MDO', 'epidemiologia@hospital.cat', 'Email epidemiologia', NOW(), NOW(), 1);
```

**Estructura dels registres:**
- **categoria**: `'EMAIL_MDO'` (identificador de la categoria)
- **clau**: L'adreça de correu electrònic destinatària
- **valor**: Descripció del destinatari (opcional)
- **actiu**: `1` per emails actius, `0` per desactivar-los

---

## ?? Format de l'Email MDO

### Exemple d'Email Enviat:

```
Subject: ?? MDO DETECTADA - Mostra M001234567 - 15/01/2025 10:30

=================================================
  ?? ALERTA MDO - MALALTIA DE DECLARACIÓ OBLIGATÒRIA
=================================================

Data detecció: 15/01/2025 10:30:15

INFORMACIÓ DE LA MOSTRA:
-------------------
• Etiqueta:         M001234567
• Pacient SAP:      12345678
• Data resultat:    14/01/2025 15:20
• Nombre resultats: 1

RESULTATS MDO DETECTATS:
-------------------

Resultat 1:
  • Tipus prova:       CULTIU MYCOBACTERIUM
  • Microorganisme:    Mycobacterium tuberculosis
  • Resultat:          POSITIU ??
  • Centre:            HOSPITAL UNIVERSITARI
  • Servei:            MEDICINA INTERNA
  • Metge:             Dr. Joan Garcia
  • Data validació:    14/01/2025 18:30

=================================================

?? ATENCIÓ:
Aquesta mostra ha estat identificada com a MDO
(Malaltia de Declaració Obligatòria).
Cal seguir els protocols establerts per a la seva gestió.

=================================================

--
Aquest és un missatge automàtic del sistema MultiR
Prioritat: ALTA
```

---

## ?? Flux d'Enviament d'Email

```
Detecció MDO a ProcessarMostresUseCase
    ?
DetectarMostraMDO(mostra) retorna true
    ?
EnviarEmailAlertaMDO(mostra)
    ?
Comprova si _emailService != null
    ?
Obté destinataris de parametres_aplicacio (EMAIL_MDO)
    ?
Valida que hi hagi destinataris
    ?
_emailService.EnviarEmailMDO(mostra, emailsMDO)
    ?
GenerarCosEmailMDO(mostra, DateTime.Now)
    ?
Configura MailMessage amb prioritat ALTA
    ?
Envia via SMTP
    ?
Registra resultat al log
```

---

## ?? Logs Generats

### Quan s'envia un email MDO amb èxit:

```
?? Comprovant si la mostra és MDO (Malaltia de Declaració Obligatòria)...
   ?? MDO detectat!
      Tipus prova: CULTIU MYCOBACTERIUM
      Microorganisme: Mycobacterium tuberculosis
      Estat resultat: POSITIU
? MOSTRA MDO confirmada - 1 resultat(s) MDO detectat(s)
?? Aquesta mostra requereix gestió especial per MDO
?? Enviant email d'alerta MDO a 3 destinatari(s)...
?? Preparant enviament d'email MDO: mostra M001234567
?? Autenticació SMTP: smtp_user@hospital.cat
?? Enviant email MDO a 3 destinatari(s) via smtp.hospital.cat:587...
? Email MDO enviat a: mdo@hospital.cat, urgencies@hospital.cat, epidemiologia@hospital.cat
? Email d'alerta MDO enviat correctament a: mdo@hospital.cat, urgencies@hospital.cat, epidemiologia@hospital.cat
```

### Quan no hi ha destinataris configurats:

```
?? Comprovant si la mostra és MDO (Malaltia de Declaració Obligatòria)...
   ?? MDO detectat!
      Tipus prova: CULTIU MYCOBACTERIUM
      Microorganisme: Mycobacterium tuberculosis
      Estat resultat: POSITIU
? MOSTRA MDO confirmada - 1 resultat(s) MDO detectat(s)
?? Aquesta mostra requereix gestió especial per MDO
?? No hi ha destinataris configurats per emails MDO a parametres_aplicacio (EMAIL_MDO)
```

### Quan el servei d'email no està configurat:

```
?? Comprovant si la mostra és MDO (Malaltia de Declaració Obligatòria)...
   ?? MDO detectat!
      Tipus prova: CULTIU MYCOBACTERIUM
      Microorganisme: Mycobacterium tuberculosis
      Estat resultat: POSITIU
? MOSTRA MDO confirmada - 1 resultat(s) MDO detectat(s)
?? Aquesta mostra requereix gestió especial per MDO
?? Servei d'email no configurat - no s'envia alerta MDO
```

---

## ?? Configuració

### 1. Activar l'enviament d'emails al `App.config`:

```xml
<appSettings>
    <!-- Email -->
    <add key="EnviarEmailLog" value="true" />
    <add key="SmtpServer" value="smtp.hospital.cat" />
    <add key="SmtpPort" value="587" />
    <add key="SmtpUsarSSL" value="true" />
    <add key="SmtpUsuari" value="multir@hospital.cat" />
    <add key="SmtpPassword" value="PASSWORD_SEGUR" />
    <add key="EmailFrom" value="multir@hospital.cat" />
    <add key="EmailsDestinataris" value="admin1@hospital.cat;admin2@hospital.cat" />
</appSettings>
```

### 2. Configurar destinataris MDO a la base de dades:

```sql
-- Afegir destinataris per emails MDO
INSERT INTO parametres_aplicacio (categoria, clau, valor, actiu)
VALUES 
('EMAIL_MDO', 'mdo@hospital.cat', 'Email principal MDO', 1),
('EMAIL_MDO', 'urgencies@hospital.cat', 'Email urgències', 1);

-- Consultar destinataris configurats
SELECT * FROM parametres_aplicacio 
WHERE categoria = 'EMAIL_MDO' 
  AND actiu = 1;

-- Desactivar un destinatari sense esborrar-lo
UPDATE parametres_aplicacio 
SET actiu = 0 
WHERE categoria = 'EMAIL_MDO' 
  AND clau = 'antiguo@hospital.cat';

-- Esborrar un destinatari
DELETE FROM parametres_aplicacio 
WHERE categoria = 'EMAIL_MDO' 
  AND clau = 'temporal@hospital.cat';
```

---

## ?? Beneficis

1. **Notificació Immediata**: Els responsables sanitaris reben l'alerta tan aviat com es detecta la MDO

2. **Informació Completa**: L'email conté tota la informació necessària per actuar

3. **Prioritat Alta**: Els emails MDO es marquen amb prioritat alta per facilitar la seva identificació

4. **Configuració Flexible**: Els destinataris es gestionen a la base de dades sense necessitat de recompilar

5. **Logs Detallats**: Totes les accions queden registrades al log per auditoria

6. **Robustesa**: Si el servei d'email falla, el processament continua i es registra l'error

7. **Reutilització**: Utilitza la mateixa infraestructura d'emails que el resum diari

---

## ? Validació

Per verificar que la funcionalitat funciona correctament:

### 1. Configurar un tipus de prova com a MDO:

```sql
UPDATE tipusprova 
SET incorpora_mdo = 1  -- MDO si resultat positiu
WHERE codi = 'CULTIU MYCOBACTERIUM';
```

### 2. Configurar destinataris de prova:

```sql
INSERT INTO parametres_aplicacio (categoria, clau, valor, actiu)
VALUES ('EMAIL_MDO', 'test@hospital.cat', 'Email de prova', 1);
```

### 3. Processar una mostra amb aquesta prova

### 4. Comprovar:
- ? Es detecta com a MDO al log
- ? S'intenta enviar l'email
- ? L'email arriba als destinataris configurats
- ? El format i contingut són correctes
- ? La prioritat és ALTA

---

## ?? Estat de la Implementació

? **Completat:**
- Mètode `EnviarEmailMDO()` a EmailService
- Mètode `GenerarCosEmailMDO()` per generar contingut
- Mètode `EnviarEmailAlertaMDO()` a ProcessarMostresUseCase
- Modificació de `DetectarMostraMDO()` per cridar l'enviament
- Integració amb ProcessamentMostresService
- Configuració al Program.cs
- Obtenció de destinataris des de `parametres_aplicacio`
- Logs detallats de tot el procés
- Compilació exitosa

?? **Dependències externes:**
- Configuració SMTP al App.config
- Destinataris configurats a `parametres_aplicacio` (EMAIL_MDO)
- Tipus de prova marcat com a MDO a la taula `tipusprova`

---

## ?? Data d'Implementació

**Data:** 2025-01-XX  
**Versió:** 1.0  
**Context:** Millora de la funcionalitat MDO amb notificació automàtica

---

## ?? Referències

- **Codi Font:**
  - `MultirIntegraModulab\Infrastructure\ExternalServices\Email\EmailService.cs`
  - `MultirIntegraModulab\Application\UseCases\ProcessarMostres\ProcessarMostresUseCase.cs`
  - `MultirIntegraModulab\Application\Services\ProcessamentMostresService.cs`
  - `MultirIntegraModulab\Program.cs`

- **Documentació relacionada:**
  - `MDO_DETECCIO.md` (detecció de MDO)
  - `AFEGIT_SHORTDESCRIPTION1_INTEGRACIO_MODULAB.md` (camp utilitzat per detectar MDO)

---

## ?? Seguretat i Privacitat

**Important:** Els emails enviats contenen informació sensible de pacients (SAP, dades clíniques). 

**Recomanacions:**
1. Configurar destinataris només amb adreces corporatives segures
2. Utilitzar SMTP amb SSL/TLS activat
3. Revisar periòdicament els destinataris configurats
4. Mantenir els logs en un lloc segur
5. Considerar la implementació de xifratge addicional si escau

---

## ?? Millores Futures (Opcionals)

1. **Email HTML**: Millorar el format visual amb HTML
2. **Adjuntar PDF**: Generar un informe PDF amb les dades de la mostra
3. **Confirmació de lectura**: Sol·licitar confirmació de lectura
4. **Integració amb sistema de tickets**: Crear automàticament un ticket o tasca
5. **SMS**: Afegir notificació per SMS per casos crítics
6. **Dashboard**: Integrar amb un dashboard de visualització de MDO en temps real
