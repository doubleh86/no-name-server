-- MySQL dump 10.13  Distrib 9.5.0, for macos14.7 (x86_64)
--
-- Host: 127.0.0.1    Database: shared_db
-- ------------------------------------------------------
-- Server version	9.5.0

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
SET @MYSQLDUMP_TEMP_LOG_BIN = @@SESSION.SQL_LOG_BIN;
SET @@SESSION.SQL_LOG_BIN= 0;

--
-- GTID state at the beginning of the backup 
--

SET @@GLOBAL.GTID_PURGED=/*!80000 '+'*/ '56dd00b4-c131-11f0-84ba-01404288e5e2:1-2758';

--
-- Current Database: `shared_db`
--

CREATE DATABASE /*!32312 IF NOT EXISTS*/ `shared_db` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

USE `shared_db`;

--
-- Table structure for table `account_info`
--

DROP TABLE IF EXISTS `account_info`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `account_info` (
  `account_id` bigint NOT NULL AUTO_INCREMENT,
  `login_id` varchar(50) NOT NULL,
  `account_type` int NOT NULL,
  `game_db_uid` int NOT NULL,
  `update_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `create_date` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`account_id`),
  KEY `idx_account_info_login_id` (`login_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `account_info`
--

LOCK TABLES `account_info` WRITE;
/*!40000 ALTER TABLE `account_info` DISABLE KEYS */;
/*!40000 ALTER TABLE `account_info` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `event_info`
--

DROP TABLE IF EXISTS `event_info`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `event_info` (
  `event_id` bigint NOT NULL AUTO_INCREMENT,
  `event_type_id` int NOT NULL,
  `event_period_type` int NOT NULL,
  `event_table_index` int NOT NULL,
  `event_extra_value` varchar(300) NOT NULL,
  `event_start_date` datetime NOT NULL,
  `event_end_date` datetime NOT NULL,
  `event_expiry_date` datetime NOT NULL,
  `update_date` datetime NOT NULL DEFAULT (utc_timestamp()),
  `create_date` datetime NOT NULL DEFAULT (utc_timestamp()),
  PRIMARY KEY (`event_id`)
) ENGINE=InnoDB AUTO_INCREMENT=1000000 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `event_info`
--

LOCK TABLES `event_info` WRITE;
/*!40000 ALTER TABLE `event_info` DISABLE KEYS */;
/*!40000 ALTER TABLE `event_info` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `game_db_info`
--

DROP TABLE IF EXISTS `game_db_info`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `game_db_info` (
  `uid` int NOT NULL,
  `host` varchar(30) DEFAULT NULL,
  `port` int DEFAULT NULL,
  `db_name` varchar(50) DEFAULT NULL,
  `db_login_id` varchar(50) DEFAULT NULL,
  `db_password` varchar(50) DEFAULT NULL,
  `slave_host` varchar(30) DEFAULT NULL,
  `slave_port` int DEFAULT NULL,
  `slave_db_name` varchar(50) DEFAULT NULL,
  `slave_db_login_id` varchar(50) DEFAULT NULL,
  `slave_db_password` varchar(50) DEFAULT NULL,
  `current_user_count` int DEFAULT '0',
  `is_use` int DEFAULT '0',
  PRIMARY KEY (`uid`),
  KEY `idx_game_db_info_is_use` (`is_use`,`current_user_count`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `game_db_info`
--

LOCK TABLES `game_db_info` WRITE;
/*!40000 ALTER TABLE `game_db_info` DISABLE KEYS */;
INSERT INTO `game_db_info` VALUES (1,'127.0.0.1',1433,'main_db','SA','Passw0rd!2025','127.0.0.1',1433,'main_db','SA','Passw0rd!2025',10,1),(2,'127.0.0.1',1433,'main_db','SA','Passw0rd!2025','127.0.0.1',1433,'main_db','SA','Passw0rd!2025',2,1);
/*!40000 ALTER TABLE `game_db_info` ENABLE KEYS */;
UNLOCK TABLES;
SET @@SESSION.SQL_LOG_BIN = @MYSQLDUMP_TEMP_LOG_BIN;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-03-08 21:48:04
