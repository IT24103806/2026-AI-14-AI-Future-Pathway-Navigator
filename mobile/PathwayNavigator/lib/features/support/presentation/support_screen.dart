import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/utils/date_format.dart';
import '../../../core/widgets/common_widgets.dart';
import '../data/consultation_models.dart';
import '../data/consultation_repository.dart';
import 'support_controller.dart';
import 'widgets/consultation_thread_view.dart';
import 'widgets/ask_consultant_sheet.dart';

/// "My questions" - the student's own list of consultant conversations, and the landing place for a
/// notification deep link.
class SupportScreen extends StatelessWidget {
  const SupportScreen({super.key, this.initialConsultationId});

  /// Set when the app was opened from a notification (`?consultation=<id>`).
  final String? initialConsultationId;

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<SupportController>(
      create: (context) {
        final controller = SupportController(context.read<ConsultationRepository>())..load();
        final id = initialConsultationId;
        if (id != null && id.isNotEmpty) controller.open(id);
        return controller;
      },
      child: const _SupportBody(),
    );
  }
}

class _SupportBody extends StatelessWidget {
  const _SupportBody();

  @override
  Widget build(BuildContext context) {
    final controller = context.watch<SupportController>();
    final selected = controller.selected;

    return Scaffold(
      appBar: AppBar(
        title: Text(selected == null ? 'My questions' : 'Conversation'),
        leading: selected == null
            ? null
            : IconButton(onPressed: controller.closeThread, icon: const Icon(Icons.arrow_back)),
      ),
      floatingActionButton: selected == null
          ? FloatingActionButton.extended(
              onPressed: () => showAskConsultantSheet(context, controller: controller),
              icon: const Icon(Icons.question_answer_outlined),
              label: const Text('Ask a question'),
            )
          : null,
      body: selected != null
          ? ListView(
              padding: const EdgeInsets.all(16),
              children: [
                _ThreadHeader(consultation: selected),
                const SizedBox(height: 12),
                ConsultationThreadView(controller: controller),
              ],
            )
          : Column(
              children: [
                if (controller.error != null)
                  Padding(padding: const EdgeInsets.all(16), child: ErrorBanner(message: controller.error!)),
                if (controller.notice != null)
                  Padding(padding: const EdgeInsets.all(16), child: SuccessBanner(message: controller.notice!)),
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
                  child: DropdownButtonFormField<String>(
                    initialValue: controller.status,
                    decoration: fieldDecoration('Show'),
                    items: [
                      for (final status in ConsultationStatus.filterOptions)
                        DropdownMenuItem(
                          value: status,
                          child: Text(status.isEmpty ? 'All questions' : ConsultationStatus.labelFor(status)),
                        ),
                    ],
                    onChanged: (value) {
                      if (value != null) controller.setStatus(value);
                    },
                  ),
                ),
                Expanded(child: _QuestionList(controller: controller)),
              ],
            ),
    );
  }
}

class _QuestionList extends StatelessWidget {
  const _QuestionList({required this.controller});

  final SupportController controller;

  @override
  Widget build(BuildContext context) {
    if (controller.isLoading) return const LoadingView();
    if (controller.pageResult.items.isEmpty) {
      return const MessageView(
        title: 'No questions yet',
        message: 'Whenever a pathway, a cost figure or a review confuses you, ask here - a real consultant answers.',
        icon: Icons.support_agent_outlined,
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: controller.pageResult.items.length,
      separatorBuilder: (_, _) => const SizedBox(height: 8),
      itemBuilder: (context, index) {
        final item = controller.pageResult.items[index];
        return Card(
          child: ListTile(
            onTap: () => controller.open(item.id),
            title: Text(item.subject),
            subtitle: Text(
              '${item.contextSummary.isEmpty ? ConsultationContext.labelFor(item.contextType) : item.contextSummary}\n'
              '${item.assignedConsultantName ?? 'Waiting for a consultant'}',
            ),
            isThreeLine: true,
            trailing: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                StatusChip(status: ConsultationStatus.labelFor(item.status)),
                const SizedBox(height: 4),
                Text(formatDate(item.createdAt), style: Theme.of(context).textTheme.labelSmall),
              ],
            ),
          ),
        );
      },
    );
  }
}

class _ThreadHeader extends StatelessWidget {
  const _ThreadHeader({required this.consultation});

  final Consultation consultation;

  @override
  Widget build(BuildContext context) {
    return SectionCard(
      title: consultation.subject,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              StatusChip(status: ConsultationStatus.labelFor(consultation.status)),
              if (consultation.isSlaBreached && !consultation.isClosed)
                const Chip(label: Text('Flagged to the consultant team'), avatar: Icon(Icons.schedule)),
            ],
          ),
          const SizedBox(height: 8),
          Text(consultation.contextSummary, style: Theme.of(context).textTheme.bodySmall),
          if (consultation.assignedConsultantName != null) ...[
            const SizedBox(height: 4),
            Text('Consultant: ${consultation.assignedConsultantName}'),
          ],
        ],
      ),
    );
  }
}
