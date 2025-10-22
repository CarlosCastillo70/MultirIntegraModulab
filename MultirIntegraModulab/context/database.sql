-- --------------------------------------------------------
-- Host:                         zeus
-- Versió del servidor:          10.5.27-MariaDB-ubu2004-log - mariadb.org binary distribution
-- SO del servidor:              debian-linux-gnu
-- HeidiSQL Versió:              12.10.0.7000
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

-- Dumping structure for table marsa.abs
CREATE TABLE IF NOT EXISTS `abs` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `descripcio` varchar(250) NOT NULL,
  `cd_hb` int(11) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=393 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.admaudit
CREATE TABLE IF NOT EXISTS `admaudit` (
  `id` bigint(20) NOT NULL AUTO_INCREMENT,
  `ts_event` timestamp NULL DEFAULT current_timestamp(),
  `user` varchar(50) DEFAULT NULL,
  `event` varchar(50) DEFAULT NULL,
  `data1` varchar(100) DEFAULT NULL,
  `data2` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=MyISAM AUTO_INCREMENT=93704 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.admmenu
CREATE TABLE IF NOT EXISTS `admmenu` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `zone` varchar(10) DEFAULT NULL,
  `codemenu` varchar(50) DEFAULT NULL,
  `codeparent` varchar(50) DEFAULT NULL,
  `text` varchar(50) DEFAULT NULL,
  `icon` varchar(50) DEFAULT NULL,
  `destination` varchar(50) DEFAULT NULL,
  `dateCreated` timestamp NULL DEFAULT current_timestamp(),
  `dateRemoved` datetime DEFAULT NULL,
  `order` int(11) DEFAULT NULL,
  `level` int(5) DEFAULT NULL,
  `dividerbefore` int(8) DEFAULT 0,
  PRIMARY KEY (`id`),
  KEY `id_pare` (`codeparent`),
  KEY `codemenu` (`codemenu`)
) ENGINE=InnoDB AUTO_INCREMENT=285 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci ROW_FORMAT=DYNAMIC;

-- Data exporting was unselected.

-- Dumping structure for table marsa.admmenuroles
CREATE TABLE IF NOT EXISTS `admmenuroles` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `role` varchar(50) DEFAULT NULL,
  `codemenu` varchar(50) DEFAULT NULL,
  `allowdeny` varchar(1) DEFAULT 'A',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.admpositions
CREATE TABLE IF NOT EXISTS `admpositions` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `position` varchar(50) DEFAULT NULL,
  `name` varchar(50) DEFAULT NULL,
  `filter` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=latin1 COLLATE=latin1_swedish_ci ROW_FORMAT=DYNAMIC COMMENT='admpositions no té control directe sobre el menú. S''ha de co';

-- Data exporting was unselected.

