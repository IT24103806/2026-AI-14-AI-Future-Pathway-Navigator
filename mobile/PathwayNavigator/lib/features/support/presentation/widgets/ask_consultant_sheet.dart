import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../../core/widgets/common_widgets.dart';
import '../../data/consultation_models.dart';
import '../../data/consultation_repository.dart';
import '../support_controller.dart';

/// The "ask a consultant" form.
///
/// The subject/body prefill comes from the journey component the student was looking at, and the
/// [contextType]/[contextRefId] travel with the question so the consultant sees the same evidence and
/// the answer can be written back into that same component.
class AskConsultantSheet extends StatefulWidget {
  const AskConsultantSheet({
    super.key,
    required this.controller,
    this.contextType = ConsultationContext.general,
    this.contextRefId,
    this.contextLabel = '',
    this.prefillSubject = '',
    this.prefillBody = '',
  });

  final SupportController controller;
  final String contextType;
  final String? contextRefId;
  final String contextLabel;
  final String prefillSubject;
  final String prefillBody;

  @override
  State<AskConsultantSheet> createState() => _AskConsultantSheetState();
}

class _AskConsultantSheetState extends State<AskConsultantSheet> {
  late final TextEditingController _subject = TextEditingController(text: widget.prefillSubject);
  late final TextEditingController _body = TextEditingController(text: widget.prefillBody);
  String _category = 'GuideRequest';
  String _priority = ConsultationPriority.p2;

  static const Map<String, String> _categories = {
    'GuideRequest': 'I need a guide',
    'DoubtAnswer': 'I have a question about a result',
    'RealityCheckClarification': 'I do not understand my Reality Check',
    'PathwayAdvice': 'I need pathway advice',
    'MarketCourseInfo': 'I want course / market information',
  };

  @override
  void dispose() {
    _subject.dispose();
    _body.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final subject = _subject.text.trim();
    final body = _body.text.trim();
    if (subject.length < 4 || body.length < 10) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Add a subject and describe your question (at least 10 characters).')),
      );
      return;
    }
    final created = await context.read<SupportController>().ask(
      subject: subject,
      body: body,
      contextType: widget.contextType,
      contextRefId: widget.contextRefId,
      category: _category,
      priority: _priority,
    );
    if (created != null && mounted) Navigator.of(context).pop();
  }

  @override
  Widget build(BuildContext context) {
    // Watched, not read: the sheet owns the send button and must re-enable it when the request ends.
    final controller = context.watch<SupportController>();
    return Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.of(context).viewInsets.bottom),
      child: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text('Ask a consultant', style: Theme.of(context).textTheme.titleLarge),
            if (widget.contextLabel.isNotEmpty) ...[
              const SizedBox(height: 4),
              Text('About: ${widget.contextLabel}', style: Theme.of(context).textTheme.bodySmall),
            ],
            const SizedBox(height: 16),
            TextField(
              controller: _subject,
              maxLength: 140,
              decoration: fieldDecoration('Subject', hint: 'What do you need help with?'),
            ),
            const SizedBox(height: 8),
            TextField(
              controller: _body,
              maxLines: 5,
              maxLength: 4000,
              decoration: fieldDecoration('Your question', hint: 'Explain what you are stuck on.'),
            ),
            const SizedBox(height: 8),
            DropdownButtonFormField<String>(
              initialValue: _category,
              decoration: fieldDecoration('Type'),
              items: [
                for (final entry in _categories.entries) DropdownMenuItem(value: entry.key, child: Text(entry.value)),
              ],
              onChanged: (value) {
                if (value != null) setState(() => _category = value);
              },
            ),
            const SizedBox(height: 12),
            DropdownButtonFormField<String>(
              initialValue: _priority,
              decoration: fieldDecoration('How urgent is it?'),
              items: const [
                DropdownMenuItem(value: ConsultationPriority.p1, child: Text('Urgent - I am blocked')),
                DropdownMenuItem(value: ConsultationPriority.p2, child: Text('Normal')),
                DropdownMenuItem(value: ConsultationPriority.p3, child: Text('Just curious')),
              ],
              onChanged: (value) {
                if (value != null) setState(() => _priority = value);
              },
            ),
            const SizedBox(height: 16),
            ValueListenableBuilder<TextEditingValue>(
              valueListenable: _body,
              builder: (context, _, _) => LoadingButton(
                label: 'Send to a consultant',
                loading: controller.isBusy,
                onPressed: _submit,
              ),
            ),
            const SizedBox(height: 8),
            const Text('A real consultant replies here. You will get a notification when they do.'),
          ],
        ),
      ),
    );
  }
}

/// Opens the sheet from anywhere in the journey.
///
/// Pass [controller] when the caller already owns one (the support screen does, so the list refreshes);
/// otherwise a short-lived controller is created from [ConsultationRepository] and disposed on close.
Future<void> showAskConsultantSheet(
  BuildContext context, {
  SupportController? controller,
  String contextType = ConsultationContext.general,
  String? contextRefId,
  String contextLabel = '',
  String prefillSubject = '',
  String prefillBody = '',
}) async {
  final owned = controller == null;
  final active = controller ?? SupportController(context.read<ConsultationRepository>());
  await showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => ChangeNotifierProvider<SupportController>.value(
      value: active,
      child: AskConsultantSheet(
        controller: active,
        contextType: contextType,
        contextRefId: contextRefId,
        contextLabel: contextLabel,
        prefillSubject: prefillSubject,
        prefillBody: prefillBody,
      ),
    ),
  );
  if (owned) active.dispose();
}
