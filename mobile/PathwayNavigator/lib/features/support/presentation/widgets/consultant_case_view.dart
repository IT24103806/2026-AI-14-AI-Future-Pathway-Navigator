import 'package:flutter/material.dart';

import '../../../../core/utils/date_format.dart';
import '../../../../core/widgets/common_widgets.dart';
import '../../data/consultation_models.dart';
import '../consultant_desk_controller.dart';

/// The open case: the frozen context, the Agent 5 brief, the thread, internal notes and the composer.
///
/// The Agent 5 draft is inserted into the visible text field and can always be edited; [usedDraft] is
/// remembered so the "draft assisted" metric survives those edits.
class ConsultantCaseView extends StatefulWidget {
  const ConsultantCaseView({super.key, required this.controller});

  final ConsultantDeskController controller;

  @override
  State<ConsultantCaseView> createState() => _ConsultantCaseViewState();
}

class _ConsultantCaseViewState extends State<ConsultantCaseView> {
  final _reply = TextEditingController();
  final _note = TextEditingController();
  final _stageKey = TextEditingController();
  final _guidanceNote = TextEditingController();
  final _checklist = TextEditingController();
  final _resourceLabel = TextEditingController();
  final _resourceUrl = TextEditingController();
  final _resolution = TextEditingController();
  bool _usedDraft = false;
  bool _closeAfterReply = false;
  bool _showNotes = false;

  @override
  void dispose() {
    _reply.dispose();
    _note.dispose();
    _stageKey.dispose();
    _guidanceNote.dispose();
    _checklist.dispose();
    _resourceLabel.dispose();
    _resourceUrl.dispose();
    _resolution.dispose();
    super.dispose();
  }

  Future<void> _insertDraft(String tone) async {
    final draft = await widget.controller.loadDraft(tone: tone);
    if (draft == null) return;
    setState(() {
      _reply.text = draft.body;
      _usedDraft = true;
    });
  }

  Future<void> _send() async {
    final resources = _resourceUrl.text.trim().isEmpty
        ? <ConsultationResource>[]
        : <ConsultationResource>[
            ConsultationResource(
              label: _resourceLabel.text.trim().isEmpty ? _resourceUrl.text.trim() : _resourceLabel.text.trim(),
              url: _resourceUrl.text.trim(),
            ),
          ];
    final hasGuidance =
        _stageKey.text.trim().isNotEmpty || _guidanceNote.text.trim().isNotEmpty || _checklist.text.trim().isNotEmpty;
    final guidance = hasGuidance
        ? ConsultationGuidance(
            stageKey: _stageKey.text.trim().isEmpty ? null : _stageKey.text.trim(),
            note: _guidanceNote.text.trim().isEmpty ? null : _guidanceNote.text.trim(),
            resources: resources,
            checklist: _checklist.text.split('\n').map((line) => line.trim()).where((line) => line.isNotEmpty).toList(),
          )
        : null;

    final ok = await widget.controller.sendReply(
      message: _reply.text,
      resources: resources,
      guidance: guidance,
      resolutionSummary: _resolution.text.trim().isEmpty ? null : _resolution.text.trim(),
      closeAfterReply: _closeAfterReply,
      usedAgentDraft: _usedDraft,
    );

    if (ok && mounted) {
      setState(() {
        _reply.clear();
        _stageKey.clear();
        _guidanceNote.clear();
        _checklist.clear();
        _resourceLabel.clear();
        _resourceUrl.clear();
        _resolution.clear();
        _usedDraft = false;
        _closeAfterReply = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final controller = widget.controller;
    final consultation = controller.selected;
    if (consultation == null) return const SizedBox.shrink();
    final theme = Theme.of(context);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (controller.error != null) ...[ErrorBanner(message: controller.error!), const SizedBox(height: 12)],
        if (controller.notice != null) ...[SuccessBanner(message: controller.notice!), const SizedBox(height: 12)],

        SectionCard(
          title: consultation.subject,
          icon: Icons.help_outline,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  StatusChip(status: ConsultationStatus.labelFor(consultation.status)),
                  Chip(label: Text('${consultation.priority} - ${consultation.slaLabel}')),
                  Chip(label: Text(ConsultationContext.labelFor(consultation.contextType))),
                ],
              ),
              const SizedBox(height: 8),
              Text(consultation.body),
            ],
          ),
        ),
        const SizedBox(height: 12),

        SectionCard(
          title: 'Frozen context (what the student saw)',
          icon: Icons.lock_outline,
          child: Text(consultation.contextSummary),
        ),
        const SizedBox(height: 12),

        if (controller.brief != null) _BriefCard(brief: controller.brief!),
        if (consultation.agentTriage != null) _TriageCard(triage: consultation.agentTriage!),

