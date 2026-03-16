-- ========================================================================
-- SCRIPT SQL: Paràmetres d'Aplicació Recomanats
-- ========================================================================
-- 
-- Descripció:
--   Inserció de paràmetres de configuració de l'aplicació a la taula
--   parametres_aplicacio, migrant configuració des de App.config.
--
-- Data creació: Gener 2025
-- Versió: 1.0
-- Prerequisit: Taula parametres_aplicacio ja creada
--
-- ========================================================================

-- ========================================================================
-- 1. CONFIG_GENERAL - Configuració General
-- ========================================================================

INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
VALUES
-- Límit de processament
('CONFIG_GENERAL', 'LIMIT_RESULTATS_PROVES', '0', 
 'Límit de mostres per execució (0=il·limitat)', 'INT', 1, 'SYSTEM'),

-- Timeout WebService
('CONFIG_GENERAL', 'WEBSERVICE_TIMEOUT', '30', 
 'Timeout en segons per WebService pacients', 'INT', 1, 'SYSTEM'),

-- Cache
('CONFIG_GENERAL', 'MINUTS_VIGENCIA_CACHE', '60', 
 'Minuts de vigència del cache de microorganismes', 'INT', 1, 'SYSTEM'),

-- Historial
('CONFIG_GENERAL', 'DIES_RETENCIO_HISTORIAL', '90', 
 'Dies de retenció d''històric (auditories, sincronització)', 'INT', 1, 'SYSTEM'),

-- Paral·lelisme
('CONFIG_GENERAL', 'PROCESSAR_EN_PARALEL', '0', 
 'Activar processament paral·lel (0=NO, 1=SÍ)', 'BOOL', 1, 'SYSTEM'),

('CONFIG_GENERAL', 'MAX_GRAU_PARALELISME', '4', 
 'Màxim threads en paral·lel si està activat', 'INT', 1, 'SYSTEM');

-- ========================================================================
-- 2. CONFIG_CARREGA - Configuració de Càrrega de Dades
-- ========================================================================

INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
VALUES
-- Tipus de càrrega activa
('CONFIG_CARREGA', 'CARREGA_INCREMENTAL_ACTIVA', '1', 
 'Activar càrrega incremental (1=SÍ, 0=NO)', 'BOOL', 1, 'SYSTEM'),

('CONFIG_CARREGA', 'CARREGA_DIES_ENRERE_ACTIVA', '0', 
 'Activar càrrega per dies enrere', 'BOOL', 0, 'SYSTEM'),

('CONFIG_CARREGA', 'CARREGA_RANG_DATES_ACTIVA', '0', 
 'Activar càrrega per rang de dates', 'BOOL', 0, 'SYSTEM'),

-- Paràmetres càrrega incremental
('CONFIG_CARREGA', 'DIES_REVISIO_SEGURETAT', '7', 
 'Dies de finestra per validacions tardanes', 'INT', 1, 'SYSTEM'),

-- Paràmetres càrrega dies enrere
('CONFIG_CARREGA', 'NOMBRE_DIES_ENRERE', '1', 
 'Nombre de dies enrere per carregar', 'INT', 1, 'SYSTEM'),

-- Paràmetres càrrega rang dates
('CONFIG_CARREGA', 'DATA_INICI', '01/01/2025', 
 'Data inici rang (dd/MM/yyyy)', 'DATE', 1, 'SYSTEM'),

('CONFIG_CARREGA', 'DATA_FI', '31/01/2025', 
 'Data fi rang (dd/MM/yyyy)', 'DATE', 1, 'SYSTEM');

-- ========================================================================
-- 3. MMR_CONFIG - Configuració Multiresistents
-- ========================================================================

INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
VALUES
-- Vigència de positius
('MMR_CONFIG', 'DIES_VIGENCIA_POSITIUS_DEFAULT', '365', 
 'Dies vigència per defecte si tipus mostra no té definit', 'INT', 1, 'SYSTEM'),

-- Comportament tipus mostra
('MMR_CONFIG', 'COMPORTAMENT_TIPUS_MOSTRA_DEFAULT', '0', 
 'Comportament per defecte si tipus mostra no existeix (0 o 1)', 'INT', 1, 'SYSTEM'),

-- Comprovacions negatius
('MMR_CONFIG', 'ACTIVAR_COMPROVACIO_1', '1', 
 'Activar comprovació 1 per negatius (comportament=1)', 'BOOL', 1, 'SYSTEM'),

