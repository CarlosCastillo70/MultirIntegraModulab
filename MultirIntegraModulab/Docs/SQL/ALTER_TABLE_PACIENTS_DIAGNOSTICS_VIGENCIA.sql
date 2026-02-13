-- =====================================================
-- SCRIPT: Afegir camps de vigència a pacients_diagnostics
-- DATA: Gener 2025
-- DESCRIPCIÓ: Afegeix camps per controlar la vigència dels diagnòstics
-- =====================================================

USE marsa;

-- Afegir camp vigent
ALTER TABLE pacients_diagnostics 
ADD COLUMN vigent CHAR(1) DEFAULT 'S' COMMENT 'S=Sí vigent, N=No vigent';

-- Afegir camp responsable_no_vigent
ALTER TABLE pacients_diagnostics 
ADD COLUMN responsable_no_vigent VARCHAR(100) DEFAULT NULL COMMENT 'Usuari que ha marcat com a no vigent';

-- Afegir camp data_no_vigent
ALTER TABLE pacients_diagnostics 
ADD COLUMN data_no_vigent DATETIME DEFAULT NULL COMMENT 'Data quan s\'ha marcat com a no vigent';

-- Afegir índex per millorar rendiment de consultes per vigència
ALTER TABLE pacients_diagnostics 
ADD INDEX idx_vigent (vigent);

-- Afegir índex compost per npat i vigent
ALTER TABLE pacients_diagnostics 
ADD INDEX idx_npat_vigent (npat, vigent);

-- Inicialitzar tots els diagnòstics existents com a vigents
UPDATE pacients_diagnostics 
SET vigent = 'S' 
WHERE vigent IS NULL 
  AND dt_delete IS NULL;

-- =====================================================
-- VERIFICACIÓ
-- =====================================================

-- Comprovar que els camps s'han creat correctament
SELECT 
    COLUMN_NAME,
    COLUMN_TYPE,
    COLUMN_DEFAULT,
    IS_NULLABLE,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'marsa'
  AND TABLE_NAME = 'pacients_diagnostics'
  AND COLUMN_NAME IN ('vigent', 'responsable_no_vigent', 'data_no_vigent');

-- Comprovar índexs
SHOW INDEX FROM pacients_diagnostics WHERE Key_name IN ('idx_vigent', 'idx_npat_vigent');

-- Comptar diagnòstics vigents i no vigents
SELECT 
    vigent,
    COUNT(*) as total
FROM pacients_diagnostics
WHERE dt_delete IS NULL
GROUP BY vigent;

-- =====================================================
-- FI DEL SCRIPT
-- =====================================================
