"""Генератор PDF-руководства для Text to Audiobook C#.
Запуск: python build_guide_pdf.py
Результат: dist/РУКОВОДСТВО ПОЛЬЗОВАТЕЛЯ.pdf
"""

from reportlab.lib.pagesizes import A4
from reportlab.lib import colors
from reportlab.lib.units import mm
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.lib.utils import ImageReader
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle
import os, sys, textwrap

OUT = os.path.join(os.path.dirname(__file__), "dist", "РУКОВОДСТВО ПОЛЬЗОВАТЕЛЯ.pdf")
os.makedirs(os.path.dirname(OUT), exist_ok=True)

# ── Размеры страницы ────────────────────────────────────────────────
W, H = A4          # 595 x 842 pt
ML, MR = 28, 28    # margins left/right
MT, MB = 30, 25    # margins top/bottom

# ── Палитра (тёмная тема приложения) ───────────────────────────────
BG_DARK  = colors.HexColor("#1A1A2E")   # фон страницы
BG_CARD  = colors.HexColor("#16213E")   # карточки
BG_INPUT = colors.HexColor("#0F3460")   # поля ввода/чипы
ACCENT   = colors.HexColor("#6C63FF")   # фиолетовый
ACCENT2  = colors.HexColor("#5A52D5")
GREEN    = colors.HexColor("#2ECC71")
AMBER    = colors.HexColor("#F39C12")
CORAL    = colors.HexColor("#E74C3C")
CYAN     = colors.HexColor("#89CFF0")
TEXT     = colors.HexColor("#EAEAEA")
MUTED    = colors.HexColor("#A0A0B8")
WHITE    = colors.white


def new_page(c: canvas.Canvas, page_num=None):
    """Заливка фона и опциональный номер страницы."""
    c.setFillColor(BG_DARK)
    c.rect(0, 0, W, H, fill=1, stroke=0)
    if page_num is not None:
        c.setFillColor(MUTED)
        c.setFont("Helvetica", 8)
        c.drawRightString(W - ML, MB - 12, str(page_num))


def hbar(c, x, y, w, h, col, radius=4):
    """Закруглённый прямоугольник."""
    c.setFillColor(col)
    c.roundRect(x, y, w, h, radius, fill=1, stroke=0)


def card(c, x, y, w, h, fill=None, border=None):
    """Карточка с рамкой."""
    c.setFillColor(fill or BG_CARD)
    c.setStrokeColor(border or BG_INPUT)
    c.setLineWidth(1)
    c.roundRect(x, y, w, h, 6, fill=1, stroke=1)


def label(c, text, x, y, size=11.0, col=None, font="Helvetica", bold=False, align="left"):
    """Одна строка текста."""
    c.setFillColor(col or TEXT)
    fn = font + ("-Bold" if bold else "")
    c.setFont(fn, size)
    if align == "center":
        c.drawCentredString(x, y, text)
    elif align == "right":
        c.drawRightString(x, y, text)
    else:
        c.drawString(x, y, text)


def wrapped_text(c, text, x, y, max_width, size=10.0, col=None, line_h=14, font="Helvetica", bold=False):
    """Многострочный текст с переносом слов, возвращает y после последней строки."""
    c.setFillColor(col or TEXT)
    fn = font + ("-Bold" if bold else "")
    c.setFont(fn, size)
    # Оценочное число символов на строку (pt / ширина символа ~0.55)
    chars = max(10, int(max_width / (size * 0.55)))
    lines = []
    for raw_line in text.split("\n"):
        if raw_line.strip() == "":
            lines.append("")
        else:
            lines.extend(textwrap.wrap(raw_line, chars) or [""])
    for line in lines:
        c.drawString(x, y, line)
        y -= line_h
    return y


def kicker(c, text, y):
    """Маленький заголовок-надпись над основным (uppercase + accentcolor)."""
    c.setFillColor(ACCENT)
    c.setFont("Helvetica-Bold", 9)
    c.drawString(ML, y, text.upper())


