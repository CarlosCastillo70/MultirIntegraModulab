-- ========================================================================
-- SCRIPT SQL: Configuració de destinataris d'emails per MDO
-- ========================================================================
-- 
-- Descripció:
--   Configura els destinataris d'emails d'alerta per a Malalties de 
--   Declaració Obligatòria (MDO) a la taula parametres_aplicacio.
--
-- Data creació: Gener 2025
-- Versió: 1.0
-- Base de dades: MySQL
-- Taula afectada: parametres_aplicacio
--
-- ========================================================================

-- ========================================================================
-- 1. CONSULTAR DESTINATARIS ACTUALS
-- ========================================================================

SELECT 
    id,
    categoria,
    clau AS email,
    valor AS descripcio,
    actiu,
    dt_create,
    dt_update
FROM parametres_aplicacio
WHERE categoria = 'EMAIL_MDO'
ORDER BY actiu DESC, clau;

-- ========================================================================
-- 2. AFEGIR DESTINATARIS PER MDO
-- ========================================================================

-- Exemple: Afegir el responsable de MDO
INSERT INTO parametres_aplicacio (categoria, clau, valor, dt_create, dt_update, actiu)
VALUES ('EMAIL_MDO', 'mdo@hospital.cat', 'Responsable MDO', NOW(), NOW(), 1);

-- Exemple: Afegir el servei d'urgències
INSERT INTO parametres_aplicacio (categoria, clau, valor, dt_create, dt_update, actiu)
VALUES ('EMAIL_MDO', 'urgencies@hospital.cat', 'Servei Urgències', NOW(), NOW(), 1);

-- Exemple: Afegir el servei d'epidemiologia
INSERT INTO parametres_aplicacio (categoria, clau, valor, dt_create, dt_update, actiu)
VALUES ('EMAIL_MDO', 'epidemiologia@hospital.cat', 'Servei Epidemiologia', NOW(), NOW(), 1);

-- Exemple: Afegir un supervisor
INSERT INTO parametres_aplicacio (categoria, clau, valor, dt_create, dt_update, actiu)
VALUES ('EMAIL_MDO', 'supervisor.laboratori@hospital.cat', 'Supervisor Laboratori', NOW(), NOW(), 1);

-- ========================================================================
-- 3. MODIFICAR DESTINATARIS EXISTENTS
-- ========================================================================

-- Desactivar un destinatari (sense esborrar-lo)
UPDATE parametres_aplicacio 
SET actiu = 0, 
    dt_update = NOW()
WHERE categoria = 'EMAIL_MDO' 
  AND clau = 'antiguo@hospital.cat';

-- Reactivar un destinatari
UPDATE parametres_aplicacio 
SET actiu = 1, 
    dt_update = NOW()
WHERE categoria = 'EMAIL_MDO' 
  AND clau = 'mdo@hospital.cat';

-- Actualitzar la descripció d'un destinatari
UPDATE parametres_aplicacio 
SET valor = 'Nova descripció',
    dt_update = NOW()
WHERE categoria = 'EMAIL_MDO' 
  AND clau = 'mdo@hospital.cat';

-- ========================================================================
-- 4. ESBORRAR DESTINATARIS
-- ========================================================================

-- ATENCIÓ: Això esborra permanentment el destinatari
-- Considerar desactivar-lo (actiu=0) en lloc d'esborrar-lo

-- Esborrar un destinatari específic
DELETE FROM parametres_aplicacio 
WHERE categoria = 'EMAIL_MDO' 
  AND clau = 'temporal@hospital.cat';

-- Esborrar TOTS els destinataris MDO inactius
DELETE FROM parametres_aplicacio 
WHERE categoria = 'EMAIL_MDO' 
  AND actiu = 0;

-- ========================================================================
-- 5. VERIFICACIÓ
-- ========================================================================

-- Comptar destinataris actius
SELECT 
    COUNT(*) as total_destinataris_actius
FROM parametres_aplicacio
WHERE categoria = 'EMAIL_MDO'
  AND actiu = 1;

-- Llistar tots els destinataris amb detalls
SELECT 
    id,
    clau AS email,
    valor AS descripcio,
    CASE 
        WHEN actiu = 1 THEN '? Actiu'
        ELSE '? Inactiu'
    END AS estat,
    dt_create AS data_creacio,
    dt_update AS ultima_modificacio
FROM parametres_aplicacio
WHERE categoria = 'EMAIL_MDO'
ORDER BY actiu DESC, clau;

-- Verificar si hi ha algun destinatari configurat
SELECT 
    CASE 
        WHEN COUNT(*) > 0 THEN CONCAT('? Hi ha ', COUNT(*), ' destinatari(s) configurat(s)')
        ELSE '?? NO hi ha cap destinatari configurat - cal afegir-ne!'
    END AS estat_configuracio
FROM parametres_aplicacio
WHERE categoria = 'EMAIL_MDO'
  AND actiu = 1;

-- ========================================================================
-- 6. EXEMPLES D'ÚS PER ENTORNS
-- ========================================================================

-- DESENVOLUPAMENT: Afegir email de proves
INSERT INTO parametres_aplicacio (categoria, clau, valor, dt_create, dt_update, actiu)
VALUES ('EMAIL_MDO', 'desenvolupament@hospital.cat', 'Email proves desenvolupament', NOW(), NOW(), 1);

