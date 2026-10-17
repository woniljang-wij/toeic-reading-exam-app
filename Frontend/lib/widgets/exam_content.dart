import 'dart:convert';

import 'package:flutter/material.dart';

class ExamContent extends StatelessWidget {
  const ExamContent(
    this.content, {
    this.textStyle,
    super.key,
  });

  final String content;
  final TextStyle? textStyle;

  static final RegExp _imagePattern = RegExp(
    r'\{\{IMAGE:data:image/(?:png|jpeg|gif|webp);base64,([A-Za-z0-9+/=]+)\}\}',
  );

  @override
  Widget build(BuildContext context) {
    final children = <Widget>[];
    var cursor = 0;
    for (final match in _imagePattern.allMatches(content)) {
      if (match.start > cursor) {
        children.add(Text(
          content.substring(cursor, match.start),
          style: textStyle,
        ));
      }

      try {
        children.add(
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 8),
            child: Image.memory(
              base64Decode(match.group(1)!),
              fit: BoxFit.contain,
              errorBuilder: (context, error, stackTrace) => const Text(
                'Image could not be displayed.',
                style: TextStyle(color: Colors.redAccent, fontSize: 12),
              ),
            ),
          ),
        );
      } on FormatException {
        children.add(const Text(
          'Image data is invalid.',
          style: TextStyle(color: Colors.redAccent, fontSize: 12),
        ));
      }
      cursor = match.end;
    }
    if (cursor < content.length || children.isEmpty) {
      children.add(Text(content.substring(cursor), style: textStyle));
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: children,
    );
  }
}
