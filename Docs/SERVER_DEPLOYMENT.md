# Инструкция: калькулятор и MariaDB на отдельном сервере

Работа 1.7.2. Тропицын Максим Владимирович, НМТ-433901, вариант 16.
Сайт: порт 5316. База: 16-WebCalculatorDb. Таблица: DataInputVariants.

Все команды Bash ниже вводятся в PuTTY на целевом Linux-сервере. На своём компьютере нужны PuTTY, браузер и HeidiSQL. Копировать команды по одной строке, без приглашения student@... или знака $.

**1. Обновить GitHub.**

Распаковать Calculator_MariaDB_Lab2.zip и перенести содержимое в папку проекта в своём репозитории. Если проект лежит в App_practical, обновить её содержимое. Не создавать лишнюю вложенную папку Calculator_MariaDB_Lab2 внутри App_practical.

Отправить исходники, Dockerfile, compose.yaml, папку Migrations, database, scripts и .env.example. Файл .env с действующими паролями не отправлять. Выполнить commit и push привычным способом.

**2. Войти на сервер через PuTTY.**

Использовать адрес и логин целевого сервера, сохранённые в своей сессии PuTTY. Тип SSH, порт SSH — назначенный сервером, обычно 22. Порт 5316 относится к сайту, а не к SSH.

**3. Получить свежие файлы. Выбрать подходящий вариант.**

Если прежний репозиторий уже есть на сервере:

```bash
cd ~/433901_devops_TropitsynMaxim/App_practical
```

```bash
git status --short
```

```bash
git pull --ff-only
```

Если git pull сообщает о конфликте или локальных изменениях, сначала разобраться с перечисленными файлами, не удаляя их через git reset --hard.

Если сервер новый и репозиторий ещё не клонировали:

```bash
cd ~
```

```bash
git clone URL_ТВОЕГО_РЕПОЗИТОРИЯ calculator-project
```

Вместо URL_ТВОЕГО_РЕПОЗИТОРИЯ вставить HTTPS-ссылку из кнопки Code на GitHub. Затем:

```bash
cd ~/calculator-project
```

Если файлы приложения лежат в App_practical, дополнительно:

```bash
cd App_practical
```

Проверить папку:

```bash
pwd
```

```bash
ls
```

Здесь должны быть compose.yaml, Dockerfile, CalculatorWeb и scripts. Все следующие команды выполняются из этой папки. Если git отсутствует на Ubuntu/Debian: sudo apt-get update, затем sudo apt-get install -y git.

**4. Проверить Docker.**

```bash
docker --version
```

```bash
sudo docker compose version
```

```bash
sudo docker info
```

Если всё работает, перейти к шагу 5. Для нового Ubuntu/Debian без Docker:

```bash
cat /etc/os-release
```

```bash
sudo bash scripts/install-docker-server.sh
```

Установщик подключает официальный apt-репозиторий Docker и устанавливает Engine, Buildx и Compose. Уже установленный Docker с Compose он не меняет. При другой ОС или конфликтующих пакетах останавливается с объяснением; существующие контейнерные runtime автоматически не удаляет. На общем учебном сервере Docker обычно уже установлен.

Если Docker установлен, но его служба остановлена, на сервере с systemd запустить её:

```bash
sudo systemctl start docker
```

**5. Создать .env и исправить права.**

```bash
sudo bash scripts/init-server-env.sh
```

Создаётся .env со случайными паролями, APP_PORT=5316, DB_PORT=53316 и именем проекта tropitsyn16. Существующий .env сохраняется без изменений.

```bash
sudo chown "$(id -un)" .env
```

```bash
chmod 600 .env
```

Для старого .env установить серверные параметры отдельными командами. Пароли они не меняют:

```bash
sed -i 's/^APP_PORT=.*/APP_PORT=5316/' .env
```

```bash
sed -i 's/^DB_PORT=.*/DB_PORT=53316/' .env
```

```bash
sed -i 's/^COMPOSE_PROJECT_NAME=.*/COMPOSE_PROJECT_NAME=tropitsyn16/' .env
```

Если строка отсутствует, соответствующее значение по умолчанию уже задано в compose.yaml. Проверить файл:

```bash
nano .env
```

Ожидаемая структура:

```dotenv
COMPOSE_PROJECT_NAME=tropitsyn16
STUDENT_NUMBER=16
APP_PORT=5316
DB_PORT=53316
MARIADB_PASSWORD=реальный_пароль_от_скрипта
MARIADB_ROOT_PASSWORD=другой_реальный_пароль_от_скрипта
```

