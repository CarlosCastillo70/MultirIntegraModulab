-- ========================================================================
-- SCRIPT SQL: Renombrar EMAIL_DESTINATARIS a EMAIL_RESUM_CARREGA
-- ========================================================================
-- 
-- Descripció:
--   Canvia la clau del paràmetre EMAIL_DESTINATARIS a EMAIL_RESUM_CARREGA
--   per fer més descriptiu el seu propòsit (emails de resum de càrrega).
--
-- Data creació: Gener 2025
-- Versió: 1.0
-- Base de dades: MySQL
-- Taula afectada: parametres_aplicacio
--
-- ========================================================================

-- ========================================================================
-- 1. VERIFICAR CONFIGURACIÓ ACTUAL
-- ========================================================================

-- Consultar el paràmetre actual
SELECT 
    id,
    categoria,
    clau,
    valor,
    actiu,
    dt_create,
    dt_update
FROM parametres_aplicacio
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'EMAIL_DESTINATARIS';

-- ========================================================================
-- 2. RENOMBRAR LA CLAU
-- ========================================================================

-- Actualitzar la clau de EMAIL_DESTINATARIS a EMAIL_RESUM_CARREGA
UPDATE parametres_aplicacio 
SET clau = 'EMAIL_RESUM_CARREGA',
    dt_update = NOW()
WHERE categoria = 'CONFIG_GENERAL' 
  AND clau = 'EMAIL_DESTINATARIS';

-- Verificar que s'ha fet el canvi
SELECT 
    id,
    categoria,
    clau,
    valor,
    actiu,
    dt_create,
    dt_update
FROM parametres_aplicacio
WHERE categoria = 'CONFIG_GENERAL'
  AND clau = 'EMAIL_RESUM_CARREGA';

-- ========================================================================
-- 3. VERIFICAR QUE NO QUEDI CAP REGISTRE AMB LA CLAU ANTIGA
-- ========================================================================

SELECT COUNT(*) as registres_amb_clau_antiga
FROM parametres_aplicacio
WHERE clau = 'EMAIL_DESTINATARIS';

-- Hauria de retornar 0

-- ========================================================================
-- 4. SI NO EXISTEIX EL PARÀMETRE, CREAR-LO
-- ========================================================================

-- Exemple: Si no existeix cap registre amb EMAIL_DESTINATARIS ni EMAIL_RESUM_CARREGA
/*
INSERT INTO parametres_aplicacio (categoria, clau, valor, dt_create, dt_update, actiu)
VALUES ('CONFIG_GENERAL', 'EMAIL_RESUM_CARREGA', 'admin@hospital.cat;it@hospital.cat', NOW(), NOW(), 1);
*/

-- ========================================================================
-- 5. VERIFICACIÓ FINAL
-- ========================================================================

-- Llistar tots els paràmetres d'email
SELECT 
    id,
    categoria,
    clau,
    valor,
    actiu
FROM parametres_aplicacio
WHERE categoria = 'CONFIG_GENERAL'
  AND (clau LIKE '%EMAIL%' OR clau LIKE '%MAIL%')
ORDER BY clau;

-- Esperem veure:
-- - EMAIL_FROM
-- - EMAIL_RESUM_CARREGA (abans EMAIL_DESTINATARIS)
-- - HABILITAR_NOTIFICACIONS_EMAIL

-- ========================================================================
-- 6. ROLLBACK (si cal desfer els canvis)
-- ========================================================================

-- NOMÉS SI CAL DESFER EL CANVI:
/*
UPDATE parametres_aplicacio 
SET clau = 'EMAIL_DESTINATARIS',
    dt_update = NOW()
WHERE categoria = 'CONFIG_GENERAL' 
  AND clau = 'EMAIL_RESUM_CARREGA';
*/

-- ========================================================================
-- NOTES IMPORTANTS
-- ========================================================================

/*
1. ABANS D'EXECUTAR:
   - Fer backup de la taula parametres_aplicacio
   - Verificar que l'aplicació està aturada
   - Comprovar que no hi ha processos en execució

2. FORMAT DEL VALOR:
   - Els emails han d'estar separats per punt i coma (;)
   - Exemple: 'admin1@hospital.cat;admin2@hospital.cat;it@hospital.cat'
   - El sistema fa Trim() automàticament dels espais

3. DESPRÉS DEL CANVI:
   - Verificar que l'aplicació llegeix correctament els nous paràmetres
   - Comprovar els logs per assegurar que es carreguen els emails
   - Provar l'enviament d'un email de resum

4. DIFERÈNCIA AMB EMAIL_MDO:
   
   EMAIL_RESUM_CARREGA (antic EMAIL_DESTINATARIS):
   - Categoria: CONFIG_GENERAL
   - Clau: EMAIL_RESUM_CARREGA
   - Valor: 'admin1@hospital.cat;admin2@hospital.cat' (múltiples emails en UN registre)
   - Ús: Emails de resum diari del processament
   
   EMAIL_MDO:
   - Categoria: CONFIG_GENERAL
   - Clau: EMAIL_MDO
   - Valor: 'mdo@hospital.cat' (un email per registre)
   - Ús: Alertes específiques de MDO
   - Múltiples registres amb la mateixa clau però diferents valors

5. COMPATIBILITAT:
   - El codi té fallback a App.config si no troba el paràmetre a BD
   - Si el canvi falla, l'aplicació continuarà funcionant amb App.config
*/

-- ========================================================================
-- FI DE L'SCRIPT
-- ========================================================================
