-- ============================================================================
-- Script de creació de la taula integracio_modulab_sincronitzacio
-- Sistema d'optimització de càrregues de Modulab a MultiR
-- ============================================================================
-- Versió: 1.0
-- Data: 2025-01-20
-- Autor: Sistema MultirIntegraModulab
-- ============================================================================

USE marsa;

-- Crear taula de control de sincronització
CREATE TABLE IF NOT EXISTS integracio_modulab_sincronitzacio (
    id INT AUTO_INCREMENT PRIMARY KEY COMMENT 'Identificador únic del registre',
    
    -- Dates màximes processades per filtrar futures càrregues
    data_resultat_max_processada DATETIME NULL COMMENT 'Data resultat màxima processada en última càrrega exitosa',
    data_validacio_max_processada DATETIME NULL COMMENT 'Data validació màxima processada en última càrrega exitosa',
    
    -- Informació de la sincronització
    data_sincronitzacio DATETIME NOT NULL COMMENT 'Data i hora de la sincronització',
    nombre_mostres_processades INT DEFAULT 0 COMMENT 'Nombre de mostres processades en aquesta sincronització',
    nombre_mostres_error INT DEFAULT 0 COMMENT 'Nombre de mostres amb error',
    
    -- Configuració de seguretat
    dies_revisio_seguretat INT DEFAULT 7 COMMENT 'Dies de revisió per validacions tardanes (finestra de seguretat)',
    
    -- Estat i observacions
    estat VARCHAR(20) DEFAULT 'OK' COMMENT 'Estat de la sincronització: OK, ERROR, PARCIAL',
    observacions TEXT NULL COMMENT 'Observacions o missatges d''error',
    
    -- Mètriques de rendiment
    durada_segons DECIMAL(10,2) NULL COMMENT 'Durada del processament en segons',
    
    -- Timestamps de control
    dt_create TIMESTAMP DEFAULT CURRENT_TIMESTAMP COMMENT 'Data de creació del registre',
    dt_update TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT 'Data d''última actualització',
    
    -- Índexs per optimitzar consultes
    INDEX idx_data_sincronitzacio (data_sincronitzacio DESC) COMMENT 'Per obtenir última sincronització',
    INDEX idx_estat (estat) COMMENT 'Per filtrar per estat',
    INDEX idx_data_resultat (data_resultat_max_processada) COMMENT 'Per consultes de dates resultat',
    INDEX idx_data_validacio (data_validacio_max_processada) COMMENT 'Per consultes de dates validació'
    
) ENGINE=InnoDB 
  DEFAULT CHARSET=utf8 
  COLLATE=utf8_unicode_ci
  COMMENT='Control de sincronització per optimitzar càrregues de Modulab. Guarda tracking de cada execució per filtrar futures càrregues només per les dades noves.';

-- ============================================================================
-- Comentaris explicatius
-- ============================================================================
/*
FUNCIONALITAT:
--------------
Aquesta taula implementa un sistema de tracking de sincronitzacions per optimitzar
les càrregues de dades des d'Oracle (Modulab) cap a MySQL (MultiR).

PROBLEMA QUE RESOL:
-------------------
1. Evitar carregar totes les mostres cada cop (millora rendiment)
2. Filtrar només mostres noves o actualitzades
3. Gestionar validacions tardanes (mostres validades dies després)
4. Auditoria completa de cada execució

LÒGICA DE FILTRES:
------------------
Les futures càrregues filtraran les dades d'Oracle amb:

1. DATA_RESULTAT > data_resultat_max_processada
   → Noves mostres amb resultats recents

2. DATA_VALIDACIO > data_validacio_max_processada
   → Mostres validades recentment

3. Finestra de seguretat (dies_revisio_seguretat, defecte 7 dies):
   → Mostres amb DATA_RESULTAT recent que tenen DATA_VALIDACIO nova
   → Evita perdre validacions tardanes

ESTATS POSSIBLES:
-----------------
- OK: Processament completat sense errors
- ERROR: S'han produït errors crítics
- PARCIAL: Processament amb alguns errors però continuat

EXEMPLE D'ÚS:
-------------
-- Obtenir última sincronització exitosa:
SELECT * FROM integracio_modulab_sincronitzacio 
WHERE estat IN ('OK', 'PARCIAL')
ORDER BY data_sincronitzacio DESC 
LIMIT 1;

-- Inserir nova sincronització:
INSERT INTO integracio_modulab_sincronitzacio (
    data_resultat_max_processada,
    data_validacio_max_processada,
    data_sincronitzacio,
    nombre_mostres_processades,
    nombre_mostres_error,
    dies_revisio_seguretat,
    estat,
    durada_segons
) VALUES (
    '2025-01-20 14:30:00',
    '2025-01-20 15:45:00',
    NOW(),
    150,
    2,
    7,
    'OK',
    182.45
);

-- Neteja de registres antics (més de 90 dies):
DELETE FROM integracio_modulab_sincronitzacio
WHERE data_sincronitzacio < DATE_SUB(NOW(), INTERVAL 90 DAY);

MANTENIMENT:
------------
- Es recomana netejar registres amb més de 90 dies
- Monitoritzar mida de la taula periòdicament
- Revisar registres amb estat ERROR per investigar problemes
*/

-- ============================================================================
-- Verificació
-- ============================================================================
-- Mostrar estructura de la taula creada
DESCRIBE integracio_modulab_sincronitzacio;

-- Mostrar índexs creats
SHOW INDEX FROM integracio_modulab_sincronitzacio;

-- Verificar que està buida (primera instal·lació)
SELECT COUNT(*) AS total_registres FROM integracio_modulab_sincronitzacio;

-- ============================================================================
-- Fi de l'script
-- ============================================================================
SELECT '✅ Taula integracio_modulab_sincronitzacio creada correctament' AS resultat;
