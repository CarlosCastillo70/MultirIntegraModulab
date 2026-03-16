-- ========================================================================
-- SCRIPT SQL: Verificar Paràmetres Migrats a BD
-- ========================================================================
-- 
-- Descripció:
--   Verifica que els 4 paràmetres migrats existeixen a la taula
--   parametres_aplicacio i mostren els seus valors actuals.
--
-- Paràmetres migrats:
--   1. DIES_VIGENCIA_POSITIUS_DEFAULT
--   2. EMAIL_FROM
--   3. EMAIL_DESTINATARIS
--   4. HABILITAR_NOTIFICACIONS_EMAIL
--
-- Data: Gener 2025
-- ========================================================================

-- Verificar que la taula existeix
SELECT 'Taula parametres_aplicacio' as verificacio, 
       CASE WHEN COUNT(*) > 0 THEN 'EXISTS' ELSE 'NO EXISTS' END as estat
FROM information_schema.tables 
WHERE table_schema = DATABASE() 
  AND table_name = 'parametres_aplicacio';

-- Verificar paràmetres específics migrats
SELECT 
    categoria,
    clau,
    valor,
    descripcio,
    tipus_dada,
    actiu,
    dt_create,
    dt_update
FROM parametres_aplicacio
WHERE categoria = 'CONFIG_GENERAL'
  AND clau IN (
      'DIES_VIGENCIA_POSITIUS_DEFAULT',
      'EMAIL_FROM',
      'EMAIL_DESTINATARIS',
      'HABILITAR_NOTIFICACIONS_EMAIL'
  )
  AND dt_delete IS NULL
ORDER BY clau;

-- Comptar paràmetres actius
SELECT 
    'Total paràmetres CONFIG_GENERAL' as descripcio,
    COUNT(*) as total,
    SUM(CASE WHEN actiu = 1 THEN 1 ELSE 0 END) as actius,
    SUM(CASE WHEN actiu = 0 THEN 1 ELSE 0 END) as inactius
FROM parametres_aplicacio
WHERE categoria = 'CONFIG_GENERAL'
  AND dt_delete IS NULL;

-- Verificar valors dels 4 paràmetres (SI NO EXISTEIXEN, INSERIR-LOS)
-- Només executar l'INSERT si els paràmetres NO existeixen

-- 1. DIES_VIGENCIA_POSITIUS_DEFAULT
INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
SELECT 
    'CONFIG_GENERAL',
    'DIES_VIGENCIA_POSITIUS_DEFAULT',
    '365',
    'Dies per defecte vigència positius (utilitzat per Comprovació 2)',
    'INT',
    1,
    'SYSTEM'
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 
    FROM parametres_aplicacio 
    WHERE categoria = 'CONFIG_GENERAL' 
      AND clau = 'DIES_VIGENCIA_POSITIUS_DEFAULT'
      AND dt_delete IS NULL
);

-- 2. EMAIL_FROM (remitent)
INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
SELECT 
    'CONFIG_GENERAL',
    'EMAIL_FROM',
    'ccastillo.ics@gencat.cat',
    'Email remitent per notificacions del sistema',
    'STRING',
    1,
    'SYSTEM'
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 
    FROM parametres_aplicacio 
    WHERE categoria = 'CONFIG_GENERAL' 
      AND clau = 'EMAIL_FROM'
      AND dt_delete IS NULL
);

-- 3. EMAIL_DESTINATARIS (destinataris separats per ;)
INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
SELECT 
    'CONFIG_GENERAL',
    'EMAIL_DESTINATARIS',
    'carloscastillollucia@gmail.com',
    'Emails destinataris per notificacions (separats per punt i coma)',
    'STRING',
    1,
    'SYSTEM'
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 
    FROM parametres_aplicacio 
    WHERE categoria = 'CONFIG_GENERAL' 
      AND clau = 'EMAIL_DESTINATARIS'
      AND dt_delete IS NULL
);

