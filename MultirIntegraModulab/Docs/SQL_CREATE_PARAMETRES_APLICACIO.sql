-- ========================================================================
-- SCRIPT SQL: Crear taula parametres_aplicacio
-- ========================================================================
-- 
-- Descripció:
--   Crea una taula genèrica per gestionar paràmetres de configuració
--   de l'aplicació MultirIntegraModulab.
--
--   Inicialment s'utilitza per centres que permeten VR, però està
--   dissenyada per ser escalable i afegir altres paràmetres en el futur.
--
-- Data creació: Gener 2025
-- Versió: 1.0
-- Base de dades: MySQL
--
-- ========================================================================

-- Crear taula
CREATE TABLE parametres_aplicacio (
    id INT AUTO_INCREMENT PRIMARY KEY,
    categoria VARCHAR(50) NOT NULL COMMENT 'Categoria del paràmetre (VR_CENTRES, CONFIG_GENERAL, etc.)',
    clau VARCHAR(100) NOT NULL COMMENT 'Clau del paràmetre',
    valor TEXT NOT NULL COMMENT 'Valor del paràmetre',
    descripcio TEXT NULL COMMENT 'Descripció del paràmetre',
    tipus_dada VARCHAR(20) DEFAULT 'STRING' COMMENT 'STRING, INT, BOOL, JSON, DATE',
    actiu INT(1) DEFAULT 1 COMMENT '1=actiu, 0=inactiu',
    dt_create TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    dt_update TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    dt_delete TIMESTAMP NULL DEFAULT NULL,
    usuari_modificacio VARCHAR(50) NULL COMMENT 'Usuari que ha fet l''últim canvi',
    
    UNIQUE KEY uk_categoria_clau_delete (categoria, clau, dt_delete),
    INDEX idx_categoria (categoria),
    INDEX idx_actiu (actiu),
    INDEX idx_dt_delete (dt_delete)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 
COMMENT='Paràmetres de configuració de l''aplicació';

-- ========================================================================
-- DADES INICIALS: CENTRES QUE PERMETEN VIRUS RESPIRATORIS
-- ========================================================================

-- IMPORTANT: Substitueix aquests valors pels centres reals del teu hospital
-- Els noms dels centres han de coincidir exactament amb el camp 
-- CENTRE_DESCRIPCIO que arriba d'Oracle (Modulab)

INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu)
VALUES
-- Exemple: Afegeix els centres que permeten incorporar VR
('VR_CENTRES', 'HOSPITAL UNIVERSITARI DR. JOSEP TRUETA', '1', 'Centre principal - Permet VR', 'BOOL', 1),
('VR_CENTRES', 'HOSPITAL DE SANTA CATERINA', '1', 'Centre secundari - Permet VR', 'BOOL', 1);

-- Afegeix aquí els teus centres específics:
-- ('VR_CENTRES', 'NOM_DEL_TEU_CENTRE', '1', 'Descripció', 'BOOL', 1),

-- ========================================================================
-- VERIFICACIÓ
-- ========================================================================

-- Comprovar que la taula s'ha creat correctament
DESCRIBE parametres_aplicacio;

-- Veure tots els centres VR configurats
SELECT 
    clau as centre,
    descripcio,
    actiu,
    dt_create as data_creacio
FROM parametres_aplicacio
WHERE categoria = 'VR_CENTRES'
  AND actiu = 1
  AND dt_delete IS NULL
ORDER BY clau;

-- Comptar centres actius per VR
SELECT 
    COUNT(*) as total_centres_vr
FROM parametres_aplicacio
WHERE categoria = 'VR_CENTRES'
  AND actiu = 1
  AND dt_delete IS NULL;

-- ========================================================================
-- EXEMPLES D'ÚS (per gestionar centres)
-- ========================================================================

-- Afegir un nou centre que permet VR
/*
INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
VALUES
('VR_CENTRES', 'CAP GIRONA-1', '1', 'Atenció primària - Permet VR', 'BOOL', 1, 'admin');
*/

-- Desactivar un centre (temporalment)
/*
UPDATE parametres_aplicacio
SET actiu = 0, 
    usuari_modificacio = 'admin'
WHERE categoria = 'VR_CENTRES'
  AND clau = 'HOSPITAL DE SANTA CATERINA';
*/

