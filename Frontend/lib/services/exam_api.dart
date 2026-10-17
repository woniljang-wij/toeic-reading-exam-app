import 'dart:convert';

import 'package:http/http.dart' as http;

import '../models/exam.dart';

class ExamApi {
  ExamApi({http.Client? client}) : _client = client ?? http.Client();

  static const baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5000/api',
  );

  final http.Client _client;

  Future<List<ExamSummary>> listExams() async {
    final data = await _get('/exams') as List<dynamic>;
    return data
        .map((item) =>
            ExamSummary.fromJson(item as Map<String, dynamic>))
        .toList();
  }

  Future<StudentExam> getExam(int examId) async =>
      StudentExam.fromJson(await _get('/exams/$examId') as Map<String, dynamic>);

  Future<AttemptStart> startAttempt(int examId, String candidateId) async {
    final response = await _client.post(
      Uri.parse('$baseUrl/exams/$examId/attempts'),
      headers: const {'Content-Type': 'application/json'},
      body: jsonEncode({'candidateId': candidateId}),
    );
    return AttemptStart.fromJson(_readResponse(response));
  }

  Future<void> saveAnswers(int attemptId, Map<int, String?> answers) async {
    final response = await _client.put(
      Uri.parse('$baseUrl/attempts/$attemptId/answers'),
      headers: const {'Content-Type': 'application/json'},
      body: jsonEncode({'answers': _answersPayload(answers)}),
    );
    _ensureSuccess(response);
  }

  Future<void> submit(int attemptId, Map<int, String?> answers) async {
    final response = await _client.post(
      Uri.parse('$baseUrl/attempts/$attemptId/submit'),
      headers: const {'Content-Type': 'application/json'},
      body: jsonEncode({'answers': _answersPayload(answers)}),
    );
    _ensureSuccess(response);
  }

  Future<AttemptResult> getResult(int attemptId) async {
    final response = await _client.get(
      Uri.parse('$baseUrl/attempts/$attemptId/result'),
    );
    return AttemptResult.fromJson(_readResponse(response));
  }

  Future<dynamic> _get(String path) async {
    final response = await _client.get(Uri.parse('$baseUrl$path'));
    return _readResponse(response);
  }

  List<Map<String, dynamic>> _answersPayload(Map<int, String?> answers) =>
      answers.entries
          .map((entry) => {
                'questionNumber': entry.key,
                'choiceKey': entry.value,
              })
          .toList();

  dynamic _readResponse(http.Response response) {
    _ensureSuccess(response);
    return jsonDecode(response.body);
  }

  void _ensureSuccess(http.Response response) {
    if (response.statusCode >= 200 && response.statusCode < 300) return;
    var message = 'Request failed (${response.statusCode}).';
    try {
      final body = jsonDecode(response.body) as Map<String, dynamic>;
      message = (body['errors'] as List<dynamic>?)?.join(' ') ??
          body['error'] as String? ??
          message;
    } on FormatException {
      if (response.body.isNotEmpty) message = response.body;
    }
    throw ExamApiException(message);
  }

  void dispose() => _client.close();
}

class ExamApiException implements Exception {
  const ExamApiException(this.message);

  final String message;

  @override
  String toString() => message;
}
