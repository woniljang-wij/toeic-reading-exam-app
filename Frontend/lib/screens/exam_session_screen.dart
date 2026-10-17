import 'dart:async';

import 'package:flutter/material.dart';

import '../app_language.dart';
import '../models/exam.dart';
import '../services/exam_api.dart';
import '../widgets/exam_content.dart';
import 'result_screen.dart';

class ExamSessionScreen extends StatefulWidget {
  const ExamSessionScreen({
    required this.api,
    required this.exam,
    required this.attempt,
    super.key,
  });

  final ExamApi api;
  final StudentExam exam;
  final AttemptStart attempt;

  @override
  State<ExamSessionScreen> createState() => _ExamSessionScreenState();
}

class _ExamSessionScreenState extends State<ExamSessionScreen> {
  final Map<int, String?> _answers = {};
  Timer? _clock;
  Timer? _autosaveTimer;
  int _questionIndex = 0;
  Duration _remaining = Duration.zero;
  bool _saving = false;
  bool _submitting = false;
  bool _warnedAboutTime = false;

  ExamQuestion get _question => widget.exam.questions[_questionIndex];

  int get _answeredCount =>
      _answers.values.where((choice) => choice != null).length;

  @override
  void initState() {
    super.initState();
    _updateRemaining();
    _clock = Timer.periodic(const Duration(seconds: 1), (_) => _tick());
    _autosaveTimer = Timer.periodic(
      const Duration(seconds: 12),
      (_) => _save(showMessage: false),
    );
  }

  @override
  void dispose() {
    _clock?.cancel();
    _autosaveTimer?.cancel();
    super.dispose();
  }