Последние две строки — пояснение: сохраняются реальные значения, созданные скриптом. Из nano выйти Ctrl+X; если правили — Ctrl+O, Enter, Ctrl+X. Пароли уже инициализированного тома не меняются простой правкой .env: нужно сохранить действующие значения.

**6. Проверить занятость портов.**

```bash
sudo ss -lntp | grep -E ':5316|:53316'
```

```bash
sudo docker ps
```

Для первого запуска нужны свободные порты. Если 5316 занимает твой старый калькулятор первой работы, взять его имя из docker ps и остановить именно этот контейнер:

```bash
sudo docker stop ИМЯ_ТВОЕГО_СТАРОГО_КОНТЕЙНЕРА
```

Подставить настоящее имя вместо текста-заглушки. Если уже работают tropitsyn16-web-1 и tropitsyn16-db-1, это текущий проект: Compose обновит их сам. Если 53316 занят чужим приложением, выбрать свободный локальный порт в DB_PORT через nano .env и использовать его же в Destination туннеля PuTTY. Сайт оставить на 5316.

**7. Проверить настройки Compose.**

```bash
sudo docker compose config --quiet
```

Нормальный результат — завершение без ошибки. В этой форме пароли не выводятся.

**8. Создать и запустить контейнер MariaDB.**

```bash
sudo docker compose pull db
```

Загружается официальный образ mariadb:11.4. Свой Dockerfile для базы не нужен.

```bash
sudo docker compose up -d --wait db
```

Создаются сеть, постоянный том и контейнер. При первом старте MariaDB создаёт базу 16-WebCalculatorDb и пользователя calculator. --wait ожидает проверки готовности.

```bash
sudo bash scripts/prepare-database.sh
```

Этот шаг также создаёт базу, если её нет, и выдаёт calculator права на неё. Он нужен и при повторном использовании тома: переменные MARIADB_DATABASE выполняют первоначальную настройку только для нового тома. Таблицу приложения создаст EF-миграция на следующем шаге.

```bash
sudo docker compose ps db
```

Ожидается работающий контейнер со статусом healthy.

**9. Собрать и запустить контейнер калькулятора.**

```bash
sudo docker compose build web
```

Dockerfile собирает приложение в SDK-образе и переносит его в runtime-образ. .NET SDK на сервер отдельно устанавливать не требуется. Получается образ 16-calculator:latest.

```bash
sudo docker compose up -d --wait web
```

Приложение применяет включённую миграцию m_init и создаёт DataInputVariants. Повторно создавать миграцию не нужно. EF проверяет __EFMigrationsHistory; ранее применённые миграции повторно не исполняются. Проверка готовности web обращается к /health/ready и проверяет доступ к таблице.

```bash
sudo docker compose ps
```

```bash
sudo docker compose images
```

Работают tropitsyn16-db-1 и tropitsyn16-web-1. У MariaDB внутри контейнера порт 3306, у web — 5316. Приложение соединяется с db:3306 по внутренней сети Docker.

**10. Проверить сайт.**

```bash
curl -f http://127.0.0.1:5316/health/ready
```

Ожидается OK. В браузере на своём компьютере открыть:

```text
http://IP_ТВОЕГО_СЕРВЕРА:5316
```

Подставить адрес целевого сервера из PuTTY. Выполнить минимум три расчёта: 12 + 4, 7 / 2, -3 × 4. Каждый успешный расчёт создаёт отдельную запись.

**11. Проверить записи прямо из PuTTY.**

```bash
sudo docker compose exec db mariadb -u calculator -p 16-WebCalculatorDb
```

Ввести значение MARIADB_PASSWORD из .env. При вводе символы пароля не показываются. Теперь открыто приглашение MariaDB: следующие строки — SQL, их вводить здесь, а не в Bash:

```sql
SHOW TABLES;
```

```sql
SELECT * FROM DataInputVariants;
```

```sql
SELECT * FROM __EFMigrationsHistory;
```

В первой таблице видны операнды, действие и результат; в служебной — m_init. Выйти:

```sql
exit
```

**12. Настроить PuTTY для HeidiSQL.**

На своём компьютере открыть настройки сохранённой серверной сессии. Слева выбрать Connection → SSH → Tunnels и заполнить:

| Поле | Значение |
| --- | --- |
| Source port | 13316 |
| Destination | 127.0.0.1:53316 |
| Режим | Local |

