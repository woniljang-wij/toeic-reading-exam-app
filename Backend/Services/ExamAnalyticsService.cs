using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend;

public sealed record ExamAnalyticsDto(
    int AttemptCount,
    double? AverageScore,
    IReadOnlyList<MostMissedQuestionDto> MostMissedQuestions);

public sealed record MostMissedQuestionDto(int Number, string Prompt, int IncorrectCount);

public sealed class ExamAnalyticsService(ToeicDbContext db)
{
    public async Task<ExamAnalyticsDto> GetAsync(int examId, CancellationToken cancellationToken)
    {
        var attempts = await db.Attempts
            .Where(attempt => attempt.ExamId == examId)
            .Select(attempt => new
            {
                IsSubmitted = attempt.SubmittedAtUtc != null,
                Score = attempt.Answers.Count(answer => answer.IsCorrect)
            })
            .ToListAsync(cancellationToken);
        var completedAttempts = attempts.Where(attempt => attempt.IsSubmitted).ToArray();

        var mostMissed = await db.AttemptAnswers
            .Where(answer => answer.ExamAttempt.ExamId == examId &&
                             answer.ExamAttempt.SubmittedAtUtc != null &&
                             answer.SelectedChoiceKey != null &&
                             !answer.IsCorrect)
            .GroupBy(answer => new { answer.ExamQuestion.Number, answer.ExamQuestion.Prompt })
            .Select(group => new MostMissedQuestionDto(
                group.Key.Number,
                group.Key.Prompt,
                group.Count()))
            .OrderByDescending(question => question.IncorrectCount)
            .ThenBy(question => question.Number)
            .Take(10)
            .ToListAsync(cancellationToken);

        return new ExamAnalyticsDto(
            attempts.Count,
            completedAttempts.Length == 0 ? null : completedAttempts.Average(attempt => attempt.Score),
            mostMissed);
    }
}
