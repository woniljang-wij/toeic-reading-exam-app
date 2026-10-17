using Backend.Models;
using Abp.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api")]
[IgnoreAntiforgeryToken]
[DontWrapResult]
public sealed class AttemptsController(ToeicDbContext db) : ControllerBase
{
    [HttpPost("exams/{examId:int}/attempts")]
    public async Task<IActionResult> Start(
        int examId,
        [FromBody] StartAttemptRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CandidateId))
            return BadRequest(new { error = "A candidate identifier is required." });

        var exam = await db.Exams.SingleOrDefaultAsync(
            item => item.Id == examId && item.IsPublished,
            cancellationToken);
        if (exam is null)
            return NotFound();

        var attempt = new ExamAttempt
        {
            ExamId = exam.Id,
            CandidateId = request.CandidateId.Trim(),
            StartedAtUtc = DateTime.UtcNow
        };
        db.Attempts.Add(attempt);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new StartAttemptResponse(
            attempt.Id,
            attempt.StartedAtUtc,
            attempt.StartedAtUtc.AddMinutes(exam.DurationMinutes),
            exam.DurationMinutes));
    }

    [HttpPut("attempts/{attemptId:int}/answers")]
    public async Task<IActionResult> SaveAnswers(
        int attemptId,
        [FromBody] SaveAnswersRequest request,
        CancellationToken cancellationToken)
    {
        var attempt = await LoadOpenAttempt(attemptId, cancellationToken);
        if (attempt is null)
            return Conflict(new { error = "The attempt does not exist, has expired, or has already been submitted." });
        var error = await ValidateAnswers(attempt, request.Answers, cancellationToken);
        if (error is not null)
            return BadRequest(new { error });

        await UpsertAnswers(attempt, request.Answers, cancellationToken);
        return NoContent();
    }

    [HttpPost("attempts/{attemptId:int}/submit")]
    public async Task<IActionResult> Submit(
        int attemptId,
        [FromBody] SaveAnswersRequest request,
        CancellationToken cancellationToken)
    {
        var attempt = await LoadOpenAttempt(attemptId, cancellationToken);
        if (attempt is null)
            return Conflict(new { error = "The attempt does not exist, has expired, or has already been submitted." });
        var error = await ValidateAnswers(attempt, request.Answers, cancellationToken);
        if (error is not null)
            return BadRequest(new { error });

        await UpsertAnswers(attempt, request.Answers, cancellationToken);
        attempt.SubmittedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var correctCount = await db.AttemptAnswers
            .CountAsync(answer => answer.ExamAttemptId == attempt.Id && answer.IsCorrect, cancellationToken);
        var totalCount = await db.Questions.CountAsync(
            question => question.ExamPart.ExamId == attempt.ExamId,
            cancellationToken);
        var answeredCount = await db.AttemptAnswers
            .CountAsync(answer => answer.ExamAttemptId == attempt.Id && answer.SelectedChoiceKey != null, cancellationToken);
        return Ok(new AttemptResultDto(
            correctCount,
            totalCount - answeredCount,
            answeredCount - correctCount,
            totalCount));
    }

    [HttpGet("attempts/{attemptId:int}/result")]
    public async Task<IActionResult> Result(int attemptId, CancellationToken cancellationToken)
    {
        var attempt = await db.Attempts
            .Include(item => item.Exam)
            .SingleOrDefaultAsync(item => item.Id == attemptId, cancellationToken);
        if (attempt is null)
            return NotFound();
        if (attempt.SubmittedAtUtc is null)
            return Conflict(new { error = "The attempt has not been submitted." });

        var answerEntities = await db.AttemptAnswers
            .Where(answer => answer.ExamAttemptId == attempt.Id)
            .Include(answer => answer.ExamQuestion)
                .ThenInclude(question => question.Choices)
            .ToListAsync(cancellationToken);
        var answers = answerEntities
            .OrderBy(answer => answer.ExamQuestion.Number)
            .Select(answer => new ResultAnswerDto(
                answer.ExamQuestion.Number,
                answer.ExamQuestion.Prompt,
                answer.SelectedChoiceKey,
                answer.ExamQuestion.CorrectChoiceKey,
                answer.ExamQuestion.Choices
                    .SingleOrDefault(choice => choice.Key == answer.SelectedChoiceKey)?.Content,
                answer.ExamQuestion.Choices
                    .SingleOrDefault(choice => choice.Key == answer.ExamQuestion.CorrectChoiceKey)?.Content,
                answer.ExamQuestion.Hint,
                answer.IsCorrect))
            .ToArray();
        var total = await db.Questions.CountAsync(question => question.ExamPart.ExamId == attempt.ExamId, cancellationToken);
        var correct = answers.Count(answer => answer.IsCorrect);
        return Ok(new DetailedAttemptResultDto(
            attempt.Exam.Title,
            correct,
            total - answers.Count(answer => answer.SelectedChoiceKey is not null),
            answers.Count(answer => answer.SelectedChoiceKey is not null && !answer.IsCorrect),
            total,
            answers));
    }

    private async Task<ExamAttempt?> LoadOpenAttempt(int attemptId, CancellationToken cancellationToken)
    {
        var attempt = await db.Attempts
            .Include(item => item.Exam)
            .Include(item => item.Answers)
            .SingleOrDefaultAsync(item => item.Id == attemptId, cancellationToken);
        if (attempt is null || attempt.SubmittedAtUtc is not null)
            return null;
        if (DateTime.UtcNow < attempt.StartedAtUtc.AddMinutes(attempt.Exam.DurationMinutes))
            return attempt;

        attempt.SubmittedAtUtc = attempt.StartedAtUtc.AddMinutes(attempt.Exam.DurationMinutes);
        await db.SaveChangesAsync(cancellationToken);
        return null;
    }

    private async Task<string?> ValidateAnswers(
        ExamAttempt attempt,
        IReadOnlyList<SelectedAnswerDto> answers,
        CancellationToken cancellationToken)
    {
        if (answers is null)
            return "The answers list is required.";
        if (answers.Select(answer => answer.QuestionNumber).Distinct().Count() != answers.Count)
            return "A question may appear only once in the submission.";

        var questions = await db.Questions
            .Where(question => question.ExamPart.ExamId == attempt.ExamId)
            .Include(question => question.Choices)
            .ToDictionaryAsync(question => question.Number, cancellationToken);
        foreach (var answer in answers)
        {
            if (!questions.TryGetValue(answer.QuestionNumber, out var question))
                return $"Question {answer.QuestionNumber} does not belong to this exam.";
            if (answer.ChoiceKey is not null &&
                !question.Choices.Any(choice => choice.Key == answer.ChoiceKey))
                return $"Choice for question {answer.QuestionNumber} is invalid.";
        }
        return null;
    }

    private async Task UpsertAnswers(
        ExamAttempt attempt,
        IReadOnlyList<SelectedAnswerDto> answers,
        CancellationToken cancellationToken)
    {
        var questions = await db.Questions
            .Where(question => question.ExamPart.ExamId == attempt.ExamId)
            .ToDictionaryAsync(question => question.Number, cancellationToken);
        foreach (var answer in answers)
        {
            var stored = attempt.Answers.SingleOrDefault(
                existing => existing.ExamQuestionId == questions[answer.QuestionNumber].Id);
            if (stored is null)
            {
                stored = new AttemptAnswer
                {
                    ExamAttemptId = attempt.Id,
                    ExamQuestionId = questions[answer.QuestionNumber].Id
                };
                db.AttemptAnswers.Add(stored);
            }
            stored.SelectedChoiceKey = answer.ChoiceKey;
            stored.IsCorrect = answer.ChoiceKey is not null &&
                               answer.ChoiceKey == questions[answer.QuestionNumber].CorrectChoiceKey;
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed record StartAttemptRequest(string CandidateId);
public sealed record StartAttemptResponse(int AttemptId, DateTime StartedAtUtc, DateTime DeadlineUtc, int DurationMinutes);
public sealed record SelectedAnswerDto(int QuestionNumber, string? ChoiceKey);
public sealed record SaveAnswersRequest(IReadOnlyList<SelectedAnswerDto> Answers);
public sealed record AttemptResultDto(int Correct, int Skipped, int Incorrect, int Total);
public sealed record ResultAnswerDto(
    int Number,
    string Prompt,
    string? SelectedChoiceKey,
    string CorrectChoiceKey,
    string? SelectedChoiceContent,
    string? CorrectChoiceContent,
    string Hint,
    bool IsCorrect);
public sealed record DetailedAttemptResultDto(
    string ExamTitle,
    int Correct,
    int Skipped,
    int Incorrect,
    int Total,
    IReadOnlyList<ResultAnswerDto> Answers);
