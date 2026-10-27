-- Run as root once for an existing MariaDB volume; never resets passwords or deletes data.
CREATE DATABASE IF NOT EXISTS `16-WebCalculatorDb`
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
GRANT ALL PRIVILEGES ON `16-WebCalculatorDb`.* TO 'calculator'@'%';
