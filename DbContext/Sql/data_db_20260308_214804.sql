-- MySQL dump 10.13  Distrib 9.5.0, for macos14.7 (x86_64)
--
-- Host: 127.0.0.1    Database: data_db
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
-- Current Database: `data_db`
--

CREATE DATABASE /*!32312 IF NOT EXISTS*/ `data_db` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

USE `data_db`;

--
-- Table structure for table `map_info`
--

DROP TABLE IF EXISTS `map_info`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `map_info` (
  `zone_id` int NOT NULL,
  `world_id` int NOT NULL,
  `size_x` int NOT NULL,
  `size_z` int NOT NULL,
  `chunk_size` int DEFAULT NULL,
  `world_offset_x` int DEFAULT NULL,
  `world_offset_z` int DEFAULT NULL,
  PRIMARY KEY (`zone_id`,`world_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `map_info`
--

LOCK TABLES `map_info` WRITE;
/*!40000 ALTER TABLE `map_info` DISABLE KEYS */;
INSERT INTO `map_info` VALUES (101,1,2000,2000,25,0,0),(102,1,2000,2000,25,2000,0),(103,1,2000,2000,25,0,2000),(104,1,2000,2000,25,2000,2000);
/*!40000 ALTER TABLE `map_info` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `monster_group`
--

DROP TABLE IF EXISTS `monster_group`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `monster_group` (
  `monster_group_id` int NOT NULL,
  `world_id` int DEFAULT NULL,
  `monster_id_list` varchar(255) DEFAULT NULL,
  `zone_id` int DEFAULT NULL,
  `position_x` int DEFAULT NULL,
  `position_y` int DEFAULT NULL,
  `position_z` int DEFAULT NULL,
  `roam_radius` float DEFAULT NULL,
  PRIMARY KEY (`monster_group_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `monster_group`
--

LOCK TABLES `monster_group` WRITE;
/*!40000 ALTER TABLE `monster_group` DISABLE KEYS */;
INSERT INTO `monster_group` VALUES (1,1,'1˜2˜3',101,300,1,250,5),(2,1,'1˜2˜3',101,400,1,460,35);
/*!40000 ALTER TABLE `monster_group` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `monster_info`
--

DROP TABLE IF EXISTS `monster_info`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `monster_info` (
  `monster_id` int NOT NULL,
  `monster_skill_1` int DEFAULT NULL,
  `monster_skill_2` int DEFAULT NULL,
  `monster_skill_3` int DEFAULT NULL,
  `monster_type` int DEFAULT NULL,
  `monster_hp` int DEFAULT NULL,
  `monster_speed` float DEFAULT NULL,
  `max_anchor_distance` float DEFAULT NULL,
  PRIMARY KEY (`monster_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `monster_info`
--

LOCK TABLES `monster_info` WRITE;
/*!40000 ALTER TABLE `monster_info` DISABLE KEYS */;
INSERT INTO `monster_info` VALUES (1,0,0,0,0,25000,5,35),(2,1,0,0,0,10000,7,35),(3,0,2,0,2,500000,6,35);
/*!40000 ALTER TABLE `monster_info` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `skill_info`
--

DROP TABLE IF EXISTS `skill_info`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `skill_info` (
  `skill_id` int NOT NULL,
  `skill_name` varchar(20) DEFAULT NULL,
  `skill_cool_time` int DEFAULT NULL,
  `skill_range` float DEFAULT NULL,
  `skill_damage` float DEFAULT NULL,
  PRIMARY KEY (`skill_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `skill_info`
--

LOCK TABLES `skill_info` WRITE;
/*!40000 ALTER TABLE `skill_info` DISABLE KEYS */;
INSERT INTO `skill_info` VALUES (1,'default_skill',5,10,20);
/*!40000 ALTER TABLE `skill_info` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `world_info`
--

DROP TABLE IF EXISTS `world_info`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `world_info` (
  `world_id` int NOT NULL,
  `world_name` varchar(200) DEFAULT NULL,
  `start_position_x` int DEFAULT NULL,
  `start_position_z` int DEFAULT NULL,
  `end_position_x` int DEFAULT NULL,
  `end_position_z` int DEFAULT NULL,
  PRIMARY KEY (`world_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `world_info`
--

LOCK TABLES `world_info` WRITE;
/*!40000 ALTER TABLE `world_info` DISABLE KEYS */;
INSERT INTO `world_info` VALUES (1,'testWorld',0,0,100000,100000);
/*!40000 ALTER TABLE `world_info` ENABLE KEYS */;
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

-- Dump completed on 2026-03-08 21:48:05