  void _tick() {
    _updateRemaining();
    if (_remaining <= Duration.zero) {
      _submit(fromTimer: true);
      return;
    }
    if (!_warnedAboutTime && _remaining <= const Duration(minutes: 5)) {
      _warnedAboutTime = true;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(AppLanguageScope.text(
            context,
            english: '5 minutes remaining. Review your unanswered questions.',
            vietnamese: 'Còn 5 phút. Hãy kiểm tra các câu chưa trả lời.',
          )),
          behavior: SnackBarBehavior.floating,
        ),
      );
    }
  }

  void _updateRemaining() {
    final remaining = widget.attempt.deadline.difference(DateTime.now().toUtc());
    if (!mounted) {
      _remaining = remaining.isNegative ? Duration.zero : remaining;
      return;
    }
    setState(() {
      _remaining = remaining.isNegative ? Duration.zero : remaining;
    });
  }

  Future<void> _save({required bool showMessage}) async {
    if (_saving || _submitting || _answers.isEmpty) return;
    setState(() => _saving = true);
    try {
      await widget.api.saveAnswers(widget.attempt.id, _answers);
      if (showMessage && mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(AppLanguageScope.text(context,
                english: 'Your answers are saved.',
                vietnamese: 'Đã lưu câu trả lời.')),
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } on ExamApiException catch (error) {
      if (showMessage && mounted) _showError(error.message);
    } catch (_) {
      if (showMessage && mounted) {
        _showError(AppLanguageScope.text(
          context,
          english: 'Answers could not be saved. Check your connection.',
          vietnamese: 'Không thể lưu câu trả lời. Hãy kiểm tra kết nối.',
        ));
      }
    } finally {
      if (mounted) setState(() => _saving = false);
    }
  }

  Future<void> _confirmSubmit() async {
    if (_submitting) return;
    final unanswered = widget.exam.questions.length - _answeredCount;
    final shouldSubmit = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(unanswered == 0
            ? AppLanguageScope.text(context,
                english: 'Submit your exam?', vietnamese: 'Nộp bài thi?')
            : AppLanguageScope.text(context,
                english: 'Almost there', vietnamese: 'Sắp hoàn thành')),
        content: Text(
          unanswered == 0
              ? AppLanguageScope.text(
                  context,
                  english: 'You answered every question. Submit now?',
                  vietnamese: 'Bạn đã trả lời tất cả câu hỏi. Nộp bài ngay?',
                )
              : AppLanguageScope.text(
                  context,
                  english: '$unanswered ${unanswered == 1 ? 'question is' : 'questions are'} '
                      'still unanswered. You can return to them or submit now.',
                  vietnamese:
                      'Còn $unanswered câu chưa trả lời. Bạn có thể quay lại '
                      'làm tiếp hoặc nộp bài ngay.',
                ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text(AppLanguageScope.text(context,
                english: 'Review answers', vietnamese: 'Xem lại bài')),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text(AppLanguageScope.text(context,
                english: 'Submit exam', vietnamese: 'Nộp bài thi')),
          ),
        ],
      ),
    );
    if (shouldSubmit == true) await _submit(fromTimer: false);
  }

  Future<void> _submit({required bool fromTimer}) async {
    if (_submitting) return;
    setState(() => _submitting = true);
    _clock?.cancel();
    _autosaveTimer?.cancel();
    try {
      try {
        await widget.api.submit(widget.attempt.id, _answers);
      } on ExamApiException {
        if (!fromTimer && DateTime.now().toUtc().isBefore(widget.attempt.deadline)) {
          rethrow;
        }
      }
      final result = await widget.api.getResult(widget.attempt.id);
      if (!mounted) return;
      await Navigator.of(context).pushReplacement(
        MaterialPageRoute<void>(
          builder: (_) => ResultScreen(result: result),
        ),
      );
    } on ExamApiException catch (error) {
      if (mounted) {
        _showError(error.message);
        setState(() => _submitting = false);
      }
    } catch (_) {
      if (mounted) {
        _showError(AppLanguageScope.text(
          context,
          english: 'Your exam could not be submitted. Please check your connection.',
          vietnamese: 'Không thể nộp bài. Hãy kiểm tra kết nối.',
        ));
        setState(() => _submitting = false);
      }
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), behavior: SnackBarBehavior.floating),
    );
  }

  void _moveTo(int index) {
    if (index < 0 || index >= widget.exam.questions.length) return;
    setState(() => _questionIndex = index);
    Navigator.pop(context);
  }

  Future<void> _openQuestionPicker() async {
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (context) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(18, 4, 18, 20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                AppLanguageScope.text(context,
                    english: 'Question navigator',
                    vietnamese: 'Danh sách câu hỏi'),
                style: TextStyle(fontSize: 17, fontWeight: FontWeight.w800),
              ),
              const SizedBox(height: 6),
              Text(
                AppLanguageScope.text(context,
                    english: 'Tap a number to jump to that question.',
                    vietnamese: 'Chạm vào số để chuyển đến câu hỏi.'),
                style: TextStyle(color: Color(0xFF625D67), fontSize: 12),
              ),
              const SizedBox(height: 15),
              SizedBox(
                height: MediaQuery.sizeOf(context).height * .48,
                child: GridView.builder(
                  itemCount: widget.exam.questions.length,
                  gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: 6,
                    mainAxisSpacing: 8,
                    crossAxisSpacing: 8,
                    childAspectRatio: 1.25,
                  ),
                  itemBuilder: (context, index) {
                    final question = widget.exam.questions[index];
                    final selected = _answers[question.number] != null;
                    final current = index == _questionIndex;
                    return OutlinedButton(
                      onPressed: () => _moveTo(index),
                      style: OutlinedButton.styleFrom(
                        padding: EdgeInsets.zero,
                        foregroundColor: current
                            ? Colors.white
                            : const Color(0xFF49454F),
                        backgroundColor: current
                            ? const Color(0xFF6750A4)
                            : selected
                                ? const Color(0xFFEAF8F2)
                                : Colors.white,
                        side: BorderSide(
                          color: current
                              ? const Color(0xFF6750A4)
                              : selected
                                  ? const Color(0xFF9AD8BF)
                                  : const Color(0xFFE7E0EC),
                        ),
                      ),
                      child: Text('${question.number}'),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  String get _timerText {
    final minutes = _remaining.inMinutes.remainder(60).toString().padLeft(2, '0');
    final seconds = _remaining.inSeconds.remainder(60).toString().padLeft(2, '0');
    final hours = _remaining.inHours;
    return hours > 0 ? '$hours:$minutes:$seconds' : '$minutes:$seconds';
  }

  @override
  Widget build(BuildContext context) {
    final progress = (_questionIndex + 1) / widget.exam.questions.length;
    final timerColor = _remaining <= const Duration(minutes: 5)
        ? const Color(0xFFD34F57)
        : const Color(0xFF1C1B1F);

    return PopScope(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop && !_submitting) _confirmSubmit();
      },
      child: Scaffold(
        appBar: AppBar(
          leading: IconButton(
            tooltip: AppLanguageScope.text(context,
                english: 'Submit exam', vietnamese: 'Nộp bài thi'),
            onPressed: _confirmSubmit,
            icon: const Icon(Icons.close_rounded),
          ),
          title: Text(
            widget.exam.title,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w700),
          ),
          actions: [
            const AppLanguageButton(),
            TextButton.icon(
              onPressed: _openQuestionPicker,
              icon: const Icon(Icons.grid_view_rounded, size: 17),
              label: Text(AppLanguageScope.text(context,
                  english: 'Questions', vietnamese: 'Câu hỏi')),
            ),
          ],
        ),
        body: SafeArea(
          child: Column(
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(20, 1, 20, 12),
                child: Column(
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: ClipRRect(
                            borderRadius: BorderRadius.circular(6),
                            child: LinearProgressIndicator(
                              value: progress,
                              minHeight: 6,
                              backgroundColor: const Color(0xFFE7E0EC),
                              color: const Color(0xFF6750A4),
                            ),
                          ),
                        ),
                        const SizedBox(width: 13),
                        Text(
                          '${_questionIndex + 1} / ${widget.exam.questions.length}',
                          style: const TextStyle(
                            color: Color(0xFF625D67),
                            fontSize: 11,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    Row(
                      children: [
                        const Icon(Icons.timer_outlined,
                            color: Color(0xFF625D67), size: 18),
                        const SizedBox(width: 7),
                        Text(
                          AppLanguageScope.text(context,
                              english: 'TIME LEFT', vietnamese: 'THỜI GIAN CÒN'),
                          style: TextStyle(
                            color: timerColor.withValues(alpha: .76),
                            fontWeight: FontWeight.w700,
                            fontSize: 9,
                            letterSpacing: 1.1,
                          ),
                        ),
                        const Spacer(),
                        Text(
                          _timerText,
                          style: TextStyle(
                            color: timerColor,
                            fontFeatures: const [FontFeature.tabularFigures()],
                            fontWeight: FontWeight.w800,
                            fontSize: 17,
                          ),
                        ),
                        const SizedBox(width: 12),
                        Text(
                          AppLanguageScope.text(
                            context,
                            english: '$_answeredCount answered',
                            vietnamese: '$_answeredCount đã trả lời',
                          ),
                          style: const TextStyle(
                            color: Color(0xFF625D67),
                            fontSize: 10,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              const Divider(height: 1),
              Expanded(
                child: SingleChildScrollView(
                  key: ValueKey(_question.number),
                  padding: const EdgeInsets.fromLTRB(20, 19, 20, 20),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      _PartPill(part: _question.part),
                      const SizedBox(height: 13),
                      if (_question.passage != null) ...[
                        _PassageCard(content: _question.passage!),
                        const SizedBox(height: 18),
                      ],
                      Text(
                        AppLanguageScope.text(
                          context,
                          english: 'Question ${_question.number}',
                          vietnamese: 'Câu ${_question.number}',
                        ),
                        style: const TextStyle(
                          fontWeight: FontWeight.w800,
                          fontSize: 13,
                          color: Color(0xFF625D67),
                        ),
                      ),
                      const SizedBox(height: 9),
                      ExamContent(
                        _question.prompt,
                        textStyle: const TextStyle(
                          fontWeight: FontWeight.w600,
                          fontSize: 17,
                          height: 1.5,
                          color: Color(0xFF1C1B1F),
                        ),
                      ),
                      const SizedBox(height: 17),
                      ..._question.choices.map(
                        (choice) => _ChoiceTile(
                          choice: choice,
                          selected: _answers[_question.number] == choice.key,
                          onTap: () => setState(
                            () => _answers[_question.number] = choice.key,
                          ),
                        ),
                      ),
                      if (_answers[_question.number] != null)
                        Align(
                          alignment: Alignment.centerRight,
                          child: TextButton.icon(
                            onPressed: () => setState(
                              () => _answers[_question.number] = null,
                            ),
                            icon: const Icon(Icons.undo_rounded, size: 16),
                            label: Text(AppLanguageScope.text(context,
                                english: 'Clear answer',
                                vietnamese: 'Xóa câu trả lời')),
                          ),
                        ),
                    ],
                  ),
                ),
              ),
              SafeArea(
                top: false,
                child: Container(
                  padding: const EdgeInsets.fromLTRB(18, 12, 18, 14),
                  decoration: const BoxDecoration(
                    color: Colors.white,
                    border: Border(top: BorderSide(color: Color(0xFFE7E0EC))),
                  ),
                  child: Row(
                    children: [
                      OutlinedButton(
                        onPressed: _questionIndex == 0
                            ? null
                            : () => setState(() => _questionIndex--),
                        style: OutlinedButton.styleFrom(
                          minimumSize: const Size(46, 48),
                          padding: EdgeInsets.zero,
                        ),
                        child: const Icon(Icons.arrow_back_rounded, size: 18),
                      ),
                      const SizedBox(width: 9),
                      Expanded(
                        child: OutlinedButton.icon(
                          onPressed: _saving ? null : () => _save(showMessage: true),
                          icon: Icon(
                            _saving ? Icons.sync_rounded : Icons.bookmark_border,
                            size: 17,
                          ),
                          label: Text(_saving
                              ? AppLanguageScope.text(context,
                                  english: 'Saving…', vietnamese: 'Đang lưu…')
                              : AppLanguageScope.text(context,
                                  english: 'Save progress',
                                  vietnamese: 'Lưu tiến độ')),
                          style: OutlinedButton.styleFrom(
                            foregroundColor: const Color(0xFF625D67),
                            minimumSize: const Size(0, 48),
                          ),
                        ),
                      ),
                      const SizedBox(width: 9),
                      if (_questionIndex < widget.exam.questions.length - 1)
                        FilledButton(
                          onPressed: () => setState(() => _questionIndex++),
                          style: FilledButton.styleFrom(
                            minimumSize: const Size(50, 48),
                            padding: const EdgeInsets.symmetric(horizontal: 14),
                          ),
                          child: const Icon(Icons.arrow_forward_rounded, size: 19),
                        )
                      else
                        FilledButton(
                          onPressed: _submitting ? null : _confirmSubmit,
                          style: FilledButton.styleFrom(
                            minimumSize: const Size(88, 48),
                            padding: const EdgeInsets.symmetric(horizontal: 15),
                          ),
                          child: _submitting
                              ? const SizedBox(
                                  width: 18,
                                  height: 18,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: Colors.white,
                                  ),
                                )
                              : Text(AppLanguageScope.text(context,
                                  english: 'Submit', vietnamese: 'Nộp bài')),
                        ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _PartPill extends StatelessWidget {
  const _PartPill({required this.part});

  final int part;

  @override
  Widget build(BuildContext context) {
    final title = switch (part) {
      5 => AppLanguageScope.text(context,
          english: 'PART 5 · INCOMPLETE SENTENCES',
          vietnamese: 'PHẦN 5 · HOÀN THÀNH CÂU'),
      6 => AppLanguageScope.text(context,
          english: 'PART 6 · TEXT COMPLETION',
          vietnamese: 'PHẦN 6 · HOÀN THÀNH ĐOẠN VĂN'),
      _ => AppLanguageScope.text(context,
          english: 'PART 7 · READING COMPREHENSION',
          vietnamese: 'PHẦN 7 · ĐỌC HIỂU'),
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: const Color(0xFFEADDFF),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Text(
        title,
        style: const TextStyle(
          color: Color(0xFF6750A4),
          fontSize: 9,
          fontWeight: FontWeight.w800,
          letterSpacing: .55,
        ),
      ),
    );
  }
}

class _PassageCard extends StatelessWidget {
  const _PassageCard({required this.content});

  final String content;

  @override
  Widget build(BuildContext context) => Container(
        constraints: const BoxConstraints(maxHeight: 225),
        width: double.infinity,
        padding: const EdgeInsets.all(15),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: const Color(0xFFE7E0EC)),
        ),
        child: SingleChildScrollView(
          child:           ExamContent(
            content,
            textStyle: const TextStyle(
              color: Color(0xFF49454F),
              fontSize: 12,
              height: 1.65,
            ),
          ),
        ),
      );
}

class _ChoiceTile extends StatelessWidget {
  const _ChoiceTile({
    required this.choice,
    required this.selected,
    required this.onTap,
  });

  final AnswerChoice choice;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 9),
        child: Material(
          color: selected ? const Color(0xFFEADDFF) : Colors.white,
          borderRadius: BorderRadius.circular(13),
          child: InkWell(
            onTap: onTap,
            borderRadius: BorderRadius.circular(13),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
              decoration: BoxDecoration(
                borderRadius: BorderRadius.circular(13),
                border: Border.all(
                  color: selected
                      ? const Color(0xFF6750A4)
                      : const Color(0xFFE7E0EC),
                  width: selected ? 1.4 : 1,
                ),
              ),
              child: Row(
                children: [
                  Container(
                    width: 30,
                    height: 30,
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      color: selected
                          ? const Color(0xFF6750A4)
                          : const Color(0xFFF3EDF7),
                      borderRadius: BorderRadius.circular(9),
                    ),
                    child: Text(
                      choice.key,
                      style: TextStyle(
                        color: selected ? Colors.white : const Color(0xFF6C748B),
                        fontSize: 12,
                        fontWeight: FontWeight.w800,
                      ),
                    ),
                  ),
                  const SizedBox(width: 11),
                  Expanded(
                    child: ExamContent(
                      choice.content,
                      textStyle: const TextStyle(
                        color: Color(0xFF303750),
                        fontSize: 12,
                        height: 1.45,
                      ),
                    ),
                  ),
                  const SizedBox(width: 7),
                  Icon(
                    selected
                        ? Icons.check_circle_rounded
                        : Icons.circle_outlined,
                    size: 19,
                    color: selected
                        ? const Color(0xFF6750A4)
                        : const Color(0xFFD5D8E2),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
}
