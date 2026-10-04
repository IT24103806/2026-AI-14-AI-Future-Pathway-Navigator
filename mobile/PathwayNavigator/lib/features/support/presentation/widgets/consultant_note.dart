import 'package:flutter/material.dart';

import '../../data/consultation_models.dart';

/// Rendered inside the journey component a consultant wrote back to (a pathway card, a roadmap
/// milestone, the Reality Check panel).
///
/// This is the payoff of the feature: the student does not have to find a separate inbox - the answer
/// is attached to the thing they were looking at when they asked.
class ConsultantNote extends StatelessWidget {
  const ConsultantNote({super.key, required this.guidance, this.title = 'Consultant guidance', this.stageKey});

  /// Raw stored JSON (`{}` when empty), or an already-parsed list.
  final dynamic guidance;
  final String title;

  /// When set, only guidance attached to this milestone is shown.
  final String? stageKey;

  @override
  Widget build(BuildContext context) {
    var items = ConsultationGuidance.parseList(guidance);
    if (stageKey != null && stageKey!.isNotEmpty) {
      items = items.where((item) => item.stageKey == null || item.stageKey == stageKey).toList();
    }
    if (items.isEmpty) return const SizedBox.shrink();

    final theme = Theme.of(context);
    return Card(
      color: theme.colorScheme.secondaryContainer,
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.handshake_outlined, size: 18),
                const SizedBox(width: 8),
                Expanded(child: Text(title, style: theme.textTheme.titleSmall)),
              ],
            ),
            const SizedBox(height: 8),
            for (final item in items) ...[
              if ((item.note ?? '').isNotEmpty) Text(item.note!),
              for (final entry in item.checklist) Text('- $entry'),
              for (final step in item.nextSteps) Text('${item.nextSteps.indexOf(step) + 1}. $step'),
              for (final resource in item.resources) Text('${resource.label}: ${resource.url}'),
              if (item.consultantName != null)
                Text(item.consultantName!, style: theme.textTheme.labelSmall),
              const SizedBox(height: 6),
            ],
          ],
        ),
      ),
    );
  }
}
