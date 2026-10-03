import 'package:flutter/material.dart';

import '../../../../core/utils/date_format.dart';
import '../../../../core/widgets/common_widgets.dart';
import '../../data/consultation_models.dart';
import '../support_controller.dart';

/// The student's conversation with a consultant: the messages, the guidance that was written back,
/// and the reply box that keeps the journey moving.
class ConsultationThreadView extends StatefulWidget {
  const ConsultationThreadView({super.key, required this.controller});

  final SupportController controller;

  @override
  State<ConsultationThreadView> createState() => _ConsultationThreadViewState();
}

class _ConsultationThreadViewState extends State<ConsultationThreadView> {
  final _replyController = TextEditingController();
  int _rating = 0;
  final _feedbackController = TextEditingController();

  @override
  void dispose() {
    _replyController.dispose();
    _feedbackController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final consultation = widget.controller.selected;
    if (consultation == null) return const SizedBox.shrink();

    final guidance = consultation.guidance;
    final theme = Theme.of(context);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (widget.controller.error != null) ...[ErrorBanner(message: widget.controller.error!), const SizedBox(height: 12)],
        if (widget.controller.notice != null) ...[SuccessBanner(message: widget.controller.notice!), const SizedBox(height: 12)],

        for (final message in consultation.visibleMessages) _MessageBubble(message: message),

        if (guidance.isNotEmpty) ...[
          const SizedBox(height: 12),
          for (final item in guidance)
            SectionCard(
              title: 'Consultant guidance',
              icon: Icons.handshake_outlined,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if ((item.note ?? '').isNotEmpty) Text(item.note!),
                  if (item.checklist.isNotEmpty) ...[
                    const SizedBox(height: 8),
                    for (final entry in item.checklist) Text('- $entry'),
                  ],
                  if (item.nextSteps.isNotEmpty) ...[
                    const SizedBox(height: 8),
                    Text('Next steps', style: theme.textTheme.titleSmall),
                    for (final step in item.nextSteps) Text('${item.nextSteps.indexOf(step) + 1}. $step'),
                  ],
                  if (item.resources.isNotEmpty) ...[
                    const SizedBox(height: 8),
                    for (final resource in item.resources) Text('${resource.label}: ${resource.url}'),
                  ],
                  if (item.consultantName != null) ...[
                    const SizedBox(height: 4),
                    Text(item.consultantName!, style: theme.textTheme.labelSmall),
                  ],
                ],
              ),
            ),
        ],

        const SizedBox(height: 12),
        if (consultation.isClosed)
          SectionCard(
            title: 'Closed',
            icon: Icons.check_circle_outline,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(consultation.resolutionSummary ?? 'This question has been resolved.'),
                if (consultation.studentRating != null) Text('You rated this ${consultation.studentRating}/5.'),
                const SizedBox(height: 12),
                LoadingButton(
                  label: 'Reopen within 7 days',
                  loading: widget.controller.isBusy,
                  onPressed: widget.controller.reopen,
                ),
              ],
            ),
          )
        else
          Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              TextField(
                controller: _replyController,
                maxLines: 3,
                maxLength: 4000,
                decoration: fieldDecoration('Continue the conversation', hint: 'Ask a follow-up or say what happened next.'),
              ),
              const SizedBox(height: 8),
              LoadingButton(
                label: 'Send',
                loading: widget.controller.isBusy,
                icon: Icons.send_outlined,
                onPressed: () async {
                  final text = _replyController.text.trim();
                  if (text.length < 2) return;
                  final sent = await widget.controller.sendMessage(text);
                  if (sent) _replyController.clear();
                },
              ),
              const SizedBox(height: 20),
              SectionCard(
                title: 'Was this resolved?',
                icon: Icons.star_outline,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        for (var star = 1; star <= 5; star++)
                          IconButton(
                            onPressed: () => setState(() => _rating = star),
                            icon: Icon(star <= _rating ? Icons.star : Icons.star_border),
                            tooltip: '$star star${star == 1 ? '' : 's'}',
                          ),
                      ],
                    ),
                    TextField(
                      controller: _feedbackController,
                      maxLines: 2,
                      decoration: fieldDecoration('Anything to add? (optional)'),
                    ),
                    const SizedBox(height: 8),
                    LoadingButton(
                      label: 'Mark as resolved',
                      loading: widget.controller.isBusy,
                      onPressed: () => widget.controller.closeRequest(
                        rating: _rating == 0 ? null : _rating,
                        feedback: _feedbackController.text.trim().isEmpty ? null : _feedbackController.text.trim(),
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
      ],
    );
  }
}

class _MessageBubble extends StatelessWidget {
  const _MessageBubble({required this.message});

  final ConsultationMessage message;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final mine = !message.isFromConsultant;

    return Align(
      alignment: mine ? Alignment.centerRight : Alignment.centerLeft,
      child: Container(
        margin: const EdgeInsets.only(bottom: 8),
        padding: const EdgeInsets.all(12),
        constraints: const BoxConstraints(maxWidth: 480),
        decoration: BoxDecoration(
          color: mine ? scheme.primaryContainer : scheme.surfaceContainerHighest,
          borderRadius: BorderRadius.circular(12),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('${message.authorName} - ${formatDateTime(message.createdAt)}',
                style: Theme.of(context).textTheme.labelSmall),
            const SizedBox(height: 4),
            Text(message.body),
            if (message.resources.isNotEmpty) ...[
              const SizedBox(height: 8),
              for (final resource in message.resources) Text('${resource.label}: ${resource.url}'),
            ],
          ],
        ),
      ),
    );
  }
}
