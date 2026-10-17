using Backend.Models;
using Abp.Web.Models;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api")]
[DontWrapResult]
public sealed class ExamsController(
    ToeicDbContext db,
    TaggedExamParser parser,
    ExamAnalyticsService analytics) : ControllerBase
{
    [HttpGet("exams")]
    public async Task<ActionResult<IReadOnlyList<ExamSummaryDto>>> ListPublished(CancellationToken cancellationToken)
    {
        var exams = await db.Exams
            .Where(exam => exam.IsPublished)
            .OrderByDescending(exam => exam.PublishedAtUtc)
            .Select(exam => new ExamSummaryDto(
                exam.Id,
                exam.Title,
                exam.DurationMinutes,
                exam.Parts.Sum(part => part.Questions.Count),
                exam.IsPublished))
            .ToListAsync(cancellationToken);
        return Ok(exams);
    }

    [HttpGet("exams/{examId:int}")]
    public async Task<ActionResult<StudentExamDto>> GetPublished(int examId, CancellationToken cancellationToken)
    {
        var exam = await db.Exams
            .AsNoTracking()
            .Where(item => item.Id == examId && item.IsPublished)
            .SingleOrDefaultAsync(cancellationToken);
        if (exam is null)
            return NotFound();

        var parts = await db.ExamParts
            .AsNoTracking()
            .Where(part => part.ExamId == exam.Id)
            .ToListAsync(cancellationToken);
        var partIds = parts.Select(part => part.Id).ToArray();
        if (partIds.Length == 0)
            return Ok(StudentExamDto.From(exam));

        var passages = await db.Passages
            .AsNoTracking()
            .Where(passage => partIds.Contains(passage.ExamPartId))
            .ToListAsync(cancellationToken);
        var questions = await db.Questions
            .AsNoTracking()
            .Where(question => partIds.Contains(question.ExamPartId))
            .ToListAsync(cancellationToken);
        var questionIds = questions.Select(question => question.Id).ToArray();
        var choices = questionIds.Length == 0
            ? []
            : await db.Choices
                .AsNoTracking()
                .Where(choice => questionIds.Contains(choice.ExamQuestionId))
                .ToListAsync(cancellationToken);

        var choicesByQuestion = choices.ToLookup(choice => choice.ExamQuestionId);
        foreach (var question in questions)
            question.Choices = choicesByQuestion[question.Id].ToList();

        var questionsByPassage = questions
            .Where(question => question.PassageId.HasValue)
            .ToLookup(question => question.PassageId!.Value);
        foreach (var passage in passages)
            passage.Questions = questionsByPassage[passage.Id].ToList();

        var questionsByPart = questions.ToLookup(question => question.ExamPartId);
        var passagesByPart = passages.ToLookup(passage => passage.ExamPartId);
        foreach (var part in parts)
        {
            part.Questions = questionsByPart[part.Id].ToList();
            part.Passages = passagesByPart[part.Id].ToList();
        }

        exam.Parts = parts;
        return Ok(StudentExamDto.From(exam));
    }

    [HttpPost("admin/exams/import")]
    [RequestSizeLimit(25_000_000)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { errors = new[] { "Choose a non-empty .docx file." } });
        if (!Path.GetExtension(file.FileName).Equals(".docx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { errors = new[] { "Only .docx files are supported." } });
        if (file.Length > 25_000_000)
            return BadRequest(new { errors = new[] { "The .docx file must be 25 MB or smaller." } });

        ParsedExam parsed;
        await using (var stream = file.OpenReadStream())
        {
            try
            {
                parsed = parser.Parse(stream);
            }
            catch (TaggedExamValidationException exception)
            {
                return BadRequest(new { errors = exception.Errors });
            }
            catch (OpenXmlPackageException)
            {
                return BadRequest(new { errors = new[] { "The uploaded file is not a readable Word .docx document." } });
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var exam = ToEntity(parsed);
        db.Exams.Add(exam);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return CreatedAtAction(
            nameof(GetAdminExam),
            new { examId = exam.Id },
            new { exam.Id, exam.Title, questionCount = CountQuestions(parsed), exam.IsPublished });
    }

    [HttpGet("admin/exams")]
    public async Task<ActionResult<IReadOnlyList<ExamSummaryDto>>> ListAll(CancellationToken cancellationToken)
    {
        var exams = await db.Exams
            .OrderByDescending(exam => exam.CreatedAtUtc)
            .Select(exam => new ExamSummaryDto(
                exam.Id,
                exam.Title,
                exam.DurationMinutes,
                exam.Parts.Sum(part => part.Questions.Count),
                exam.IsPublished))
            .ToListAsync(cancellationToken);
        return Ok(exams);
    }

    [HttpGet("admin/exams/{examId:int}")]
    public async Task<IActionResult> GetAdminExam(int examId, CancellationToken cancellationToken)
    {
        var exam = await db.Exams
            .Where(item => item.Id == examId)
            .Include(item => item.Parts)
                .ThenInclude(part => part.Questions)
                    .ThenInclude(question => question.Choices)
            .Include(item => item.Parts)
                .ThenInclude(part => part.Passages)
                    .ThenInclude(passage => passage.Questions)
                        .ThenInclude(question => question.Choices)
            .AsSplitQuery()
            .SingleOrDefaultAsync(cancellationToken);
        return exam is null ? NotFound() : Ok(TeacherExamDto.From(exam));
    }

    [HttpPut("admin/exams/{examId:int}/publish")]
    public async Task<IActionResult> SetPublished(
        int examId,
        [FromBody] PublishExamRequest request,
        CancellationToken cancellationToken)
    {
        var exam = await db.Exams.SingleOrDefaultAsync(item => item.Id == examId, cancellationToken);
        if (exam is null)
            return NotFound();
        exam.IsPublished = request.IsPublished;
        exam.PublishedAtUtc = request.IsPublished ? DateTime.UtcNow : null;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("admin/exams/{examId:int}")]
    public async Task<IActionResult> UpdateExam(
        int examId,
        [FromBody] UpdateExamRequest request,
        CancellationToken cancellationToken)
    {
        var exam = await db.Exams
            .Include(item => item.Attempts)
            .SingleOrDefaultAsync(item => item.Id == examId, cancellationToken);
        if (exam is null)
            return NotFound();
        if (!CanEdit(exam, out var error))
            return Conflict(new { error });
        if (string.IsNullOrWhiteSpace(request.Title) || request.DurationMinutes <= 0)
            return BadRequest(new { error = "Exam title and a positive duration are required." });

        exam.Title = request.Title.Trim();
        exam.DurationMinutes = request.DurationMinutes;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("admin/exams/{examId:int}/questions/{number:int}")]
    public async Task<IActionResult> UpdateQuestion(
        int examId,
        int number,
        [FromBody] UpdateQuestionRequest request,
        CancellationToken cancellationToken)
    {
        var exam = await db.Exams
            .Include(item => item.Attempts)
            .SingleOrDefaultAsync(item => item.Id == examId, cancellationToken);
        if (exam is null)
            return NotFound();
        if (!CanEdit(exam, out var error))
            return Conflict(new { error });

        var question = await db.Questions
            .Where(item => item.ExamPart.ExamId == examId && item.Number == number)
            .Include(item => item.Choices)
            .SingleOrDefaultAsync(cancellationToken);
        if (question is null)
            return NotFound();
        if (request.Choices is null || request.Choices.Any(choice => choice is null))
            return BadRequest(new { error = "All four answer choices are required." });
        var choiceKeys = request.Choices.Select(choice => choice.Key).Order().ToArray();
        if (string.IsNullOrWhiteSpace(request.Prompt) ||
            string.IsNullOrWhiteSpace(request.Hint) ||
            !new[] { "A", "B", "C", "D" }.SequenceEqual(choiceKeys) ||
            request.Choices.Any(choice => string.IsNullOrWhiteSpace(choice.Content)) ||
            !request.Choices.Any(choice => choice.Key == request.CorrectChoiceKey))
            return BadRequest(new { error = "Provide the prompt, hint, choices A-D, and an answer key A-D." });

        question.Prompt = request.Prompt.Trim();
        question.Hint = request.Hint.Trim();
        question.CorrectChoiceKey = request.CorrectChoiceKey;
        foreach (var choice in request.Choices)
            question.Choices.Single(item => item.Key == choice.Key).Content = choice.Content.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("admin/exams/{examId:int}/passages/{sourceId}")]
    public async Task<IActionResult> UpdatePassage(
        int examId,
        string sourceId,
        [FromBody] UpdatePassageRequest request,
        CancellationToken cancellationToken)
    {
        var exam = await db.Exams
            .Include(item => item.Attempts)
            .SingleOrDefaultAsync(item => item.Id == examId, cancellationToken);
        if (exam is null)
            return NotFound();
        if (!CanEdit(exam, out var error))
            return Conflict(new { error });
        if (string.IsNullOrWhiteSpace(request.Content))
            return BadRequest(new { error = "Passage content cannot be empty." });

        var passage = await db.Passages.SingleOrDefaultAsync(
            item => item.ExamPart.ExamId == examId && item.SourceId == sourceId,
            cancellationToken);
        if (passage is null)
            return NotFound();
        passage.Content = request.Content.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("admin/exams/{examId:int}/analytics")]
    public async Task<ActionResult<ExamAnalyticsDto>> Analytics(int examId, CancellationToken cancellationToken)
    {
        if (!await db.Exams.AnyAsync(item => item.Id == examId, cancellationToken))
            return NotFound();
        return Ok(await analytics.GetAsync(examId, cancellationToken));
    }

    private static Exam ToEntity(ParsedExam parsed) => new()
    {
        Title = parsed.Title,
        DurationMinutes = parsed.DurationMinutes,
        Parts = parsed.Parts.Select(part =>
        {
            var passageEntities = part.Passages.Select(passage => new Passage
            {
                SourceId = passage.SourceId,
                Content = passage.Content,
                ShuffleQuestions = passage.ShuffleQuestions,
                ShuffleAnswers = passage.ShuffleAnswers
            }).ToArray();
            var questions = part.Questions
                .Concat(part.Passages.SelectMany(passage => passage.Questions))
                .Select(ToEntity)
                .ToDictionary(question => question.Number);
            foreach (var passage in part.Passages)
            {
                var entity = passageEntities.Single(item => item.SourceId == passage.SourceId);
                entity.Questions = passage.Questions.Select(question => questions[question.Number]).ToList();
            }
            return new ExamPart
            {
                Number = part.Number,
                ShuffleQuestions = part.ShuffleQuestions,
                ShuffleAnswers = part.ShuffleAnswers,
                Questions = questions.Values.ToList(),
                Passages = passageEntities.ToList()
            };
        }).ToList()
    };

    private static ExamQuestion ToEntity(ParsedQuestion question) => new()
    {
        Number = question.Number,
        Prompt = question.Prompt,
        CorrectChoiceKey = question.Answer,
        Hint = question.Hint,
        ShuffleChoices = question.ShuffleChoices,
        Choices = question.Choices.Select((choice, index) => new AnswerChoice
        {
            Key = choice.Key,
            Content = choice.Content,
            Position = index
        }).ToList()
    };

    private static int CountQuestions(ParsedExam exam) =>
        exam.Parts.Sum(part => part.Questions.Count + part.Passages.Sum(passage => passage.Questions.Count));

    private static bool CanEdit(Exam exam, out string error)
    {
        if (exam.IsPublished)
        {
            error = "Unpublish the exam before editing it.";
            return false;
        }
        if (exam.Attempts.Count != 0)
        {
            error = "An exam with attempts cannot be edited; create a new draft to preserve results.";
            return false;
        }
        error = string.Empty;
        return true;
    }
}

public sealed record ExamSummaryDto(
    int Id,
    string Title,
    int DurationMinutes,
    int QuestionCount,
    bool IsPublished);
public sealed record StudentChoiceDto(string Key, string Content);
public sealed record StudentQuestionDto(
    int Number,
    string Prompt,
    bool ShuffleChoices,
    IReadOnlyList<StudentChoiceDto> Choices)
{
    public static StudentQuestionDto From(ExamQuestion question, bool shuffleChoices = false) => new(
        question.Number,
        question.Prompt,
        question.ShuffleChoices || shuffleChoices,
        ExamShuffle.WhenEnabled(
            question.Choices.OrderBy(choice => choice.Position).ToArray(),
            question.ShuffleChoices || shuffleChoices)
            .Select(choice => new StudentChoiceDto(choice.Key, choice.Content))
            .ToArray());
}

public sealed record StudentPassageDto(
    string SourceId,
    string Content,
    bool ShuffleQuestions,
    bool ShuffleAnswers,
    IReadOnlyList<StudentQuestionDto> Questions)
{
    public static StudentPassageDto From(Passage passage, bool shuffleAnswers = false) => new(
        passage.SourceId,
        passage.Content,
        passage.ShuffleQuestions,
        passage.ShuffleAnswers,
        ExamShuffle.WhenEnabled(
                passage.Questions.OrderBy(question => question.Number).ToArray(),
                passage.ShuffleQuestions)
            .Select(question => StudentQuestionDto.From(
                question,
                shuffleAnswers || passage.ShuffleAnswers))
            .ToArray());
}

public sealed record StudentPartDto(
    int Number,
    bool ShuffleQuestions,
    bool ShuffleAnswers,
    IReadOnlyList<int> QuestionOrder,
    IReadOnlyList<StudentQuestionDto> Questions,
    IReadOnlyList<StudentPassageDto> Passages)
{
    public static StudentPartDto From(ExamPart part)
    {
        var directQuestions = part.Questions
            .Where(question => question.PassageId is null)
            .OrderBy(question => question.Number)
            .ToArray();
        var passages = part.Passages
            .OrderBy(passage => passage.Id)
            .ToArray();
        var blocks = directQuestions
            .Select(question => new[] { question.Number })
            .Concat(passages.Select(passage =>
                ExamShuffle.WhenEnabled(
                        passage.Questions.OrderBy(question => question.Number).ToArray(),
                        passage.ShuffleQuestions)
                    .Select(question => question.Number)
                    .ToArray()))
            .ToArray();
        var questionOrder = ExamShuffle.WhenEnabled(blocks, part.ShuffleQuestions)
            .SelectMany(block => block)
            .ToArray();
        return new StudentPartDto(
            part.Number,
            part.ShuffleQuestions,
            part.ShuffleAnswers,
            questionOrder,
            directQuestions
                .Select(question => StudentQuestionDto.From(question, part.ShuffleAnswers))
                .ToArray(),
            ExamShuffle.WhenEnabled(passages, part.ShuffleQuestions)
                .Select(passage => StudentPassageDto.From(passage, part.ShuffleAnswers))
                .ToArray());
    }
}

internal static class ExamShuffle
{
    public static IReadOnlyList<T> WhenEnabled<T>(IReadOnlyList<T> source, bool enabled)
    {
        if (!enabled || source.Count < 2)
            return source;

        var shuffled = source.ToArray();
        for (var index = shuffled.Length - 1; index > 0; index--)
        {
            var other = Random.Shared.Next(index + 1);
            (shuffled[index], shuffled[other]) = (shuffled[other], shuffled[index]);
        }

        if (shuffled.SequenceEqual(source))
            Array.Reverse(shuffled);
        return shuffled;
    }
}

public sealed record StudentExamDto(int Id, string Title, int DurationMinutes, IReadOnlyList<StudentPartDto> Parts)
{
    public static StudentExamDto From(Exam exam) => new(
        exam.Id,
        exam.Title,
        exam.DurationMinutes,
        exam.Parts.OrderBy(part => part.Number).Select(StudentPartDto.From).ToArray());
}

public sealed record TeacherQuestionDto(
    int Number,
    string Prompt,
    string CorrectChoiceKey,
    string Hint,
    IReadOnlyList<StudentChoiceDto> Choices);

public sealed record TeacherPassageDto(
    string SourceId,
    string Content,
    IReadOnlyList<TeacherQuestionDto> Questions);

public sealed record TeacherPartDto(
    int Number,
    IReadOnlyList<TeacherQuestionDto> Questions,
    IReadOnlyList<TeacherPassageDto> Passages);

public sealed record TeacherExamDto(int Id, string Title, int DurationMinutes, bool IsPublished, IReadOnlyList<TeacherPartDto> Parts)
{
    public static TeacherExamDto From(Exam exam) => new(
        exam.Id,
        exam.Title,
        exam.DurationMinutes,
        exam.IsPublished,
        exam.Parts.OrderBy(part => part.Number).Select(part => new TeacherPartDto(
            part.Number,
            part.Questions.Where(question => question.PassageId is null)
                .OrderBy(question => question.Number).Select(ToTeacherQuestion).ToArray(),
            part.Passages.OrderBy(passage => passage.Id).Select(passage => new TeacherPassageDto(
                passage.SourceId,
                passage.Content,
                passage.Questions.OrderBy(question => question.Number).Select(ToTeacherQuestion).ToArray())).ToArray())).ToArray());

    private static TeacherQuestionDto ToTeacherQuestion(ExamQuestion question) => new(
        question.Number,
        question.Prompt,
        question.CorrectChoiceKey,
        question.Hint,
        question.Choices.OrderBy(choice => choice.Position)
            .Select(choice => new StudentChoiceDto(choice.Key, choice.Content))
            .ToArray());
}

public sealed record PublishExamRequest(bool IsPublished);
public sealed record UpdateExamRequest(string Title, int DurationMinutes);
public sealed record UpdateChoiceRequest(string Key, string Content);
public sealed record UpdateQuestionRequest(
    string Prompt,
    string CorrectChoiceKey,
    string Hint,
    IReadOnlyList<UpdateChoiceRequest> Choices);
public sealed record UpdatePassageRequest(string Content);
