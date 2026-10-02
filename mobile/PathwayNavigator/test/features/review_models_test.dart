import 'package:flutter_test/flutter_test.dart';
import 'package:pathway_navigator/features/reality_check/data/review_models.dart';

import '../support/fakes.dart';

void main() {
  group('PathwayReview.fromJson', () {
    test('parses JSON-string list columns', () {
      final review = fakeReview();

      expect(review.missingSkills, ['Python', 'Maths']);
      expect(review.subjectRequirements, ['Mathematics']);
      expect(review.gapClosurePlan, hasLength(2));
      expect(review.feasibilityScore, 62);
      expect(review.isPending, isTrue);
      expect(review.createdAt.isUtc, isTrue);
    });

    test('accepts snake_case audit entries as emitted by the AI service', () {
      final review = fakeReview();

      expect(review.toolCalls.single.name, 'lookup');
      expect(review.toolCalls.single.durationMs, closeTo(12.4, 0.001));
      expect(review.toolCalls.single.summary, 'found');
      expect(review.executionTrace.single.name, 'validate');
    });

    test('accepts camelCase audit entries too', () {
      final json = reviewJson()
        ..['toolCallsJson'] = [
          {'toolName': 'lookup', 'status': 'success', 'durationMs': 5}
        ];
      final review = PathwayReview.fromJson(json);
      expect(review.toolCalls.single.name, 'lookup');
      expect(review.toolCalls.single.durationMs, 5);
    });

    test('survives missing and malformed fields', () {
      final review = PathwayReview.fromJson({'status': 'Approved', 'missingSkillsJson': 'oops'});
      expect(review.missingSkills, isEmpty);
      expect(review.targetCareer, '');
      expect(review.isPending, isFalse);
    });
  });

  group('Reality Check pre-fill', () {
    test('review exposes the submitted academic context and resubmission rules', () {
      final review = fakeReview(status: 'NeedsRevision');

      expect(review.alStream, 'Physical Science');
      expect(review.alResults, 'A, B, C');
      expect(review.budgetLevel, 'Low');
      expect(review.currentSkills, ['Python']);
      expect(review.canResubmit, isTrue);
      expect(fakeReview(status: 'Rejected').canResubmit, isTrue);
      expect(fakeReview(status: 'Pending').canResubmit, isFalse);
      expect(fakeReview(status: 'Approved').canResubmit, isFalse);
    });

    test('older API responses without the new fields still parse with safe defaults', () {
      final review = PathwayReview.fromJson({'status': 'Approved'});
      expect(review.alStream, '');
      expect(review.budgetLevel, 'Medium');
      expect(review.currentSkills, isEmpty);
    });

    test('merge prefers the review, fills gaps from the profile and de-duplicates skills', () {
      final merged = RealityCheckPrefill.merge(
        const RealityCheckPrefill(alStream: '', alResults: 'A, B, C', currentSkills: ['Python']),
        const RealityCheckPrefill(alStream: 'Maths', alResults: 'C, C, C', budgetLevel: 'High', currentSkills: ['python', 'SQL']),
      );

      expect(merged.alStream, 'Maths');
      expect(merged.alResults, 'A, B, C');
      expect(merged.currentSkills, ['Python', 'SQL']);
      expect(merged.hasSavedValues, isTrue);
      expect(RealityCheckPrefill.empty.hasSavedValues, isFalse);
    });
  });

  group('ReviewQuery', () {
    test('omits an empty search and resets nothing implicitly', () {
      const query = ReviewQuery(search: '  ');
      expect(query.toQuery()['search'], isNull);
      expect(query.toQuery()['status'], 'Pending');
      expect(query.copyWith(page: 3).page, 3);
    });
  });

  group('RealityCheckInput', () {
    test('normalises grades and splits skills', () {
      final input = RealityCheckInput(
        targetCareer: 'AI Engineer',
        alStream: ' Physical Science ',
        alResults: ' a, b ,c ',
        budgetLevel: 'Low',
        currentSkills: RealityCheckInput.parseSkills('Python, , communication ,'),
      );
      final json = input.toJson();
      expect(json['alResults'], 'A, B ,C');
      expect(json['alStream'], 'Physical Science');
      expect(json['currentSkills'], ['Python', 'communication']);
    });
  });

  test('ReviewPage parses the paged envelope', () {
    final page = ReviewPage.fromJson({
      'items': [reviewJson(), reviewJson(id: 'r2')],
      'page': 2,
      'pageSize': 10,
      'totalCount': 12,
      'totalPages': 2,
    });
    expect(page.items.map((r) => r.id), ['r1', 'r2']);
    expect(page.totalPages, 2);
  });
}