('MMR_CONFIG', 'ACTIVAR_COMPROVACIO_2', '1', 
 'Activar comprovació 2 per negatius (positius vigents)', 'BOOL', 1, 'SYSTEM'),

-- Control mostres antigues
('MMR_CONFIG', 'PROCESSAR_MOSTRES_ANTIGUES', '1', 
 'Processar mostres amb data_resultat anterior (1=SÍ)', 'BOOL', 1, 'SYSTEM');

-- ========================================================================
-- 4. CONFIG_WEBSERVICE - Configuració WebServices
-- ========================================================================

INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
VALUES
('CONFIG_WEBSERVICE', 'URL_PRODUCCIO', 
 'http://10.80.160.178/flamma/ws/consultaPacient/consultaPacient.php', 
 'URL WebService pacients (Producció)', 'STRING', 1, 'SYSTEM'),

('CONFIG_WEBSERVICE', 'URL_PREPRODUCCIO', 
 'http://10.80.160.179/flamma/ws/consultaPacient/consultaPacient.php', 
 'URL WebService pacients (Preproducció)', 'STRING', 1, 'SYSTEM'),

('CONFIG_WEBSERVICE', 'RETRIES_MAX', '3', 
 'Nombre màxim de reintents si falla', 'INT', 1, 'SYSTEM');

-- ========================================================================
-- 5. VR_CONFIG - Configuració Virus Respiratoris
-- ========================================================================

INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
VALUES
('VR_CONFIG', 'GENERAR_NOTA_CURS_CLINIC', '1', 
 'Generar nota curs clínic automàticament (1=SÍ)', 'BOOL', 1, 'SYSTEM'),

('VR_CONFIG', 'TIPUS_NOTA_PER_DEFECTE', '1', 
 'Tipus nota per defecte si microorganisme no té definit', 'INT', 1, 'SYSTEM'),

('VR_CONFIG', 'REBUTJAR_SI_CENTRE_NO_CONFIGURAT', '1', 
 'Rebutjar VR si centre no està a VR_CENTRES (1=SÍ)', 'BOOL', 1, 'SYSTEM');

-- ========================================================================
-- 6. TIPUS_MOSTRA_EQUIV - Tipus de Mostra Equivalents (JSON)
-- ========================================================================

INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
VALUES
('TIPUS_MOSTRA_EQUIV', 'SANG', 
 '["SANG VENOSA","SANG ARTERIAL","SANG CAPILAR"]', 
 'Tipus de mostra equivalents a Sang', 'JSON', 1, 'SYSTEM'),

('TIPUS_MOSTRA_EQUIV', 'RESPIRATORI', 
 '["ESPUTO","EXSUDAT BRONQUIAL","ASPIRAT BRONQUIAL","BAS"]', 
 'Tipus respiratoris equivalents', 'JSON', 1, 'SYSTEM'),

('TIPUS_MOSTRA_EQUIV', 'ORINA', 
 '["ORINA","ORINA MITJA MICCIÓ","ORINA SONDA"]', 
 'Tipus d''orina equivalents', 'JSON', 1, 'SYSTEM'),

('TIPUS_MOSTRA_EQUIV', 'FROTIS_RECTAL', 
 '["FROTIS RECTAL","FROTIS ANAL"]', 
 'Tipus frotis rectal equivalents', 'JSON', 1, 'SYSTEM');

-- ========================================================================
-- 7. CONFIG_EMAIL - Configuració Emails (OPCIONAL)
-- ========================================================================

INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
VALUES
('CONFIG_EMAIL', 'ENVIAR_EMAIL_LOG', '0', 
 'Enviar email amb log execució (0=NO, 1=SÍ)', 'BOOL', 1, 'SYSTEM'),

('CONFIG_EMAIL', 'SMTP_SERVER', 'smtp.hospital.cat', 
 'Servidor SMTP', 'STRING', 1, 'SYSTEM'),

('CONFIG_EMAIL', 'SMTP_PORT', '587', 
 'Port SMTP', 'INT', 1, 'SYSTEM'),

('CONFIG_EMAIL', 'SMTP_USAR_SSL', '1', 
 'Utilitzar SSL/TLS (1=SÍ)', 'BOOL', 1, 'SYSTEM'),

('CONFIG_EMAIL', 'EMAIL_FROM', 'multir@hospital.cat', 
 'Email remitent', 'STRING', 1, 'SYSTEM'),

