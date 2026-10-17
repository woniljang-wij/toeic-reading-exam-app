class ExamSummary {
  const ExamSummary({
    required this.id,
    required this.title,
    required this.durationMinutes,
    required this.questionCount,
  });

  final int id;
  final String title;
  final int durationMinutes;
  final int questionCount;

  factory ExamSummary.fromJson(Map<String, dynamic> json) => ExamSummary(
        id: json['id'] as int,
        title: json['title'] as String,
        durationMinutes: json['durationMinutes'] as int,
        questionCount: json['questionCount'] as int,
      );
}

class StudentExam {
  const StudentExam({
    required this.id,
    required this.title,
    required this.durationMinutes,
    required this.questions,
  });

  final int id;
  final String title;
  final int durationMinutes;
  final List<ExamQuestion> questions;

  factory StudentExam.fromJson(Map<String, dynamic> json) {
    final questions = <ExamQuestion>[];
    for (final part in (json['parts'] as List<dynamic>)) {
      final partJson = part as Map<String, dynamic>;
      final partQuestions = <int, ExamQuestion>{};
      for (final question in (partJson['questions'] as List<dynamic>)) {
        final parsed = ExamQuestion.fromJson(
          question as Map<String, dynamic>,
          part: partJson['number'] as int,
        );
        partQuestions[parsed.number] = parsed;
      }
      for (final passage in (partJson['passages'] as List<dynamic>)) {
        final passageJson = passage as Map<String, dynamic>;
        for (final question in (passageJson['questions'] as List<dynamic>)) {
          final parsed = ExamQuestion.fromJson(
            question as Map<String, dynamic>,
            part: partJson['number'] as int,
            passage: passageJson['content'] as String,
          );
          partQuestions[parsed.number] = parsed;
        }
      }
      final questionOrder = partJson['questionOrder'] as List<dynamic>?;
      if (questionOrder == null || questionOrder.isEmpty) {
        questions.addAll(partQuestions.values);
      } else {
        for (final number in questionOrder.cast<int>()) {
          final question = partQuestions.remove(number);
          if (question == null) {
            throw FormatException(
              'Question order refers to missing or duplicate question $number.',
            );
          }
          questions.add(question);
        }
        if (partQuestions.isNotEmpty) {
          throw FormatException(
            'Question order is missing questions ${partQuestions.keys.join(', ')}.',
          );
        }
      }
    }
    return StudentExam(
      id: json['id'] as int,
      title: json['title'] as String,
      durationMinutes: json['durationMinutes'] as int,
      questions: questions,
    );
  }
}

class ExamQuestion {
  const ExamQuestion({
    required this.number,
    required this.part,
    required this.prompt,
    required this.choices,
    this.passage,
  });

  final int number;
  final int part;
  final String prompt;
  final String? passage;
  final List<AnswerChoice> choices;

  factory ExamQuestion.fromJson(
    Map<String, dynamic> json, {
    required int part,
    String? passage,
  }) =>
      ExamQuestion(
        number: json['number'] as int,
        part: part,
        prompt: json['prompt'] as String,
        passage: passage,
        choices: (json['choices'] as List<dynamic>)
            .map((choice) =>
                AnswerChoice.fromJson(choice as Map<String, dynamic>))
            .toList(),
      );
}

class AnswerChoice {
  const AnswerChoice({required this.key, required this.content});

  final String key;
  final String content;

  factory AnswerChoice.fromJson(Map<String, dynamic> json) => AnswerChoice(
        key: json['key'] as String,
        content: json['content'] as String,
      );
}

class AttemptStart {
  const AttemptStart({
    required this.id,
    required this.startedAt,
    required this.deadline,
  });

  final int id;
  final DateTime startedAt;
  final DateTime deadline;

  factory AttemptStart.fromJson(Map<String, dynamic> json) => AttemptStart(
        id: json['attemptId'] as int,
        startedAt: DateTime.parse(json['startedAtUtc'] as String).toUtc(),
        deadline: DateTime.parse(json['deadlineUtc'] as String).toUtc(),
      );
}

class AttemptResult {
  const AttemptResult({
    required this.title,
    required this.correct,
    required this.skipped,
    required this.incorrect,
    required this.total,
    required this.answers,
  });

  final String title;
  final int correct;
  final int skipped;
  final int incorrect;
  final int total;
  final List<ResultAnswer> answers;

  factory AttemptResult.fromJson(Map<String, dynamic> json) => AttemptResult(
        title: json['examTitle'] as String? ?? 'Your result',
        correct: json['correct'] as int,
        skipped: json['skipped'] as int,
        incorrect: json['incorrect'] as int,
        total: json['total'] as int,
        answers: (json['answers'] as List<dynamic>? ?? [])
            .map((answer) =>
                ResultAnswer.fromJson(answer as Map<String, dynamic>))
            .toList(),
      );
}

class ResultAnswer {
  const ResultAnswer({
    required this.number,
    required this.prompt,
    required this.selectedChoiceKey,
    required this.correctChoiceKey,
    required this.selectedChoiceContent,
    required this.correctChoiceContent,
    required this.hint,
    required this.isCorrect,
  });

  final int number;
  final String prompt;
  final String? selectedChoiceKey;
  final String correctChoiceKey;
  final String? selectedChoiceContent;
  final String? correctChoiceContent;
  final String hint;
  final bool isCorrect;

  factory ResultAnswer.fromJson(Map<String, dynamic> json) => ResultAnswer(
        number: json['number'] as int,
        prompt: json['prompt'] as String,
        selectedChoiceKey: json['selectedChoiceKey'] as String?,
        correctChoiceKey: json['correctChoiceKey'] as String,
        selectedChoiceContent: json['selectedChoiceContent'] as String?,
        correctChoiceContent: json['correctChoiceContent'] as String?,
        hint: json['hint'] as String,
        isCorrect: json['isCorrect'] as bool,
      );
}
