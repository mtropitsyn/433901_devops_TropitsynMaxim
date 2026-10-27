# Калькулятор и MariaDB — работа 1.7.2

Тропицын Максим Владимирович, НМТ-433901, вариант 16.
Администрирование информационных сетей. Преподаватель: Гурин И. А.

Проект для отдельного Linux-сервера: GitHub → Docker Compose → приложение и MariaDB.
Полная инструкция: [docs/SERVER_DEPLOYMENT.md](docs/SERVER_DEPLOYMENT.md).
Короткие команды: [docs/SERVER_COMMANDS.txt](docs/SERVER_COMMANDS.txt).

ASP.NET Core MVC выполняет четыре арифметические операции и сохраняет каждый успешный расчёт через Entity Framework Core и Pomelo в MariaDB. Некорректный ввод, деление на ноль и переполнение не создают записи. Если сохранение не удалось, форма показывает ошибку. Вычисления и хранение используют decimal.

| Параметр | Значение |
| --- | --- |
| Сайт | http://IP_СЕРВЕРА:5316 |
| Compose-проект | tropitsyn16 |
| Контейнеры | tropitsyn16-web-1, tropitsyn16-db-1 |
| База | 16-WebCalculatorDb |
| Пользователь БД | calculator |
| Таблица | DataInputVariants |
| Поля | ID_DataInputVariant, Operand_1, Operand_2, Type_operation, Result |
| Приложение → БД | db:3306 во внутренней сети Docker |
| БД на сервере | 127.0.0.1:53316 |
| HeidiSQL через туннель PuTTY | 127.0.0.1:13316 на компьютере пользователя |
| Постоянный том | tropitsyn16_mariadb-data при стандартном имени проекта |

Порт сайта 5316 выделен пользователю. Порт 53316 выбран для loopback-доступа к БД; если он занят, нужно изменить DB_PORT в .env и Destination туннеля. Пароли генерируются на сервере и не входят в Git.

Подключены Microsoft.EntityFrameworkCore, Relational, Design и Tools 8.0.14, Pomelo.EntityFrameworkCore.MySql 8.0.3, MySqlConnector 2.6.2. Миграция m_init уже включена в CalculatorWeb/Migrations. При старте приложения Database.MigrateAsync() применяет отсутствующие миграции. Повторно создавать m_init не требуется. database/migrations.sql — SQL этой миграции для просмотра; отдельно исполнять его при обычном запуске не нужно.

Основной порядок на сервере, из папки с compose.yaml:

```bash
sudo bash scripts/init-server-env.sh
sudo docker compose pull db
sudo docker compose up -d --wait db
sudo bash scripts/prepare-database.sh
sudo docker compose build web
sudo docker compose up -d --wait web
sudo docker compose ps
curl -f http://127.0.0.1:5316/health/ready
```

Подготовка старой папки, права на .env, занятые порты, установка Docker и HeidiSQL описаны в полной инструкции. На новом сервере создаётся новая база; GitHub не переносит содержимое томов с другой машины. Остановка и повторный запуск сохраняют записи в томе.

Проверка файлов проекта без контейнеров:

```text
dotnet tool restore
dotnet build CalculatorDocker.sln -c Release
dotnet run --project CalculatorWeb.Checks -c Release --no-build
dotnet ef migrations has-pending-model-changes --project CalculatorWeb -c Release --no-build
```

CalculatorWeb.Checks содержит 32 проверки арифметики и контроллера. Скрипты Test-Web.ps1 и Test-Database.ps1 проверяют уже работающие контейнеры и добавляют тестовые расчёты. GitHub Actions выполняет контейнерные проверки на runner GitHub, если .github/workflows расположен в корне отдельного репозитория с этим проектом. Если проект лежит в App_practical, расположение workflow и рабочую директорию настраивают в корне общего репозитория. Ручной запуск на сервере от Actions не зависит.

Основные файлы:

- CalculatorWeb/Data — сущность, DbContext и сохранение через EF.
- CalculatorWeb/Migrations — m_init и снимок модели.
- Dockerfile, compose.yaml — сборка и два контейнера.
- database/prepare.sql — создание базы и выдача доступа без удаления данных.
- database/migrations.sql — сгенерированный EF SQL.
- scripts/init-server-env.sh — серверный .env со случайными паролями.
- scripts/install-docker-server.sh — установка Docker на Ubuntu/Debian, если его нет.
- scripts/prepare-database.sh — подготовка базы при первом и повторном запуске.

PostgreSQL_Lab, старые отчёты и журналы в общей рабочей папке относятся к прошлым заданиям. Они не включены в чистый архив Calculator_MariaDB_Lab2.zip. Старый database/init.sql также исключён из архива и не подключается текущим compose.yaml.

Основание: «Основы методологии Development Operation», 2025, п. 1.7.2, с. 36–42. Kafka, Jenkins, Prometheus и Grafana относятся к последующим работам и в проект второй работы не входят.