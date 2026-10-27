CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `DataInputVariants` (
    `ID_DataInputVariant` int NOT NULL AUTO_INCREMENT,
    `Operand_1` decimal(57,28) NOT NULL,
    `Operand_2` decimal(57,28) NOT NULL,
    `Type_operation` varchar(16) CHARACTER SET utf8mb4 NOT NULL,
    `Result` decimal(57,28) NOT NULL,
    CONSTRAINT `PK_DataInputVariants` PRIMARY KEY (`ID_DataInputVariant`)
) CHARACTER SET=utf8mb4;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261027130146_m_init', '8.0.14');

COMMIT;

