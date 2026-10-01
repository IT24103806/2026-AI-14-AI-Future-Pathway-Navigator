import 'package:flutter/material.dart';

import '../../../core/widgets/common_widgets.dart';
import '../data/review_models.dart';

/// Read-only rendering of Agent 4's evidence, shared by the student and counsellor screens.
class ReviewEvidenceView extends StatelessWidget {
  const ReviewEvidenceView({super.key, required this.review, this.showAudit = false});

  final PathwayReview review;

  /// Counsellors/admins also see validation results, tool calls and the execution trace.
  final bool showAudit;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        SectionCard(
          title: 'Feasibility',
          icon: Icons.speed,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              ScoreBar(label: 'Feasibility score', value: review.feasibilityScore),
              const SizedBox(height: 12),
              Text(review.feasibilitySummary?.trim().isNotEmpty == true
                  ? review.feasibilitySummary!
                  : 'The pathway was checked against available evidence.'),
              if (review.isHighRisk) ...[
                const SizedBox(height: 12),
                ErrorBanner(message: 'Risk requiring review: ${review.riskReason ?? 'No reason given.'}'),
              ],
            ],
          ),
        ),
        SectionCard(
          title: 'Degree or qualification',
          icon: Icons.school_outlined,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(review.degreeRequirement.isEmpty ? 'Counsellor verification is required.' : review.degreeRequirement),
              const SizedBox(height: 12),
              Text('Required subjects', style: theme.textTheme.titleSmall),
              const SizedBox(height: 4),
              TagWrap(items: review.subjectRequirements, emptyText: 'No verified subjects listed.'),
            ],
          ),
        ),
        SectionCard(
          title: 'Entry requirements & cost',
          icon: Icons.fact_check_outlined,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              _BulletList(items: review.entryRequirements, emptyText: 'No verified entry requirements listed.'),
              const SizedBox(height: 12),
              Text('Cost guidance', style: theme.textTheme.titleSmall),
              const SizedBox(height: 4),
              Text(review.costGuidance.isEmpty ? 'Verify the total cost before making a commitment.' : review.costGuidance),
            ],
          ),
        ),
        SectionCard(
          title: 'Missing skills',
          icon: Icons.build_circle_outlined,
          child: TagWrap(items: review.missingSkills, emptyText: 'No major prerequisite skill gap was identified.'),
        ),
        SectionCard(
          title: 'Shortest gap-closing plan',
          icon: Icons.checklist,
          child: _BulletList(
            items: review.gapClosurePlan,
            numbered: true,
            emptyText: 'The plan will appear after the next Reality Check.',
          ),
        ),
        if (showAudit)
          SectionCard(
            title: 'Agent 4 audit evidence',
            icon: Icons.manage_search,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Evidence sources', style: theme.textTheme.titleSmall),
                _BulletList(items: review.evidenceSources, emptyText: 'None recorded.'),
                const SizedBox(height: 12),
                Text('Validation results', style: theme.textTheme.titleSmall),
                _BulletList(items: review.validationResults, emptyText: 'None recorded.'),
                const SizedBox(height: 12),
                Text('Controlled tools', style: theme.textTheme.titleSmall),
                _BulletList(
                  items: [for (final t in review.toolCalls) _describe(t)],
                  emptyText: 'None recorded.',
                ),
                const SizedBox(height: 12),
                Text('Execution trace', style: theme.textTheme.titleSmall),
                _BulletList(
                  items: [for (final t in review.executionTrace) _describe(t)],
                  emptyText: 'None recorded.',
                ),
                if (review.agentError != null && review.agentError!.isNotEmpty) ...[
                  const SizedBox(height: 12),
                  ErrorBanner(message: 'Agent error: ${review.agentError}'),
                ],
              ],
            ),
          ),
      ],
    );
  }

  static String _describe(AuditEntry entry) {
    final duration = entry.durationMs == null ? '' : ' (${entry.durationMs!.round()} ms)';
    final summary = (entry.summary ?? '').isEmpty ? '' : ' - ${entry.summary}';
    return '${entry.name}: ${entry.status}$duration$summary';
  }
}

class _BulletList extends StatelessWidget {
  const _BulletList({required this.items, required this.emptyText, this.numbered = false});

  final List<String> items;
  final String emptyText;
  final bool numbered;

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) {
      return Text(emptyText, style: TextStyle(color: Theme.of(context).colorScheme.onSurfaceVariant));
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (var i = 0; i < items.length; i++)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 2),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                SizedBox(width: 24, child: Text(numbered ? '${i + 1}.' : '•')),
                Expanded(child: Text(items[i])),
              ],
            ),
          ),
      ],
    );
  }
}
