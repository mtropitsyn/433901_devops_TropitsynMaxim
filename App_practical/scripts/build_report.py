"""Build the editable Markdown report and its PDF from project sources.

Dependencies: reportlab, Pillow. Run from any directory: python scripts/build_report.py
"""
from pathlib import Path
from html import escape
import os
import textwrap
import json

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER, TA_JUSTIFY, TA_LEFT
from reportlab.lib.styles import ParagraphStyle, getSampleStyleSheet
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    SimpleDocTemplate, Paragraph, Spacer, PageBreak, Image,
    Table, TableStyle, Preformatted, KeepTogether,
)

ROOT = Path(__file__).resolve().parents[1]
verification_path = ROOT / 'docs' / 'evidence' / 'docker-verification.json'
docker_evidence = json.loads(verification_path.read_text(encoding='utf-8-sig')) if verification_path.exists() else {}
docker_verified = docker_evidence.get('Verified') is True
OUT = ROOT / 'output' / 'pdf'
OUT.mkdir(parents=True, exist_ok=True)
windows_fonts = Path(os.environ.get('WINDIR', 'C:/Windows')) / 'Fonts'
font_files = {
    'Body': windows_fonts / 'times.ttf',
    'BodyBold': windows_fonts / 'timesbd.ttf',
    'Mono': windows_fonts / 'consola.ttf',
}
if not all(p.exists() for p in font_files.values()):
    font_files = {
        'Body': Path('/usr/share/fonts/truetype/dejavu/DejaVuSerif.ttf'),
        'BodyBold': Path('/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf'),
        'Mono': Path('/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf'),
    }
for name, path in font_files.items():
    pdfmetrics.registerFont(TTFont(name, str(path)))
pdfmetrics.registerFontFamily('Body', normal='Body', bold='BodyBold')

styles = getSampleStyleSheet()
styles.add(ParagraphStyle('ReportBody', fontName='Body', fontSize=12, leading=17,
                          alignment=TA_JUSTIFY, spaceAfter=9))
styles.add(ParagraphStyle('ReportHeading', fontName='BodyBold', fontSize=15,
                          leading=20, textColor=colors.black, spaceBefore=12,
                          spaceAfter=12, keepWithNext=True))
styles.add(ParagraphStyle('ReportTitle', fontName='BodyBold', fontSize=20,
                          leading=27, alignment=TA_CENTER, spaceAfter=20))
styles.add(ParagraphStyle('ReportCenter', fontName='Body', fontSize=13,
                          leading=19, alignment=TA_CENTER, spaceAfter=10))
styles.add(ParagraphStyle('ReportCaption', fontName='Body', fontSize=10,
                          leading=14, alignment=TA_CENTER, spaceBefore=7,
                          spaceAfter=12))
styles.add(ParagraphStyle('ReportCell', fontName='Body', fontSize=10,
                          leading=13, spaceAfter=0))
styles.add(ParagraphStyle('ReportCode', fontName='Mono', fontSize=8,
                          leading=11, spaceAfter=10))
styles.add(ParagraphStyle('ReportSource', parent=styles['ReportBody'], alignment=TA_LEFT))

story = []
md = []

def para(text, style='ReportBody'):
    story.append(Paragraph(escape(text).replace('\n', '<br/>'), styles[style]))
    md.append(text + '\n')

def heading(text):
    story.append(Paragraph(escape(text), styles['ReportHeading']))
    md.append('## ' + text + '\n')

def code(text):
    lines = []
    for line in text.rstrip().splitlines():
        lines.extend(textwrap.wrap(line.expandtabs(4), width=91,
                                  replace_whitespace=False,
                                  drop_whitespace=False,
                                  break_long_words=False,
                                  break_on_hyphens=False,
                                  subsequent_indent='    ') or [''])
    story.append(Preformatted('\n'.join(lines), styles['ReportCode']))
    md.append('```\n' + text.rstrip() + '\n```\n')