-- Dumping structure for table marsa.admroles
CREATE TABLE IF NOT EXISTS `admroles` (
  `role` varchar(50) NOT NULL,
  `roleDescription` varchar(50) DEFAULT NULL,
  `start` varchar(50) DEFAULT NULL,
  `weight` int(11) DEFAULT NULL,
  PRIMARY KEY (`role`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.admuserpositions
CREATE TABLE IF NOT EXISTS `admuserpositions` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `user` varchar(50) NOT NULL DEFAULT '0',
  `position` varchar(50) NOT NULL DEFAULT '0',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=31 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci ROW_FORMAT=DYNAMIC;

-- Data exporting was unselected.

-- Dumping structure for table marsa.admuserroles
CREATE TABLE IF NOT EXISTS `admuserroles` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `user` varchar(50) DEFAULT NULL,
  `role` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=58 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.admusers
CREATE TABLE IF NOT EXISTS `admusers` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `user` varchar(50) NOT NULL,
  `name` varchar(50) DEFAULT NULL,
  `obs` varchar(50) DEFAULT NULL,
  `dt_creation` timestamp NULL DEFAULT current_timestamp(),
  `dt_dan` timestamp NULL DEFAULT NULL,
  `correu` varchar(50) DEFAULT NULL,
  `usu_modif` varchar(50) DEFAULT NULL,
  `dt_modif` timestamp NULL DEFAULT NULL,
  `usu_dan` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `user` (`user`)
) ENGINE=InnoDB AUTO_INCREMENT=65 DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.adquisicio
CREATE TABLE IF NOT EXISTS `adquisicio` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `codi` varchar(50) DEFAULT NULL,
  `descripcio` varchar(100) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `actiu` int(11) DEFAULT 1 COMMENT '1=SI 0=NO',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=13 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Adquisició';

-- Data exporting was unselected.

-- Dumping structure for table marsa.aillaments
CREATE TABLE IF NOT EXISTS `aillaments` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `codi_llit` varchar(10) NOT NULL,
  `pacient` varchar(10) DEFAULT NULL,
  `habitacio` varchar(10) DEFAULT NULL,
  `planta` varchar(10) DEFAULT NULL,
  `codi_ut` varchar(10) DEFAULT NULL,
  `desc_ut` varchar(50) DEFAULT NULL,
  `codi_servei` varchar(10) DEFAULT NULL,
  `desc_servei` varchar(50) DEFAULT NULL,
  `codi_motiu_aillament` varchar(10) DEFAULT NULL,
  `desc_motiu_aillament` varchar(50) DEFAULT NULL,
  `data_inici_aillament` datetime DEFAULT NULL,
  `data_fi_aillament` datetime DEFAULT NULL,
  `data_update` datetime DEFAULT NULL,
  `data_delete` datetime DEFAULT NULL,
  `revisat` char(1) DEFAULT 'N',
  PRIMARY KEY (`id`),
  KEY `codi_llit` (`codi_llit`)
) ENGINE=InnoDB AUTO_INCREMENT=5729 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.aillament_microorganisme
CREATE TABLE IF NOT EXISTS `aillament_microorganisme` (
  `aillament_id` int(11) NOT NULL COMMENT 'Id del aillament',
  `pacient_diagnostic_id` int(11) NOT NULL COMMENT 'Id del microorganisme'
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Un aillament pot ser degut a un o mes d´un microorganisme';

-- Data exporting was unselected.

-- Dumping structure for table marsa.cens
CREATE TABLE IF NOT EXISTS `cens` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `cd_tenant` varchar(10) DEFAULT NULL,
  `npat` varchar(50) DEFAULT NULL,
  `nepi` varchar(45) DEFAULT NULL,
  `nom` varchar(45) DEFAULT NULL,
  `cognoms` varchar(90) DEFAULT NULL,
  `sexe` varchar(5) DEFAULT NULL,
  `dt_naixement` date DEFAULT NULL,
  `cd_servei` varchar(45) DEFAULT NULL,
  `ds_servei` varchar(50) DEFAULT NULL,
  `cd_ut` varchar(45) DEFAULT NULL,
  `ds_ut` varchar(45) DEFAULT NULL,
  `cd_llit` varchar(45) DEFAULT NULL,
  `ds_llit` varchar(45) DEFAULT NULL,
  `cd_tipepi` varchar(20) DEFAULT NULL,
  `dt_ingres` datetime DEFAULT NULL,
  `dt_alta` datetime DEFAULT NULL,
  `dt_create` datetime DEFAULT current_timestamp(),
  `origen` varchar(2) DEFAULT NULL COMMENT 'HS = Hospitalitzacio, HD = Hosp. de dia',
  PRIMARY KEY (`id`),
  KEY `ind_epi` (`cd_tenant`,`npat`,`nepi`)
) ENGINE=InnoDB AUTO_INCREMENT=287890437 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.ci_sessions
CREATE TABLE IF NOT EXISTS `ci_sessions` (
  `id` varchar(40) NOT NULL,
  `ip_address` varchar(45) NOT NULL,
  `timestamp` int(10) unsigned NOT NULL DEFAULT 0,
  `data` blob NOT NULL,
  PRIMARY KEY (`id`),
  KEY `ci_sessions_timestamp` (`timestamp`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci ROW_FORMAT=DYNAMIC;

-- Data exporting was unselected.

-- Dumping structure for table marsa.dades_access
CREATE TABLE IF NOT EXISTS `dades_access` (
  `npat` varchar(50) DEFAULT NULL,
  `data_mostra` datetime DEFAULT NULL,
  `microorganisme` varchar(50) DEFAULT NULL,
  `mecanisme_resistencia` varchar(50) DEFAULT NULL,
  `unitat` varchar(50) DEFAULT NULL,
  `servei` varchar(50) DEFAULT NULL,
  `motiu_solicitud` varchar(50) DEFAULT NULL,
  `estat_clinic` varchar(50) DEFAULT NULL,
  `localitzacio` varchar(50) DEFAULT NULL,
  `adquisicio` varchar(50) DEFAULT NULL,
  `lloc_deteccio` varchar(50) DEFAULT NULL,
  `tipus_mostra` varchar(50) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci ROW_FORMAT=DYNAMIC COMMENT='Extracció de totes les dades de modulab. Es fa anar per intentar migrar les dades actuals a nomenclatura modulab';

-- Data exporting was unselected.

-- Dumping structure for table marsa.estat_clinic
CREATE TABLE IF NOT EXISTS `estat_clinic` (
  `id` int(1) NOT NULL AUTO_INCREMENT,
  `codi` varchar(2) DEFAULT NULL,
  `descripcio` varchar(30) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `actiu` int(1) DEFAULT 1 COMMENT '1=SI 0=NO',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Estat clínic';

-- Data exporting was unselected.

-- Dumping structure for table marsa.integracio_modulab
CREATE TABLE IF NOT EXISTS `integracio_modulab` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `etiqueta_id` int(11) DEFAULT NULL,
  `pacient_sap` varchar(20) DEFAULT NULL,
  `cip` varchar(15) DEFAULT NULL,
  `colegiat_id` varchar(50) DEFAULT NULL,
  `nom_metge` varchar(100) DEFAULT NULL,
  `centre_descripcio` varchar(100) DEFAULT NULL,
  `data_peticio_truc` timestamp NULL DEFAULT NULL,
  `aillament_descripcio` varchar(100) DEFAULT NULL,
  `mecanisme_resistencia1_id` varchar(20) DEFAULT NULL,
  `mecanisme_resistencia_descrip` varchar(100) DEFAULT NULL,
  `servei_descripcio` varchar(100) DEFAULT NULL,
  `prova_descripcio` varchar(100) DEFAULT NULL,
  `mostra_descripcio` varchar(100) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT NULL COMMENT 'Data de la integració',
  `resultat` varchar(10) DEFAULT NULL COMMENT 'Codi del resultat de la integració',
  `data_resultat` timestamp NULL DEFAULT NULL COMMENT 'Data del resultat de modulab',
  `data_validacio` timestamp NULL DEFAULT NULL COMMENT 'Data en que s´ha validat la mostra a modulab',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=10784 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.integracio_modulab_aux
CREATE TABLE IF NOT EXISTS `integracio_modulab_aux` (
  `camp` varchar(50) NOT NULL,
  `valor` varchar(50) NOT NULL,
  `dt_update` datetime DEFAULT NULL COMMENT 'Data ultima actualització'
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.integracio_modulab_resultats
CREATE TABLE IF NOT EXISTS `integracio_modulab_resultats` (
  `id` int(1) NOT NULL AUTO_INCREMENT,
  `codi` varchar(5) DEFAULT NULL,
  `descripcio` varchar(300) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=15 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Resultats de la integració Modulab';

-- Data exporting was unselected.

-- Dumping structure for table marsa.llits
CREATE TABLE IF NOT EXISTS `llits` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `codi_llit` varchar(10) NOT NULL,
  `nom_llit` varchar(50) NOT NULL,
  `habitacio` varchar(10) NOT NULL,
  `planta` varchar(10) NOT NULL,
  `valid_de` date DEFAULT NULL,
  `valid_fins` date DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `codi_ut` varchar(10) DEFAULT NULL,
  `desc_ut` varchar(50) DEFAULT NULL,
  `codi_servei` varchar(10) DEFAULT NULL,
  `desc_servei` varchar(50) DEFAULT NULL,
  `codi_situacio` char(1) DEFAULT NULL,
  `desc_situacio` varchar(50) DEFAULT NULL,
  `codi_motiu_bloqueig` varchar(10) DEFAULT NULL,
  `desc_motiu_bloqueig` varchar(50) DEFAULT NULL,
  `tract_cures_intensives` char(1) DEFAULT NULL,
  `pacient` varchar(10) DEFAULT NULL,
  `nom_pacient` varchar(50) DEFAULT NULL,
  `cognom1_pacient` varchar(50) DEFAULT NULL,
  `cognom2_pacient` varchar(50) DEFAULT NULL,
  `edat_pacient` varchar(10) DEFAULT NULL,
  `sexe_pacient` varchar(20) DEFAULT NULL,
  `codi_servei_pacient` varchar(10) DEFAULT NULL,
  `desc_servei_pacient` varchar(50) DEFAULT NULL,
  `codi_class_tipus_uci` varchar(20) DEFAULT NULL,
  `desc_class_tipus_uci` varchar(50) DEFAULT NULL,
  `codi_motiu_aillament` varchar(10) DEFAULT NULL,
  `desc_motiu_aillament` varchar(50) DEFAULT NULL,
  `marca` char(1) DEFAULT NULL,
  PRIMARY KEY (`id`),
  KEY `codi_llit` (`codi_llit`)
) ENGINE=InnoDB AUTO_INCREMENT=1553 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.llocsdeteccio
CREATE TABLE IF NOT EXISTS `llocsdeteccio` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `codi` varchar(50) DEFAULT NULL,
  `descripcio` varchar(100) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `actiu` int(11) DEFAULT 1 COMMENT '1=SI 0=NO',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=209 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Llocs de detecció';

-- Data exporting was unselected.

-- Dumping structure for table marsa.localitzacio_infeccio
CREATE TABLE IF NOT EXISTS `localitzacio_infeccio` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `codi` varchar(150) DEFAULT NULL,
  `descripcio` varchar(150) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `actiu` int(11) DEFAULT 1 COMMENT '1=SI 0=NO',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=154 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Localització de la infecció';

-- Data exporting was unselected.

-- Dumping structure for table marsa.mecanismes
CREATE TABLE IF NOT EXISTS `mecanismes` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `codi` varchar(20) DEFAULT NULL,
  `descripcio` varchar(100) DEFAULT NULL,
  `tipus_mecanisme` varchar(100) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `actiu` int(11) DEFAULT 1 COMMENT '1=SI 0=NO',
  `incorpora_modulab` tinyint(4) DEFAULT 0 COMMENT 'Incorporar de modulab 1=si incorporar / 0 = no incorporar',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=1137 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Mecanismes de resistència';

-- Data exporting was unselected.

-- Dumping structure for table marsa.microorganismes
CREATE TABLE IF NOT EXISTS `microorganismes` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `codi` varchar(100) DEFAULT NULL,
  `descripcio` varchar(100) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `actiu` int(11) DEFAULT 1 COMMENT '1=SI 0=NO',
  `dies_vigencia` int(11) DEFAULT 365 COMMENT 'Dies de vigència',
  `especial` tinyint(4) DEFAULT 0 COMMENT 'Incorporació obligatoria al integrar dades modulab',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=535 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Families de bitxos';

-- Data exporting was unselected.

-- Dumping structure for table marsa.microorganismes_modulab
CREATE TABLE IF NOT EXISTS `microorganismes_modulab` (
  `descripcio` varchar(200) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.microorganisme_mecanisme_no_incorporar
CREATE TABLE IF NOT EXISTS `microorganisme_mecanisme_no_incorporar` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `microorganisme` varchar(100) DEFAULT NULL,
  `mecanisme` varchar(100) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  KEY `id` (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci ROW_FORMAT=DYNAMIC COMMENT='Combinacions codi microorganisme / codi mecanisme que no es volen incorporar en el procés de integració de Modulab';

-- Data exporting was unselected.

-- Dumping structure for table marsa.modulab_tot
CREATE TABLE IF NOT EXISTS `modulab_tot` (
  `ETIQUETA_ID` varchar(50) DEFAULT NULL,
  `PACIENT_SAP` varchar(50) DEFAULT NULL,
  `CIP` varchar(50) DEFAULT NULL,
  `CENTRE_DESCRIPCIO` varchar(50) DEFAULT NULL,
  `DATA_PETICIO` datetime DEFAULT NULL,
  `MICROORGANISME` varchar(50) DEFAULT NULL,
  `MECANISME_RESISTENCIA` varchar(50) DEFAULT NULL,
  `MECANISME_RESISTENCIA_DESCRIP` varchar(200) DEFAULT NULL,
  `SERVEI_DESCRIPCIO` varchar(100) DEFAULT NULL,
  `PROVA_DESCRIPCIO` varchar(100) DEFAULT NULL,
  `MOSTRA_DESCRIPCIO` varchar(100) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Extracció de totes les dades de modulab. Es fa anar per intentar migrar les dades actuals a nomenclatura modulab';

-- Data exporting was unselected.

-- Dumping structure for table marsa.modulab_tot10
CREATE TABLE IF NOT EXISTS `modulab_tot10` (
  `ETIQUETA_ID` varchar(50) DEFAULT NULL,
  `PACIENT_SAP` varchar(50) DEFAULT NULL,
  `CIP` varchar(50) DEFAULT NULL,
  `CENTRE_DESCRIPCIO` varchar(50) DEFAULT NULL,
  `DATA_PETICIO` datetime DEFAULT NULL,
  `MICROORGANISME` varchar(50) DEFAULT NULL,
  `MECANISME_RESISTENCIA` varchar(50) DEFAULT NULL,
  `MECANISME_RESISTENCIA_DESCRIP` varchar(200) DEFAULT NULL,
  `SERVEI_DESCRIPCIO` varchar(100) DEFAULT NULL,
  `PROVA_DESCRIPCIO` varchar(100) DEFAULT NULL,
  `MOSTRA_DESCRIPCIO` varchar(100) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Extracció de totes les dades de modulab. Es fa anar per intentar migrar les dades actuals a nomenclatura modulab';

-- Data exporting was unselected.

-- Dumping structure for table marsa.mostra_microorganisme
CREATE TABLE IF NOT EXISTS `mostra_microorganisme` (
  `pacient_diagnostic_mostra_id` int(11) NOT NULL COMMENT 'Id de la  aillament',
  `pacient_diagnostic_id` int(11) NOT NULL COMMENT 'Id del microorganisme del pacient',
  PRIMARY KEY (`pacient_diagnostic_mostra_id`,`pacient_diagnostic_id`) USING BTREE,
  KEY `fk_pacients_diagnostics` (`pacient_diagnostic_id`) USING BTREE,
  CONSTRAINT `FK_mostra_microorganisme_pacients_diagnostics_mostra` FOREIGN KEY (`pacient_diagnostic_mostra_id`) REFERENCES `pacients_diagnostics_mostra` (`id`) ON DELETE CASCADE,
  CONSTRAINT `fk_pacients_diagnostics` FOREIGN KEY (`pacient_diagnostic_id`) REFERENCES `pacients_diagnostics` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Una mostra es pot associar a un o mes d´un microorganisme';

-- Data exporting was unselected.

-- Dumping structure for table marsa.motius
CREATE TABLE IF NOT EXISTS `motius` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `codi` varchar(20) DEFAULT NULL,
  `descripcio` varchar(100) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `actiu` int(11) DEFAULT 1 COMMENT '1=SI 0=NO',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Motius de sol·licitud de mostra';

-- Data exporting was unselected.

-- Dumping structure for table marsa.omnium_tot
CREATE TABLE IF NOT EXISTS `omnium_tot` (
  `NPAT` varchar(50) DEFAULT NULL,
  `DATA_DIAGNOSTIC` datetime DEFAULT NULL,
  `CODI_MICROORGANISME` varchar(100) DEFAULT NULL,
  `NOM_MICROORGANISME` varchar(100) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.omnium_tot_tmp
CREATE TABLE IF NOT EXISTS `omnium_tot_tmp` (
  `NPAT` varchar(50) DEFAULT NULL,
  `DATA_DIAGNOSTIC` datetime DEFAULT NULL,
  `CODI_MICROORGANISME` varchar(100) DEFAULT NULL,
  `NOM_MICROORGANISME` varchar(100) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Extracció de totes les dades de Omnium. Es fa anar per intentar migrar les dades actuals a nomenclatura modulab';

-- Data exporting was unselected.

-- Dumping structure for table marsa.pacients
CREATE TABLE IF NOT EXISTS `pacients` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `npat` varchar(20) DEFAULT NULL,
  `nom` varchar(50) DEFAULT NULL,
  `cognom1` varchar(50) DEFAULT NULL,
  `cognom2` varchar(50) DEFAULT NULL,
  `dt_naixement` date DEFAULT NULL,
  `sexe` varchar(5) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT NULL,
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `observacions` varchar(50) DEFAULT NULL,
  `fitxa` char(1) DEFAULT NULL COMMENT 'hi ha fitxa fisica',
  `cip` varchar(15) DEFAULT NULL,
  `bitxo_hg` varchar(50) DEFAULT NULL,
  `dt_exitus` date DEFAULT NULL COMMENT 'Data èxitus',
  `abs_referencia` varchar(50) DEFAULT NULL COMMENT 'ABS de referència',
  `dt_canviws` timestamp NULL DEFAULT NULL,
  `consolidat` char(1) DEFAULT NULL COMMENT 'registre s’ha consolidat després importació modulab (N=no, NULL=si)',
  `usuari` varchar(50) DEFAULT NULL COMMENT 'usuari que ha fet alta',
  PRIMARY KEY (`id`),
  KEY `npat` (`npat`),
  KEY `nom_cognom1_cognom2` (`cognom1`,`cognom2`,`nom`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=21175 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.pacients_diagnostics
CREATE TABLE IF NOT EXISTS `pacients_diagnostics` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `npat` varchar(20) DEFAULT NULL,
  `data_diagnostic` date DEFAULT NULL,
  `usuari` varchar(50) DEFAULT NULL,
  `bitxo` varchar(100) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `microorganisme` varchar(50) DEFAULT NULL,
  `mecanisme` varchar(50) DEFAULT NULL COMMENT 'Mecanisme de resistencia',
  `tipus_mecanisme` varchar(100) DEFAULT NULL COMMENT 'Tipus de mecanisme',
  `lloc_deteccio` varchar(100) DEFAULT NULL COMMENT 'Lloc de detecció',
  `adquisicio` varchar(50) DEFAULT NULL COMMENT 'Adquisició',
  `estat_clinic` varchar(2) DEFAULT NULL COMMENT 'Estat clínic',
  `localitzacio_infeccio` varchar(150) DEFAULT NULL COMMENT 'Localització de la infecció',
  `data_ingres` date DEFAULT NULL COMMENT 'Data ingrés',
  `data_alta` date DEFAULT NULL COMMENT 'Data alta',
  `servei` varchar(50) DEFAULT NULL COMMENT 'Servei',
  `unitat` varchar(50) DEFAULT NULL COMMENT 'Unitat',
  `consolidat` char(1) DEFAULT NULL COMMENT 'registre s’ha consolidat després importació modulab (N=no, NULL=si)',
  `migrat` char(1) DEFAULT NULL COMMENT 'per controlar si s´ha migrat o no. esborrar un cop feta la migració',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=23273 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.pacients_diagnostics_mostra
CREATE TABLE IF NOT EXISTS `pacients_diagnostics_mostra` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `npat` varchar(20) DEFAULT NULL,
  `data_diagnostic` date DEFAULT NULL,
  `data_mostra` date DEFAULT NULL COMMENT 'data de la mostra',
  `tipus_mostra` varchar(50) DEFAULT NULL COMMENT 'tipus de mostra',
  `motiu` varchar(50) DEFAULT NULL COMMENT 'estat clinic',
  `usuari` varchar(50) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `valoracio` char(1) DEFAULT '0' COMMENT 'valoració de la mostra ',
  `consolidat` char(1) DEFAULT NULL COMMENT 'registre s’ha consolidat després importació modulab (N=no, NULL=si)',
  `microorganisme` varchar(50) DEFAULT NULL,
  `tipus_prova` varchar(300) DEFAULT NULL COMMENT 'tipus de prova',
  `tipus_mostra_m` varchar(100) DEFAULT NULL COMMENT 'tipus mostra segons nomenclatura modulab',
  `migrat` char(1) DEFAULT NULL COMMENT 'per controlar si s´ha migrat o no. esborrar un cop feta la migració',
  `etiqueta` varchar(15) DEFAULT NULL COMMENT 'etiqueta id de la mostra a modulab',
  `estat_integracio_m` varchar(1) DEFAULT NULL COMMENT 'estat de la mostra a la integració de modulab. P=pendent de validar / V=validada',
  `data_resultat` timestamp NULL DEFAULT NULL COMMENT 'Data del resultat de modulab',
  `data_validacio` timestamp NULL DEFAULT NULL COMMENT 'Data en que s´ha validat la mostra a modulab',
  PRIMARY KEY (`id`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=33948 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci ROW_FORMAT=DYNAMIC;

-- Data exporting was unselected.

-- Dumping structure for table marsa.pacients_diagnostics_mostra_historial
CREATE TABLE IF NOT EXISTS `pacients_diagnostics_mostra_historial` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `etiqueta` varchar(50) NOT NULL,
  `versio` int(11) NOT NULL,
  `tipus_canvi` enum('VALIDADA_AMB_CANVIS','REVALIDADA_AMB_CANVIS','DESVALIDADA_AMB_CANVIS') NOT NULL,
  `combinacions_anteriors` text DEFAULT NULL,
  `data_resultat_anterior` datetime DEFAULT NULL,
  `data_validacio_anterior` datetime DEFAULT NULL,
  `combinacions_noves` text DEFAULT NULL,
  `data_resultat_nova` datetime DEFAULT NULL,
  `data_validacio_nova` datetime DEFAULT NULL,
  `data_canvi` datetime NOT NULL DEFAULT current_timestamp(),
  `proces_origen` varchar(50) DEFAULT 'IntegracioModulab',
  PRIMARY KEY (`id`),
  KEY `idx_etiqueta_versio` (`etiqueta`,`versio`),
  KEY `idx_data_canvi` (`data_canvi`),
  KEY `idx_tipus_canvi` (`tipus_canvi`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for view marsa.pacients_diagnostics_view
-- Creating temporary table to overcome VIEW dependency errors
CREATE TABLE `pacients_diagnostics_view` (
	`id` INT(11) NOT NULL,
	`npat` VARCHAR(1) NULL COLLATE 'utf8_unicode_ci',
	`microorganisme_mecanisme` VARCHAR(1) NULL COLLATE 'utf8_unicode_ci'
) ENGINE=MyISAM;

-- Dumping structure for table marsa.resultats_cultius
CREATE TABLE IF NOT EXISTS `resultats_cultius` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `npat` varchar(20) DEFAULT NULL,
  `microorganisme` varchar(20) DEFAULT NULL,
  `cultiu` varchar(50) DEFAULT NULL,
  `data` date DEFAULT NULL,
  `resultat` tinyint(4) DEFAULT 0,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=54 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.tipusmecanismes
CREATE TABLE IF NOT EXISTS `tipusmecanismes` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `codi` varchar(100) DEFAULT NULL,
  `descripcio` varchar(100) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `actiu` int(11) DEFAULT 1 COMMENT '1=SI 0=NO',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=44 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Tipus de Mecanismes de resistència';

-- Data exporting was unselected.

-- Dumping structure for table marsa.tipusmostra
CREATE TABLE IF NOT EXISTS `tipusmostra` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `codi` varchar(100) DEFAULT NULL,
  `descripcio` varchar(100) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `actiu` int(11) DEFAULT 1 COMMENT '1=SI 0=NO',
  `comportament` int(1) DEFAULT 0 COMMENT '0=Incorpora si positiu per aquest tipus 1=Incorpora si hi ha algun positiu',
  `dies_vigencia_positiu` int(11) DEFAULT 455 COMMENT 'Per defecte 1 any i 3 mesos',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=138 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Tipus de Mostres';

-- Data exporting was unselected.

-- Dumping structure for table marsa.tipusmostra_equivalents
CREATE TABLE IF NOT EXISTS `tipusmostra_equivalents` (
  `tipusmostra_id` int(11) NOT NULL,
  `tipusmostra_id_equivalent` int(11) NOT NULL,
  PRIMARY KEY (`tipusmostra_id`,`tipusmostra_id_equivalent`),
  KEY `tipusmostra_id_equivalent` (`tipusmostra_id_equivalent`),
  CONSTRAINT `tipusmostra_equivalents_ibfk_1` FOREIGN KEY (`tipusmostra_id`) REFERENCES `tipusmostra_m` (`id`),
  CONSTRAINT `tipusmostra_equivalents_ibfk_2` FOREIGN KEY (`tipusmostra_id_equivalent`) REFERENCES `tipusmostra_m` (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci;

-- Data exporting was unselected.

-- Dumping structure for table marsa.tipusmostra_m
CREATE TABLE IF NOT EXISTS `tipusmostra_m` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `codi` varchar(100) DEFAULT NULL,
  `descripcio` varchar(100) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `actiu` int(11) DEFAULT 1 COMMENT '1=SI 0=NO',
  `comportament` int(1) DEFAULT 0 COMMENT '0=RES 1=INCORPORAR SEMPRE SI HI HA ALGUN POSITIU',
  `dies_vigencia_positiu` int(11) DEFAULT NULL COMMENT 'Numero de dies que es vigent un positiu',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=69 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Tipus de Mostres Modulab';

-- Data exporting was unselected.

-- Dumping structure for table marsa.tipusprova
CREATE TABLE IF NOT EXISTS `tipusprova` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `codi` varchar(300) DEFAULT NULL,
  `descripcio` varchar(300) DEFAULT NULL,
  `comportament` int(1) DEFAULT NULL COMMENT '0=RES 1=SI POSITIU 2=SI QUALSEVOL',
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `actiu` int(1) DEFAULT 1 COMMENT '1=SI 0=NO',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=4780 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci ROW_FORMAT=DYNAMIC COMMENT='Tipus de Mostres';

-- Data exporting was unselected.

-- Dumping structure for table marsa.valoracio_mostra
CREATE TABLE IF NOT EXISTS `valoracio_mostra` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `codi` varchar(1) DEFAULT NULL,
  `descripcio` varchar(50) DEFAULT NULL,
  `dt_create` timestamp NULL DEFAULT current_timestamp(),
  `dt_update` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `dt_delete` timestamp NULL DEFAULT NULL,
  `actiu` int(11) DEFAULT 1 COMMENT '1=SI 0=NO',
  `color` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8 COLLATE=utf8_unicode_ci COMMENT='Codis valoracions de les mostres';

-- Data exporting was unselected.

-- Removing temporary table and create final VIEW structure
DROP TABLE IF EXISTS `pacients_diagnostics_view`;
CREATE ALGORITHM=UNDEFINED SQL SECURITY DEFINER VIEW `pacients_diagnostics_view` AS select `pacients_diagnostics`.`id` AS `id`,`pacients_diagnostics`.`npat` AS `npat`,concat(`pacients_diagnostics`.`microorganisme`,' - ',`pacients_diagnostics`.`mecanisme`) AS `microorganisme_mecanisme` from `pacients_diagnostics`
;

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
