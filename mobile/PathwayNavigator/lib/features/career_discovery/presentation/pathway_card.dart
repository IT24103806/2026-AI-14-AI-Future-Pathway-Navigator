import 'package:flutter/material.dart';

import '../../../core/widgets/common_widgets.dart';
import '../data/career_models.dart';

class PathwayCard extends StatelessWidget {
  const PathwayCard({
    super.key,
    required this.recommendation,
    required this.rank,
    required this.onBuildPlan,
    required this.isBuilding,
    required this.planningBusy,
  });

  final PathwayRecommendation recommendation;
  final int rank;
  final VoidCallback onBuildPlan;
  final bool isBuilding;

  /// True while *any* roadmap is being built (disables the other cards' buttons).
  final bool planningBusy;

  static IconData _trendIcon(String trend) {
    switch (trend) {
      case 'rising':
        return Icons.trending_up;
      case 'declining':
        return Icons.trending_down;
      default:
        return Icons.trending_flat;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final rec = recommendation;
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (rank == 0)
              Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: Chip(
                  avatar: const Icon(Icons.emoji_events_outlined, size: 18),
                  label: const Text('Best match'),
                  backgroundColor: theme.colorScheme.primaryContainer,
                ),
              ),
            Text(rec.label, style: theme.textTheme.labelLarge),
            Text(rec.pathwayName, style: theme.textTheme.titleLarge),
            const SizedBox(height: 4),
            Row(
              children: [
                Icon(_trendIcon(rec.trend), size: 18),
                const SizedBox(width: 4),
                Text('Market trend: ${rec.trend.isEmpty ? 'unknown' : rec.trend}'),
              ],
            ),
            const SizedBox(height: 12),
            ScoreBar(label: 'Match', value: rec.matchScore),
            ScoreBar(label: 'Demand', value: rec.demandScore),
            ScoreBar(label: 'Competition', value: rec.competitionScore),
            const SizedBox(height: 8),
            Text(
              rec.isLiveData ? 'Live market data (Adzuna)' : 'Simulated market data - treat as an estimate',
              style: theme.textTheme.bodySmall,
            ),
            if (rec.salaryRangeLkr.isNotEmpty) ...[
              const SizedBox(height: 4),
              Text('Salary guide: ${rec.salaryRangeLkr}', style: theme.textTheme.bodySmall),
            ],
            const SizedBox(height: 12),
            Text(rec.reasoning),
            const SizedBox(height: 12),
            Text('Missing skills', style: theme.textTheme.titleSmall),
            const SizedBox(height: 4),
            TagWrap(items: rec.missingSkills, emptyText: 'No gaps identified'),
            const SizedBox(height: 12),
            Text('Recommended courses', style: theme.textTheme.titleSmall),
            const SizedBox(height: 4),
            TagWrap(items: rec.recommendedCourses),
            if (rec.roadmap.isNotEmpty) ...[
              const SizedBox(height: 12),
              ExpansionTile(
                tilePadding: EdgeInsets.zero,
                title: const Text('Phases'),
                children: [
                  for (final step in rec.roadmap)
                    ListTile(
                      dense: true,
                      contentPadding: EdgeInsets.zero,
                      title: Text(step.phase),
                      subtitle: Text(step.course),
                    ),
                ],
              ),
            ],
            if (rec.hasDeepDive) ...[
              const SizedBox(height: 8),
              ExpansionTile(
                tilePadding: EdgeInsets.zero,
                title: const Text('Career deep-dive, projects & SL routes'),
                expandedCrossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (rec.dayInTheLife.isNotEmpty) ...[
                    Text('Day in the life', style: theme.textTheme.titleSmall),
                    const SizedBox(height: 4),
                    Text(rec.dayInTheLife),
                    const SizedBox(height: 12),
                  ],
                  if (rec.industryTools.isNotEmpty) ...[
                    Text('Industry tools & stack', style: theme.textTheme.titleSmall),
                    const SizedBox(height: 4),
                    TagWrap(items: rec.industryTools),
                    const SizedBox(height: 12),
                  ],
                  if (rec.portfolioProjects.isNotEmpty) ...[
                    Text('Starter portfolio projects', style: theme.textTheme.titleSmall),
                    const SizedBox(height: 4),
                    for (final project in rec.portfolioProjects)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 4),
                        child: Text('• $project'),
                      ),
                    const SizedBox(height: 8),
                  ],
                  if (rec.recommendedCertifications.isNotEmpty) ...[
                    Text('Industry certifications', style: theme.textTheme.titleSmall),
                    const SizedBox(height: 4),
                    for (final cert in rec.recommendedCertifications)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 4),
                        child: Text('• $cert'),
                      ),
                    const SizedBox(height: 8),
                  ],
                  if (rec.sriLankanEducationRoutes.isNotEmpty) ...[
                    Text('Sri Lankan degree, foundation & NVQ routes', style: theme.textTheme.titleSmall),
                    const SizedBox(height: 4),
                    for (final route in rec.sriLankanEducationRoutes)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 4),
                        child: Text('• $route'),
                      ),
                  ],
                ],
              ),
            ],
            const SizedBox(height: 12),
            LoadingButton(
              label: 'Build step-by-step roadmap',
              loading: isBuilding,
              onPressed: planningBusy ? null : onBuildPlan,
            ),
          ],
        ),
      ),
    );
  }
}