def table(headers, rows, widths):
    cells = [[Paragraph(escape(str(t)), styles['ReportCell']) for t in row]
             for row in [headers, *rows]]
    t = Table(cells, colWidths=[v * mm for v in widths], repeatRows=1,
              hAlign='LEFT')
    t.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), colors.HexColor('#EDEDED')),
        ('GRID', (0, 0), (-1, -1), 0.5, colors.HexColor('#D9D9D9')),
        ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
        ('LEFTPADDING', (0, 0), (-1, -1), 7),
        ('RIGHTPADDING', (0, 0), (-1, -1), 7),
        ('TOPPADDING', (0, 0), (-1, -1), 7),
        ('BOTTOMPADDING', (0, 0), (-1, -1), 7),
    ]))
    story.extend([t, Spacer(1, 5 * mm)])
    md.append('| ' + ' | '.join(headers) + ' |\n| ' + ' | '.join(['---'] * len(headers)) + ' |\n')
    md.extend('| ' + ' | '.join(map(str, row)) + ' |\n' for row in rows)

def screenshot(name, caption, max_height=192 * mm):
    path = ROOT / 'docs' / 'screenshots' / name
    if path.exists():
        from PIL import Image as PILImage
        with PILImage.open(path) as im:
            width, height = im.size
        scale = min(158 * mm / width, max_height / height)
        image = Image(str(path), width=width * scale, height=height * scale)
        image.hAlign = 'CENTER'
        story.append(KeepTogether([image, Paragraph(escape(caption), styles['ReportCaption'])]))
        md.append(f'![{caption}](screenshots/{name})\n')

para('Уральский федеральный университет\nимени первого Президента России Б. Н. Ельцина', 'ReportCenter')
story.append(Spacer(1, 37 * mm))
para('Отчет по практической работе\nКонтейнеризация приложения\nКалькулятор с использованием Docker', 'ReportTitle')
para('ASP.NET Core MVC\nЗадание 1.7.1', 'ReportCenter')
story.append(Spacer(1, 28 * mm))
para('Выполнил Тропицын Максим Владимирович\nГруппа НМТ-433901\nНомер в журнале 16\nПреподаватель Лавров В. В.', 'ReportCenter')
story.append(Spacer(1, 23 * mm))
para('Екатеринбург 2026\nДата подготовки 6 октября 2026 года', 'ReportCenter')
story.append(PageBreak())

heading('1 Цель работы и состояние выполнения')
para('Цель работы - разработать веб-калькулятор на ASP.NET Core MVC, подготовить Dockerfile, разместить исходный код на GitHub и проверить сборку, запуск, остановку и повторный запуск контейнера.')
if docker_verified:
    para('Приложение прошло локальную сборку и проверку вычислений и HTTP-формы. На рабочем компьютере установлены WSL 3.0.1 и Docker Desktop 4.94.0. Образ 16-calculator:latest собран, контейнер calculator-16 запущен на порту 5016. Проверены HTTP-форма, остановка, недоступность после остановки и повторный запуск. Публикация исходного кода на GitHub выполняется студентом самостоятельно.')
else:
    para('Разработанное приложение прошло локальную сборку и проверку вычислений и HTTP-формы. Dockerfile и сценарии контейнерного запуска подготовлены. Фактическая сборка и запуск контейнера пока не выполнены: Docker Desktop и WSL отсутствуют на рабочем компьютере. Публикация исходного кода на GitHub выполняется студентом самостоятельно.')
table(['Этап', 'Результат на дату подготовки'], [
    ['Приложение MVC', 'Разработано и проверено локально'],
    ['Dockerfile', 'Создан; образ успешно собран' if docker_verified else 'Создан; контейнерная сборка ожидает выполнения'],
    ['GitHub', 'Подготовлены команды; публикация ожидает выполнения'],
    ['Docker build и docker run', 'Выполнены и проверены' if docker_verified else 'Ожидают доступной среды Docker'],
    ['Docker stop и docker start', 'Выполнены; недоступность и восстановление проверены' if docker_verified else 'Подготовлен сценарий демонстрации'],
    ['Отчет', 'Требуются скриншоты GitHub, клонирования и окна Visual Studio' if docker_verified else 'Описание и локальные скриншоты готовы; требуются свидетельства GitHub и Docker'],
], [49, 109])

