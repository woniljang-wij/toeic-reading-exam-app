import 'package:flutter/material.dart';

import 'app_language.dart';
import 'screens/exam_list_screen.dart';

void main() {
  runApp(const ReadingApp());
}

class ReadingApp extends StatelessWidget {
  const ReadingApp({super.key});

  @override
  Widget build(BuildContext context) {
    const ink = Color(0xFF1C1B1F);
    const accent = Color(0xFF6750A4);
    const surface = Color(0xFFF3EDF7);
    return MaterialApp(
      title: 'Readwise TOEIC',
      debugShowCheckedModeBanner: false,
      builder: (context, child) {
        final mediaQuery = MediaQuery.of(context);
        return LayoutBuilder(
          builder: (context, constraints) {
            final viewportWidth = constraints.maxWidth.isFinite
                ? constraints.maxWidth
                : mediaQuery.size.width;
            final viewportHeight = constraints.maxHeight.isFinite
                ? constraints.maxHeight
                : mediaQuery.size.height;
            final appWidth = viewportWidth > 390 ? 390.0 : viewportWidth;

            return ColoredBox(
              color: const Color(0xFFE7E0EC),
              child: Center(
                child: DecoratedBox(
                  decoration: BoxDecoration(
                    color: surface,
                    boxShadow: viewportWidth > appWidth
                        ? const [
                            BoxShadow(
                              color: Color(0x26000000),
                              blurRadius: 28,
                              offset: Offset(0, 4),
                            ),
                          ]
                        : null,
                  ),
                  child: SizedBox(
                    width: appWidth,
                    height: viewportHeight,
                    child: MediaQuery(
                      data: mediaQuery.copyWith(
                        size: Size(appWidth, viewportHeight),
                      ),
                      child: AppLanguageScope(
                        child: child ?? const SizedBox.shrink(),
                      ),
                    ),
                  ),
                ),
              ),
            );
          },
        );
      },
      theme: ThemeData(
        useMaterial3: true,
        scaffoldBackgroundColor: surface,
        colorScheme: ColorScheme.fromSeed(
          seedColor: accent,
          primary: accent,
          surface: surface,
          onSurface: ink,
        ),
        fontFamily: 'Roboto',
        appBarTheme: const AppBarTheme(
          backgroundColor: surface,
          foregroundColor: ink,
          centerTitle: false,
          elevation: 0,
        ),
        filledButtonTheme: FilledButtonThemeData(
          style: FilledButton.styleFrom(
            backgroundColor: accent,
            foregroundColor: Colors.white,
            minimumSize: const Size(0, 50),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(14),
            ),
          ),
        ),
        cardTheme: CardThemeData(
          color: Colors.white,
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(18),
            side: const BorderSide(color: Color(0xFFE7E0EC)),
          ),
        ),
      ),
      home: const ExamListScreen(),
    );
  }
}
