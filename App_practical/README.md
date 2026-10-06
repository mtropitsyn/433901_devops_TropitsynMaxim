# Калькулятор ASP.NET Core MVC в Docker

Практическая работа по п. 1.7.1 пособия [«Основы методологии Development Operation»](https://elar.urfu.ru/handle/10995/145068), с. 31–35.

**Студент:** Тропицын Максим Владимирович, НМТ-433901. **Преподаватель:** Лавров В. В. **Номер в журнале:** 16. **Порт:** 5016.

Приложение выполняет сложение, вычитание, умножение и деление. Вычисления выполняются на сервере с типом `decimal`. Поддерживаются отрицательные числа, точка и запятая в дробной части. Некорректный ввод, деление на ноль и переполнение показывают сообщение на странице. POST-форма защищена от CSRF. ФИО и группа отображаются в футере.

## 1. Открытие и локальный запуск

Требуется .NET SDK 8 или более новый SDK с поддержкой `net8.0`. В Visual Studio откройте `CalculatorDocker.sln` и выберите `CalculatorWeb` стартовым проектом. Альтернативный запуск из PowerShell в корне репозитория:

```powershell
dotnet build CalculatorDocker.sln -c Release
dotnet run --project CalculatorWeb
```

Откройте [http://localhost:5016](http://localhost:5016). Остановка локального процесса: `Ctrl+C` в его терминале.

Проверки вычислений и HTTP-формы:

```powershell
dotnet run --project CalculatorWeb.Checks -c Release
./scripts/Test-Web.ps1 -BaseUrl http://localhost:5016
```

Первый сценарий проверяет 23 случая без сторонних тестовых пакетов. Второй проверяет 13 HTTP-сценариев, включая реальные POST-запросы с токеном формы, ошибки ввода и отказ при отсутствии CSRF-токена. Для второй команды приложение должно быть запущено.

## 2. Установка Docker

На Windows установите [Docker Desktop по официальной инструкции](https://docs.docker.com/desktop/setup/install/windows-install/), настройте WSL 2 или другой поддерживаемый backend, запустите Docker Desktop и выберите Linux containers. Проверьте обе части Docker:

```powershell
docker --version
docker version
```

`docker version` должен показывать Client и Server. На рабочем компьютере уже установлены WSL 3.0.1 и Docker Desktop 4.94.0; используется Linux Engine 29.8.2.

6 октября 2026 года образ `16-calculator:latest` успешно собран, контейнер `calculator-16` запущен и прошёл все 13 HTTP-проверок. Проверены остановка, недоступность приложения после остановки и восстановление после `docker start`. Итоговое состояние контейнера — `exited` с кодом 0. Для открытия готового калькулятора запустите Docker Desktop, выполните `docker start calculator-16` и откройте [http://localhost:5016](http://localhost:5016).

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

После публикации в разделе **Actions** запустится workflow `Build and verify calculator container`. Он собирает образ из Dockerfile, проверяет веб-приложение, остановку и повторный запуск контейнера. Workflow не публикует образ в реестр. Его успешный результат необходимо проверить на GitHub; до запуска он не подтверждает работоспособность контейнера.

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

Точка в конце команды означает, что контекст сборки находится в текущей папке с Dockerfile. `.dockerignore` исключает локальные `bin`, `obj`, историю Git и отчёт из контекста.

## 5. Запуск контейнера и публикация порта

Остановите локальный запуск через `dotnet run`, чтобы освободить порт 5016. Затем:

```powershell
docker run -d -p 5016:5016 --name calculator-16 16-calculator:latest
docker ps
docker logs calculator-16
```

Откройте [http://localhost:5016](http://localhost:5016). Для удалённого сервера используйте его адрес и порт 5016. `-d` запускает контейнер в фоне, `-p` связывает порт хоста и контейнера, `--name` задаёт имя.

Под «публикацией» в этом задании понимается доступ к приложению через опубликованный порт и запуск `docker start`. GitHub хранит исходный код; команда `git push` не запускает веб-приложение.

## 6. Остановка и повторный запуск

```powershell
docker stop calculator-16
docker ps -a
docker inspect --format '{{.State.Status}}' calculator-16
```

Состояние должно быть `exited`. Обновите страницу браузера: приложение должно стать недоступным. Повторный запуск того же контейнера:

```powershell
docker start calculator-16
docker ps
```

После запуска страница снова должна открываться. Для завершения демонстрации:

```powershell
docker stop calculator-16
```

Все операции можно выполнить одним сценарием с HTTP-проверками:

```powershell
./scripts/Demo-Docker.ps1 -StudentNumber 16
```

Сценарий оставляет созданный контейнер остановленным и сохраняет существующие контейнеры. Если имя `calculator-16` занято, используйте ручные команды с другим именем или предварительно освободите имя самостоятельно.

## 7. Отчёт и необходимые скриншоты

Редактируемый отчёт: `docs/report.md`. PDF: `output/pdf/Docker_Report_Tropitsyn.pdf`. PDF создаётся командой `python scripts/build_report.py` при наличии зависимостей Python `reportlab` и `Pillow`. Персональные данные, реальные результаты Docker-проверки, скриншоты калькулятора в контейнере и листинги контроллера, `appsettings.json`, `appsettings.Development.json`, `Program.cs` включены в отчёт. Форма отчёта из задания не была предоставлена, поэтому использована обычная структура практической работы.

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
- `CalculatorWeb/Services/` — арифметика и разбор чисел.
- `CalculatorWeb/Views/` — Razor-представления.
- `CalculatorWeb/wwwroot/` — стили без внешних CDN.
- `CalculatorWeb.Checks/` — проверки вычислений и контроллера.
- `Dockerfile`, `.dockerignore` — сборка и окружение контейнера.
- `scripts/` — настройка индивидуального порта и демонстрационные сценарии.
- `.github/workflows/docker.yml` — автоматическая проверка в GitHub Actions.

Для другого номера в журнале используйте `./scripts/Configure-Student.ps1 -StudentNumber N` и сборку `docker build --build-arg APP_PORT=5000+N` с уже вычисленным числом вместо выражения. Для этой работы все настройки готовы для номера 16.