        SectionCard(
          title: 'Conversation',
          icon: Icons.forum_outlined,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              for (final message in consultation.visibleMessages) ...[
                Text('${message.authorName} - ${message.authorRole}', style: theme.textTheme.labelSmall),
                Text(message.body),
                const SizedBox(height: 8),
              ],
              if (consultation.visibleMessages.isEmpty) const Text('No messages yet.'),
            ],
          ),
        ),
        const SizedBox(height: 12),

        Row(
          children: [
            if (!consultation.isClaimed)
              Expanded(
                child: LoadingButton(
                  label: 'Claim',
                  loading: controller.isBusy,
                  onPressed: () => controller.claim(),
                ),
              )
            else ...[
              Expanded(
                child: LoadingButton(
                  label: 'Release',
                  loading: controller.isBusy,
                  onPressed: () => controller.release(),
                ),
              ),
            ],
            const SizedBox(width: 8),
            Expanded(
              child: LoadingButton(
                label: 'Escalate',
                loading: controller.isBusy,
                onPressed: () => controller.escalate(),
              ),
            ),
          ],
        ),
        const SizedBox(height: 12),

        SectionCard(
          title: 'Your reply',
          icon: Icons.edit_outlined,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  const Expanded(child: Text('Draft assistance (Agent 5)')),
                  TextButton(onPressed: controller.isBusy ? null : () => _insertDraft('Supportive'), child: const Text('Supportive')),
                  TextButton(onPressed: controller.isBusy ? null : () => _insertDraft('Direct'), child: const Text('Direct')),
                ],
              ),
              TextField(
                controller: _reply,
                maxLines: 7,
                maxLength: 4000,
                decoration: fieldDecoration('Write to the student', hint: 'Answer the question, then say what to do next.'),
              ),
              const SizedBox(height: 8),
              TextField(
                controller: _stageKey,
                decoration: fieldDecoration('Roadmap stage key (optional)', hint: 'e.g. skills'),
              ),
              TextField(
                controller: _guidanceNote,
                maxLines: 2,
                decoration: fieldDecoration('Guidance note shown inside the pathway (optional)'),
              ),
              TextField(
                controller: _checklist,
                maxLines: 3,
                decoration: fieldDecoration('Checklist - one item per line (optional)'),
              ),
              const SizedBox(height: 8),
              TextField(controller: _resourceLabel, decoration: fieldDecoration('Guide label (optional)')),
              TextField(controller: _resourceUrl, decoration: fieldDecoration('Guide URL (optional)', hint: 'https://...')),
              TextField(controller: _resolution, decoration: fieldDecoration('Resolution summary (shown when closed)')),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                value: _closeAfterReply,
                onChanged: (value) => setState(() => _closeAfterReply = value),
                title: const Text('Send and close this question'),
              ),
              LoadingButton(
                label: 'Send reply to student',
                loading: controller.isBusy,
                icon: Icons.send_outlined,
                onPressed: _send,
              ),
              const SizedBox(height: 8),
              const AiDisclosureBanner(
                text: 'Sending notifies the student and attaches your guidance to their pathway. '
                    'Approving or rejecting a pathway stays with a counsellor.',
              ),
            ],
          ),
        ),
        const SizedBox(height: 12),

        SectionCard(
          title: 'Internal notes',
          icon: Icons.sticky_note_2_outlined,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const Text('Internal notes are never shown to the student.'),
              const SizedBox(height: 8),
              TextField(
                controller: _note,
                maxLines: 3,
                decoration: fieldDecoration('Add a note for the consultant team'),
              ),
              const SizedBox(height: 8),
              LoadingButton(
                label: 'Add note',
                loading: controller.isBusy,
                onPressed: () async {
                  if (_note.text.trim().length < 2) return;
                  final ok = await controller.addNote(_note.text.trim());
                  if (ok && mounted) _note.clear();
                },
              ),
              if (consultation.internalNotes.isNotEmpty) ...[
                const SizedBox(height: 8),
                TextButton(
                  onPressed: () => setState(() => _showNotes = !_showNotes),
                  child: Text(_showNotes ? 'Hide notes' : 'Show ${consultation.internalNotes.length} note(s)'),
                ),
                if (_showNotes)
                  for (final note in consultation.internalNotes)
                    ListTile(
                      dense: true,
                      title: Text(note.authorName),
                      subtitle: Text(note.body),
                      trailing: Text(formatDateTime(note.createdAt)),
                    ),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

class _BriefCard extends StatelessWidget {
  const _BriefCard({required this.brief});

  final ConsultantBrief brief;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 12),
        child: SectionCard(
          title: 'Agent 5 case brief',
          icon: Icons.auto_awesome_outlined,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('${brief.headline} - confidence ${(brief.confidence * 100).toStringAsFixed(0)}%',
                  style: Theme.of(context).textTheme.labelMedium),
              const SizedBox(height: 8),
              for (final section in brief.sections) ...[
                Text(section.title, style: Theme.of(context).textTheme.titleSmall),
                for (final point in section.points) Text('- $point'),
                const SizedBox(height: 6),
              ],
            ],
          ),
        ),
      );
}

class _TriageCard extends StatelessWidget {
  const _TriageCard({required this.triage});

  final ConsultationTriage triage;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.only(bottom: 12),
        child: SectionCard(
          title: 'Agent 5 triage evidence',
          icon: Icons.insights_outlined,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Category: ${triage.category}'),
              Text('Priority: ${triage.priority} - SLA ${triage.suggestedSlaHours}h'),
              Text('Expertise: ${triage.expertiseTags.isEmpty ? 'general' : triage.expertiseTags.join(', ')}'),
              Text('Language: ${triage.language} - sentiment: ${triage.sentiment}'),
              if (triage.safetyFlags.isNotEmpty)
                Text('Safety flags: ${triage.safetyFlags.join(', ')}',
                    style: TextStyle(color: Theme.of(context).colorScheme.error)),
            ],
          ),
        ),
      );
}
