import 'package:flutter_test/flutter_test.dart';
import 'package:pathway_navigator/core/error/app_exception.dart';
import 'package:pathway_navigator/features/reality_check/data/review_models.dart';
import 'package:pathway_navigator/features/reality_check/presentation/counsellor_queue_controller.dart';
import 'package:pathway_navigator/features/reality_check/presentation/counsellor_review_controller.dart';
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
