import 'package:flutter_test/flutter_test.dart';
import 'package:pathway_navigator/core/error/app_exception.dart';
import 'package:pathway_navigator/features/reality_check/data/review_models.dart';
import 'package:pathway_navigator/features/reality_check/presentation/counsellor_queue_controller.dart';
import 'package:pathway_navigator/features/reality_check/presentation/counsellor_review_controller.dart';
import 'package:pathway_navigator/features/profile/data/profile_models.dart';
import 'package:pathway_navigator/features/reality_check/presentation/reality_check_prefill_controller.dart';
import 'package:pathway_navigator/features/reality_check/presentation/student_reality_controller.dart';

import '../support/fakes.dart';

void main() {
  group('StudentRealityController', () {
    test('no review yet is an empty state, not an error', () async {
      final repository = FakeReviewRepository();
      final controller = StudentRealityController(repository);

      await controller.load();

      expect(controller.latest, isNull);
      expect(controller.error, isNull);
      expect(controller.isLoading, isFalse);
    });

    test('falls back to the newest history item when there is no current status', () async {
      final repository = FakeReviewRepository()..history = [fakeReview(id: 'h1', status: 'Approved')];
      final controller = StudentRealityController(repository);

      await controller.load();

      expect(controller.latest!.id, 'h1');
    });

    test('prefers the current status', () async {
      final repository = FakeReviewRepository()
        ..myStatus = fakeReview(id: 'now')
        ..history = [fakeReview(id: 'old', status: 'Rejected')];
      final controller = StudentRealityController(repository);

      await controller.load();

      expect(controller.latest!.id, 'now');
      expect(controller.history, hasLength(1));
    });

    test('real failures are surfaced', () async {
      final repository = FakeReviewRepository()..error = const AppException('Server down');
      final controller = StudentRealityController(repository);

      await controller.load();

      expect(controller.error, 'Server down');
    });
  });

  group('StudentRealityController revision loop', () {
    const input = RealityCheckInput(
      targetCareer: 'AI Engineer',
      alStream: 'Physical Science',
      alResults: 'A, B, C',
      budgetLevel: 'Low',
      currentSkills: ['Python', 'Maths'],
    );

    test('NeedsRevision opens the revision form automatically; Approved does not', () async {
      final revision = StudentRealityController(FakeReviewRepository()..myStatus = fakeReview(status: 'NeedsRevision'));
      await revision.load();
      expect(revision.revisionOpen, isTrue);
      expect(revision.canRevise, isTrue);

      final approved = StudentRealityController(FakeReviewRepository()..myStatus = fakeReview(status: 'Approved'));
      await approved.load();
      expect(approved.revisionOpen, isFalse);
      expect(approved.canRevise, isTrue);
      approved.openRevision();
      expect(approved.revisionOpen, isTrue);
    });

    test('a pending review cannot be revised', () async {
      final controller = StudentRealityController(FakeReviewRepository()..myStatus = fakeReview());
      await controller.load();

      controller.openRevision();

      expect(controller.canRevise, isFalse);
      expect(controller.revisionOpen, isFalse);
    });

    test('resubmit sends the review id, then replaces the latest result and prepends history', () async {
      final repository = FakeReviewRepository()
        ..myStatus = fakeReview(status: 'NeedsRevision')
        ..history = [fakeReview(status: 'NeedsRevision')]
        ..resubmitResult = fakeReview(id: 'r2', status: 'Approved');
      final controller = StudentRealityController(repository);
      await controller.load();

      final updated = await controller.resubmit(input);

      expect(repository.lastResubmission!.id, 'r1');
      expect(repository.lastResubmission!.input.currentSkills, ['Python', 'Maths']);
      expect(updated.id, 'r2');
      expect(controller.latest!.id, 'r2');
      expect(controller.history.map((r) => r.id), ['r2', 'r1']);
      expect(controller.revisionOpen, isFalse);
      expect(controller.resubmitNotice, contains('Approved'));
    });

    test('a failed resubmission keeps the form open and rethrows for the form to display', () async {
      final repository = FakeReviewRepository()..myStatus = fakeReview(status: 'NeedsRevision');
      final controller = StudentRealityController(repository);
      await controller.load();
      repository.error = const AppException('AI service down');

      await expectLater(controller.resubmit(input), throwsA(isA<AppException>()));

      expect(controller.latest!.id, 'r1');
      expect(controller.revisionOpen, isTrue);
    });
  });

  group('RealityCheckPrefillController', () {
    test('combines the saved profile with the latest review', () async {
      final controller = RealityCheckPrefillController(
        FakeProfileRepository(
          profile: const StudentProfile(
            academicStage: 'After A/L',
            coreSkills: ['SQL'],
            hobbiesInterests: [],
            careerAmbitions: 'AI',
            alStream: 'Maths',
            alResults: 'C, C, C',
            onboardingMethod: 'StandardForm',
            isOnboardingCompleted: true,
          ),
        ),
        FakeReviewRepository()..myStatus = fakeReview(),
      );

      await controller.load();

      expect(controller.isLoading, isFalse);
      expect(controller.prefill.alStream, 'Physical Science'); // review wins
      expect(controller.prefill.alResults, 'A, B, C');
      expect(controller.prefill.targetCareer, 'AI Engineer');
      expect(controller.prefill.currentSkills, ['Python', 'SQL']);
      controller.dispose();
    });

    test('failures fall back to an empty form instead of blocking the student', () async {
      final controller = RealityCheckPrefillController(
        FakeProfileRepository(),
        FakeReviewRepository()..error = const AppException('offline'),
      );

      await controller.load();

      expect(controller.isLoading, isFalse);
      expect(controller.prefill.hasSavedValues, isFalse);
      controller.dispose();
    });
  });

  group('CounsellorQueueController', () {
    test('filters reset to page 1 and reload', () async {
      final repository = FakeReviewRepository();
      final controller = CounsellorQueueController(repository);
      await controller.load();

      controller.goToPage(1);
      controller.setStatus(ReviewStatus.approved);
      await Future<void>.delayed(Duration.zero);

      expect(repository.queries.last.status, ReviewStatus.approved);
      expect(repository.queries.last.page, 1);
      controller.dispose();
    });

    test('ignores out-of-range pages', () async {
      final repository = FakeReviewRepository()
        ..page = ReviewPage(items: [fakeReview()], page: 1, totalCount: 1, totalPages: 1);
      final controller = CounsellorQueueController(repository);
      await controller.load();
      final before = repository.queries.length;

      controller.goToPage(5);
      controller.goToPage(0);

      expect(repository.queries.length, before);
      controller.dispose();
    });

    test('search is debounced', () async {
      final repository = FakeReviewRepository();
      final controller = CounsellorQueueController(repository);
      await controller.load();
      final before = repository.queries.length;

      controller.setSearch('a');
      controller.setSearch('ai');
      expect(repository.queries.length, before);

      await Future<void>.delayed(const Duration(milliseconds: 500));
      expect(repository.queries.length, before + 1);
      expect(repository.queries.last.search, 'ai');
      controller.dispose();
    });
  });

  group('CounsellorReviewController', () {
    test('feedback shorter than 5 characters never reaches the API', () async {
      final repository = FakeReviewRepository()..page = ReviewPage(items: [fakeReview()], page: 1, totalCount: 1, totalPages: 1);
      final controller = CounsellorReviewController(repository, 'r1');
      await controller.load();

      final ok = await controller.submit(ReviewStatus.approved, ' ok ');

      expect(ok, isFalse);
      expect(repository.lastDecision, isNull);
      expect(controller.error, contains('5 characters'));
    });

    test('a valid decision is trimmed, sent and flagged as decided', () async {
      final repository = FakeReviewRepository()..page = ReviewPage(items: [fakeReview()], page: 1, totalCount: 1, totalPages: 1);
      final controller = CounsellorReviewController(repository, 'r1');
      await controller.load();

      final ok = await controller.submit(ReviewStatus.needsRevision, '  Please add evidence.  ');

      expect(ok, isTrue);
      expect(controller.decided, isTrue);
      expect(repository.lastDecision!.decision, 'NeedsRevision');
      expect(repository.lastDecision!.feedback, 'Please add evidence.');
    });
  });
}