heading('2 Разработка приложения')
para('Создано решение CalculatorDocker.sln с MVC-проектом CalculatorWeb и проектом проверок CalculatorWeb.Checks. Целевая платформа - .NET 8, как в примере учебного пособия. Разработка выполнена в исходных файлах; решение можно открыть в Visual Studio.')
para('Модель CalculatorViewModel хранит два операнда и выбранную операцию. Контроллер CalculatorController обрабатывает GET-запрос страницы и POST-запрос формы. CalculatorService разбирает ввод и выполняет арифметические операции. Razor-представление показывает результат или сообщение об ошибке.')
para('Числа обрабатываются как decimal. Дробный разделитель может быть точкой или запятой. Проверяются обязательность ввода, корректность числа, допустимость операции, деление на ноль и переполнение. На странице отображаются ФИО и группа студента. POST-форма использует защитный токен ASP.NET Core.')
para('Номер студента в журнале - 16. Индивидуальный порт рассчитан по правилу 5000 + 16 = 5016. В appsettings.json адрес Kestrel задан как http://0.0.0.0:5016. В браузере используется http://localhost:5016.')
code('dotnet build CalculatorDocker.sln -c Release\ndotnet run --project CalculatorWeb\ndotnet run --project CalculatorWeb.Checks -c Release\n./scripts/Test-Web.ps1 -BaseUrl http://localhost:5016')
para('Сборка завершилась без ошибок и предупреждений. Пройдено 23 проверки арифметики и контроллера, а также 13 проверок HTTP-интерфейса. Команда dotnet publish с параметром UseAppHost=false успешно создала опубликованную DLL и необходимые файлы. Текстовые журналы сохранены в docs/evidence.')
table(['Проверка', 'Ввод', 'Результат'], [
    ['Сложение', '12 + 4', '16'], ['Вычитание', '12 - 4', '8'],
    ['Умножение', '-3 × 4', '-12'], ['Деление', '7 ÷ 2', '3,5'],
    ['Дробные числа', '0,1 + 0.2', '0,3'],
    ['Деление на ноль', '10 ÷ 0', 'Сообщение об ошибке'],
    ['Некорректный ввод', 'abc и 2', 'Сообщение об ошибке'],
    ['POST без токена', 'Запрос без CSRF-токена', 'HTTP 400'],
], [47, 57, 54])
story.append(PageBreak())

heading('3 Проверка веб интерфейса')
para(('Приложение в контейнере calculator-16' if docker_verified else 'Локальное приложение') + ' открыто в браузере на порту 5016. Введены числа 12,5 и 4, выбрано умножение и нажата кнопка вычисления. Получен результат 50.')
screenshot('docker-calculator-result.jpg' if docker_verified else 'calculator-result.jpg', 'Рисунок 1 - Калькулятор на localhost:5016 и результат умножения' + (' в Docker' if docker_verified else ' при локальном запуске'))
story.append(PageBreak())
heading('4 Проверка обработки ошибки')
para('При делении 10 на 0 приложение показывает сообщение «На ноль делить нельзя. Введите другое число». Ошибочный результат не выводится, введенные значения сохраняются для исправления.')
screenshot('docker-calculator-zero-error.jpg' if docker_verified else 'calculator-zero-error.jpg', 'Рисунок 2 - Сообщение при делении на ноль' + (' в контейнере' if docker_verified else ' в локальном приложении'))
story.append(PageBreak())

heading('5 Создание Dockerfile')
para('В корневой папке решения создан Dockerfile с двумя этапами. На этапе build используется SDK .NET 8, восстанавливается проект и выполняется публикация. На окончательном этапе используется ASP.NET Core Runtime 8. В образ копируются опубликованные файлы, задается окружение Production, порт 5016 и запуск DLL. Приложение запускается от непривилегированного пользователя APP_UID.')
para('Файл .dockerignore исключает bin, obj, служебные папки Git, отчеты и временные материалы из контекста сборки. Параметр APP_PORT позволяет изменить порт при сборке образа. Значение для этой работы по умолчанию - 5016.')
code((ROOT / 'Dockerfile').read_text(encoding='utf-8-sig'))

