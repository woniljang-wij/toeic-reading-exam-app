namespace Backend.Models;

public sealed class Exam
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAtUtc { get; set; }
    public List<ExamPart> Parts { get; set; } = [];
    public List<ExamAttempt> Attempts { get; set; } = [];
}

public sealed class ExamPart
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;
    public int Number { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleAnswers { get; set; }
    public List<Passage> Passages { get; set; } = [];
    public List<ExamQuestion> Questions { get; set; } = [];
}

public sealed class Passage
{
    public int Id { get; set; }
    public int ExamPartId { get; set; }
    public ExamPart ExamPart { get; set; } = null!;
    public required string SourceId { get; set; }
    public required string Content { get; set; }
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleAnswers { get; set; }
    public List<ExamQuestion> Questions { get; set; } = [];
}

public sealed class ExamQuestion
{
    public int Id { get; set; }
    public int ExamPartId { get; set; }
    public ExamPart ExamPart { get; set; } = null!;
    public int? PassageId { get; set; }
    public Passage? Passage { get; set; }
    public int Number { get; set; }
    public required string Prompt { get; set; }
    public required string CorrectChoiceKey { get; set; }
    public required string Hint { get; set; }
    public bool ShuffleChoices { get; set; }
    public List<AnswerChoice> Choices { get; set; } = [];
}

public sealed class AnswerChoice
{
    public int Id { get; set; }
    public int ExamQuestionId { get; set; }
    public ExamQuestion ExamQuestion { get; set; } = null!;
    public required string Key { get; set; }
    public required string Content { get; set; }
    public int Position { get; set; }
}

public sealed class ExamAttempt
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public Exam Exam { get; set; } = null!;
    public required string CandidateId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public List<AttemptAnswer> Answers { get; set; } = [];
}

public sealed class AttemptAnswer
{
    public int Id { get; set; }
    public int ExamAttemptId { get; set; }
    public ExamAttempt ExamAttempt { get; set; } = null!;
    public int ExamQuestionId { get; set; }
    public ExamQuestion ExamQuestion { get; set; } = null!;
    public string? SelectedChoiceKey { get; set; }
    public bool IsCorrect { get; set; }
}
