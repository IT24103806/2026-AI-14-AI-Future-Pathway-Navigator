import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/constants/routes.dart';
import '../../../core/utils/date_format.dart';
import '../../../core/widgets/common_widgets.dart';
import '../data/review_models.dart';
import '../data/review_repository.dart';
import 'review_evidence_view.dart';
import 'student_reality_controller.dart';

class StudentRealityScreen extends StatelessWidget {
  const StudentRealityScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<StudentRealityController>(
      create: (context) => StudentRealityController(context.read<ReviewRepository>())..load(),
      child: const _StudentRealityBody(),
    );
  }
}

const Map<String, (String, String)> _statusCopy = {
  ReviewStatus.pending: (
    'Waiting for counsellor',
    'Your high-impact pathway is paused until an authorized counsellor reviews it.',
  ),
  ReviewStatus.approved: ('Approved', 'The counsellor approved this Reality Check result.'),
  ReviewStatus.rejected: ('Rejected', 'Review the counsellor feedback before choosing another path.'),
  ReviewStatus.needsRevision: (
    'Revision requested',
    'Update the pathway using the counsellor feedback and run the Reality Check again.',
  ),
};

class _StudentRealityBody extends StatelessWidget {
  const _StudentRealityBody();

  @override
  Widget build(BuildContext context) {
    final controller = context.watch<StudentRealityController>();
    return Scaffold(
      appBar: AppBar(
        title: const Text('Reality Check'),
        actions: [
          IconButton(
            tooltip: 'Refresh status',
            onPressed: controller.isLoading ? null : controller.load,
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: _content(context, controller),
    );
  }

  Widget _content(BuildContext context, StudentRealityController controller) {
    if (controller.isLoading) return const LoadingView(message: 'Loading Agent 4 evidence…');
    if (controller.error != null) {
      return Padding(
        padding: const EdgeInsets.all(16),
        child: ErrorBanner(message: controller.error!, onRetry: controller.load),
      );
    }
    final review = controller.latest;
    if (review == null) {
      return MessageView(
        icon: Icons.explore_outlined,
        title: 'No Reality Check yet',
        message: 'Choose a career pathway first. Agent 4 will then check whether it is achievable for '
            'your results, subjects, budget and current skills.',
        actionLabel: 'Choose a career pathway',
        onAction: () => context.push(AppRoutes.careerDiscovery),
      );
    }

    final copy = _statusCopy[review.status] ?? (review.status, 'Your pathway status has been updated.');
    final feedback = (review.counsellorFeedback ?? '').isNotEmpty
        ? review.counsellorFeedback!
        : (review.isPending ? 'Waiting for an authorized counsellor decision.' : 'No feedback was provided.');

    return RefreshIndicator(
      onRefresh: controller.load,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text(review.targetCareer, style: Theme.of(context).textTheme.headlineSmall),
          const SizedBox(height: 4),
          Text('Workflow ${review.workflowId}', style: Theme.of(context).textTheme.bodySmall),
          const SizedBox(height: 12),
          SectionCard(
            title: copy.$1,
            icon: StatusChip.iconFor(review.status),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(copy.$2),
                const SizedBox(height: 8),
                StatusChip(status: review.status),
              ],
            ),
          ),
          ReviewEvidenceView(review: review),
          SectionCard(title: 'Counsellor feedback', icon: Icons.record_voice_over_outlined, child: Text(feedback)),
          const AiDisclosureBanner(),
          const SizedBox(height: 8),
          SectionCard(
            title: 'Review history',
            icon: Icons.history,
            child: controller.history.isEmpty
                ? const Text('No previous reviews.')
                : Column(
                    children: [
                      for (final item in controller.history)
                        ListTile(
                          contentPadding: EdgeInsets.zero,
                          leading: Icon(StatusChip.iconFor(item.status), color: StatusChip.colorFor(item.status)),
                          title: Text(item.targetCareer),
                          subtitle: Text(formatDateTime(item.createdAt)),
                          trailing: Text(StatusChip.labelFor(item.status)),
                        ),
                    ],
                  ),
          ),
        ],
      ),
    );
  }
}