def section_title(c, text, y, page_num=None):
    """Крупный заголовок слайда."""
    label(c, text, ML, y, size=20, col=TEXT, bold=True)
    c.setStrokeColor(ACCENT)
    c.setLineWidth(1.5)
    c.line(ML, y - 5, W - MR, y - 5)


def footer_line(c, text, page_num):
    c.setFillColor(MUTED)
    c.setFont("Helvetica", 8)
    c.drawString(ML, MB - 12, text)
    c.drawRightString(W - MR, MB - 12, f"{page_num:02d}")


def bullet(c, x, y, col=None):
    c.setFillColor(col or ACCENT)
    c.circle(x + 3, y + 3.5, 2.5, fill=1, stroke=0)


def blist(c, items, x, y, w, size=10, col=None, bullet_col=None, line_h=15):
    """Маркированный список, возвращает y."""
    for item in items:
        bullet(c, x, y, bullet_col)
        wrapped_text(c, item, x + 12, y, w - 12, size=size, col=col, line_h=line_h)
        # грубая оценка высоты
        chars = max(10, int((w - 12) / (size * 0.55)))
        n = max(1, len(item) // chars + (1 if len(item) % chars else 0))
        lines_count = sum(max(1, len(textwrap.wrap(p, chars) or [""]))
                          for p in item.split("\n")) if "\n" in item else max(1, n)
        y -= lines_count * line_h + 3
    return y


# ═══════════════════════════════════════════════════════════════════
c = canvas.Canvas(OUT, pagesize=A4)
c.setTitle("Text to Audiobook v14 (C#) — Руководство пользователя")
c.setAuthor("Andrey Buchin")

# ══════════════════════════════════════════════════════════════════
# СТРАНИЦА 1: ОБЛОЖКА
# ══════════════════════════════════════════════════════════════════
new_page(c)

# Боковая полоса
hbar(c, 0, 0, 12, H, ACCENT)
# Декоративный квадрат
hbar(c, W - 55, H - 55, 40, 40, BG_INPUT, radius=8)
hbar(c, W - 50, H - 50, 30, 30, ACCENT, radius=6)

label(c, "Text to Audiobook", ML + 14, H - 140, size=42, col=TEXT, bold=True)
label(c, "v14  ·  C#  ·  WPF  ·  .NET 9", ML + 14, H - 170, size=16, col=ACCENT, font="Helvetica")
label(c, "Конвертер текстовых книг в аудиокниги MP3", ML + 14, H - 192, size=12, col=MUTED)

# Три чипа режимов
chips = [
    (" Онлайн  Edge TTS",  GREEN),
    (" Piper TTS нейросеть", ACCENT),
    (" Windows SAPI оффлайн", AMBER),
]
cx = ML + 14
for text, col in chips:
    tw = 170
    hbar(c, cx, H - 245, tw, 26, BG_INPUT, radius=6)
    c.setStrokeColor(col); c.setLineWidth(1.5)
    c.roundRect(cx, H - 245, tw, 26, 6, fill=0, stroke=1)
    label(c, text, cx + 10, H - 235, size=11, col=col)
    cx += tw + 10

# Разделитель
c.setStrokeColor(BG_INPUT); c.setLineWidth(1)
c.line(ML + 14, H - 270, W - MR, H - 270)

# Короткое описание
desc = ("Программа превращает .txt файлы в аудиокниги по главам. "
        "Три режима синтеза речи: высококачественный Microsoft Edge TTS онлайн, "
        "нейросетевой Piper TTS без интернета и встроенные голоса Windows SAPI.")
wrapped_text(c, desc, ML + 14, H - 295, W - MR - ML - 14, size=12, col=TEXT, line_h=18)

# Блоки внизу обложки
boxes = [
    ("Запуск", "Двойной клик на .exe\nУстанавливать ничего не нужно", GREEN),
    ("Требования", "Windows 10 / 11 64-bit\n~170 МБ диска", ACCENT),
    ("Форматы", "Вход: .txt (любая кодировка)\nВыход: MP3 по главам", AMBER),
]
bx = ML + 14; by = 130
bw = (W - ML - MR - 14 - 20) / 3
for title_t, body_t, col in boxes:
    card(c, bx, by, bw - 5, 85, BG_CARD, col)
    hbar(c, bx, by + 85 - 26, bw - 5, 26, col, radius=0)
    label(c, title_t, bx + 8, by + 85 - 16, size=12, col=BG_DARK, bold=True)
    wrapped_text(c, body_t, bx + 8, by + 58, bw - 20, size=10, col=MUTED, line_h=15)
    bx += bw + 10

footer_line(c, "Text to Audiobook v14 (C#)  ·  Руководство пользователя", 1)
c.showPage()

# ══════════════════════════════════════════════════════════════════
# СТРАНИЦА 2: БЫСТРЫЙ СТАРТ
# ══════════════════════════════════════════════════════════════════
new_page(c)
kicker(c, "Быстрый старт", H - MT - 2)
section_title(c, "Три шага до первой аудиокниги", H - MT - 24)

steps = [
    (GREEN, "1",  "Выберите книгу",
     "Нажмите «Обзор» рядом с полем «Исходный файл» и выберите .txt файл. "
     "Программа автоматически определит язык и подберёт лучший голос. "
     "Папка для сохранения заполнится рядом с книгой: название_audio/"),
    (ACCENT, "2", "Проверьте настройки",
     "Режим «Авто» выберет лучший доступный способ озвучки. "
     "Скорость 0% — нормальный темп. Оставьте «Объединить в один файл» включённым. "
     "Кодировка «auto» работает в 99% случаев."),
    (AMBER, "3",  "Нажмите «Начать конвертацию»",
     "В нижней части экрана появится прогресс и лог. "
     "По завершении откроется окно с результатом и кнопкой «Открыть аудиофайл». "
     "Кнопка «Стоп» сохраняет прогресс — продолжить можно позже."),
]

sy = H - MT - 60
for col, num, step_title, step_body in steps:
    sh = 90
    card(c, ML, sy - sh, W - ML - MR, sh, BG_CARD, col)
    # Номер
    hbar(c, ML, sy - sh, 36, sh, col, radius=0)
    c.setFillColor(BG_DARK); c.setFont("Helvetica-Bold", 26)
    c.drawCentredString(ML + 18, sy - sh/2 - 9, num)
    # Заголовок шага
    label(c, step_title, ML + 46, sy - 20, size=14, col=col, bold=True)
    wrapped_text(c, step_body, ML + 46, sy - 38, W - ML - MR - 56, size=10.5, col=TEXT, line_h=15)
    sy -= sh + 12

# Блок "Продолжение"
sy -= 5
card(c, ML, sy - 60, W - ML - MR, 60, BG_INPUT, ACCENT)
hbar(c, ML, sy - 60, 10, 60, ACCENT, radius=0)
label(c, "  Прерывание не страшно — продолжение с места остановки", ML + 18, sy - 18, size=12, col=ACCENT, bold=True)
wrapped_text(c, "Если конвертация была остановлена, при следующем запуске вверху появится "
             "зелёная плашка «Продолжить: Название книги (12/40 — 30%)». "
             "Нажмите «Продолжить» — программа пропустит готовые главы и возобновит с места остановки.",
             ML + 18, sy - 36, W - ML - MR - 28, size=10.5, col=TEXT, line_h=15)

footer_line(c, "Text to Audiobook v14 (C#)  ·  Руководство пользователя", 2)
c.showPage()

# ══════════════════════════════════════════════════════════════════
# СТРАНИЦА 3: ТРИ РЕЖИМА TTS
# ══════════════════════════════════════════════════════════════════
new_page(c)
kicker(c, "Режимы синтеза речи", H - MT - 2)
section_title(c, "Три способа озвучки", H - MT - 24)

modes_data = [
    (GREEN, "Онлайн — Edge TTS", "Microsoft Neural, лучшее качество",
     ["Дмитрий, Светлана (рус.), Guy, Jenny (англ.)",
      "Естественный, живой голос — как у диктора",
      "Голоса всех языков мира",
      "Авто-повтор при недоступности: 5 попыток"],
     ["Требует подключения к интернету"],
     "★★★★★"),
    (ACCENT, "Piper TTS — нейросеть оффлайн", "ONNX модели, без интернета",
     ["Нейросетевое качество — почти как Edge TTS",
      "Работает без интернета после установки",
      "Отдельные модели .onnx для каждого языка",
      "Быстрее SAPI, звучит естественно"],
     ["Нужно скачать piper.exe + модели (разово)"],
     "★★★★☆"),
    (AMBER, "Оффлайн — Windows SAPI", "System.Speech, встроен в Windows",
     ["Всегда доступен — ничего не нужно устанавливать",
      "Работает полностью без интернета",
      "Голоса из языковых пакетов Windows"],
     ["Роботоподобный голос, качество ниже",
      "Нужны установленные языковые пакеты"],
     "★★★☆☆"),
]

my = H - MT - 60
for col, title_t, sub_t, pros, cons, stars in modes_data:
    mh = 115
    card(c, ML, my - mh, W - ML - MR, mh, BG_CARD, col)
    hbar(c, ML, my - mh, 10, mh, col, radius=0)
    label(c, title_t, ML + 18, my - 18, size=13, col=col, bold=True)
    label(c, sub_t,   ML + 18, my - 32, size=10, col=MUTED)
    # плюсы
    px = ML + 18; py = my - 50; pw = (W - ML - MR - 28) * 0.65
    for pro in pros:
        c.setFillColor(GREEN); c.circle(px + 3, py + 3, 2.5, fill=1, stroke=0)
        label(c, pro, px + 12, py, size=10, col=TEXT)
        py -= 14
    # минусы
    cx2 = ML + 18 + pw + 15; cy2 = my - 50
    for con in cons:
        c.setFillColor(CORAL); c.circle(cx2 + 3, cy2 + 3, 2.5, fill=1, stroke=0)
        label(c, con, cx2 + 12, cy2, size=10, col=TEXT)
        cy2 -= 14
    # звёзды
    label(c, stars, W - MR - 60, my - 18, size=13, col=col)
    my -= mh + 10

# Совет
sy = my - 5
card(c, ML, sy - 36, W - ML - MR, 36, BG_INPUT, ACCENT)
hbar(c, ML, sy - 36, 10, 36, ACCENT, radius=0)
label(c, "  Совет: оставьте режим «Авто» — программа сама выберет лучший доступный способ.",
      ML + 18, sy - 22, size=11, col=TEXT)

footer_line(c, "Text to Audiobook v14 (C#)  ·  Руководство пользователя", 3)
c.showPage()

# ══════════════════════════════════════════════════════════════════
# СТРАНИЦА 4: НАСТРОЙКИ И ИНТЕРФЕЙС
# ══════════════════════════════════════════════════════════════════
new_page(c)
kicker(c, "Интерфейс", H - MT - 2)
section_title(c, "Настройки и кнопки управления", H - MT - 24)

sy = H - MT - 50

# Левая колонка — настройки
lw = (W - ML - MR - 16) / 2
card(c, ML, sy - 250, lw, 250, BG_CARD, ACCENT)
label(c, "Настройки", ML + 10, sy - 18, size=13, col=ACCENT, bold=True)

settings_items = [
    ("Скорость  −50% … +50%",   "0% — нормальный темп. Минус — медленнее, плюс — быстрее."),
    ("Кодировка",                "'auto' — определяется автоматически (рекомендуется)."),
    ("Объединить в один файл",   "Все главы → один книга_full.mp3. Включено по умолчанию."),
    ("Не разбивать на главы",    "Вся книга как один фрагмент. Для рассказов и коротких текстов."),
    ("Удалить фрагменты",        "Удаляет отдельные файлы глав после объединения."),
]
iy = sy - 38
for stitle, sbody in settings_items:
    label(c, stitle, ML + 10, iy, size=10.5, col=TEXT, bold=True)
    wrapped_text(c, sbody, ML + 10, iy - 13, lw - 20, size=9.5, col=MUTED, line_h=13)
    iy -= 42

# Правая колонка — кнопки
rx = ML + lw + 16
card(c, rx, sy - 250, lw, 250, BG_CARD, GREEN)
label(c, "Кнопки управления", rx + 10, sy - 18, size=13, col=GREEN, bold=True)

btns = [
    (GREEN, "Начать конвертацию", "Запуск синтеза."),
    (AMBER, "Пауза / Продолжить", "Приостановить и возобновить.\nВо время паузы можно сменить голос."),
    (CORAL, "Стоп",               "Остановить. Готовые файлы сохраняются."),
    (ACCENT,"Открыть папку",      "Открывает папку с результатами в Проводнике."),
]
by2 = sy - 38
for col, btn_t, btn_d in btns:
    hbar(c, rx + 10, by2 - 2, lw - 20, 18, col, radius=4)
    label(c, btn_t, rx + 16, by2 + 2, size=9.5, col=BG_DARK, bold=True)
    by2 -= 22
    wrapped_text(c, btn_d, rx + 10, by2, lw - 20, size=9.5, col=MUTED, line_h=13)
    by2 -= (13 * btn_d.count("\n") + 28)

# Структура выходных файлов
sy2 = sy - 270
card(c, ML, sy2 - 115, W - ML - MR, 115, BG_CARD, CYAN)
label(c, "Структура выходных файлов", ML + 10, sy2 - 16, size=12, col=CYAN, bold=True)
tree = [
    "Мастер_и_Маргарита_audio/          ← папка рядом с книгой",
    "  001_Глава 1. Никогда не разговаривайте с неизвестными.mp3",
    "  002_Глава 2. Понтий Пилат.mp3",
    "  003_Глава 3. Седьмое доказательство.mp3",
    "  ...",
    "  Мастер_и_Маргарита_full.mp3      ← итоговый файл (при включённом «Объединить»)",
]
ty = sy2 - 34
c.setFont("Courier", 9)
for tline in tree:
    col_t = CYAN if tline.startswith("  М") or tline.startswith("М") else TEXT
    if "full" in tline:
        col_t = GREEN
    c.setFillColor(col_t)
    c.drawString(ML + 10, ty, tline)
    ty -= 13

footer_line(c, "Text to Audiobook v14 (C#)  ·  Руководство пользователя", 4)
c.showPage()

# ══════════════════════════════════════════════════════════════════
# СТРАНИЦА 5: УСТАНОВКА PIPER TTS
# ══════════════════════════════════════════════════════════════════
new_page(c)
kicker(c, "Piper TTS", H - MT - 2)
section_title(c, "Установка Piper TTS (оффлайн, без интернета)", H - MT - 24)

sy = H - MT - 50

# Что такое Piper
card(c, ML, sy - 60, W - ML - MR, 60, BG_CARD, ACCENT)
hbar(c, ML, sy - 60, 10, 60, ACCENT, radius=0)
label(c, "  Piper TTS — нейросетевой синтез речи высокого качества, полностью оффлайн.", ML + 18, sy - 18, size=11.5, col=TEXT, bold=True)
label(c, "  Голоса в формате .onnx. Устанавливается один раз — работает без интернета.", ML + 18, sy - 34, size=10.5, col=MUTED)
label(c, "  Значительно лучше Windows SAPI по качеству звука.", ML + 18, sy - 50, size=10.5, col=MUTED)

sy -= 75

# Шаги установки
install_steps = [
    (ACCENT, "Скачайте Piper",
     "Перейдите на: https://github.com/rhasspy/piper/releases\n"
     "Скачайте: piper_windows_amd64.zip"),
    (ACCENT, "Распакуйте в папку 'piper'",
     "Создайте папку 'piper' рядом с TextToAudiobookCSharp.exe\n"
     "Распакуйте содержимое архива туда (включая espeak-ng-data/)"),
    (GREEN,  "Скачайте голосовые модели (.onnx)",
     "Русский:    https://huggingface.co/rhasspy/piper-voices\n"
     "Файлы: ru_RU-dmitri-medium.onnx + ru_RU-dmitri-medium.onnx.json"),
    (CYAN,   "Перезапустите программу",
     "Режим 'Piper TTS' покажет найденные голоса.\n"
     "Новые голоса появятся в выпадающем списке с пометкой [Piper]."),
]

for i, (col, step_t, step_d) in enumerate(install_steps):
    sh = 65
    card(c, ML, sy - sh, W - ML - MR, sh, BG_CARD, col)
    hbar(c, ML, sy - sh, 8, sh, col, radius=0)
    # Номер кружочком
    c.setFillColor(col); c.circle(ML + 22, sy - sh/2, 10, fill=1, stroke=0)
    c.setFillColor(BG_DARK); c.setFont("Helvetica-Bold", 11)
    c.drawCentredString(ML + 22, sy - sh/2 - 4, str(i+1))
    label(c, step_t, ML + 40, sy - 16, size=11.5, col=col, bold=True)
    wrapped_text(c, step_d, ML + 40, sy - 32, W - ML - MR - 50, size=10, col=TEXT, line_h=14)
    sy -= sh + 8

# Структура папки piper
sy -= 5
card(c, ML, sy - 100, W - ML - MR, 100, BG_INPUT, CYAN)
label(c, "Ожидаемая структура папки:", ML + 10, sy - 14, size=11, col=CYAN, bold=True)
piper_tree = [
    "TextToAudiobookCSharp.exe",
    "piper/",
    "    piper.exe",
    "    espeak-ng-data/",
    "    ru_RU-dmitri-medium.onnx",
    "    ru_RU-dmitri-medium.onnx.json",
    "    en_US-ryan-high.onnx",
    "    en_US-ryan-high.onnx.json",
]
ty = sy - 30
c.setFont("Courier", 10)
for line in piper_tree:
    c.setFillColor(CYAN if not line.startswith(" ") and not line.startswith("p") else TEXT)
    if ".onnx" in line: c.setFillColor(GREEN)
    if "espeak" in line: c.setFillColor(AMBER)
    c.drawString(ML + 14, ty, line)
    ty -= 13

footer_line(c, "Text to Audiobook v14 (C#)  ·  Руководство пользователя", 5)
c.showPage()

# ══════════════════════════════════════════════════════════════════
# СТРАНИЦА 6: РЕШЕНИЕ ПРОБЛЕМ
# ══════════════════════════════════════════════════════════════════
new_page(c)
kicker(c, "Помощь", H - MT - 2)
section_title(c, "Решение типичных проблем", H - MT - 24)

problems = [
    (CORAL, "Кракозябры в журнале",
     "Измените 'Кодировка' с 'auto' на 'windows-1251' или 'utf-8'."),
    (AMBER, "Онлайн-режим не работает",
     "Проверьте интернет. Программа делает до 5 попыток автоматически. "
     "Переключитесь на Piper или SAPI при недоступности."),
    (CYAN,  "Нет голосов SAPI",
     "Установите: Параметры Windows → Время и язык → Речь → Добавить голоса. "
     "Рекомендуется: Irina или Pavel (русские)."),
    (ACCENT,"Piper: 'exe не найден'",
     "Убедитесь что папка 'piper' с piper.exe находится РЯДОМ с .exe программы."),
    (GREEN, "Программа зависла",
     "Нажмите «Стоп», подождите 10-15 сек. Готовые файлы сохраняются. "
     "Перезапустите и продолжите с места остановки."),
    (MUTED, "SmartScreen при первом запуске",
     "Нажмите 'Подробнее' → 'Выполнить в любом случае'. "
     "Это нормально — программа не подписана коммерческим сертификатом."),
]

py2 = H - MT - 50
for col, prob_t, prob_d in problems:
    ph = 58
    card(c, ML, py2 - ph, W - ML - MR, ph, BG_CARD, col)
    hbar(c, ML, py2 - ph, 8, ph, col, radius=0)
    label(c, prob_t, ML + 18, py2 - 16, size=12, col=col, bold=True)
    wrapped_text(c, prob_d, ML + 18, py2 - 32, W - ML - MR - 28, size=10.5, col=TEXT, line_h=15)
    py2 -= ph + 8

# Блок советов
py2 -= 5
card(c, ML, py2 - 80, W - ML - MR, 80, BG_CARD, GREEN)
label(c, " Советы", ML + 10, py2 - 16, size=13, col=GREEN, bold=True)
tips = [
    "Используйте паузу, если нужно освободить ресурсы — голос можно сменить во время паузы.",
    "Файл audiobook_history.json в папке с программой — не удаляйте, там история книг.",
    "Главы определяются по «Глава N», «Chapter N», «Часть N» и т.д. Для рассказов используйте 'Не разбивать'.",
]
ty2 = py2 - 34
for tip in tips:
    bullet(c, ML + 10, ty2, GREEN)
    wrapped_text(c, tip, ML + 22, ty2, W - ML - MR - 32, size=10, col=TEXT, line_h=14)
    ty2 -= 18

footer_line(c, "Text to Audiobook v14 (C#)  ·  Руководство пользователя", 6)
c.showPage()

# ══════════════════════════════════════════════════════════════════
# СТРАНИЦА 7: ТЕХНИЧЕСКИЕ ДЕТАЛИ + КОНТАКТ
# ══════════════════════════════════════════════════════════════════
new_page(c)
kicker(c, "Технические детали", H - MT - 2)
section_title(c, "Характеристики и контакт", H - MT - 24)

sy = H - MT - 50

# Сетка характеристик 3×2
stats = [
    (".NET 9 · WPF",    "платформа",                  ACCENT),
    ("166 МБ",          "размер exe (self-contained)", GREEN),
    ("5",               "NuGet-пакетов",               CYAN),
    ("3",               "движка TTS",                  AMBER),
    ("3 000",           "символов макс. на фрагмент",  ACCENT),
    ("0",               "необходимых установок",        GREEN),
]
sw = (W - ML - MR - 20) / 3
sx = ML
for i, (val, desc, col) in enumerate(stats):
    if i == 3: sx = ML; sy -= 80
    card(c, sx, sy - 72, sw - 8, 72, BG_CARD, col)
    label(c, val, sx + sw//2 - 4, sy - 30, size=28, col=col, bold=True, align="center")
    label(c, desc, sx + sw//2 - 4, sy - 52, size=9.5, col=MUTED, align="center")
    sx += sw

sy -= 92

# Стек технологий
card(c, ML, sy - 120, W - ML - MR, 120, BG_CARD, ACCENT)
label(c, "Стек технологий", ML + 10, sy - 16, size=13, col=ACCENT, bold=True)

tech_cols = [
    ("Язык / платформа", ["C# 13  ·  .NET 9", "WPF (Windows Presentation Foundation)", "XAML + code-behind"]),
    ("NuGet-пакеты",     ["NAudio 2.2.1  — работа с аудио", "NAudio.Lame — WAV → MP3", "Newtonsoft.Json — история", "System.Speech — SAPI", "UTF.Unknown — кодировки"]),
    ("TTS",              ["Microsoft Edge TTS (WebSocket)", "Piper TTS (piper.exe + ONNX)", "Windows SAPI (System.Speech)"]),
]
tx = ML + 10
for col_t, items in tech_cols:
    label(c, col_t, tx, sy - 34, size=10.5, col=CYAN, bold=True)
    ty3 = sy - 50
    for item in items:
        bullet(c, tx, ty3, ACCENT)
        label(c, item, tx + 12, ty3, size=9.5, col=TEXT)
        ty3 -= 14
    tx += (W - ML - MR - 20) / 3

sy -= 132

# Контакт
hbar(c, ML, sy - 55, W - ML - MR, 55, BG_INPUT, radius=8)
c.setStrokeColor(ACCENT); c.setLineWidth(1.5)
c.roundRect(ML, sy - 55, W - ML - MR, 55, 8, fill=0, stroke=1)
label(c, "Поддержка и обратная связь", ML + 16, sy - 18, size=13, col=ACCENT, bold=True)
label(c, "buchin1andrey@gmail.com", ML + 16, sy - 36, size=12, col=TEXT)
label(c, "Сообщайте об ошибках и пожеланиях по электронной почте.", ML + 16, sy - 50, size=10, col=MUTED)

footer_line(c, "Text to Audiobook v14 (C#)  ·  Руководство пользователя", 7)
c.showPage()

# ── Финальная запись ───────────────────────────────────────────────
c.save()
print(f"OK: {OUT}")
