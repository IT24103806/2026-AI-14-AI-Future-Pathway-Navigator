import 'dart:convert';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;

class Member4ApiException implements Exception {
  final String message;
  final int? statusCode;
  const Member4ApiException(this.message, [this.statusCode]);
  @override String toString() => message;
}

class PathwayReviewStatus {
  final String id, status, targetCareer, workflowId, agentStatus;
  final int feasibilityScore;
  final bool isHighRisk;
  final String? riskReason, feedback;
  final String degreeRequirement, costGuidance;
  final List<String> missingSkills, subjectRequirements, entryRequirements, gapClosurePlan;
  final DateTime createdAt;

  const PathwayReviewStatus({required this.id, required this.status, required this.targetCareer,
    required this.workflowId, required this.agentStatus, required this.feasibilityScore,
    required this.isHighRisk, this.riskReason, this.feedback, required this.missingSkills,
    this.degreeRequirement = 'Requires counsellor verification.', this.costGuidance = 'Requires counsellor verification.',
    this.subjectRequirements = const [], this.entryRequirements = const [], this.gapClosurePlan = const [],
    required this.createdAt});

  factory PathwayReviewStatus.fromJson(Map<String, dynamic> json) {
    List<String> decodeList(dynamic value) {
      if (value == null) return [];
      final decoded = value is String ? jsonDecode(value) : value;
      return List<String>.from(decoded as List);
    }
    return PathwayReviewStatus(
      id: json['id'] as String, status: json['status'] as String,
      targetCareer: json['targetCareer'] as String? ?? 'Career pathway',
      workflowId: json['workflowId'] as String? ?? '', agentStatus: json['agentStatus'] as String? ?? '',
      feasibilityScore: json['feasibilityScore'] as int? ?? 0,
      isHighRisk: json['isHighRisk'] as bool? ?? false, riskReason: json['riskReason'] as String?,
      feedback: json['counsellorFeedback'] as String?, missingSkills: decodeList(json['missingSkillsJson']),
      degreeRequirement: json['degreeRequirement'] as String? ?? 'Requires counsellor verification.',
      costGuidance: json['costGuidance'] as String? ?? 'Requires counsellor verification.',
      subjectRequirements: decodeList(json['subjectRequirementsJson']),
      entryRequirements: decodeList(json['entryRequirementsJson']),
      gapClosurePlan: decodeList(json['gapClosurePlanJson']),
      createdAt: DateTime.parse(json['createdAt'] as String),
    );
  }
}

class Member4Api {
  static const _storage = FlutterSecureStorage();
  final String baseUrl;
  final http.Client client;
  Member4Api({String? baseUrl, http.Client? client})
      : baseUrl = baseUrl ?? const String.fromEnvironment('API_BASE_URL', defaultValue: 'https://localhost:7047'),
        client = client ?? http.Client();

  Future<String?> token() => _storage.read(key: 'access_token');
  Future<void> saveToken(String value) => _storage.write(key: 'access_token', value: value);
  Future<void> logout() => _storage.delete(key: 'access_token');

  Future<void> login(String email, String password) async {
    final response = await client.post(Uri.parse('$baseUrl/api/Auth/login'),
      headers: {'Content-Type': 'application/json'}, body: jsonEncode({'email': email, 'password': password}));
    final data = response.body.isEmpty ? <String, dynamic>{} : jsonDecode(response.body) as Map<String, dynamic>;
    if (response.statusCode < 200 || response.statusCode >= 300) throw Member4ApiException(data['message']?.toString() ?? 'Login failed.', response.statusCode);
    final accessToken = (data['token'] ?? data['accessToken'])?.toString();
    if (accessToken == null || accessToken.isEmpty) throw const Member4ApiException('The server returned no access token.');
    await saveToken(accessToken);
  }

  Future<http.Response> _get(String path) async {
    final accessToken = await token();
    if (accessToken == null) throw const Member4ApiException('Please sign in first.', 401);
    return client.get(Uri.parse('$baseUrl$path'), headers: {'Authorization': 'Bearer $accessToken', 'Accept': 'application/json'});
  }

  Future<PathwayReviewStatus> getMyStatus() async {
    final response = await _get('/api/counsellor-review/me/status');
    if (response.statusCode == 404) throw const Member4ApiException('No pathway review has been submitted yet.', 404);
    if (response.statusCode == 401 || response.statusCode == 403) throw const Member4ApiException('Your session expired. Please sign in again.', 401);
    if (response.statusCode != 200) throw Member4ApiException('Unable to load pathway status.', response.statusCode);
    return PathwayReviewStatus.fromJson(jsonDecode(response.body) as Map<String, dynamic>);
  }

  Future<List<PathwayReviewStatus>> getMyHistory() async {
    final response = await _get('/api/counsellor-review/me/history');
    if (response.statusCode != 200) throw Member4ApiException('Unable to load review history.', response.statusCode);
    return (jsonDecode(response.body) as List).map((item) => PathwayReviewStatus.fromJson(item as Map<String, dynamic>)).toList();
  }

  Future<PathwayReviewStatus> startRealityCheck({required String analysisId, required String targetCareer,
      required String alStream, required String alResults, required String budgetLevel, required List<String> skills}) async {
    final accessToken = await token();
    if (accessToken == null) throw const Member4ApiException('Please sign in first.', 401);
    final response = await client.post(Uri.parse('$baseUrl/api/counsellor-review/analysis/$analysisId/evaluate'),
      headers: {'Authorization': 'Bearer $accessToken', 'Content-Type': 'application/json'},
      body: jsonEncode({'targetCareer': targetCareer, 'alStream': alStream, 'alResults': alResults,
        'budgetLevel': budgetLevel, 'currentSkills': skills}));
    final data = response.body.isEmpty ? <String, dynamic>{} : jsonDecode(response.body) as Map<String, dynamic>;
    if (response.statusCode < 200 || response.statusCode >= 300) throw Member4ApiException(data['message']?.toString() ?? 'Reality check failed.', response.statusCode);
    return PathwayReviewStatus.fromJson(data);
  }
}
