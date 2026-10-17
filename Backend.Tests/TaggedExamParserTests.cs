using System.Text;
using Backend;
using Backend.Controllers;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Backend.Tests;

public sealed class TaggedExamParserTests
{
    private readonly TaggedExamParser _parser = new();

    [Fact]
    public void Parse_ValidReadingExam_PreservesQuestionGroupingAndTags()
    {
        using var document = CreateDocx(BuildValidExam());

        var parsed = _parser.Parse(document);

        Assert.Equal("Reading Test 8", parsed.Title);
        Assert.Equal(75, parsed.DurationMinutes);
        Assert.Equal(new[] { 5, 6, 7 }, parsed.Parts.Select(part => part.Number));
        Assert.Equal(30, parsed.Parts[0].Questions.Count);
        Assert.Equal(4, parsed.Parts[1].Passages.Count);
        Assert.All(parsed.Parts[1].Passages, passage => Assert.Equal(4, passage.Questions.Count));
        Assert.Equal(54, parsed.Parts[2].Passages.Sum(passage => passage.Questions.Count));
        Assert.All(parsed.Parts.SelectMany(part => part.Passages), passage => Assert.False(passage.ShuffleQuestions));
        Assert.Equal(100, parsed.Parts.Sum(part =>
            part.Questions.Count + part.Passages.Sum(passage => passage.Questions.Count)));
    }

    [Theory]
    [InlineData("[ANSWER]A[/ANSWER]", "[ANSWER][/ANSWER]")]
    [InlineData("[HINT]Use the sentence context to choose the best answer.[/HINT]", "")]
    [InlineData("[CHOICE key=\"D\"]Option D[/CHOICE]", "")]
    public void Parse_InvalidQuestionContent_RejectsWholeDocument(string original, string replacement)
    {
        var text = BuildValidExam().Replace(original, replacement, StringComparison.Ordinal);
        using var document = CreateDocx(text);

        var error = Assert.Throws<TaggedExamValidationException>(() => _parser.Parse(document));

        Assert.NotEmpty(error.Errors);
    }

