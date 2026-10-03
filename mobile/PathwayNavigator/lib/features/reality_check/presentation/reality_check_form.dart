import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../core/constants/routes.dart';
import '../../../core/error/app_exception.dart';
import '../../../core/utils/validators.dart';
import '../../../core/widgets/common_widgets.dart';
import '../data/review_models.dart';

/// Agent 4 input form, used both for the first run (Career Discovery) and for the
/// "Needs revision" resubmission loop (Reality Check screen). It is purely presentational:
/// the caller supplies [submit], so the same form drives `evaluate` and `resubmit`.
class RealityCheckForm extends StatefulWidget {
  const RealityCheckForm({
    super.key,
    required this.analysisId,
    required this.careers,
    required this.submit,
    this.prefill = RealityCheckPrefill.empty,
    this.suggestedSkills = const <String>[],
    this.isResubmission = false,
  });

  final String? analysisId;
  final List<String> careers;
  final RealityCheckPrefill prefill;

  /// Skills Agent 4 reported as missing; shown as one-tap "add" chips when revising.
  final List<String> suggestedSkills;
  final bool isResubmission;
  final Future<PathwayReview> Function(RealityCheckInput input) submit;

  @override
  State<RealityCheckForm> createState() => _RealityCheckFormState();
}

class _RealityCheckFormState extends State<RealityCheckForm> {
  final _formKey = GlobalKey<FormState>();
  late final TextEditingController _stream = TextEditingController(text: widget.prefill.alStream);
  late final TextEditingController _grades = TextEditingController(text: widget.prefill.alResults);
  late final TextEditingController _skills = TextEditingController(text: widget.prefill.currentSkills.join(', '));
  late List<String> _careerOptions;
  String? _career;
  late String _budget;
  bool _busy = false;
  String? _error;
  String? _success;

  @override
  void initState() {
    super.initState();
    final preferred = widget.prefill.targetCareer;
    _careerOptions = [
      ...widget.careers,
      if (preferred != null && !widget.careers.contains(preferred)) preferred,
    ];
    _career = preferred ?? (_careerOptions.length == 1 ? _careerOptions.first : null);
    _budget = RealityCheckInput.budgetLevels.contains(widget.prefill.budgetLevel) ? widget.prefill.budgetLevel : 'Medium';
    _skills.addListener(() => setState(() {}));
  }

  @override
  void dispose() {
    _stream.dispose();
    _grades.dispose();
    _skills.dispose();
    super.dispose();
  }

  bool _hasSkill(String skill) =>
      RealityCheckInput.parseSkills(_skills.text).any((existing) => existing.toLowerCase() == skill.toLowerCase());

  void _addSkill(String skill) {
    if (_hasSkill(skill)) return;
    _skills.text = [...RealityCheckInput.parseSkills(_skills.text), skill].join(', ');
  }

  Future<void> _submit() async {
    if (widget.analysisId == null || !_formKey.currentState!.validate()) return;
    setState(() {
      _busy = true;
      _error = null;
      _success = null;
    });
    try {
      final review = await widget.submit(
        RealityCheckInput(
          targetCareer: _career!,
          alStream: _stream.text,
          alResults: _grades.text,
          budgetLevel: _budget,
          currentSkills: RealityCheckInput.parseSkills(_skills.text),
        ),
      );
      if (mounted) {
        setState(() => _success = 'Reality Check saved: ${StatusChip.labelFor(review.status)}.');
      }
    } catch (error) {
      if (mounted) {
        setState(() => _error = describeError(error, fallback: 'Reality Check failed. Please try again.'));
      }
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final revising = widget.isResubmission;
    return SectionCard(
      title: revising ? 'Revise & resubmit Reality Check' : 'Agent 4 · Reality Check',
      icon: revising ? Icons.replay_circle_filled_outlined : Icons.verified_user_outlined,
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              revising
                  ? 'Update your grades, budget or newly gained skills using the counsellor feedback, then run Agent 4 again.'
                  : 'Select a recommended career and enter your actual A/L results, budget and skills. '
                      'A counsellor reviews any flagged risks.',
            ),
            if (widget.prefill.hasSavedValues) ...[
              const SizedBox(height: 8),
              Row(
                children: [
                  const Icon(Icons.auto_awesome, size: 16),
                  const SizedBox(width: 6),
                  Expanded(
                    child: Text('Pre-filled from your saved profile', style: Theme.of(context).textTheme.bodySmall),
                  ),
                ],
              ),
            ],
            if (revising && widget.suggestedSkills.isNotEmpty) ...[
              const SizedBox(height: 12),
              Text('Completed a gap-closing step? Tap a missing skill to add it:',
                  style: Theme.of(context).textTheme.bodySmall),
              const SizedBox(height: 4),
              Wrap(
                spacing: 8,
                runSpacing: 4,
                children: [
                  for (final skill in widget.suggestedSkills)
                    ActionChip(
                      label: Text(_hasSkill(skill) ? '✓ $skill' : '+ $skill'),
                      onPressed: _hasSkill(skill) ? null : () => _addSkill(skill),
                    ),
                ],
              ),
            ],
            const SizedBox(height: 16),
            DropdownButtonFormField<String>(
              initialValue: _career,
              isExpanded: true,
              decoration: fieldDecoration('Recommended career'),
              items: [for (final c in _careerOptions) DropdownMenuItem(value: c, child: Text(c))],
              onChanged: (value) => setState(() => _career = value),
              validator: (value) => value == null ? 'Select a career' : null,
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _stream,
              decoration: fieldDecoration('A/L stream', hint: 'e.g. Physical Science'),
              validator: (value) => Validators.length(value, min: 2, max: 80, label: 'A/L stream'),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _grades,
              textCapitalization: TextCapitalization.characters,
              decoration: fieldDecoration('A/L grades', hint: 'A,B,C'),
              validator: Validators.alResults,
            ),
            const SizedBox(height: 16),
            DropdownButtonFormField<String>(
              initialValue: _budget,
              decoration: fieldDecoration('Budget'),
              items: [for (final b in RealityCheckInput.budgetLevels) DropdownMenuItem(value: b, child: Text(b))],
              onChanged: (value) => setState(() => _budget = value ?? _budget),
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _skills,
              decoration: fieldDecoration('Current skills (comma separated)', hint: 'Python, communication'),
            ),
            if (_error != null) ...[
              const SizedBox(height: 12),
              ErrorBanner(message: _error!),
            ],
            if (_success != null) ...[
              const SizedBox(height: 12),
              Text(_success!),
              if (!revising)
                TextButton(
                  onPressed: () => context.push(AppRoutes.realityCheck),
                  child: const Text('View Reality Check'),
                ),
            ],
            const SizedBox(height: 16),
            LoadingButton(
              label: revising ? 'Resubmit Reality Check' : 'Run Reality Check',
              onPressed: widget.analysisId == null ? null : _submit,
              loading: _busy,
            ),
          ],
        ),
      ),
    );
  }
}
