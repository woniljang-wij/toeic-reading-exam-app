import 'package:flutter/material.dart';

import '../app_language.dart';
import '../models/exam.dart';
import '../widgets/exam_content.dart';

class ResultScreen extends StatelessWidget {
  const ResultScreen({required this.result, super.key});

  final AttemptResult result;

  static const _ink = Color(0xFF1C1B1F);
  static const _muted = Color(0xFF625D67);
  static const _primary = Color(0xFF6750A4);
  static const _outline = Color(0xFFE7E0EC);

  @override
  Widget build(BuildContext context) {
    final review = result.answers.isEmpty
        ? null
        : result.answers.firstWhere(
            (answer) => !answer.isCorrect,
            orElse: () => result.answers.first,
          );
    return Scaffold(
      appBar: AppBar(
        automaticallyImplyLeading: false,
        titleSpacing: 4,
        title: Row(
          children: [
            IconButton(
              onPressed: () =>
                  Navigator.of(context).popUntil((route) => route.isFirst),
              icon: const Icon(Icons.arrow_back_ios_new_rounded, size: 18),
              color: _primary,
            ),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    result.title,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: _ink,
                      fontSize: 16,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  Text(
                    AppLanguageScope.text(context,
                        english: 'TOEIC READING',
                        vietnamese: 'ĐỌC HIỂU TOEIC'),
                    style: TextStyle(
                      color: _primary,
                      fontSize: 10,
                      fontWeight: FontWeight.w700,
                      letterSpacing: .5,
                    ),
                  ),
                ],
              ),
            ),
            const AppLanguageButton(),
          ],
        ),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 24),
          children: [
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 22),
              decoration: BoxDecoration(
                color: _primary,
                borderRadius: BorderRadius.circular(18),
              ),
              child: Column(
                children: [
                  Text(
                    AppLanguageScope.text(context,
                        english: 'EXAM COMPLETED',
                        vietnamese: 'ĐÃ HOÀN THÀNH BÀI THI'),
                    style: TextStyle(
                      color: Color(0xFFEADDFF),
                      fontSize: 11,
                      fontWeight: FontWeight.w700,
                      letterSpacing: .4,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    '${result.correct} / ${result.total}',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 36,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  const SizedBox(height: 4),
                  Text(
                    AppLanguageScope.text(
                      context,
                      english: 'Good work — your result is ready.',
                      vietnamese: 'Làm tốt lắm — kết quả của bạn đã sẵn sàng.',
                    ),
                    style: TextStyle(color: Colors.white, fontSize: 14),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: _ResultCount(
                    label: AppLanguageScope.text(context,
                        english: 'CORRECT', vietnamese: 'ĐÚNG'),
                    value: result.correct,
                    color: const Color(0xFF2E7D5B),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: _ResultCount(
                    label: AppLanguageScope.text(context,
                        english: 'INCORRECT', vietnamese: 'SAI'),
                    value: result.incorrect,
                    color: const Color(0xFFB3261E),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: _ResultCount(
                    label: AppLanguageScope.text(context,
                        english: 'SKIPPED', vietnamese: 'BỎ QUA'),
                    value: result.skipped,
                    color: _muted,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.all(18),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(14),
                border: Border.all(color: _outline),
              ),
              child: review == null
                  ? Text(
                      AppLanguageScope.text(
                        context,
                        english: 'No answer details are available for review.',
                        vietnamese: 'Không có chi tiết câu trả lời để xem lại.',
                      ),
                      style: TextStyle(color: _muted, fontSize: 13),
                    )
                  : Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          AppLanguageScope.text(context,
                              english: 'Answer review',
                              vietnamese: 'Xem lại câu trả lời'),
                          style: TextStyle(
                            color: _ink,
                            fontSize: 17,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(height: 12),
                        Text(
                          AppLanguageScope.text(context,
                              english: 'Question ${review.number}',
                              vietnamese: 'Câu ${review.number}'),
                          style: const TextStyle(
                            color: _primary,
                            fontSize: 11,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        const SizedBox(height: 8),
                        ExamContent(
                          review.prompt,
                          textStyle: const TextStyle(
                            color: _ink,
                            fontSize: 14,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        const SizedBox(height: 12),
                        Text(
                          review.selectedChoiceKey == null
                              ? AppLanguageScope.text(context,
                                  english: 'Your answer  ·  Skipped',
                                  vietnamese: 'Câu trả lời của bạn  ·  Bỏ qua')
                              : '${AppLanguageScope.text(context, english: 'Your answer', vietnamese: 'Câu trả lời của bạn')}  ·  ${review.selectedChoiceKey}',
                          style: const TextStyle(color: _muted, fontSize: 12),
                        ),
                        if (review.selectedChoiceContent != null)
                          Padding(
                            padding: const EdgeInsets.only(top: 4),
                            child: ExamContent(
                              review.selectedChoiceContent!,
                              textStyle:
                                  const TextStyle(color: _muted, fontSize: 12),
                            ),
                          ),
                          const SizedBox(height: 8),
                          Text(
                            '${AppLanguageScope.text(context, english: 'Correct answer', vietnamese: 'Đáp án đúng')}  ·  ${review.correctChoiceKey}',
                          style: const TextStyle(
                            color: Color(0xFF2E7D5B),
                            fontSize: 12,
                            fontWeight: FontWeight.w700,
                          ),
                        ),
                        if (review.correctChoiceContent != null)
                          Padding(
                            padding: const EdgeInsets.only(top: 4),
                            child: ExamContent(
                              review.correctChoiceContent!,
                              textStyle: const TextStyle(
                                color: Color(0xFF2E7D5B),
                                fontSize: 12,
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          ),
                        const SizedBox(height: 8),
                        Text(
                          '${AppLanguageScope.text(context, english: 'Hint', vietnamese: 'Gợi ý')}: ${review.hint}',
                          style: const TextStyle(
                            color: _muted,
                            fontSize: 12,
                            height: 1.45,
                          ),
                        ),
                      ],
                    ),
            ),
            const SizedBox(height: 16),
            FilledButton(
              onPressed: () =>
                  Navigator.of(context).popUntil((route) => route.isFirst),
              child: Text(AppLanguageScope.text(context,
                  english: 'Back to practice',
                  vietnamese: 'Quay lại luyện thi')),
            ),
          ],
        ),
      ),
    );
  }
}

class _ResultCount extends StatelessWidget {
  const _ResultCount({
    required this.label,
    required this.value,
    required this.color,
  });

  final String label;
  final int value;
  final Color color;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.fromLTRB(12, 18, 12, 16),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: ResultScreen._outline),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              label,
              maxLines: 1,
              style: TextStyle(
                color: color,
                fontSize: 10,
                fontWeight: FontWeight.w700,
              ),
            ),
            const SizedBox(height: 9),
            Text(
              '$value',
              style: const TextStyle(
                color: ResultScreen._ink,
                fontSize: 23,
                fontWeight: FontWeight.w700,
              ),
            ),
          ],
        ),
      );
}
