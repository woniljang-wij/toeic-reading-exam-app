from html import escape
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile


def question(number: int) -> list[str]:
    lines = [
        f'[QUESTION number="{number}" shuffleChoices="false"]',
        f"[PROMPT]Demo question {number}: choose the sample answer.[/PROMPT]",
    ]
    lines.extend(
        f'[CHOICE key="{key}"]Demo choice {key} for question {number}.[/CHOICE]'
        for key in "ABCD"
    )
    lines.extend(
        [
            "[ANSWER]A[/ANSWER]",
            f"[HINT]Demo hint for question {number}; replace this with a licensed explanation.[/HINT]",
            "[/QUESTION]",
        ]
    )
    return lines


def passage(source_id: str, first: int, last: int) -> list[str]:
    lines = [
        f'[PASSAGE id="{source_id}" shuffleQuestions="false" shuffleAnswers="false"]',
        f"[PASSAGE_TEXT]Demo reading passage {source_id}. This original placeholder text is included only to demonstrate the grouped-question import workflow.[/PASSAGE_TEXT]",
    ]
    for number in range(first, last + 1):
        lines.extend(question(number))
    lines.append("[/PASSAGE]")
    return lines


def build_tagged_text() -> list[str]:
    lines = [
        '[EXAM title="DEMO - Tagged Reading Exam" durationMinutes="75"]',
        '[PART number="5" shuffleQuestions="false" shuffleAnswers="false"]',
    ]
    for number in range(101, 131):
        lines.extend(question(number))
    lines.append("[/PART]")
    lines.append('[PART number="6" shuffleQuestions="false" shuffleAnswers="false"]')
    for group in range(4):
        first = 131 + group * 4
        lines.extend(passage(f"P{first}", first, first + 3))
    lines.append("[/PART]")
    lines.append('[PART number="7" shuffleQuestions="false" shuffleAnswers="false"]')
    first = 147
    passage_number = 1
    while first <= 200:
        last = min(first + 3, 200)
        lines.extend(passage(f"R{passage_number}", first, last))
        first = last + 1
        passage_number += 1
    lines.extend(["[/PART]", "[/EXAM]"])
    return lines


def main() -> None:
    output = Path(__file__).resolve().parents[1] / "sample-data" / "Demo_Tagged_Reading_Exam.docx"
    output.parent.mkdir(parents=True, exist_ok=True)
    paragraphs = "".join(
        f'<w:p><w:r><w:t xml:space="preserve">{escape(line)}</w:t></w:r></w:p>'
        for line in build_tagged_text()
    )
    document_xml = (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">'
        f"<w:body>{paragraphs}<w:sectPr/></w:body></w:document>"
    )
    content_types = (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">'
        '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>'
        '<Default Extension="xml" ContentType="application/xml"/>'
        '<Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>'
        "</Types>"
    )
    relationships = (
        '<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
        '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
        '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>'
        "</Relationships>"
    )
    with ZipFile(output, "w", ZIP_DEFLATED) as archive:
        archive.writestr("[Content_Types].xml", content_types)
        archive.writestr("_rels/.rels", relationships)
        archive.writestr("word/document.xml", document_xml)
    print(output)


if __name__ == "__main__":
    main()