-- PREPRODUCCIÓ: Afegir email de preproducció
INSERT INTO parametres_aplicacio (categoria, clau, valor, dt_create, dt_update, actiu)
VALUES ('EMAIL_MDO', 'preproduccio@hospital.cat', 'Email proves preproducció', NOW(), NOW(), 1);

-- PRODUCCIÓ: Afegir emails reals
INSERT INTO parametres_aplicacio (categoria, clau, valor, dt_create, dt_update, actiu)
VALUES 
('EMAIL_MDO', 'mdo@hospital.cat', 'Responsable MDO - Producció', NOW(), NOW(), 1),
('EMAIL_MDO', 'urgencies@hospital.cat', 'Urgències - Producció', NOW(), NOW(), 1),
('EMAIL_MDO', 'epidemiologia@hospital.cat', 'Epidemiologia - Producció', NOW(), NOW(), 1);

-- ========================================================================
-- 7. MIGRACIÓ ENTRE ENTORNS
-- ========================================================================

-- Desactivar tots els destinataris de desenvolupament en producció
UPDATE parametres_aplicacio 
SET actiu = 0, 
    dt_update = NOW()
WHERE categoria = 'EMAIL_MDO' 
  AND clau LIKE '%desenvolupament%';

-- ========================================================================
-- 8. AUDITORIA
-- ========================================================================

-- Veure historial de canvis (si existeix taula d'auditoria)
-- Nota: Aquest és un exemple, ajustar segons la implementació
/*
SELECT 
    a.id,
    a.accio,
    a.taula,
    a.registre_id,
    a.usuari,
    a.dt_create,
    p.clau AS email_afectat
FROM auditoria a
LEFT JOIN parametres_aplicacio p ON p.id = a.registre_id
WHERE a.taula = 'parametres_aplicacio'
  AND p.categoria = 'EMAIL_MDO'
ORDER BY a.dt_create DESC
LIMIT 20;
*/

-- ========================================================================
-- 9. MANTENIMENT
-- ========================================================================

-- Netejar destinataris duplicats (mantenir el més recent)
DELETE p1 FROM parametres_aplicacio p1
INNER JOIN parametres_aplicacio p2 
WHERE p1.categoria = 'EMAIL_MDO'
  AND p2.categoria = 'EMAIL_MDO'
  AND p1.clau = p2.clau
  AND p1.id < p2.id;

-- Verificar que no hi hagi duplicats
SELECT 
    clau,
    COUNT(*) as vegades
FROM parametres_aplicacio
WHERE categoria = 'EMAIL_MDO'
GROUP BY clau
HAVING COUNT(*) > 1;

-- ========================================================================
-- 10. ROLLBACK (si cal desfer tots els canvis)
-- ========================================================================

-- ATENCIÓ: Això esborrarà TOTS els destinataris MDO!
-- Executar només si cal desfer completament la configuració

-- Esborrar tots els destinataris MDO
-- DELETE FROM parametres_aplicacio WHERE categoria = 'EMAIL_MDO';

-- Verificar que s'han esborrat
-- SELECT COUNT(*) FROM parametres_aplicacio WHERE categoria = 'EMAIL_MDO';

-- ========================================================================
-- NOTES IMPORTANTS
-- ========================================================================

/*
1. CATEGORIA:
   - SEMPRE utilitzar 'EMAIL_MDO' (case-sensitive!)
   
2. CLAU (EMAIL):
   - Ha de ser una adreça d'email vàlida
   - Recomanat utilitzar emails corporatius
   - No utilitzar emails personals per seguretat
   
3. VALOR (DESCRIPCIÓ):
   - Opcional però recomanat
   - Ajuda a identificar el destinatari
   - Exemple: "Responsable MDO", "Servei Urgències"
   
4. ACTIU:
   - 1 = Actiu (rebrà emails)
   - 0 = Inactiu (NO rebrà emails però es manté registrat)
   
5. SEGURETAT:
   - Els emails contenen informació sensible de pacients
   - Utilitzar només adreces corporatives segures
   - Revisar periòdicament els destinataris
   - Mantenir el mínim nombre de destinataris necessaris
   
6. PROVES:
   - Configurar emails de prova en entorns de desenvolupament
   - Desactivar emails de prova en producció
   - Verificar que els emails arribin correctament
   
7. BACKUP:
   - Fer backup dels destinataris abans de canvis massius
   - Documentar els canvis realitzats
*/

-- ========================================================================
-- EXEMPLE COMPLET: CONFIGURACIÓ INICIAL
-- ========================================================================

/*
-- Pas 1: Esborrar configuració antiga (si existeix)
DELETE FROM parametres_aplicacio WHERE categoria = 'EMAIL_MDO';

-- Pas 2: Afegir destinataris inicials
INSERT INTO parametres_aplicacio (categoria, clau, valor, dt_create, dt_update, actiu)
VALUES 
('EMAIL_MDO', 'mdo@hospital.cat', 'Responsable MDO', NOW(), NOW(), 1),
('EMAIL_MDO', 'urgencies@hospital.cat', 'Servei Urgències', NOW(), NOW(), 1),
('EMAIL_MDO', 'epidemiologia@hospital.cat', 'Servei Epidemiologia', NOW(), NOW(), 1);

-- Pas 3: Verificar que s'han creat correctament
SELECT * FROM parametres_aplicacio WHERE categoria = 'EMAIL_MDO';

-- Pas 4: Provar l'enviament (executar la integració amb una mostra MDO de prova)
*/

-- ========================================================================
-- FI DE L'SCRIPT
-- ========================================================================