heading('6 Размещение исходного кода на GitHub')
para('Для публикации требуется создать пустой личный репозиторий на GitHub и выполнить команды ниже. В URL нужно заменить YOUR_GITHUB_LOGIN своим логином, а при другом имени репозитория - также имя docker-calculator. Публикация на дату подготовки отчета еще не выполнена.')
code('git add .\ngit commit -m "Add ASP.NET Core MVC calculator and Dockerfile"\ngit branch -M main\ngit remote add origin https://github.com/YOUR_GITHUB_LOGIN/docker-calculator.git\ngit push -u origin main')
para('После публикации необходимо указать фактическую ссылку на репозиторий и вставить скриншот страницы GitHub. Для демонстрации клонирования на сервере или другом компьютере используются следующие команды.')
code('git clone https://github.com/YOUR_GITHUB_LOGIN/docker-calculator.git\ncd docker-calculator')
story.append(PageBreak())

heading('7 Сборка и запуск Docker контейнера')
para('Для этого этапа необходимо установить и запустить Docker Desktop с поддержкой Linux containers либо использовать сервер с Docker Engine. Команда docker version должна показывать сведения о Client и Server. Перед контейнерным запуском следует остановить локальный процесс dotnet run, чтобы освободить порт 5016.')
code('docker version\ndocker build -t 16-calculator:latest .\ndocker images -a\ndocker run -d -p 5016:5016 --name calculator-16 16-calculator:latest\ndocker ps\ndocker logs calculator-16')
para('Имя образа начинается с номера студента в журнале. Параметр -d задает фоновый запуск, -p публикует порт 5016 хоста и связывает его с портом 5016 контейнера. После запуска нужно открыть http://localhost:5016 или адрес удаленного сервера с этим портом.')
if docker_verified:
    para('Образ успешно собран из Dockerfile. Контейнер запущен и обслуживает страницу калькулятора и endpoint /health на порту 5016. Все 13 HTTP-проверок пройдены внутри контейнера. Журналы сборки, версии Docker, списка образов и контейнеров сохранены в docs/evidence.')
    para('Идентификатор собранного образа:')
    code(docker_evidence.get('ImageId', ''))
    para('Пользователь контейнера ' + docker_evidence.get('User', '') + '. Окружение Production. Опубликованный порт 5016.')
else:
    para('Ожидаемый результат этапа - созданный образ в docker images, работающий контейнер в docker ps и доступный калькулятор в браузере. Эти результаты должны быть подтверждены реальным выполнением команд и скриншотами; на дату подготовки отчета контейнерный запуск не проверен.')

heading('8 Остановка и повторный запуск')
code("docker stop calculator-16\ndocker ps -a\ndocker inspect --format '{{.State.Status}}' calculator-16\ndocker start calculator-16\ndocker ps\ndocker stop calculator-16")
para('После docker stop ожидается состояние exited. При обновлении страницы приложение должно быть недоступно. После docker start тот же контейнер должен снова обслуживать запросы. Остановка сохраняет контейнер и его имя для повторного запуска.')
if docker_verified:
    para('При фактической проверке после остановки получено состояние exited, HTTP-запрос не установил соединение. После docker start endpoint /health снова вернул OK. По завершении проверки контейнер оставлен остановленным. Результаты зафиксированы в docker-lifecycle.txt и docker-verification.json.')
    table(['Проверка', 'Фактический результат'], [
        ['Контейнер', docker_evidence.get('ContainerId', '')[:12] + ' / calculator-16'],
        ['Публикация порта', '0.0.0.0:5016 -> 5016/tcp'],
        ['HTTP-проверки', '13 из 13 пройдены'],
        ['После docker stop', 'exited; запрос к /health не установил соединение'],
        ['После docker start', '/health вернул 200 OK'],
        ['Завершение демонстрации', 'Контейнер остановлен; код завершения 0'],
    ], [57, 101])