Нажать Add. В списке появится L13316 127.0.0.1:53316. Вернуться в Session, сохранить настройки и подключиться. Если старый сеанс открыт, закрыть его и открыть с сохранённым туннелем. Контейнеры при закрытии PuTTY продолжают работать.

Если DB_PORT меняли, Destination должен содержать выбранный порт. Source port 13316 относится к своему компьютеру; если занят, выбрать другой и затем указать его в HeidiSQL. SSH-порт остаётся прежним, обычно 22. Во время работы HeidiSQL PuTTY оставлять открытым.

**13. Подключиться через HeidiSQL.**

Создать новый сеанс:

| Поле | Значение |
| --- | --- |
| Тип сети | MariaDB или MySQL (TCP/IP) |
| Хост | 127.0.0.1 |
| Порт | 13316 |
| Пользователь | calculator |
| Пароль | MARIADB_PASSWORD из .env целевого сервера |
| База | 16-WebCalculatorDb |

Подключиться, выбрать базу и таблицу DataInputVariants. Во вкладке запроса выполнить:

```sql
SELECT * FROM DataInputVariants;
```

После нового расчёта на сайте повторить запрос — появится строка. Для отчёта снять заполненные настройки до подключения; пароль оставить скрытым точками.

```text
Браузер → IP сервера:5316 → web:5316 → db:3306
HeidiSQL → свой компьютер:13316 → туннель SSH
        → сервер 127.0.0.1:53316 → db:3306
```

База опубликована только на loopback сервера. Открывать 53316 всему Интернету для этого подключения не требуется.

**14. Показать остановку и повторный запуск.**

```bash
sudo docker compose stop web
```

```bash
sudo docker compose ps -a
```

Сайт станет недоступен, база останется работать. Повторный запуск сайта:

```bash
sudo docker compose up -d --wait web
```

Остановить оба контейнера:

```bash
sudo docker compose stop
```

Запустить оба снова:

```bash
sudo docker compose up -d --wait
```

Записи сохраняются в томе. docker compose down удаляет контейнеры и сеть, сохраняя том. Не добавлять -v, если нужно сохранить расчёты.

**15. Обновлять после следующих push.**

Из той же серверной папки:

```bash
git pull --ff-only
```

```bash
sudo docker compose build web
```

```bash
sudo docker compose up -d --wait web
```

```bash
curl -f http://127.0.0.1:5316/health/ready
```

Том остаётся прежним, новые миграции применяются при запуске приложения.

**16. Скриншоты для отчёта.**

По второй работе нужны снимки Visual Studio с appsettings.json и appsettings.Development.json; GitHub с compose.yaml; получения проекта на сервере; списка образов; HeidiSQL с заполненной таблицей; работающего сайта на 5316. Дополнительно снять папку Migrations, m_init в __EFMigrationsHistory, два контейнера и остановку/повторный запуск сайта.

Параметры должны быть уже введены перед снимком действия. Убирать указатель мыши из области снимка. Пароли оставлять скрытыми. Скриншоты сервера и GitHub делать после реального выполнения.

**17. Ошибки и диагностика.**

```bash
sudo docker compose logs --tail=80 web
```

```bash
sudo docker compose logs --tail=80 db
```

```bash
sudo docker compose ps -a
```

| Сообщение | Что проверить |
| --- | --- |
| no configuration file provided | Текущая папка содержит compose.yaml |
| MARIADB_PASSWORD is required | .env создан рядом с compose.yaml |
| Permission denied для .env | Выполнены chown и chmod из шага 5 |
| port is already allocated | Кто занимает 5316 или DB_PORT; остановить только свой старый контейнер |
| Access denied / Unknown database | Пароли соответствуют уже созданному тому, выполнен prepare-database.sh |
| DataInputVariants doesn't exist | В web logs проверить выполнение миграции |
| Серверный curl даёт OK, сайт извне недоступен | Адрес сервера и входящий TCP 5316 в сетевых правилах сервера/хостинга |
| HeidiSQL не подключается | PuTTY открыт с туннелем, Destination совпадает с DB_PORT, HeidiSQL использует 13316 |

На новом сервере создаётся новая база. История с другой машины или из другого тома переносится отдельным SQL-экспортом/импортом: GitHub передаёт исходники, а не данные Docker-томов.

Источники: [Docker Ubuntu](https://docs.docker.com/engine/install/ubuntu/), [Docker Debian](https://docs.docker.com/engine/install/debian/), [миграции EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying), [Pomelo 8.0.3](https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql/8.0.3).
