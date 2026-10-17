import 'package:flutter/material.dart';

import '../app_language.dart';
import '../models/exam.dart';
import '../services/exam_api.dart';
import 'exam_session_screen.dart';

class ExamListScreen extends StatefulWidget {
  const ExamListScreen({super.key});

  @override
  State<ExamListScreen> createState() => _ExamListScreenState();
}

class _ExamListScreenState extends State<ExamListScreen> {
  final _api = ExamApi();
  late Future<List<ExamSummary>> _exams;

  @override
  void initState() {
    super.initState();
    _exams = _api.listExams();
  }

  @override
  void dispose() {
    _api.dispose();
    super.dispose();
  }

  void _reload() {
    setState(() => _exams = _api.listExams());
  }

  Future<void> _startExam(ExamSummary summary) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(AppLanguageScope.text(context,
            english: 'Ready to begin?', vietnamese: 'Sẵn sàng bắt đầu?')),
        content: Text(
          AppLanguageScope.text(
            context,
            english: '${summary.title} has ${summary.questionCount} questions. '
                'You will have ${summary.durationMinutes} minutes and the timer '
                'cannot be paused.',
            vietnamese:
                '${summary.title} có ${summary.questionCount} câu hỏi. '
                'Bạn có ${summary.durationMinutes} phút và không thể tạm dừng giờ làm bài.',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: Text(AppLanguageScope.text(context,
                english: 'Not yet', vietnamese: 'Chưa')),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: Text(AppLanguageScope.text(context,
                english: 'Start exam', vietnamese: 'Bắt đầu thi')),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    try {
      final exam = await _api.getExam(summary.id);
      final attempt = await _api.startAttempt(
        summary.id,
        'learner-${DateTime.now().microsecondsSinceEpoch}',
      );
      if (!mounted) return;
      await Navigator.of(context).push(
        MaterialPageRoute<void>(
          builder: (_) => ExamSessionScreen(
            api: _api,
            exam: exam,
            attempt: attempt,
          ),
        ),
      );
      if (mounted) _reload();
    } on ExamApiException catch (error) {
      if (mounted) _showError(error.message);
    } catch (_) {
      if (mounted) {
        _showError(AppLanguageScope.text(
          context,
          english:
              'The exam could not be opened. Check the server connection and try again.',
          vietnamese:
              'Không thể mở đề thi. Hãy kiểm tra kết nối máy chủ rồi thử lại.',
        ));
      }
    }
  }

  void _showError(String message) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), behavior: SnackBarBehavior.floating),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: () async => _reload(),
          child: ListView(
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 12),
            children: [
              SizedBox(
                height: 34,
                child: Row(
                  children: [
                    const Icon(
                      Icons.auto_stories_rounded,
                      color: Color(0xFF6750A4),
                      size: 26,
                    ),
                    const SizedBox(width: 8),
                    const Text(
                      'readwise',
                      style:
                          TextStyle(fontSize: 19, fontWeight: FontWeight.w800),
                    ),
                    const Spacer(),
                    const AppLanguageButton(),
                    SizedBox(
                      width: 34,
                      height: 34,
                      child: IconButton(
                        tooltip: 'Refresh exams',
                        onPressed: _reload,
                        padding: EdgeInsets.zero,
                        icon: const Icon(Icons.refresh_rounded, size: 22),
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 12),
              _WelcomeCard(onExplore: () {}),
              const SizedBox(height: 12),
              Row(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          AppLanguageScope.text(context,
                              english: 'Practice library',
                              vietnamese: 'Thư viện đề thi'),
                          style: TextStyle(
                            fontSize: 20,
                            fontWeight: FontWeight.w800,
                            letterSpacing: -0.5,
                          ),
                        ),
                        SizedBox(height: 4),
                        Text(
                          AppLanguageScope.text(
                            context,
                            english:
                                'Build your confidence, one passage at a time.',
                            vietnamese:
                                'Tự tin hơn qua từng bài đọc.',
                          ),
                          style: TextStyle(
                            color: Color(0xFF625D67),
                            fontSize: 14,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 11),
              FutureBuilder<List<ExamSummary>>(
                future: _exams,
                builder: (context, snapshot) {
                  if (snapshot.connectionState == ConnectionState.waiting) {
                    return const Padding(
                      padding: EdgeInsets.all(40),
                      child: Center(child: CircularProgressIndicator()),
                    );
                  }
                  if (snapshot.hasError) {
                    return _StateMessage(
                      icon: Icons.wifi_off_rounded,
                      title: AppLanguageScope.text(context,
                          english: 'Couldn’t load your exams',
                          vietnamese: 'Không tải được danh sách đề thi'),
                      message: snapshot.error is ExamApiException
                          ? (snapshot.error as ExamApiException).message
                          : AppLanguageScope.text(
                              context,
                              english:
                                  'Check your connection to the exam server, then retry.',
                              vietnamese:
                                  'Kiểm tra kết nối máy chủ đề thi rồi thử lại.',
                            ),
                      actionLabel: AppLanguageScope.text(context,
                          english: 'Try again', vietnamese: 'Thử lại'),
                      onAction: _reload,
                    );
                  }
                  final exams = snapshot.data ?? [];
                  if (exams.isEmpty) {
                    return _StateMessage(
                      icon: Icons.inbox_rounded,
                      title: AppLanguageScope.text(context,
                          english: 'No published exams yet',
                          vietnamese: 'Chưa có đề thi được đăng'),
                      message: AppLanguageScope.text(
                        context,
                        english:
                            'When your teacher publishes a Reading test, it will appear here.',
                        vietnamese:
                            'Đề thi Reading sẽ xuất hiện tại đây sau khi giảng viên đăng.',
                      ),
                    );
                  }
                  return Column(
                    children: List.generate(
                      exams.length,
                      (index) => Padding(
                        padding: EdgeInsets.only(
                          bottom: index == exams.length - 1 ? 0 : 12,
                        ),
                        child: _ExamCard(
                          exam: exams[index],
                          onStart: () => _startExam(exams[index]),
                        ),
                      ),
                    ),
                  );
                },
              ),
              const SizedBox(height: 12),
              const _TipCard(),
              const SizedBox(height: 12),
              NavigationBar(
                height: 50,
                selectedIndex: 0,
                destinations: [
                  NavigationDestination(
                    icon: Icon(Icons.home_outlined),
                    selectedIcon: Icon(Icons.home_rounded),
                    label: AppLanguageScope.text(context,
                        english: 'Practice', vietnamese: 'Luyện thi'),
                  ),
                  NavigationDestination(
                    icon: Icon(Icons.insights_outlined),
                    label: AppLanguageScope.text(context,
                        english: 'Progress', vietnamese: 'Tiến độ'),
                  ),
                  NavigationDestination(
                    icon: Icon(Icons.person_outline_rounded),
                    label: AppLanguageScope.text(context,
                        english: 'Profile', vietnamese: 'Hồ sơ'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _WelcomeCard extends StatelessWidget {
  const _WelcomeCard({required this.onExplore});

  final VoidCallback onExplore;

  @override
  Widget build(BuildContext context) => Container(
        height: 191,
        padding: const EdgeInsets.all(18),
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(20),
          gradient: const LinearGradient(
            colors: [Color(0xFF4F378B), Color(0xFF6750A4)],
            begin: Alignment.topLeft,
            end: Alignment.bottomRight,
          ),
          boxShadow: const [
            BoxShadow(
              color: Color(0x1D343C7F),
              blurRadius: 22,
              offset: Offset(0, 9),
            ),
          ],
        ),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Container(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: .12),
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Text(
                      AppLanguageScope.text(context,
                          english: 'YOUR NEXT MILESTONE',
                          vietnamese: 'MỤC TIÊU TIẾP THEO'),
                      style: TextStyle(
                        color: Color(0xFFD0BCFF),
                        fontSize: 9,
                        fontWeight: FontWeight.w700,
                        letterSpacing: 1.1,
                      ),
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    AppLanguageScope.text(context,
                        english: 'Small steps.\nStronger scores.',
                        vietnamese: 'Tiến bộ từng bước.\nĐiểm số tốt hơn.'),
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 22,
                      height: 1.25,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Text(
                    AppLanguageScope.text(
                      context,
                      english: 'A focused 75-minute session starts here.',
                      vietnamese: 'Bắt đầu 75 phút luyện thi tập trung.',
                    ),
                    style: TextStyle(color: Color(0xFFEADDFF), fontSize: 11),
                  ),
                  const SizedBox(height: 6),
                  FilledButton.tonal(
                    onPressed: onExplore,
                    style: FilledButton.styleFrom(
                      backgroundColor: Colors.white,
                      foregroundColor: const Color(0xFF6750A4),
                      minimumSize: const Size(115, 48),
                      tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                      padding: const EdgeInsets.symmetric(horizontal: 13),
                    ),
                    child: Text(
                      AppLanguageScope.text(context,
                          english: 'Explore tests', vietnamese: 'Xem đề thi'),
                      style: TextStyle(
                        fontWeight: FontWeight.w700,
                        fontSize: 11,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 0),
            const _HeroMark(),
          ],
        ),
      );
}

class _HeroMark extends StatelessWidget {
  const _HeroMark();

  @override
  Widget build(BuildContext context) => Container(
        width: 64,
        height: 80,
        decoration: BoxDecoration(
          color: Colors.white.withValues(alpha: .1),
          borderRadius: BorderRadius.circular(19),
          border: Border.all(color: Colors.white.withValues(alpha: .16)),
        ),
        child: const Stack(
          alignment: Alignment.center,
          children: [
            Icon(Icons.menu_book_rounded, size: 43, color: Colors.white),
            Positioned(
              right: 9,
              top: 9,
              child: Icon(Icons.auto_awesome, size: 15, color: Color(0xFFFFD78A)),
            ),
          ],
        ),
      );
}

class _ExamCard extends StatelessWidget {
  const _ExamCard({required this.exam, required this.onStart});

  final ExamSummary exam;
  final VoidCallback onStart;

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Container(
                    width: 36,
                    height: 36,
                    decoration: BoxDecoration(
                      color: const Color(0xFFEADDFF),
                      borderRadius: BorderRadius.circular(13),
                    ),
                    child: const Icon(
                      Icons.article_rounded,
                      color: Color(0xFF6750A4),
                      size: 21,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      exam.title,
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(
                        fontSize: 15,
                        fontWeight: FontWeight.w700,
                        color: Color(0xFF1C1B1F),
                      ),
                    ),
                  ),
                  const Icon(Icons.more_horiz, color: Color(0xFF79747E)),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  _ExamDetail(
                    icon: Icons.help_outline_rounded,
                    label: AppLanguageScope.text(
                      context,
                      english: '${exam.questionCount} questions',
                      vietnamese: '${exam.questionCount} câu hỏi',
                    ),
                  ),
                  const SizedBox(width: 15),
                  _ExamDetail(
                    icon: Icons.schedule_rounded,
                    label: AppLanguageScope.text(
                      context,
                      english: '${exam.durationMinutes} min',
                      vietnamese: '${exam.durationMinutes} phút',
                    ),
                  ),
                  const Spacer(),
                  Container(
                    padding:
                        const EdgeInsets.symmetric(horizontal: 9, vertical: 5),
                    decoration: BoxDecoration(
                      color: const Color(0xFFEAF8F2),
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Text(
                      AppLanguageScope.text(context,
                          english:                           AppLanguageScope.text(context,
                              english: 'READY', vietnamese: 'SẴN SÀNG'), vietnamese: 'SẴN SÀNG'),
                      style: TextStyle(
                        color: Color(0xFF268B68),
                        fontWeight: FontWeight.w800,
                        fontSize: 8,
                        letterSpacing: .8,
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              SizedBox(
                width: double.infinity,
                height: 48,
                child: FilledButton(
                  onPressed: onStart,
                  style: FilledButton.styleFrom(
                    minimumSize: const Size(0, 48),
                    tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  ),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(AppLanguageScope.text(context,
                          english: 'Start practice',
                          vietnamese: 'Bắt đầu luyện thi')),
                      SizedBox(width: 7),
                      Icon(Icons.arrow_forward_rounded, size: 17),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      );
}

class _ExamDetail extends StatelessWidget {
  const _ExamDetail({required this.icon, required this.label});

  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) => Row(
        children: [
          Icon(icon, size: 14, color: const Color(0xFF625D67)),
          const SizedBox(width: 5),
          Text(
            label,
            style: const TextStyle(
              fontSize: 11,
              color: Color(0xFF625D67),
            ),
          ),
        ],
      );
}

class _StateMessage extends StatelessWidget {
  const _StateMessage({
    required this.icon,
    required this.title,
    required this.message,
    this.actionLabel,
    this.onAction,
  });

  final IconData icon;
  final String title;
  final String message;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 25),
          child: Column(
            children: [
              Icon(icon, color: const Color(0xFF6750A4), size: 30),
              const SizedBox(height: 12),
              Text(
                title,
                textAlign: TextAlign.center,
                style: const TextStyle(
                  color: Color(0xFF1C1B1F),
                  fontWeight: FontWeight.w700,
                ),
              ),
              const SizedBox(height: 7),
              Text(
                message,
                textAlign: TextAlign.center,
                style: const TextStyle(
                  color: Color(0xFF625D67),
                  fontSize: 12,
                  height: 1.5,
                ),
              ),
              if (actionLabel != null) ...[
                const SizedBox(height: 15),
                OutlinedButton(onPressed: onAction, child: Text(actionLabel!)),
              ],
            ],
          ),
        ),
      );
}

class _TipCard extends StatelessWidget {
  const _TipCard();

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 9),
        decoration: BoxDecoration(
          color: const Color(0xFFEFF1FF),
          borderRadius: BorderRadius.circular(14),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(Icons.lightbulb_outline_rounded,
                color: Color(0xFF6750A4), size: 17),
            SizedBox(width: 10),
            Expanded(
              child: Text(
                AppLanguageScope.text(
                  context,
                  english:
                      'Test-day tip: skim the passage first, then scan for details as you answer.',
                  vietnamese:
                      'Mẹo làm bài: đọc lướt đoạn văn trước, sau đó tìm thông tin chi tiết.',
                ),
                style: TextStyle(
                  color: Color(0xFF6750A4),
                  fontSize: 10,
                  height: 1.2,
                ),
              ),
            ),
          ],
        ),
      );
}
