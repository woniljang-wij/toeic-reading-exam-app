using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Backend.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Drawing = DocumentFormat.OpenXml.Wordprocessing.Drawing;
using WordText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace Backend;

public sealed record ParsedChoice(string Key, string Content);
public sealed record ParsedQuestion(
    int Number,
    string Prompt,
    string Answer,
    string Hint,
    bool ShuffleChoices,
    IReadOnlyList<ParsedChoice> Choices);
public sealed record ParsedPassage(
    string SourceId,
    string Content,
    bool ShuffleQuestions,
    bool ShuffleAnswers,
    IReadOnlyList<ParsedQuestion> Questions);
public sealed record ParsedPart(
    int Number,
    bool ShuffleQuestions,
    bool ShuffleAnswers,
    IReadOnlyList<ParsedPassage> Passages,
    IReadOnlyList<ParsedQuestion> Questions);
public sealed record ParsedExam(string Title, int DurationMinutes, IReadOnlyList<ParsedPart> Parts);

public sealed class TaggedExamValidationException(IReadOnlyList<string> errors)
    : Exception(string.Join(Environment.NewLine, errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}

public sealed class TaggedExamParser
{
    private static readonly Regex TagRegex = new(
        @"\[(?<name>[A-Z_]+)(?<attributes>[^\]]*)\](?<inline>[^\[]*)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex QuestionRegex = new(
        @"\[QUESTION\s+number=""(?<number>\d+)""(?<attributes>[^]]*)\](?<body>.*?)\[/QUESTION\]",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
    private static readonly Regex PassageRegex = new(
        @"\[PASSAGE\s+id=""(?<id>[^""]+)""(?<attributes>[^]]*)\](?<body>.*?)\[/PASSAGE\]",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);
    private static readonly Regex ChoiceRegex = new(
        @"\[CHOICE\s+key=""(?<key>[A-D])""\](?<content>.*?)\[/CHOICE\]",
        RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

    public ParsedExam Parse(Stream docxStream)
    {
        var text = ReadDocumentText(docxStream);
        var errors = new List<string>();
        ValidateTagNames(text, errors);

        var examHeader = Regex.Match(text,
            @"\[EXAM\s+title=""(?<title>[^""]+)""\s+durationMinutes=""(?<duration>\d+)""\]",
            RegexOptions.CultureInvariant);
        if (!examHeader.Success)
            errors.Add("Missing [EXAM title=\"...\" durationMinutes=\"...\"] tag.");

        var parts = new List<ParsedPart>();
        foreach (Match partMatch in Regex.Matches(text,
                     @"\[PART\s+number=""(?<number>\d+)""(?<attributes>[^]]*)\](?<body>.*?)\[/PART\]",
                     RegexOptions.Singleline | RegexOptions.CultureInvariant))
        {
            if (!int.TryParse(partMatch.Groups["number"].Value, CultureInfo.InvariantCulture, out var number))
            {
                errors.Add("A PART tag has an invalid number.");
                continue;
            }

            var partBody = partMatch.Groups["body"].Value;
            var passages = ParsePassages(partBody, number, errors);
            var directQuestions = QuestionRegex.Matches(PassageRegex.Replace(partBody, string.Empty))
                .Cast<Match>()
                .Select(match => ParseQuestion(match, errors))
                .Where(question => question is not null)
                .Cast<ParsedQuestion>()
                .ToArray();

            var attributes = partMatch.Groups["attributes"].Value;
            parts.Add(new ParsedPart(
                number,
                ReadBooleanAttribute(attributes, "shuffleQuestions", false, errors),
                ReadBooleanAttribute(attributes, "shuffleAnswers", false, errors),
                passages,
                directQuestions));
        }

        if (parts.Count != 3 || !parts.Select(x => x.Number).SequenceEqual([5, 6, 7]))
            errors.Add("The exam must contain PART 5, PART 6, and PART 7 in order.");

        var allQuestions = parts.SelectMany(x => x.Questions.Concat(x.Passages.SelectMany(p => p.Questions)))
            .OrderBy(x => x.Number)
            .ToArray();
        if (allQuestions.Length != 100)
            errors.Add($"Expected 100 questions (101-200), found {allQuestions.Length}.");
        if (!allQuestions.Select(x => x.Number).SequenceEqual(Enumerable.Range(101, 100)))
            errors.Add("Question numbers must be unique and sequential from 101 through 200.");

        foreach (var part in parts)
        {
            var expectedRange = part.Number switch
            {
                5 => (First: 101, Last: 130),
                6 => (First: 131, Last: 146),
                7 => (First: 147, Last: 200),
                _ => (First: 0, Last: -1)
            };
            var partQuestions = part.Questions.Concat(part.Passages.SelectMany(p => p.Questions)).ToArray();
            if (partQuestions.Any(q => q.Number < expectedRange.First || q.Number > expectedRange.Last))
                errors.Add($"PART {part.Number} contains a question outside its assigned number range.");
            if (part.Number == 5 && (part.Passages.Count != 0 || part.Questions.Count != 30))
                errors.Add("PART 5 must contain 30 independent questions and no passages.");
            if (part.Number is 6 or 7 && (part.Questions.Count != 0 || part.Passages.Count == 0))
                errors.Add($"PART {part.Number} questions must be grouped under passages.");
            if (part.Number == 6 && part.Passages.Any(x => x.Questions.Count != 4))
                errors.Add("Each PART 6 passage must contain exactly four questions.");
        }

        if (examHeader.Success &&
            !int.TryParse(examHeader.Groups["duration"].Value, CultureInfo.InvariantCulture, out var duration))
            errors.Add("Exam duration must be a positive integer.");
        else if (examHeader.Success &&
                 int.TryParse(examHeader.Groups["duration"].Value, CultureInfo.InvariantCulture, out var parsedDuration) &&
                 parsedDuration <= 0)
            errors.Add("Exam duration must be a positive integer.");

        if (errors.Count > 0)
            throw new TaggedExamValidationException(errors);

        return new ParsedExam(
            examHeader.Groups["title"].Value.Trim(),
            int.Parse(examHeader.Groups["duration"].Value, CultureInfo.InvariantCulture),
            parts);
    }

    private static IReadOnlyList<ParsedPassage> ParsePassages(string partBody, int partNumber, List<string> errors)
    {
        var passages = new List<ParsedPassage>();
        foreach (Match match in PassageRegex.Matches(partBody))
        {
            var body = match.Groups["body"].Value;
            var contentMatch = Regex.Match(body, @"\[PASSAGE_TEXT\](?<text>.*?)\[/PASSAGE_TEXT\]",
                RegexOptions.Singleline | RegexOptions.CultureInvariant);
            if (!contentMatch.Success || string.IsNullOrWhiteSpace(contentMatch.Groups["text"].Value))
                errors.Add($"Passage {match.Groups["id"].Value} must have non-empty passage text.");
            var questions = QuestionRegex.Matches(body)
                .Cast<Match>()
                .Select(question => ParseQuestion(question, errors))
                .Where(question => question is not null)
                .Cast<ParsedQuestion>()
                .ToArray();
            if (questions.Length == 0)
                errors.Add($"Passage {match.Groups["id"].Value} must contain at least one question.");
            if (questions.Any(x => partNumber == 6 && x.Number is < 131 or > 146) ||
                questions.Any(x => partNumber == 7 && x.Number is < 147 or > 200))
                errors.Add($"Passage {match.Groups["id"].Value} has a question outside PART {partNumber}.");

            var attributes = match.Groups["attributes"].Value;
            passages.Add(new ParsedPassage(
                match.Groups["id"].Value,
                contentMatch.Success ? contentMatch.Groups["text"].Value.Trim() : string.Empty,
                ReadBooleanAttribute(attributes, "shuffleQuestions", false, errors),
                ReadBooleanAttribute(attributes, "shuffleAnswers", false, errors),
                questions));
        }
        return passages;
    }

    private static ParsedQuestion? ParseQuestion(Match match, List<string> errors)
    {
        var number = int.Parse(match.Groups["number"].Value, CultureInfo.InvariantCulture);
        var body = match.Groups["body"].Value;
        var prompt = ReadTagContent(body, "PROMPT");
        var answer = ReadTagContent(body, "ANSWER");
        var hint = ReadTagContent(body, "HINT");
        var choices = ChoiceRegex.Matches(body)
            .Cast<Match>()
            .Select(choice => new ParsedChoice(
                choice.Groups["key"].Value,
                choice.Groups["content"].Value.Trim()))
            .ToArray();

        if (string.IsNullOrWhiteSpace(prompt))
            errors.Add($"Question {number} is missing its PROMPT tag.");
        if (string.IsNullOrWhiteSpace(hint))
            errors.Add($"Question {number} is missing its HINT tag.");
        if (!new[] { "A", "B", "C", "D" }.SequenceEqual(choices.Select(x => x.Key).Order()))
            errors.Add($"Question {number} must contain exactly one choice A, B, C, and D.");
        if (choices.Any(x => string.IsNullOrWhiteSpace(x.Content)))
            errors.Add($"Question {number} contains an empty choice.");
        if (answer.Length != 1 || !choices.Any(x => x.Key == answer))
            errors.Add($"Question {number} has an ANSWER that does not match one of its choices.");

        var attributes = match.Groups["attributes"].Value;
        return new ParsedQuestion(
            number,
            prompt.Trim(),
            answer,
            hint.Trim(),
            ReadBooleanAttribute(attributes, "shuffleChoices", false, errors),
            choices);
    }

    private static string ReadTagContent(string text, string tag)
    {
        var match = Regex.Match(text, $@"\[{tag}\](?<value>.*?)\[/{tag}\]",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["value"].Value.Trim() : string.Empty;
    }

    private static bool ReadBooleanAttribute(string attributes, string name, bool fallback, List<string> errors)
    {
        var match = Regex.Match(attributes, $@"\b{name}=""(?<value>true|false)""",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            errors.Add($"Missing required boolean attribute {name}=\"true|false\".");
            return fallback;
        }
        return bool.Parse(match.Groups["value"].Value);
    }

    private static void ValidateTagNames(string text, List<string> errors)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "EXAM", "PART", "PASSAGE", "PASSAGE_TEXT", "QUESTION", "PROMPT", "CHOICE",
            "ANSWER", "HINT"
        };
        foreach (Match match in TagRegex.Matches(text))
        {
            if (!allowed.Contains(match.Groups["name"].Value))
                errors.Add($"Unknown tag [{match.Groups["name"].Value}].");
        }
    }

    private static string ReadDocumentText(Stream stream)
    {
        using var document = WordprocessingDocument.Open(stream, false);
        var mainPart = document.MainDocumentPart
            ?? throw new TaggedExamValidationException(["The uploaded DOCX has no main document part."]);
        var body = mainPart.Document?.Body
            ?? throw new TaggedExamValidationException(["The uploaded DOCX has no document body."]);
        return string.Join(Environment.NewLine, body.Descendants<Paragraph>().Select(paragraph =>
        {
            var text = new StringBuilder();
            foreach (var element in paragraph.Descendants<OpenXmlElement>())
            {
                if (element is WordText wordText)
                {
                    text.Append(wordText.Text);
                }
                else if (element is Break)
                {
                    text.AppendLine();
                }
                else if (element is Drawing drawing)
                {
                    foreach (var image in drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>())
                    {
                        if (image.Embed?.Value is not { Length: > 0 } relationshipId ||
                            mainPart.GetPartById(relationshipId) is not ImagePart imagePart)
                            throw new TaggedExamValidationException(
                                ["The DOCX contains an image with a missing or unsupported relationship."]);

                        if (!IsSupportedImageType(imagePart.ContentType))
                            throw new TaggedExamValidationException(
                                [$"Images of type '{imagePart.ContentType}' are not supported. Use PNG, JPEG, GIF, or WebP images."]);

                        using var imageStream = imagePart.GetStream();
                        using var buffer = new MemoryStream();
                        imageStream.CopyTo(buffer);
                        text.Append("{{IMAGE:data:")
                            .Append(imagePart.ContentType)
                            .Append(";base64,")
                            .Append(Convert.ToBase64String(buffer.ToArray()))
                            .Append("}}");
                    }
                }
            }

            return text.ToString();
        }));
    }

    private static bool IsSupportedImageType(string contentType) =>
        contentType.Equals("image/png", StringComparison.OrdinalIgnoreCase) ||
        contentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
        contentType.Equals("image/gif", StringComparison.OrdinalIgnoreCase) ||
        contentType.Equals("image/webp", StringComparison.OrdinalIgnoreCase);
}