    [Fact]
    public void Parse_MissingQuestionNumber_RejectsWholeDocument()
    {
        var text = BuildValidExam().Replace(
            "[QUESTION number=\"101\" shuffleChoices=\"true\"]",
            "[QUESTION number=\"102\" shuffleChoices=\"true\"]",
            StringComparison.Ordinal);
        using var document = CreateDocx(text);

        var error = Assert.Throws<TaggedExamValidationException>(() => _parser.Parse(document));

        Assert.Contains(error.Errors, message => message.Contains("sequential", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Parse_EmbeddedPng_PreservesImageForReviewAndStudentDisplay()
    {
        using var document = CreateDocxWithImage(BuildValidExam());

        var parsed = _parser.Parse(document);
        var prompt = parsed.Parts[0].Questions[0].Prompt;

        Assert.Contains("{{IMAGE:data:image/png;base64,", prompt, StringComparison.Ordinal);
        Assert.EndsWith("}}", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void StudentPart_WhenShuffleTagsAreEnabled_ReturnsShuffledOrderAndChoices()
    {
        var questions = Enumerable.Range(101, 10)
            .Select(number => new ExamQuestion
            {
                Number = number,
                Prompt = $"Prompt {number}",
                CorrectChoiceKey = "A",
                Hint = "Context hint",
                Choices = "ABCD".Select((key, position) => new AnswerChoice
                {
                    Key = key.ToString(),
                    Content = $"Choice {key}",
                    Position = position
                }).ToList()
            })
            .ToList();
        var part = new ExamPart
        {
            Number = 5,
            ShuffleQuestions = true,
            ShuffleAnswers = true,
            Questions = questions
        };

        var studentPart = StudentPartDto.From(part);

        Assert.Equal(questions.Select(question => question.Number).Order(),
            studentPart.QuestionOrder.Order());
        Assert.NotEqual(questions.Select(question => question.Number),
            studentPart.QuestionOrder);
        Assert.All(studentPart.Questions, question =>
        {
            Assert.True(question.ShuffleChoices);
            Assert.Equal(new[] { "A", "B", "C", "D" }, question.Choices.Select(choice => choice.Key).Order());
            Assert.NotEqual(new[] { "A", "B", "C", "D" }, question.Choices.Select(choice => choice.Key));
        });
    }

    private static string BuildValidExam()
    {
        var text = new StringBuilder();
        text.AppendLine("[EXAM title=\"Reading Test 8\" durationMinutes=\"75\"]");
        text.AppendLine("[PART number=\"5\" shuffleQuestions=\"false\" shuffleAnswers=\"true\"]");
        for (var number = 101; number <= 130; number++)
            AppendQuestion(text, number);
        text.AppendLine("[/PART]");
        text.AppendLine("[PART number=\"6\" shuffleQuestions=\"false\" shuffleAnswers=\"false\"]");
        for (var passageIndex = 0; passageIndex < 4; passageIndex++)
        {
            var firstQuestion = 131 + passageIndex * 4;
            AppendPassage(text, $"P{firstQuestion}", firstQuestion, firstQuestion + 3);
        }
        text.AppendLine("[/PART]");
        text.AppendLine("[PART number=\"7\" shuffleQuestions=\"true\" shuffleAnswers=\"false\"]");
        AppendPassage(text, "P147", 147, 200);
        text.AppendLine("[/PART]");
        text.AppendLine("[/EXAM]");
        return text.ToString();
    }

    private static void AppendPassage(StringBuilder text, string id, int first, int last)
    {
        text.AppendLine($"[PASSAGE id=\"{id}\" shuffleQuestions=\"false\" shuffleAnswers=\"false\"]");
        text.AppendLine("[PASSAGE_TEXT]A group passage for the imported reading questions.[/PASSAGE_TEXT]");
        for (var number = first; number <= last; number++)
            AppendQuestion(text, number);
        text.AppendLine("[/PASSAGE]");
    }

    private static void AppendQuestion(StringBuilder text, int number)
    {
        text.AppendLine($"[QUESTION number=\"{number}\" shuffleChoices=\"true\"]");
        text.AppendLine($"[PROMPT]Sample prompt for question {number}.[/PROMPT]");
        text.AppendLine("[CHOICE key=\"A\"]Option A[/CHOICE]");
        text.AppendLine("[CHOICE key=\"B\"]Option B[/CHOICE]");
        text.AppendLine("[CHOICE key=\"C\"]Option C[/CHOICE]");
        text.AppendLine("[CHOICE key=\"D\"]Option D[/CHOICE]");
        text.AppendLine("[ANSWER]A[/ANSWER]");
        text.AppendLine("[HINT]Use the sentence context to choose the best answer.[/HINT]");
        text.AppendLine("[/QUESTION]");
    }

    private static MemoryStream CreateDocx(string content)
    {
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   true))
        {
            var mainPart = document.AddMainDocumentPart();
            var body = new Body();
            foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                body.Append(new Paragraph(new Run(new Text(line.TrimEnd('\r'))
                {
                    Space = SpaceProcessingModeValues.Preserve
                })));
            }
            mainPart.Document = new Document(body);
            mainPart.Document.Save();
        }
        stream.Position = 0;
        return stream;
    }

    private static MemoryStream CreateDocxWithImage(string content)
    {
        var stream = CreateDocx(content);
        using (var document = WordprocessingDocument.Open(stream, true))
        {
            var mainPart = document.MainDocumentPart!;
            var imagePart = mainPart.AddImagePart(ImagePartType.Png);
            imagePart.FeedData(new MemoryStream([1, 2, 3]));
            var relationshipId = mainPart.GetIdOfPart(imagePart);
            var promptParagraph = mainPart.Document!.Body!.Descendants<Paragraph>()
                .Single(paragraph => paragraph.InnerText.StartsWith(
                    "[PROMPT]Sample prompt for question 101.",
                    StringComparison.Ordinal));
            var drawing = new Drawing($"""
                <w:drawing xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"
                           xmlns:wp="http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing"
                           xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main"
                           xmlns:pic="http://schemas.openxmlformats.org/drawingml/2006/picture"
                           xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <wp:inline>
                    <wp:extent cx="914400" cy="914400"/>
                    <wp:docPr id="1" name="exam-image"/>
                    <a:graphic>
                      <a:graphicData uri="http://schemas.openxmlformats.org/drawingml/2006/picture">
                        <pic:pic>
                          <pic:blipFill><a:blip r:embed="{relationshipId}"/></pic:blipFill>
                        </pic:pic>
                      </a:graphicData>
                    </a:graphic>
                  </wp:inline>
                </w:drawing>
                """);
            promptParagraph.RemoveAllChildren<Run>();
            promptParagraph.Append(
                new Run(new Text("[PROMPT]Sample prompt for question 101. ")),
                new Run(drawing),
                new Run(new Text("[/PROMPT]")));
            mainPart.Document.Save();
        }

        stream.Position = 0;
        return stream;
    }
}
