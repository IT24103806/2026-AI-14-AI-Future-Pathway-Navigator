import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/constants/routes.dart';
import '../../../core/widgets/common_widgets.dart';
import '../../auth/presentation/auth_controller.dart';
import '../data/consultation_models.dart';
import '../data/consultation_repository.dart';
import 'consultant_desk_controller.dart';
import 'notifications_screen.dart';
import 'widgets/consultant_case_view.dart';

/// The Consultant Desk: the queue (ordered by SLA) and the open case.
///
/// Scope is enforced server-side - a consultant sees the unclaimed pool plus their own caseload, and
/// can never reach the counsellor approval queue.
class ConsultantDeskScreen extends StatelessWidget {
  const ConsultantDeskScreen({super.key, this.initialCaseId});

  /// Set when the app was opened from a notification (`?consultation=<id>`).
  final String? initialCaseId;

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<ConsultantDeskController>(
      create: (context) {
        final controller = ConsultantDeskController(context.read<ConsultationRepository>())..loadQueue();
        final id = initialCaseId;
        if (id != null && id.isNotEmpty) controller.openCase(id);
        return controller;
      },
      child: const _DeskBody(),
    );
  }
}

class _DeskBody extends StatelessWidget {
  const _DeskBody();

  @override
  Widget build(BuildContext context) {
    final controller = context.watch<ConsultantDeskController>();
    final auth = context.watch<AuthController>();
    final selected = controller.selected;

    return Scaffold(
      appBar: AppBar(
        title: Text(selected == null ? 'Consultant Desk' : 'Case'),
        leading: selected == null
            ? null
            : IconButton(onPressed: controller.closeCase, icon: const Icon(Icons.arrow_back)),
        actions: [
          NotificationBell(onPressed: () => context.push(AppRoutes.notifications)),
          IconButton(tooltip: 'Sign out', onPressed: auth.logout, icon: const Icon(Icons.logout)),
        ],
      ),
      body: selected == null
          ? Column(
              children: [
                if (controller.error != null)
                  Padding(padding: const EdgeInsets.all(16), child: ErrorBanner(message: controller.error!)),
                if (controller.stats != null) _StatsRow(stats: controller.stats!),
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
                  child: Column(
                    children: [
                      DropdownButtonFormField<String>(
                        initialValue: controller.query.scope,
                        decoration: fieldDecoration('Queue'),
                        items: [
                          for (final scope in ConsultantQueueQuery.scopes)
                            DropdownMenuItem(
                              value: scope,
                              child: Text(scope == 'Open' ? 'Unclaimed pool' : scope),
                            ),
                        ],
                        onChanged: (value) {
                          if (value != null) controller.setScope(value);
                        },
                      ),
                      const SizedBox(height: 8),
                      TextField(
                        onChanged: controller.setSearch,
                        textInputAction: TextInputAction.search,
                        decoration: fieldDecoration('Search subject or student', suffixIcon: const Icon(Icons.search)),
                      ),
                    ],
                  ),
                ),
                Expanded(child: _QueueList(controller: controller)),
              ],
            )
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [ConsultantCaseView(controller: controller)],
            ),
    );
  }
}

class _StatsRow extends StatelessWidget {
  const _StatsRow({required this.stats});

  final ConsultantStats stats;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            _Stat(value: '${stats.unclaimedPool}', label: 'unclaimed'),
            _Stat(value: '${stats.assignedToMe}', label: 'mine'),
            _Stat(value: '${stats.slaCompliancePercent.toStringAsFixed(0)}%', label: 'on time'),
            _Stat(value: stats.averageRating == 0 ? '-' : stats.averageRating.toStringAsFixed(1), label: 'rating'),
          ],
        ),
      );
}

class _Stat extends StatelessWidget {
  const _Stat({required this.value, required this.label});

  final String value;
  final String label;

  @override
  Widget build(BuildContext context) => Column(
        children: [
          Text(value, style: Theme.of(context).textTheme.titleMedium),
          Text(label, style: Theme.of(context).textTheme.labelSmall),
        ],
      );
}

class _QueueList extends StatelessWidget {
  const _QueueList({required this.controller});

  final ConsultantDeskController controller;

  @override
  Widget build(BuildContext context) {
    if (controller.isLoading) return const LoadingView();
    if (controller.queue.items.isEmpty) {
      return const MessageView(
        icon: Icons.inbox_outlined,
        title: 'Nothing here',
        message: 'The pool is clear with these filters.',
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: controller.queue.items.length,
      separatorBuilder: (_, _) => const SizedBox(height: 8),
      itemBuilder: (context, index) {
        final item = controller.queue.items[index];
        return Card(
          child: ListTile(
            onTap: () => controller.openCase(item.id),
            title: Text(item.subject),
            subtitle: Text('${item.studentName} - ${item.contextSummary}'),
            isThreeLine: true,
            trailing: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text('${item.priority} - ${item.slaLabel}',
                    style: TextStyle(color: item.isSlaBreached ? Colors.red : null)),
                const SizedBox(height: 4),
                StatusChip(status: ConsultationStatus.labelFor(item.status)),
              ],
            ),
          ),
        );
      },
    );
  }
}
