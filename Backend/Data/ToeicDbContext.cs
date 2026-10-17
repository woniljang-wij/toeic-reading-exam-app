using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend;

public sealed class ToeicDbContext(DbContextOptions<ToeicDbContext> options) : DbContext(options)
{
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamPart> ExamParts => Set<ExamPart>();
    public DbSet<Passage> Passages => Set<Passage>();
    public DbSet<ExamQuestion> Questions => Set<ExamQuestion>();
    public DbSet<AnswerChoice> Choices => Set<AnswerChoice>();
    public DbSet<ExamAttempt> Attempts => Set<ExamAttempt>();
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Exam>().HasIndex(x => x.Title);
        modelBuilder.Entity<ExamPart>().HasIndex(x => new { x.ExamId, x.Number }).IsUnique();
        modelBuilder.Entity<Passage>().HasIndex(x => new { x.ExamPartId, x.SourceId }).IsUnique();
        modelBuilder.Entity<ExamQuestion>().HasIndex(x => new { x.ExamPartId, x.Number }).IsUnique();
        modelBuilder.Entity<AnswerChoice>().HasIndex(x => new { x.ExamQuestionId, x.Key }).IsUnique();
        modelBuilder.Entity<ExamAttempt>().HasIndex(x => new { x.ExamId, x.CandidateId });
        modelBuilder.Entity<AttemptAnswer>().HasIndex(x => new { x.ExamAttemptId, x.ExamQuestionId }).IsUnique();

        modelBuilder.Entity<ExamPart>()
            .HasOne(x => x.Exam)
            .WithMany(x => x.Parts)
            .HasForeignKey(x => x.ExamId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Passage>()
            .HasOne(x => x.ExamPart)
            .WithMany(x => x.Passages)
            .HasForeignKey(x => x.ExamPartId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ExamQuestion>()
            .HasOne(x => x.ExamPart)
            .WithMany(x => x.Questions)
            .HasForeignKey(x => x.ExamPartId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ExamQuestion>()
            .HasOne(x => x.Passage)
            .WithMany(x => x.Questions)
            .HasForeignKey(x => x.PassageId)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AnswerChoice>()
            .HasOne(x => x.ExamQuestion)
            .WithMany(x => x.Choices)
            .HasForeignKey(x => x.ExamQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ExamAttempt>()
            .HasOne(x => x.Exam)
            .WithMany(x => x.Attempts)
            .HasForeignKey(x => x.ExamId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AttemptAnswer>()
            .HasOne(x => x.ExamAttempt)
            .WithMany(x => x.Answers)
            .HasForeignKey(x => x.ExamAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<AttemptAnswer>()
            .HasOne(x => x.ExamQuestion)
            .WithMany()
            .HasForeignKey(x => x.ExamQuestionId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
