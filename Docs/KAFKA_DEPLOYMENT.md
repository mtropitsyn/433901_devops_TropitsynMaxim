# Практическая работа 1.7.3. Калькулятор с Apache Kafka

Работа использует существующие контейнеры калькулятора и MariaDB и общий учебный Kafka-брокер. Порт сайта — `5316`, MariaDB доступна на сервере через `127.0.0.1:53316`, Kafka — через `93.88.178.186:9094`.

## 1. Создать свой топик

Открой [учебный Kafka UI](http://93.88.178.186:10025/). В учебном кластере выбери **Topics → Add a Topic**. Если топик `tropitsyn16-calculations` уже есть, используй его. Для нового топика задай:

| Поле | Значение |
| --- | --- |
| Topic Name | `tropitsyn16-calculations` |
| Partitions | `1` |
| Replication Factor | `1` |

Если создание закрыто правами, попроси преподавателя создать этот топик. Приложение ожидает существующий топик.

## 2. Обновить калькулятор на учебном сервере

В PuTTY выполняй по одной строке:

```bash
cd ~/433901_devops_TropitsynMaxim
git status --short
git fetch origin
git switch --track origin/kafka
```

Если локальная ветка `kafka` уже есть, вместо последней команды выполни:

```bash
git switch kafka
git pull --ff-only
```

Для отчёта можно дополнительно клонировать опубликованную ветку в отдельный каталог и снять вывод команды. Подставь свой URL GitHub:

```bash
git clone --branch kafka URL_РЕПОЗИТОРИЯ ~/kafka-report-checkout
```

Команды Docker выполняй из исходной папки `~/433901_devops_TropitsynMaxim`. В ней остаются прежние `.env` с паролями, `compose.override.yaml` с подсетью и имя проекта `tropitsyn16`. Проверь, что действующая БД здорова, и пересобери веб-контейнер:

```bash
sudo docker compose config --quiet
sudo docker compose ps db
sudo docker compose build web
sudo docker compose up -d --wait --no-deps web
sudo docker compose ps
sudo docker compose images
curl -f http://127.0.0.1:5316/health/ready
```

При старте веб-контейнер автоматически применит миграцию `KafkaRequestId`. В таблице `DataInputVariants` появится колонка `RequestId`. Предыдущие расчёты сохраняются в томе `tropitsyn16_mariadb-data`.

## 3. Проверить один расчёт в трёх местах

1. Открой [сайт калькулятора](http://93.88.178.186:5316/). Введи `12,5` и `4`, выбери умножение. Сделай скриншот заполненной формы до отправки, затем нажми «Вычислить». На странице результата должно появиться `50`. Сделай ещё 1–2 расчёта.
2. В Kafka UI открой **Topics → tropitsyn16-calculations → Messages**. Найди сообщение по операндам. В **Key** будет GUID, а в **Value** — JSON с идентификатором, операндами и операцией. Сам результат вычисляется после чтения сообщения.
3. Открой MariaDB в HeidiSQL через прежний туннель PuTTY `13316 → 127.0.0.1:53316`. Выполни запрос ниже. Сопоставь `RequestId` в строке с **Key** сообщения Kafka.

```sql
SELECT ID_DataInputVariant, RequestId, Operand_1, Operand_2,
       Type_operation, Result
FROM DataInputVariants
WHERE RequestId IS NOT NULL
ORDER BY ID_DataInputVariant DESC;
```

Если результат не появляется, проверь журнал своего веб-контейнера:

```bash
sudo docker compose logs --tail=80 web
```

`Unknown topic` указывает на отсутствие топика или неверное имя. При таймауте публикации проверь доступность общего брокера у преподавателя. Если используется другой адрес, добавь `KAFKA_BOOTSTRAP_SERVERS=ХОСТ:ПОРТ` в серверный `.env` и снова выполни `sudo docker compose up -d --wait --no-deps web`. Проверка `/health/ready` подтверждает связь с MariaDB. Сквозную работу Kafka подтверждает появление сообщения и соответствующей строки БД.

## Скриншоты и листинги для отчёта

По п. 1.7.3, с. 42–45, нужны два скриншота конфигурации в Visual Studio (`appsettings.json` и `appsettings.Development.json`), вывод клонирования и список образов в PuTTY, заполненная таблица в HeidiSQL, сообщение в Kafka UI и работающий сайт на порту `5316`. Дополнительно сохрани заполненную форму до отправки. Перед снимком уведи указатель из рабочей области и проверь, что на экране нет паролей.

В приложение к отчёту добавь листинги `CalculatorController.cs`, `KafkaConsumerService.cs`, `appsettings.json`, `appsettings.Development.json` и `Program.cs`. Листинг `KafkaProducerService.cs` показывает отправку сообщения и пригодится при защите.
