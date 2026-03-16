-- ========================================================================
-- SCRIPT: Migració de EMAIL_NOTIFICACIONS a EMAIL_FROM + EMAIL_DESTINATARIS
-- ========================================================================
-- 
-- Descripció:
--   Aquest script migra el paràmetre EMAIL_NOTIFICACIONS (que s'utilitzava
--   incorrectament per dues finalitats) a dos paràmetres separats:
--   - EMAIL_FROM: Email remitent
--   - EMAIL_DESTINATARIS: Emails destinataris (separats per ;)
--
-- IMPORTANT: Executar aquest script ABANS de desplegar la nova versió
--
-- Data: Febrer 2025
-- ========================================================================

-- Pas 1: Verificar estat actual
-- ========================================================================
SELECT 'ESTAT ACTUAL' as pas;

SELECT categoria, clau, valor, descripcio
FROM parametres_aplicacio
WHERE categoria = 'CONFIG_GENERAL'
  AND clau IN ('EMAIL_NOTIFICACIONS', 'EMAIL_FROM', 'EMAIL_DESTINATARIS')
  AND dt_delete IS NULL;

-- Pas 2: Crear EMAIL_FROM si no existeix
-- ========================================================================
SELECT 'CREAR EMAIL_FROM' as pas;

INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
SELECT 
    'CONFIG_GENERAL',
    'EMAIL_FROM',
    'ccastillo.ics@gencat.cat',
    'Email remitent per notificacions del sistema',
    'STRING',
    1,
    'ADMIN'
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 
    FROM parametres_aplicacio 
    WHERE categoria = 'CONFIG_GENERAL' 
      AND clau = 'EMAIL_FROM'
      AND dt_delete IS NULL
);

-- Pas 3: Crear EMAIL_DESTINATARIS copiant el valor de EMAIL_NOTIFICACIONS
-- ========================================================================
SELECT 'CREAR EMAIL_DESTINATARIS' as pas;

INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
SELECT 
    'CONFIG_GENERAL',
    'EMAIL_DESTINATARIS',
    COALESCE(
        (SELECT valor FROM parametres_aplicacio 
         WHERE categoria = 'CONFIG_GENERAL' 
           AND clau = 'EMAIL_NOTIFICACIONS' 
           AND dt_delete IS NULL),
        'admin@hospital.cat'
    ),
    'Emails destinataris per notificacions (separats per punt i coma)',
    'STRING',
    1,
    'ADMIN'
FROM DUAL
WHERE NOT EXISTS (
    SELECT 1 
    FROM parametres_aplicacio 
    WHERE categoria = 'CONFIG_GENERAL' 
      AND clau = 'EMAIL_DESTINATARIS'
      AND dt_delete IS NULL
);

-- Pas 4: (OPCIONAL) Eliminar EMAIL_NOTIFICACIONS ja que ja no s'utilitza
-- ========================================================================
-- NOTA: Comentat per seguretat. Descomentar després de validar que tot funciona.

/*
SELECT 'ELIMINAR EMAIL_NOTIFICACIONS (OPCIONAL)' as pas;

UPDATE parametres_aplicacio
SET dt_delete = NOW(),
    usuari_modificacio = 'ADMIN'
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'EMAIL_NOTIFICACIONS'
  AND dt_delete IS NULL;
*/

-- Pas 5: Verificar estat final
-- ========================================================================
SELECT 'ESTAT FINAL' as pas;

SELECT categoria, clau, valor, descripcio, actiu
FROM parametres_aplicacio
WHERE categoria = 'CONFIG_GENERAL'
  AND clau IN ('EMAIL_NOTIFICACIONS', 'EMAIL_FROM', 'EMAIL_DESTINATARIS')
  AND dt_delete IS NULL
ORDER BY clau;

-- ========================================================================
-- VALIDACIÓ POST-MIGRACIÓ
-- ========================================================================

SELECT 
    CASE 
        WHEN EXISTS (
            SELECT 1 FROM parametres_aplicacio 
            WHERE categoria = 'CONFIG_GENERAL' 
              AND clau = 'EMAIL_FROM' 
              AND dt_delete IS NULL
        ) AND EXISTS (
            SELECT 1 FROM parametres_aplicacio 
            WHERE categoria = 'CONFIG_GENERAL' 
              AND clau = 'EMAIL_DESTINATARIS' 
              AND dt_delete IS NULL
        ) 
        THEN '? MIGRACIÓ COMPLETA: Paràmetres EMAIL_FROM i EMAIL_DESTINATARIS creats correctament'
        ELSE '? ERROR: Falten paràmetres per crear'
    END as resultat_migracio;

-- ========================================================================
-- NOTES D'ÚS
-- ========================================================================
--
-- Per modificar els emails després de la migració:
--
-- 1. Canviar email remitent:
--    UPDATE parametres_aplicacio
--    SET valor = 'nou_remitent@hospital.cat'
--    WHERE categoria = 'CONFIG_GENERAL' AND clau = 'EMAIL_FROM';
--
-- 2. Canviar emails destinataris (múltiples separats per ;):
--    UPDATE parametres_aplicacio
--    SET valor = 'admin@hospital.cat;epidemio@hospital.cat;ti@hospital.cat'
--    WHERE categoria = 'CONFIG_GENERAL' AND clau = 'EMAIL_DESTINATARIS';
--
-- ========================================================================