-- Reactivar un centre
/*
UPDATE parametres_aplicacio
SET actiu = 1, 
    usuari_modificacio = 'admin'
WHERE categoria = 'VR_CENTRES'
  AND clau = 'HOSPITAL DE SANTA CATERINA';
*/

-- Esborrar un centre (soft delete)
/*
UPDATE parametres_aplicacio
SET dt_delete = NOW(), 
    usuari_modificacio = 'admin'
WHERE categoria = 'VR_CENTRES'
  AND clau = 'CAP GIRONA-1';
*/

-- Consultar centres per nom parcial (cerca)
/*
SELECT clau, descripcio, actiu
FROM parametres_aplicacio
WHERE categoria = 'VR_CENTRES'
  AND UPPER(clau) LIKE UPPER('%HOSPITAL%')
  AND dt_delete IS NULL
ORDER BY clau;
*/

-- ========================================================================
-- NOTES IMPORTANTS
-- ========================================================================

/*
1. CENTRES VR:
   - Categoria: 'VR_CENTRES'
   - Clau: Nom exacte del centre (ha de coincidir amb CENTRE_DESCRIPCIO d'Oracle)
   - Valor: '1' (per conveni, però no s'utilitza; l'important és que existeixi el registre)
   - Actiu: 1=SÍ permet VR, 0=NO permet VR
   
2. COMPORTAMENT AL SISTEMA:
   - Si centre NO està a la taula ? NO s'incorpora VR (auditoria CNIVR)
   - Si centre està amb actiu=0 ? NO s'incorpora VR (auditoria CNIVR)
   - Si centre està amb actiu=1 ? SÍ s'incorpora VR
   
3. COMPARACIÓ:
   - La comparació es fa amb UPPER() per evitar problemes de majúscules/minúscules
   - Assegura't que els noms coincideixen exactament amb Oracle
   
4. SOFT DELETE:
   - Quan esborres un centre, es marca dt_delete (no s'elimina físicament)
   - Permet mantenir històric i traçabilitat
   
5. AUDITORIA:
   - Utilitza el camp usuari_modificacio per saber qui ha fet els canvis
   - dt_create i dt_update es gestionen automàticament
   
6. ÍNDEX UNIQUE:
   - Permet tenir el mateix centre múltiples vegades si s'esborra (dt_delete diferent)
   - Evita duplicats actius
   
7. PARÀMETRES FUTURS:
   - La taula està preparada per afegir altres categories:
     * CONFIG_GENERAL (configuracions globals)
     * MMR_CONFIG (configuracions multiresistents)
     * NOTIFICACIONS_VR (emails per notificar)
     * Etc.
*/

-- ========================================================================
-- EXEMPLES DE CONSULTES PER A L'APLICACIÓ
-- ========================================================================

-- Comprovar si un centre permet VR (query utilitzada pel sistema)
/*
SELECT COUNT(*) 
FROM parametres_aplicacio 
WHERE categoria = 'VR_CENTRES'
  AND UPPER(clau) = UPPER('HOSPITAL UNIVERSITARI DR. JOSEP TRUETA')
  AND actiu = 1
  AND dt_delete IS NULL;
*/

-- Obtenir tots els centres que permeten VR
/*
SELECT clau 
FROM parametres_aplicacio 
WHERE categoria = 'VR_CENTRES'
  AND actiu = 1
  AND dt_delete IS NULL
ORDER BY clau;
*/

-- ========================================================================
-- ROLLBACK (si cal desfer els canvis)
-- ========================================================================

-- Esborrar dades de centres VR
-- DELETE FROM parametres_aplicacio WHERE categoria = 'VR_CENTRES';

-- Esborrar la taula completa
-- DROP TABLE IF EXISTS parametres_aplicacio;

-- ========================================================================
-- HISTORIAL DE CANVIS
-- ========================================================================

/*
Versió 1.0 (Gener 2025):
  - Creació inicial de la taula parametres_aplicacio
  - Afegits índexs per optimització
  - Categoria inicial: VR_CENTRES per controlar centres VR
  - Disseny escalable per futures categories
*/

-- ========================================================================
-- FI SCRIPT
-- ========================================================================
