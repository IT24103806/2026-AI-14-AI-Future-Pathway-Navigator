import 'package:flutter_test/flutter_test.dart';
import 'package:pathway_navigator/member4_api.dart';

void main() {
  test('parses the Member 4 review API contract', () {
    final result = PathwayReviewStatus.fromJson({
      'id': 'review-1', 'status': 'Pending', 'targetCareer': 'AI Engineer',
      'workflowId': 'wf-1', 'agentStatus': 'approval_required', 'feasibilityScore': 58,
      'isHighRisk': true, 'riskReason': 'Entry risk', 'counsellorFeedback': null,
      'missingSkillsJson': '["Python","Statistics"]', 'createdAt': '2026-09-23T00:00:00Z'
    });
    expect(result.feasibilityScore, 58);
    expect(result.missingSkills, ['Python', 'Statistics']);
    expect(result.isHighRisk, isTrue);
  });
}
