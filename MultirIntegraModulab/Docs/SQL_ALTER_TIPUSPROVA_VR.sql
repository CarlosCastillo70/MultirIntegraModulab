-- ========================================================================
-- SCRIPT SQL: Afegir camp incorpora_virus_respiratori a taula tipusprova
-- ========================================================================
-- 
-- Descripció:
--   Afegeix el camp 'incorpora_virus_respiratori' a la taula tipusprova
--   per controlar quins tipus de prova permeten incorporar virus respiratoris.
--
-- Data creació: Gener 2025
-- Versió: 1.0
-- Base de dades: MySQL
-- Taula afectada: tipusprova
--
-- ========================================================================

-- 1. Afegir camp incorpora_virus_respiratori
ALTER TABLE tipusprova 
ADD COLUMN incorpora_virus_respiratori INT(1) DEFAULT 0 
COMMENT 'Indica si aquest tipus de prova permet incorporar virus respiratoris (0=NO, 1=SÍ)';

-- 2. Crear índex per optimitzar consultes
CREATE INDEX idx_incorpora_vr ON tipusprova(incorpora_virus_respiratori);

-- ========================================================================
-- EXEMPLES D'ÚS
-- ========================================================================

-- Marcar proves PCR per VR com a incorporables
UPDATE tipusprova 
SET incorpora_virus_respiratori = 1
WHERE UPPER(codi) LIKE '%PCR%'
  AND (
    UPPER(codi) LIKE '%SARS%'
    OR UPPER(codi) LIKE '%COVID%'
    OR UPPER(codi) LIKE '%INFLUENZA%'
    OR UPPER(codi) LIKE '%VIRUS RESPIRATORI%'
  );

-- Marcar proves específiques com a NO incorporables per VR
UPDATE tipusprova 
SET incorpora_virus_respiratori = 0
WHERE UPPER(codi) IN (
    'CULTIU RUTINARI',
    'ANTIBIOGRAMA STANDARD'
);

-- ========================================================================
-- VERIFICACIÓ
-- ========================================================================

-- Comprovar que el camp s'ha afegit correctament
DESCRIBE tipusprova;

-- Veure distribució de valors
SELECT 
    incorpora_virus_respiratori,
    COUNT(*) as total,
    GROUP_CONCAT(codi SEPARATOR ', ') as exemples
FROM tipusprova
GROUP BY incorpora_virus_respiratori;

-- Llistar proves que permeten VR
SELECT 
    codi,
    descripcio,
    incorpora_virus_respiratori,
    actiu
FROM tipusprova
WHERE incorpora_virus_respiratori = 1
  AND actiu = 1
ORDER BY codi;

-- ========================================================================
-- ROLLBACK (si cal desfer els canvis)
-- ========================================================================

-- Eliminar índex
-- DROP INDEX idx_incorpora_vr ON tipusprova;

-- Eliminar camp
-- ALTER TABLE tipusprova DROP COLUMN incorpora_virus_respiratori;

-- ========================================================================
-- NOTES IMPORTANTS
-- ========================================================================

/*
1. VALORS POSSIBLES:
   - 0 (per defecte): NO permet incorporar virus respiratoris
   - 1: SÍ permet incorporar virus respiratoris
   
2. COMPORTAMENT AL SISTEMA:
   - Si incorpora_virus_respiratori = 0 ? Mostra VR NO s'incorpora
   - Si incorpora_virus_respiratori = 1 ? Mostra VR s'incorpora normalment
   - Auditoria "TPNIVR" es genera quan NO es permet incorporar
   
3. CONFIGURACIÓ INICIAL:
   - Per defecte, TOTS els tipus de prova tenen valor 0 (NO incorporar VR)
   - Cal actualitzar manualment els tipus de prova que SÍ han d'incorporar VR
   
4. CRITERIS PER MARCAR incorpora_virus_respiratori = 1:
   - PCR específics per virus respiratoris
   - Tests ràpids d'antígens per VR
   - Panells respiratoris (multiplex)
   - Seqüenciació per VR
   
5. CRITERIS PER MARCAR incorpora_virus_respiratori = 0:
   - Cultius bacterians generals
   - Antibiogrames
   - Proves sense relació amb VR
   
6. MANTENIMENT:
   - Revisar periòdicament noves proves creades
   - Actualitzar segons criteris clínics
   - Documentar canvis a la taula d'auditoria
*/

-- ========================================================================
-- HISTORIAL DE CANVIS
-- ========================================================================

/*
Versió 1.0 (Gener 2025):
  - Creació inicial del camp incorpora_virus_respiratori
  - Afegit índex per optimització
  - Valor per defecte = 0 (NO incorporar)
*/

-- ========================================================================
-- FI SCRIPT
-- ========================================================================