para('В scripts/Demo-Docker.ps1 подготовлен сценарий, который собирает образ, запускает контейнер, выполняет 13 HTTP-проверок, останавливает приложение, проверяет его недоступность, повторно запускает и снова останавливает контейнер. Существующие контейнеры не удаляются.')
code('./scripts/Demo-Docker.ps1 -StudentNumber 16')

heading('9 Материалы для завершения отчета')
para('Перед сдачей необходимо добавить фактическую ссылку на GitHub и скриншоты окна Visual Studio с appsettings.json, опубликованного репозитория и результата git clone. Для соответствия перечню методички также добавьте снимки терминала со сборкой образа и работающим контейнером по сохраненным журналам реальных команд.')
para('Форма отчета из вложения не была предоставлена; использована обычная структура практической работы с титульным листом, этапами, результатами и листингами.')
if docker_verified:
    para('Скриншоты в разделах 3 и 4 подтверждают работу приложения из Docker-контейнера. Текстовые свидетельства сборки, состояния контейнера, остановки и повторного запуска находятся в docs/evidence.')
else:
    para('Локальные скриншоты в разделах 3 и 4 подтверждают работу приложения до контейнеризации. После Docker-проверки их следует дополнить скриншотами приложения, запущенного из контейнера, и обновить состояние этапов в разделе 1.')

heading('10 Вывод')
if docker_verified:
    para('Разработан веб-калькулятор ASP.NET Core MVC и подготовлен Dockerfile. Образ собран, контейнер запущен на порту 5016; проверены вычисления, ошибки ввода, остановка и повторный запуск. Для завершения сдаваемого отчета остается опубликовать исходный код на GitHub, добавить ссылку, скриншоты репозитория, клонирования и требуемые снимки окон Visual Studio и терминала.')
else:
    para('Разработан и локально проверен веб-калькулятор ASP.NET Core MVC. Подготовлены Dockerfile, настройки порта 5016, команды GitHub и сценарии контейнерной демонстрации. Полное завершение практической работы требует фактической публикации репозитория, выполнения Docker build, Docker run, Docker stop и Docker start и добавления соответствующих свидетельств в отчет.')

heading('Источники')
para('Лавров В. В., Гурин И. А. Основы методологии Development Operation. Практикум. Екатеринбург, 2025. 140 с. Пункт 1.7.1, с. 31-35. https://elar.urfu.ru/handle/10995/145068', 'ReportSource')
para('Microsoft Learn. Run an ASP.NET Core app in Docker containers. https://learn.microsoft.com/aspnet/core/host-and-deploy/docker/building-net-docker-images', 'ReportSource')
para('Docker Docs. Install Docker Desktop on Windows. https://docs.docker.com/desktop/setup/install/windows-install/', 'ReportSource')
story.append(PageBreak())

for title, path in [
    ('Приложение А Контроллер', 'CalculatorWeb/Controllers/CalculatorController.cs'),
    ('Приложение Б Настройки приложения', 'CalculatorWeb/appsettings.json'),
    ('Приложение В Настройки среды разработки', 'CalculatorWeb/appsettings.Development.json'),
    ('Приложение Г Точка входа', 'CalculatorWeb/Program.cs'),
]:
    heading(title)
    para(path)
    code((ROOT / path).read_text(encoding='utf-8-sig'))

def footer(canvas, doc):
    if doc.page == 1:
        return
    canvas.saveState()
    canvas.setFont('Body', 10)
    canvas.drawCentredString(105 * mm, 12 * mm, str(doc.page))
    canvas.restoreState()

pdf = OUT / 'Docker_Report_Tropitsyn.pdf'
document = SimpleDocTemplate(str(pdf), pagesize=(210*mm, 297*mm),
                             leftMargin=27*mm, rightMargin=25*mm,
                             topMargin=22*mm, bottomMargin=22*mm,
                             title='Контейнеризация приложения Калькулятор',
                             author='Тропицын Максим Владимирович')
document.build(story, onFirstPage=footer, onLaterPages=footer)
(ROOT / 'docs' / 'report.md').write_text('\n'.join(md), encoding='utf-8')
print(pdf)