('CONFIG_EMAIL', 'EMAILS_DESTINATARIS', 
 'admin@hospital.cat;epidemio@hospital.cat', 
 'Destinataris (separats per ;)', 'STRING', 1, 'SYSTEM'),

('CONFIG_EMAIL', 'EMAIL_NOMES_ERRORS', '1', 
 'Enviar email només si hi ha errors (1=SÍ)', 'BOOL', 1, 'SYSTEM');

-- ========================================================================
-- 8. CONFIG_LOGGING - Configuració Logging (OPCIONAL)
-- ========================================================================

INSERT INTO parametres_aplicacio 
(categoria, clau, valor, descripcio, tipus_dada, actiu, usuari_modificacio)
VALUES
('CONFIG_LOGGING', 'LOG_DIRECTORY', 'Logs', 
 'Directori per guardar logs', 'STRING', 1, 'SYSTEM'),

('CONFIG_LOGGING', 'LOG_LEVEL', 'Info', 
 'Nivell de log: Debug, Info, Warning, Error', 'STRING', 1, 'SYSTEM'),

('CONFIG_LOGGING', 'DIES_RETENCIO_LOGS', '30', 
 'Dies de retenció de fitxers de log', 'INT', 1, 'SYSTEM');

-- ========================================================================
-- VERIFICACIÓ
-- ========================================================================

-- Comptar paràmetres per categoria
SELECT 
    categoria,
    COUNT(*) as total_parametres,
    SUM(CASE WHEN actiu = 1 THEN 1 ELSE 0 END) as actius,
    SUM(CASE WHEN actiu = 0 THEN 1 ELSE 0 END) as inactius
FROM parametres_aplicacio
GROUP BY categoria
ORDER BY categoria;

-- Veure tots els paràmetres inserits
SELECT 
    categoria,
    clau,
    valor,
    tipus_dada,
    actiu,
    descripcio
FROM parametres_aplicacio
WHERE dt_delete IS NULL
ORDER BY categoria, clau;

-- ========================================================================
-- NOTES IMPORTANTS
-- ========================================================================

/*
1. MIGRACIÓ GRADUAL:
   - Comença per categories prioritàries: CONFIG_GENERAL, CONFIG_CARREGA, MMR_CONFIG
   - Les categories opcionals (EMAIL, LOGGING) es poden afegir més endavant
   
2. VALORS PER DEFECTE:
   - Els valors inserits són recomanacions basades en App.config
   - Revisa i ajusta segons les necessitats del teu hospital
   
3. TIPUS_MOSTRA_EQUIV:
   - Els equivalents en JSON permeten comparacions flexibles
   - Afegeix més tipus segons les teves necessitats
   
4. ACTIVAR/DESACTIVAR:
   - Utilitza el camp 'actiu' per activar/desactivar paràmetres
   - No cal esborrar-los, només marcar actiu=0
   
5. INTEGRACIÓ AMB CODI:
   - Caldrà modificar ConfigurationService per llegir de BD
   - O crear un nou servei híbrid (BD + App.config com fallback)
   
6. PRIORITATS:
   - Alta: CONFIG_GENERAL, CONFIG_CARREGA, MMR_CONFIG
   - Mitjana: CONFIG_WEBSERVICE, VR_CONFIG, TIPUS_MOSTRA_EQUIV
   - Baixa: CONFIG_EMAIL, CONFIG_LOGGING (manté a App.config si vols)
*/

-- ========================================================================
-- EXEMPLES D'ÚS AL CODI
-- ========================================================================

/*
// Obtenir paràmetre INT
int timeout = _parametresHelper.ObtenirInt("CONFIG_GENERAL", "WEBSERVICE_TIMEOUT", 30);

// Obtenir paràmetre BOOL
bool paralel = _parametresHelper.ObtenirBool("CONFIG_GENERAL", "PROCESSAR_EN_PARALEL", false);

// Obtenir paràmetre STRING
string smtpServer = _parametresHelper.ObtenirString("CONFIG_EMAIL", "SMTP_SERVER", "localhost");

// Obtenir paràmetre JSON
var equivalents = _parametresHelper.ObtenirJson<List<string>>("TIPUS_MOSTRA_EQUIV", "SANG", new List<string>());

// Comprovar existència (ja implementat per VR_CENTRES)
bool existeix = _repository.ExisteixParametre("VR_CENTRES", "HOSPITAL TRUETA");
*/

-- ========================================================================
-- FI SCRIPT
-- ========================================================================
