import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/widgets/common_widgets.dart';
import '../data/review_models.dart';
import '../data/review_repository.dart';
import 'counsellor_review_controller.dart';
import 'review_evidence_view.dart';

class CounsellorReviewScreen extends StatelessWidget {
  const CounsellorReviewScreen({super.key, required this.reviewId});

  final String reviewId;

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<CounsellorReviewController>(
      create: (context) => CounsellorReviewController(context.read<ReviewRepository>(), reviewId)..load(),
      child: const _ReviewBody(),
    );
  }
}

class _ReviewBody extends StatefulWidget {
  const _ReviewBody();

  @override
  State<_ReviewBody> createState() => _ReviewBodyState();
}

class _ReviewBodyState extends State<_ReviewBody> {
  final _feedback = TextEditingController();

  @override
  void dispose() {
    _feedback.dispose();
    super.dispose();
  }

  Future<void> _decide(String decision, String label) async {
    final controller = context.read<CounsellorReviewController>();
    final messenger = ScaffoldMessenger.of(context);
    final router = GoRouter.of(context);
    final ok = await controller.submit(decision, _feedback.text);
    if (!ok) return;
    messenger.showSnackBar(SnackBar(content: Text('Decision recorded: $label. The student has been updated.')));
    router.pop(true);
  }

  @override
  Widget build(BuildContext context) {
    final controller = context.watch<CounsellorReviewController>();
    final review = controller.review;

    return PopScope<Object?>(
      canPop: false,
      onPopInvokedWithResult: (didPop, _) {
        if (!didPop) context.pop(controller.decided);
      },
      child: Scaffold(
        appBar: AppBar(title: const Text('Review')),
        body: _content(context, controller, review),
      ),
    );
  }

  Widget _content(BuildContext context, CounsellorReviewController controller, PathwayReview? review) {
    if (controller.isLoading) return const LoadingView();
    if (review == null) {
      return Padding(
        padding: const EdgeInsets.all(16),
        child: ErrorBanner(message: controller.error ?? 'Review not found.', onRetry: controller.load),
      );
    }

    final theme = Theme.of(context);
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Text(review.targetCareer, style: theme.textTheme.headlineSmall),
        const SizedBox(height: 4),
        SelectableText('Workflow ${review.workflowId}', style: theme.textTheme.bodySmall),
        const SizedBox(height: 8),
        Align(alignment: Alignment.centerLeft, child: StatusChip(status: review.status)),
        const SizedBox(height: 12),
        ReviewEvidenceView(review: review, showAudit: true),
        if (review.isPending)
          SectionCard(
            title: 'Your decision',
            icon: Icons.gavel,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const Text(
                  'Feedback is required and is shown to the student. The AI result is advice only - '
                  'your professional judgement decides.',
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: _feedback,
                  minLines: 4,
                  maxLines: 8,
                  maxLength: CounsellorReviewController.maxFeedbackLength,
                  decoration: fieldDecoration('Counsellor feedback'),
                ),
                if (controller.error != null) ...[
                  const SizedBox(height: 8),
                  ErrorBanner(message: controller.error!),
                ],
                const SizedBox(height: 12),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    FilledButton.icon(
                      onPressed: controller.isSubmitting ? null : () => _decide(ReviewStatus.approved, 'Approved'),
                      icon: const Icon(Icons.check),
                      label: const Text('Approve'),
                    ),
                    OutlinedButton.icon(
                      onPressed: controller.isSubmitting
                          ? null
                          : () => _decide(ReviewStatus.needsRevision, 'Revision requested'),
                      icon: const Icon(Icons.edit_note),
                      label: const Text('Request revision'),
                    ),
                    OutlinedButton.icon(
                      onPressed: controller.isSubmitting ? null : () => _decide(ReviewStatus.rejected, 'Rejected'),
                      icon: const Icon(Icons.close),
                      label: const Text('Reject'),
                    ),
                  ],
                ),
              ],
            ),
          )
        else
          SectionCard(
            title: 'Decision',
            icon: Icons.gavel,
            child: Text(
              (review.counsellorFeedback ?? '').isEmpty ? 'No feedback was provided.' : review.counsellorFeedback!,
            ),
          ),
      ],
    );
  }
}
