import 'package:flutter/material.dart';

enum AppLanguage { english, vietnamese }

class AppLanguageScope extends InheritedNotifier<ValueNotifier<AppLanguage>> {
  AppLanguageScope({required super.child, super.key})
      : super(notifier: language);

  static final ValueNotifier<AppLanguage> language =
      ValueNotifier(AppLanguage.english);

  static AppLanguage of(BuildContext context) {
    return context
            .dependOnInheritedWidgetOfExactType<AppLanguageScope>()
            ?.notifier
            ?.value ??
        AppLanguage.english;
  }

  static String text(
    BuildContext context, {
    required String english,
    required String vietnamese,
  }) =>
      of(context) == AppLanguage.vietnamese ? vietnamese : english;

  static void toggle() {
    language.value = language.value == AppLanguage.english
        ? AppLanguage.vietnamese
        : AppLanguage.english;
  }
}

class AppLanguageButton extends StatelessWidget {
  const AppLanguageButton({super.key});

  @override
  Widget build(BuildContext context) {
    final language = AppLanguageScope.of(context);
    return TextButton(
      onPressed: AppLanguageScope.toggle,
      style: TextButton.styleFrom(
        minimumSize: const Size(38, 34),
        padding: const EdgeInsets.symmetric(horizontal: 8),
        foregroundColor: const Color(0xFF6750A4),
        tapTargetSize: MaterialTapTargetSize.shrinkWrap,
      ),
      child: Text(
        language == AppLanguage.english ? 'EN' : 'VI',
        style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 12),
      ),
    );
  }
}
