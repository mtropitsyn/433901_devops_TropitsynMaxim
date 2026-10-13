# Калькулятор ASP.NET Core MVC в Docker

Практическая работа по п. 1.7.1 пособия [«Основы методологии Development Operation»](https://elar.urfu.ru/handle/10995/145068), с. 31–35.

**Студент:** Тропицын Максим Владимирович, НМТ-433901. **Преподаватель:** Лавров В. В. **Номер в журнале:** 16. **Порт:** 5016.

Приложение выполняет сложение, вычитание, умножение и деление. Каждый успешный расчёт записывается в MariaDB: первый операнд, второй операнд, операция и результат. Вычисления выполняются на сервере с типом `decimal`. Поддерживаются отрицательные числа, точка и запятая в дробной части. Некорректный ввод, деление на ноль и переполнение показывают сообщение на странице и не создают запись. POST-форма защищена от CSRF. ФИО и группа отображаются в футере.

## Быстрый запуск с MariaDB

Запустите Docker Desktop и выполните из корня проекта:

```powershell
./scripts/Initialize-Database.ps1
docker compose up -d --build --wait
```

Откройте [http://localhost:5016](http://localhost:5016). Посмотреть сохранённые расчёты:

```powershell
./scripts/Show-Calculations.ps1
```

Инициализатор создаёт `.env` со случайными паролями; существующий файл сохраняется. `.env` исключён из Git, архива проекта и контекста Docker-сборки. При переносе проекта на другой компьютер создайте там собственный `.env`. Также можно скопировать `.env.example` в `.env` и заменить оба пароля.

## 1. Открытие и локальный запуск

Для запуска приложения вне контейнера требуется .NET SDK 8 или более новый SDK с поддержкой `net8.0`; MariaDB запускается в Docker. В Visual Studio откройте `CalculatorDocker.sln` и выберите `CalculatorWeb` стартовым проектом. Для удобства готов сценарий, который запускает MariaDB, передаёт пароль из `.env` приложению и запускает его локально:

```powershell
dotnet build CalculatorDocker.sln -c Release
./scripts/Start-Local.ps1
```

Откройте [http://localhost:5016](http://localhost:5016). Остановка локального процесса: `Ctrl+C` в его терминале. Перед локальным запуском остановите контейнер приложения командой `docker compose stop web`, чтобы освободить порт. Для запуска из Visual Studio задайте переменную окружения `Database__Password` значением `MARIADB_PASSWORD` из `.env`; остальные локальные настройки подключения уже заданы в `appsettings.json`: `localhost:3307`, база и пользователь `calculator`.

Проверки вычислений и HTTP-формы:

```powershell
dotnet run --project CalculatorWeb.Checks -c Release
./scripts/Test-Web.ps1 -BaseUrl http://localhost:5016
./scripts/Test-Database.ps1 -VerifyDatabaseRestart
```

Первый сценарий проверяет 32 случая, включая вызов сохранения и ошибку базы. Второй проверяет 13 HTTP-сценариев. `Test-Database.ps1` дополнительно сверяет реальные строки в MariaDB, граничные значения `decimal` и сохранность истории после пересоздания контейнера базы. Для HTTP-проверок приложение и MariaDB должны быть запущены. Проверки добавляют тестовые расчёты в историю и сохраняют существующие записи.

## 2. Установка Docker

На Windows установите [Docker Desktop по официальной инструкции](https://docs.docker.com/desktop/setup/install/windows-install/), настройте WSL 2 или другой поддерживаемый backend, запустите Docker Desktop и выберите Linux containers. Проверьте обе части Docker:

```powershell
docker --version
docker version
```

`docker version` должен показывать Client и Server. На рабочем компьютере уже установлены WSL 3.0.1 и Docker Desktop 4.94.0; используется Linux Engine 29.8.2.

Обновлённая версия запускается вместе с MariaDB через `docker compose`. Контейнеры называются `calculator-lab-web-1` и `calculator-lab-db-1`. Старые журналы от 6 октября относятся к версии до добавления базы; новые проверки сохранены в `docs/evidence/mariadb-*.txt`.

## 3. Размещение на GitHub

Создайте пустой репозиторий `docker-calculator` в своём аккаунте GitHub без README и .gitignore. Из корня этой папки выполните:

```powershell
git add .
git commit -m "Add ASP.NET Core MVC calculator and Dockerfile"
git branch -M main
git remote add origin https://github.com/YOUR_GITHUB_LOGIN/docker-calculator.git
git push -u origin main
```

Замените `YOUR_GITHUB_LOGIN` своим логином и при необходимости измените имя репозитория. Git уже инициализирован в этой рабочей папке. Для другой копии проекта сначала выполните `git init`. Отчёты и временные файлы из `output/` и `tmp/` исключены из Git. `docs/screenshots/` и `docs/evidence/` содержат подтверждения выполненных проверок.

После публикации в разделе **Actions** запустится workflow `Build and verify calculator container`. Он собирает образ из Dockerfile, запускает MariaDB и приложение, проверяет HTTP-форму, сохранение расчётов и перезапуск базы. Workflow не публикует образ в реестр. Его успешный результат необходимо проверить на GitHub; удалённый workflow здесь не запускался.

Для этапа клонирования на сервере или другом компьютере:

```powershell
git clone https://github.com/YOUR_GITHUB_LOGIN/docker-calculator.git
cd docker-calculator
```

## 4. Сборка Docker-образа

Dockerfile использует два этапа: SDK `8.0` компилирует и публикует проект, а runtime `aspnet:8.0` запускает готовую DLL. В окончательном образе нет SDK. Порт 5016 задан в настройках Kestrel и окружении контейнера. Приложение работает от непривилегированного пользователя `$APP_UID`.

```powershell
docker build -t 16-calculator:latest .
docker images -a
```

Точка в конце команды означает, что контекст сборки находится в текущей папке с Dockerfile. `.dockerignore` исключает локальные `bin`, `obj`, историю Git, `.env` и отчёт из контекста. Для совместной сборки и запуска с базой используйте `docker compose up -d --build --wait`.

## 5. Запуск контейнера и публикация порта

Остановите локальный запуск через `dotnet run`, чтобы освободить порт 5016. Затем:

```powershell
./scripts/Initialize-Database.ps1
docker compose up -d --build --wait
docker compose ps
docker compose logs web
```

Откройте [http://localhost:5016](http://localhost:5016). Для удалённого сервера используйте его адрес и порт 5016. В `compose.yaml` задан проброс порта приложения `5016:5016`. Приложение подключается к `db:3306` во внутренней сети Compose. Для локальной разработки порт базы доступен только на `127.0.0.1:3307`.

Под «публикацией» в этом задании понимается доступ к приложению через опубликованный порт. GitHub хранит исходный код; команда `git push` не запускает веб-приложение.

## 6. Остановка и повторный запуск

```powershell
docker compose stop web
docker compose ps -a
```

Состояние должно быть `exited`. Обновите страницу браузера: приложение должно стать недоступным. Повторный запуск того же контейнера:

```powershell
docker compose up -d --wait
docker compose ps
```

После запуска страница снова должна открываться. Для завершения демонстрации:

```powershell
docker compose stop
```

Все операции можно выполнить одним сценарием с HTTP-проверками:

```powershell
./scripts/Demo-Docker.ps1 -StudentNumber 16
```

Сценарий проверяет MariaDB и форму, останавливает и повторно запускает приложение, затем оставляет оба контейнера остановленными. Данные остаются в томе `calculator-lab_mariadb-data`. Обычный `docker compose down` тоже сохраняет том; удаление тома удаляет историю.

## 7. Сохранение расчётов в MariaDB

Подключение реализовано через [MySqlConnector 2.6.2](https://www.nuget.org/packages/MySqlConnector/2.6.2). Таблица `calculator.calculations` создаётся из `database/init.sql` при первом запуске MariaDB с пустым томом.

| Поле | Тип | Содержимое |
| --- | --- | --- |
| `id` | `BIGINT UNSIGNED` | Номер записи |
| `first_operand` | `DECIMAL(57,28)` | Первое число |
| `second_operand` | `DECIMAL(57,28)` | Второе число |
| `operation` | `ENUM` | `add`, `subtract`, `multiply`, `divide` |
| `result` | `DECIMAL(57,28)` | Вычисленный результат |
| `created_at` | `DATETIME(6)` | Время записи в UTC |

Тип `DECIMAL(57,28)` сохраняет весь диапазон .NET `decimal`: до 29 цифр целой части и 28 дробной. Это укладывается в [ограничения DECIMAL MariaDB](https://mariadb.com/docs/server/reference/data-types/numeric-data-types/decimal). Например, `12,5 × 4` записывается как `12.5`, `4`, `multiply`, `50`.

Контроллер сначала проверяет ввод, вычисляет результат и ожидает параметризованный `INSERT`, затем показывает результат и сообщение «Расчёт сохранён». Пароль не хранится в исходниках. Если MariaDB недоступна, форма показывает ошибку сохранения; некорректный ввод, деление на ноль и переполнение не записываются. `/health` проверяет приложение, `/health/ready` дополнительно проверяет доступность таблицы базы.

Просмотр последних записей без ручного ввода пароля:

```powershell
./scripts/Show-Calculations.ps1 -Limit 20
```

Для произвольного SQL-запроса откройте клиент и введите пароль пользователя `calculator` из `.env`:

```powershell
docker compose exec db mariadb -u calculator -p calculator
```

```sql
SELECT first_operand, second_operand, operation, result
FROM calculations ORDER BY id DESC LIMIT 20;
```

## 8. Отчёт и необходимые скриншоты

Редактируемый отчёт: `docs/report.md`. PDF: `output/pdf/Docker_Report_Tropitsyn.pdf`. PDF создаётся командой `python scripts/build_report.py` при наличии зависимостей Python `reportlab` и `Pillow`. В отчёт включены персональные данные, результаты Docker-проверки, раздел о MariaDB со схемой таблицы и результатами проверки сохранения, а также текущие листинги контроллера, `appsettings.json`, `appsettings.Development.json`, `Program.cs`. Скриншоты интерфейса относятся к первичной проверке 6 октября; результаты версии с базой получены 13 октября. Форма отчёта из задания не была предоставлена, поэтому использована обычная структура практической работы.

По методичке нужны скриншоты:

1. Visual Studio с файлом `CalculatorWeb/appsettings.json` и портом 5016.
2. Репозиторий GitHub с опубликованными файлами.
3. Результат `git clone` на сервере или другом компьютере.
4. Успешный `docker build` и список `docker images -a`.
5. Запущенный контейнер в `docker ps`.
6. Калькулятор в браузере на порту 5016, запущенный именно из контейнера.

Для демонстрации завершения работы добавьте `docker stop`, состояние `exited` и недоступность приложения в браузере. В папке `docs/screenshots/` файлы `docker-calculator-*.jpg` сняты при фактическом запуске контейнера; файлы без префикса `docker-` сняты при раннем локальном запуске.

Журналы реальных команд находятся в `docs/evidence/`: `docker-build.txt`, `docker-images.txt`, `docker-running.txt`, `docker-http-checks.txt`, `docker-lifecycle.txt`, `docker-final-state.txt`. Сводка проверки — `docker-verification.json`. Чтобы завершить отчёт по перечню методички, добавьте ссылку и скриншоты GitHub, клонирования, Visual Studio и терминала с результатами Docker-команд. Снимок страницы ошибки после остановки здесь не сохранён: встроенный браузер блокирует захват своей служебной страницы, недоступность подтверждена реальным HTTP-запросом и состоянием Docker.

## Структура проекта

- `CalculatorWeb/Controllers/` — обработка GET и POST.
- `CalculatorWeb/Models/` — поля формы и правила обязательного ввода.
- `CalculatorWeb/Data/` — сохранение расчётов в MariaDB.
- `database/init.sql`, `compose.yaml` — таблица и совместный запуск базы с приложением.
- `CalculatorWeb/Services/` — арифметика и разбор чисел.
- `CalculatorWeb/Views/` — Razor-представления.
- `CalculatorWeb/wwwroot/` — стили без внешних CDN.
- `CalculatorWeb.Checks/` — проверки вычислений и контроллера.
- `Dockerfile`, `.dockerignore` — сборка и окружение контейнера.
- `scripts/` — настройка индивидуального порта и демонстрационные сценарии.
- `.github/workflows/docker.yml` — автоматическая проверка в GitHub Actions.

Для другого номера в журнале используйте `./scripts/Configure-Student.ps1 -StudentNumber N`, укажите `STUDENT_NUMBER=N` и уже вычисленный `APP_PORT=5000+N` в `.env`. Для этой работы все настройки готовы для номера 16.
