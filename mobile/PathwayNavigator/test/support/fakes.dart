import 'dart:convert';

import 'package:pathway_navigator/core/config/app_config.dart';
import 'package:pathway_navigator/core/error/app_exception.dart';
import 'package:pathway_navigator/features/auth/data/auth_models.dart';
import 'package:pathway_navigator/features/auth/data/auth_repository.dart';
import 'package:pathway_navigator/features/career_discovery/data/career_models.dart';
import 'package:pathway_navigator/features/career_discovery/data/career_repository.dart';
import 'package:pathway_navigator/features/onboarding/data/onboarding_models.dart';
import 'package:pathway_navigator/features/onboarding/data/onboarding_repository.dart';
import 'package:pathway_navigator/features/profile/data/profile_models.dart';
import 'package:pathway_navigator/features/profile/data/profile_repository.dart';
import 'package:pathway_navigator/features/reality_check/data/review_models.dart';
import 'package:pathway_navigator/features/reality_check/data/review_repository.dart';

/// Builds an unsigned JWT-shaped string whose `exp` claim is [expiresAt].
String fakeJwt(DateTime expiresAt) {
  String encode(Map<String, Object?> value) => base64Url.encode(utf8.encode(jsonEncode(value))).replaceAll('=', '');
  final exp = expiresAt.toUtc().millisecondsSinceEpoch ~/ 1000;
  return '${encode({'alg': 'none'})}.${encode({'exp': exp})}.signature';
}

AuthSession fakeSession({String role = 'Student', DateTime? expiresAt}) {
  final expiry = expiresAt ?? DateTime.now().toUtc().add(const Duration(hours: 1));
  return AuthSession(
    userId: 'user-1',
    email: 'student@example.com',
    role: role,
    token: fakeJwt(expiry),
    expiresAt: expiry,
  );
}

const AppConfig testConfig = AppConfig(apiBaseUrl: 'http://localhost:5081/api');

class FakeAuthRepository implements AuthRepository {
  FakeAuthRepository({this.session, this.error});

  AuthSession? session;
  Object? error;
  final List<String> calls = [];

  AuthSession _result() {
    final failure = error;
    if (failure != null) throw failure;
    return session ?? fakeSession();
  }

  @override
  Future<AuthSession> login({required String email, required String password}) async {
    calls.add('login:$email');
    return _result();
  }

  @override
  Future<AuthSession> register({required String email, required String password}) async {
    calls.add('register:$email');
    return _result();
  }

  @override
  Future<void> requestPasswordReset(String email) async => calls.add('forgot:$email');

  @override
  Future<void> verifyResetCode({required String email, required String code}) async => calls.add('verify:$code');

  @override
  Future<void> resetPassword({
    required String email,
    required String code,
    required String newPassword,
    required String confirmPassword,
  }) async =>
      calls.add('reset');
}

class FakeProfileRepository implements ProfileRepository {
  FakeProfileRepository({this.profile});

  StudentProfile? profile;

  @override
  Future<ProfileStatus> getStatus() async =>
      ProfileStatus(hasProfile: profile != null, isOnboardingCompleted: profile?.isOnboardingCompleted ?? false);

  @override
  Future<StudentProfile?> getProfile() async => profile;
}

class FakeOnboardingRepository implements OnboardingRepository {
  final List<({String message, List<ChatMessage> history})> chatCalls = [];
  AgentChatReply? reply;
  Object? error;
  OnboardingForm? submitted;

  @override
  Future<AgentChatReply> sendChatMessage({
    required String message,
    required List<ChatMessage> history,
    required ProfileSlots currentSlots,
  }) async {
    chatCalls.add((message: message, history: history));
    final failure = error;
    if (failure != null) throw failure;
    return reply ??
        const AgentChatReply(
          replyMessage: 'ok',
          slots: ProfileSlots.empty,
          missingSlots: [],
          isComplete: false,
          isSaved: false,
        );
  }

  @override
  Future<void> submitStandardForm(OnboardingForm form) async => submitted = form;
}

class FakeCareerRepository implements CareerRepository {
  PathwayAnalysis? analysis;
  PathwayPlan? plan;
  Object? error;

