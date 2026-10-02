import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/constants/routes.dart';
import '../../../core/utils/date_format.dart';
import '../../../core/widgets/common_widgets.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/review_models.dart';
import '../data/review_repository.dart';
import 'counsellor_queue_controller.dart';

class CounsellorQueueScreen extends StatelessWidget {
  const CounsellorQueueScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<CounsellorQueueController>(
      create: (context) => CounsellorQueueController(context.read<ReviewRepository>())..load(),
      child: const _QueueBody(),
    );
  }
}

class _QueueBody extends StatelessWidget {
  const _QueueBody();

  static const Map<String, String> _sortLabels = {
    'newest': 'Newest first',
    'oldest': 'Oldest first',
    'score': 'Lowest feasibility',
  };

  Future<void> _open(BuildContext context, CounsellorQueueController controller, PathwayReview review) async {
    final changed = await context.push<bool>(AppRoutes.counsellorReview(review.id));
    if (changed == true) controller.load();
  }

  @override
  Widget build(BuildContext context) {
    final controller = context.watch<CounsellorQueueController>();
    final auth = context.watch<AuthController>();
    final page = controller.page;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Approval Centre'),
        actions: [
          IconButton(
            tooltip: 'Settings',
            onPressed: () => context.push(AppRoutes.settings),
            icon: const Icon(Icons.settings_outlined),
          ),
          IconButton(
            tooltip: 'Sign out',
            onPressed: auth.logout,
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            child: Column(
              children: [
                TextField(
                  onChanged: controller.setSearch,
                  textInputAction: TextInputAction.search,
                  decoration: fieldDecoration('Search career or workflow ID', suffixIcon: const Icon(Icons.search)),
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    Expanded(
                      child: DropdownButtonFormField<String>(
                        initialValue: controller.query.status,
                        isExpanded: true,
                        decoration: fieldDecoration('Status'),
                        items: [
                          for (final s in ReviewStatus.filterOptions)
                            DropdownMenuItem(value: s, child: Text(StatusChip.labelFor(s))),
                        ],
                        onChanged: (value) {
                          if (value != null) controller.setStatus(value);
                        },
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: DropdownButtonFormField<String>(
                        initialValue: controller.query.sort,
                        isExpanded: true,
                        decoration: fieldDecoration('Sort'),
                        items: [
                          for (final entry in _sortLabels.entries)
                            DropdownMenuItem(value: entry.key, child: Text(entry.value)),
                        ],
                        onChanged: (value) {
                          if (value != null) controller.setSort(value);
                        },
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          Expanded(child: _list(context, controller)),
          if (page.totalPages > 1)
            SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 8),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    TextButton(
                      onPressed: controller.query.page > 1 ? () => controller.goToPage(controller.query.page - 1) : null,
                      child: const Text('Previous'),
                    ),
                    Text('Page ${controller.query.page} of ${page.totalPages}'),
                    TextButton(
                      onPressed: controller.query.page < page.totalPages
                          ? () => controller.goToPage(controller.query.page + 1)
                          : null,
                      child: const Text('Next'),
                    ),
                  ],
                ),
              ),
            ),
        ],
      ),
    );
  }

  Widget _list(BuildContext context, CounsellorQueueController controller) {
    if (controller.isLoading && controller.page.items.isEmpty) {
      return const LoadingView(message: 'Loading verified workflow records…');
    }
    if (controller.error != null) {
      return Padding(
        padding: const EdgeInsets.all(16),
        child: ErrorBanner(message: controller.error!, onRetry: controller.load),
      );
    }
    final items = controller.page.items;
    if (items.isEmpty) {
      return const MessageView(icon: Icons.inbox_outlined, title: 'No reviews match these filters.');
    }
    return RefreshIndicator(
      onRefresh: controller.load,
      child: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: items.length,
        itemBuilder: (context, index) {
          final review = items[index];
          return Card(
            child: ListTile(
              onTap: () => _open(context, controller, review),
              leading: Icon(StatusChip.iconFor(review.status), color: StatusChip.colorFor(review.status)),
              title: Text(review.targetCareer),
              subtitle: Text(
                'Feasibility ${review.feasibilityScore}% · ${formatDateTime(review.createdAt)}\n${review.workflowId}',
              ),
              isThreeLine: true,
              trailing: review.isHighRisk
                  ? Icon(Icons.warning_amber_rounded, color: Colors.orange.shade800, semanticLabel: 'High risk')
                  : const Icon(Icons.chevron_right),
            ),
          );
        },
      ),
    );
  }
}
