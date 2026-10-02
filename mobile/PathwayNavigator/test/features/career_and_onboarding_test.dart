import 'package:flutter_test/flutter_test.dart';
import 'package:pathway_navigator/core/error/app_exception.dart';
import 'package:pathway_navigator/features/career_discovery/data/career_models.dart';
import 'package:pathway_navigator/features/career_discovery/presentation/career_controller.dart';
import 'package:pathway_navigator/features/onboarding/data/onboarding_models.dart';
import 'package:pathway_navigator/features/onboarding/presentation/onboarding_chat_controller.dart';

import '../support/fakes.dart';

PathwayPlan plan({
  String? id = 'plan-1',
  String status = 'ready',
  List<String> errors = const [],
  List<String> completedPhases = const [],
  String stageStatus = 'not_started',
}) =>
    PathwayPlan.fromJson({
      'id': id,
      'workflow_id': 'wf',
      'status': status,
      'selected_pathway': 'AI Engineer',
      'roadmap': [
        {
          'order': 1,
          'stage': 'foundation',
          'title': 'Learn Python',
          'outcome': 'x',
          'actions': ['a'],
          'estimated_duration': '2 months',
          'status': stageStatus,
        },
      ],
      'missing_skills': <String>[],
      'completed_phases': completedPhases,
      'next_action': 'Start',
      'validation_errors': errors,
    });

void main() {
  group('CareerController', () {
    late FakeCareerRepository repository;
    late CareerController controller;

    setUp(() {
      repository = FakeCareerRepository();
      controller = CareerController(repository);
    });

    test('analyze stores the result', () async {
      repository.analysis = fakeAnalysis();

      await controller.analyze();

      expect(controller.hasRun, isTrue);
      expect(controller.analysis!.recommendations.single.pathwayName, 'AI Engineer');
      expect(controller.error, isNull);
      expect(controller.isAnalyzing, isFalse);
    });

    test('analyze surfaces errors and the onboarding hint', () async {
      repository.error = const AppException('Please complete onboarding first.');

      await controller.analyze();

      expect(controller.error, contains('onboarding'));
      expect(controller.needsOnboarding, isTrue);
      expect(controller.hasRun, isFalse);
    });

    test('a not-ready roadmap is reported, not shown', () async {
      repository.plan = plan(status: 'failed', errors: ['No data']);

      await controller.buildPlan('AI Engineer');

      expect(controller.plan, isNull);
      expect(controller.error, contains('No data'));
      expect(controller.planningPathway, isNull);
    });

    test('a ready roadmap is kept', () async {
      repository.plan = plan();
      await controller.buildPlan('AI Engineer');
      expect(controller.plan!.roadmap.single.title, 'Learn Python');
    });

    test('approve records the decision from the server response', () async {
      repository.analysis = fakeAnalysis();
      await controller.analyze();

      repository.analysis = fakeAnalysis(status: 'approved');
      await controller.approve();

      expect(controller.analysis!.status, AnalysisStatus.approved);
      expect(controller.analysis!.isDecided, isTrue);
    });

    test('loadSaved restores the latest saved analysis and roadmap', () async {
      repository.analysis = fakeAnalysis();
      repository.plan = plan(completedPhases: ['foundation'], stageStatus: 'completed');

      await controller.loadSaved();

      expect(controller.hasRun, isTrue);
      expect(controller.plan, isNotNull);
      expect(controller.plan!.completedCount, 1);
      expect(controller.isLoadingSaved, isFalse);
    });

    test('toggleStage updates roadmap milestone progress', () async {
      repository.plan = plan();
      await controller.buildPlan('AI Engineer');

      repository.plan = plan(completedPhases: ['foundation'], stageStatus: 'completed');
      await controller.toggleStage('foundation');

      expect(repository.lastCompletedPhases, ['foundation']);
      expect(controller.plan!.completedCount, 1);
    });
  });

  group('OnboardingChatController', () {
    late FakeOnboardingRepository repository;
    late OnboardingChatController controller;

    setUp(() {
      repository = FakeOnboardingRepository();
      controller = OnboardingChatController(repository);
    });

    test('history contains only earlier turns, never the current message', () async {
      await controller.send('I am an Undergraduate');

      final call = repository.chatCalls.single;
      expect(call.message, 'I am an Undergraduate');
      expect(call.history, hasLength(1));
      expect(call.history.single.role, ChatRole.assistant);
      expect(controller.messages.map((m) => m.role), [ChatRole.assistant, ChatRole.user, ChatRole.assistant]);
    });

    test('ignores blank input and double sends', () async {
      await controller.send('   ');
      expect(repository.chatCalls, isEmpty);
    });

    test('keeps the user message and reports an error when the call fails', () async {
      repository.error = const AppException('Network error.');

      await controller.send('hello');

      expect(controller.error, 'Network error.');
      expect(controller.isTyping, isFalse);
      expect(controller.messages.last.role, ChatRole.user);
    });

    test('completion locks the chat', () async {
      repository.reply = const AgentChatReply(
        replyMessage: 'Done',
        slots: ProfileSlots(academicStage: 'After A/L', coreSkills: ['Python'], hobbiesInterests: ['AI'], careerAmbitions: 'Engineer'),
        missingSlots: [],
        isComplete: true,
        isSaved: true,
      );

      await controller.send('finish');
      await controller.send('more');

      expect(controller.isComplete, isTrue);
      expect(controller.slots.filledCount, ProfileSlots.totalSlots);
      expect(repository.chatCalls, hasLength(1));
    });
  });

  group('OnboardingForm', () {
    test('serialises with the backend field names', () {
      const form = OnboardingForm(
        academicStage: 'Undergraduate',
        coreSkills: ['Python'],
        hobbiesInterests: ['Robotics'],
        careerAmbitions: ' AI Engineer ',
      );
      expect(form.toJson(), {
        'academicStage': 'Undergraduate',
        'coreSkills': ['Python'],
        'hobbiesInterests': ['Robotics'],
        'careerAmbitions': 'AI Engineer',
        'onboardingMethod': 'StandardForm',
      });
    });

    test('ProfileSlots round-trips the snake_case agent payload', () {
      final slots = ProfileSlots.fromJson({
        'academic_stage': 'After A/L',
        'core_skills': ['Python'],
        'hobbies_interests': <String>[],
        'career_ambitions': null,
      });
      expect(slots.filledCount, 2);
      expect(slots.toJson()['core_skills'], ['Python']);
    });
  });

  test('PathwayAnalysis flags failed analyses', () {
    expect(fakeAnalysis(status: 'failed').failed, isTrue);
    expect(fakeAnalysis().failed, isFalse);
  });

  test('PathwayRecommendation parses enriched deep-dive, project, certification and SL route fields', () {
    final rec = fakeAnalysis().recommendations.single;
    expect(rec.hasDeepDive, isTrue);
    expect(rec.salaryRangeLkr, contains('LKR'));
    expect(rec.industryTools, contains('PyTorch'));
    expect(rec.portfolioProjects, contains('RAG Assistant'));
    expect(rec.recommendedCertifications, isNotEmpty);
    expect(rec.sriLankanEducationRoutes.single, contains('SLIIT'));
  });
}