  PathwayAnalysis _analysis() {
    final failure = error;
    if (failure != null) throw failure;
    return analysis!;
  }

  @override
  Future<PathwayAnalysis> analyze() async => _analysis();

  @override
  Future<PathwayAnalysis> approve(String analysisId) async => _analysis();

  @override
  Future<PathwayAnalysis> reject(String analysisId) async => _analysis();

  @override
  Future<PathwayPlan> buildPlan(String pathwayName) async {
    final failure = error;
    if (failure != null) throw failure;
    return plan!;
  }
}

class FakeReviewRepository implements ReviewRepository {
  PathwayReview? myStatus;
  List<PathwayReview> history = [];
  ReviewPage page = ReviewPage.empty;
  Object? error;
  final List<ReviewQuery> queries = [];
  ({String id, String decision, String feedback})? lastDecision;

  @override
  Future<PathwayReview> startRealityCheck(String analysisId, RealityCheckInput input) async => myStatus!;

  @override
  Future<PathwayReview?> getMyStatus() async {
    final failure = error;
    if (failure != null) throw failure;
    return myStatus;
  }

  @override
  Future<List<PathwayReview>> getMyHistory() async => history;

  @override
  Future<ReviewPage> getReviews(ReviewQuery query) async {
    queries.add(query);
    final failure = error;
    if (failure != null) throw failure;
    return page;
  }

  @override
  Future<PathwayReview> getReview(String id) async => page.items.firstWhere((r) => r.id == id);

  @override
  Future<PathwayReview> submitDecision({required String id, required String decision, required String feedback}) async {
    lastDecision = (id: id, decision: decision, feedback: feedback);
    return page.items.first;
  }
}

/// A review as the .NET API serialises it (camelCase, list columns stored as JSON strings).
Map<String, dynamic> reviewJson({String id = 'r1', String status = 'Pending', String career = 'AI Engineer'}) => {
      'id': id,
      'pathwayAnalysisId': 'a1',
      'studentId': 's1',
      'status': status,
      'isHighRisk': true,
      'riskReason': 'Budget is low',
      'missingSkillsJson': '["Python","Maths"]',
      'feasibilitySummary': 'Challenging but possible',
      'degreeRequirement': 'BSc in Computer Science',
      'subjectRequirementsJson': '["Mathematics"]',
      'entryRequirementsJson': '["Z-score 1.5"]',
      'costGuidance': 'LKR 1.2M',
      'gapClosurePlanJson': '["Learn Python","Take maths bridging course"]',
      'evidenceSourcesJson': '["UGC handbook"]',
      'feasibilityScore': 62,
      'targetCareer': career,
      'workflowId': 'wf-1',
      'validationResultsJson': '["ok"]',
      'toolCallsJson': '[{"tool_name":"lookup","status":"success","duration_ms":12.4,"result_summary":"found"}]',
      'executionTraceJson': '[{"step":"validate","status":"success","duration_ms":3}]',
      'agentError': null,
      'counsellorFeedback': null,
      'createdAt': '2026-09-30T10:00:00Z',
      'updatedAt': '2026-09-30T10:00:00Z',
      'reviewedAt': null,
    };

PathwayReview fakeReview({String id = 'r1', String status = 'Pending', String career = 'AI Engineer'}) =>
    PathwayReview.fromJson(reviewJson(id: id, status: status, career: career));

PathwayAnalysis fakeAnalysis({String status = 'pending_approval'}) => PathwayAnalysis.fromJson({
      'id': 'a1',
      'workflow_id': 'wf-a',
      'status': status,
      'recommendations': [
        {
          'label': 'Path A',
          'pathway_name': 'AI Engineer',
          'match_score': 91,
          'demand_score': 80,
          'competition_score': 55,
          'trend': 'rising',
          'data_source': 'simulated_fallback',
          'reasoning': 'Strong maths and Python.',
          'missing_skills': ['Deep learning'],
          'recommended_courses': ['ML 101'],
          'roadmap': [
            {'phase': 'Foundation', 'course': 'Python'},
          ],
        },
      ],
      'validation_errors': <String>[],
    });

const AppException notFound = AppException('not found', kind: AppErrorKind.notFound, statusCode: 404);
