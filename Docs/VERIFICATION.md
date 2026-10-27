# Проверка исходников 27.10.2026

Проверено без запуска Docker, веб-приложения и базы данных для итоговой версии:

- dotnet build CalculatorDocker.sln -c Release --no-restore: успешно, 0 ошибок, 0 предупреждений.
- dotnet run --project CalculatorWeb.Checks -c Release --no-build: 32 проверки пройдены.
- dotnet ef migrations has-pending-model-changes --project CalculatorWeb -c Release --no-build: модель соответствует миграции m_init.
- dotnet ef migrations script: SQL сформирован в database/migrations.sql.
- bash -n: синтаксис init-server-env.sh, prepare-database.sh и install-docker-server.sh проверен без исполнения скриптов.

Финальная контейнерная проверка выполняется пользователем на целевом сервере по SERVER_DEPLOYMENT.md: создание MariaDB, запуск web, /health/ready, расчёты и SELECT из HeidiSQL. Файл не утверждает, что приложение уже развёрнуто на целевом сервере.