-- 4. HABILITAR_NOTIFICACIONS_EMAIL
INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
SELECT 
    'CONFIG_GENERAL',
    'HABILITAR_NOTIFICACIONS_EMAIL',
    '1',
    'Enviar emails automàtics (1=SÍ, 0=NO)',
    'BOOL',
    1,
    'SYSTEM'
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 
    FROM parametres_aplicacio 
    WHERE categoria = 'CONFIG_GENERAL' 
      AND clau = 'HABILITAR_NOTIFICACIONS_EMAIL'
      AND dt_delete IS NULL
);

-- Verificar inserció
SELECT 'Paràmetres després d''inserció' as verificacio;

SELECT 
    categoria,
    clau,
    valor,
    descripcio,
    actiu
FROM parametres_aplicacio
WHERE categoria = 'CONFIG_GENERAL'
  AND clau IN (
      'DIES_VIGENCIA_POSITIUS_DEFAULT',
      'EMAIL_FROM',
      'EMAIL_DESTINATARIS',
      'HABILITAR_NOTIFICACIONS_EMAIL'
  )
  AND dt_delete IS NULL
ORDER BY clau;

-- ========================================================================
-- EXEMPLES DE GESTIÓ DELS PARÀMETRES
-- ========================================================================

-- Modificar dies vigència positius
/*
UPDATE parametres_aplicacio
SET valor = '180',
    usuari_modificacio = 'usuari_modificador'
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'DIES_VIGENCIA_POSITIUS_DEFAULT';
*/

-- Modificar email remitent
/*
UPDATE parametres_aplicacio
SET valor = 'nou_remitent@hospital.cat',
    usuari_modificacio = 'usuari_modificador'
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'EMAIL_FROM';
*/

-- Modificar email destinataris
/*
UPDATE parametres_aplicacio
SET valor = 'destinatari1@hospital.cat;destinatari2@hospital.cat',
    usuari_modificacio = 'usuari_modificador'
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'EMAIL_DESTINATARIS';
*/

-- Desactivar notificacions per email
/*
UPDATE parametres_aplicacio
SET valor = '0',
    usuari_modificacio = 'usuari_modificador'
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'HABILITAR_NOTIFICACIONS_EMAIL';
*/

-- Reactivar notificacions per email
/*
UPDATE parametres_aplicacio
SET valor = '1',
    usuari_modificacio = 'usuari_modificador'
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'HABILITAR_NOTIFICACIONS_EMAIL';
*/

-- Consultar històric de modificacions (si tens taula d'audit)
/*
SELECT 
    clau,
    valor,
    dt_update,
    usuari_modificacio
FROM parametres_aplicacio
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'DIES_VIGENCIA_POSITIUS_DEFAULT'
ORDER BY dt_update DESC;
*/

-- ========================================================================
-- NOTES IMPORTANTS
-- ========================================================================

/*
1. DIES_VIGENCIA_POSITIUS_DEFAULT:
   - Valor recomanat: 365 dies (1 any)
   - Utilitzat per Comprovació 2 (negatius) quan tipus_mostra no té dies_vigencia_positiu
   - Es pot ajustar segons criteri epidemiològic

2. EMAIL_FROM:
   - Adreça email del remitent per les notificacions
   - Substitueix EmailFrom d'App.config

3. EMAIL_DESTINATARIS:
   - Adreces email dels destinataris per les notificacions
   - Separats per punt i coma (;)
   - Substitueix EmailTo d'App.config

4. HABILITAR_NOTIFICACIONS_EMAIL:
   - 1 = Enviar emails automàtics
   - 0 = NO enviar emails
   - Substitueix EnviarEmailLog d'App.config
   
5. MIGRACIÓ GRADUAL:
   - L'aplicació llegeix primer de BD
   - Si no troba el paràmetre, utilitza el valor d'App.config (fallback)
   - Es pot mantenir App.config com a backup

6. CANVIAR VALORS:
   - Els canvis es fan via UPDATE SQL
   - NO requereixen redeployment de l'aplicació
   - Els canvis són efectius immediatament (següent execució)
*/

-- ========================================================================
-- FI SCRIPT
-- ========================================================================
