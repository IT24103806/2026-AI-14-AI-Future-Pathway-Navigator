import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/constants/routes.dart';
import '../../../core/error/app_exception.dart';
import '../../../core/utils/validators.dart';
import '../../../core/widgets/common_widgets.dart';
import '../data/review_models.dart';
import '../data/review_repository.dart';

/// Agent 4 input form. Shown below the Career Discovery results.
class RealityCheckForm extends StatefulWidget {
  const RealityCheckForm({super.key, required this.analysisId, required this.careers});

  final String? analysisId;
  final List<String> careers;

  @override
  State<RealityCheckForm> createState() => _RealityCheckFormState();
}

class _RealityCheckFormState extends State<RealityCheckForm> {
  final _formKey = GlobalKey<FormState>();
  final _stream = TextEditingController();
  final _grades = TextEditingController();
  final _skills = TextEditingController();
  String? _career;
  String _budget = 'Medium';
  bool _busy = false;
  String? _error;
  String? _success;

  @override
  void dispose() {
    _stream.dispose();
    _grades.dispose();
    _skills.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final analysisId = widget.analysisId;
    if (analysisId == null || !_formKey.currentState!.validate()) return;
    final repository = context.read<ReviewRepository>();
    setState(() {
      _busy = true;
      _error = null;
      _success = null;
    });
    try {
      final review = await repository.startRealityCheck(
        analysisId,
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
    return SectionCard(
      title: 'Agent 4 · Reality Check',
      icon: Icons.verified_user_outlined,
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'Select a recommended career and enter your actual A/L results, budget and skills. '
              'A counsellor reviews any flagged risks.',
            ),
            const SizedBox(height: 16),
            DropdownButtonFormField<String>(
              initialValue: _career,
              isExpanded: true,
              decoration: fieldDecoration('Recommended career'),
              items: [for (final c in widget.careers) DropdownMenuItem(value: c, child: Text(c))],
              onChanged: (value) => setState(() => _career = value),
              validator: (value) => value == null ? 'Select a career' : null,
            ),
            const SizedBox(height: 16),
            TextFormField(
              controller: _stream,
              decoration: fieldDecoration('A/L stream', hint: 'Physical Science'),
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
              TextButton(
                onPressed: () => context.push(AppRoutes.realityCheck),
                child: const Text('View Reality Check'),
              ),
            ],
            const SizedBox(height: 16),
            LoadingButton(
              label: 'Run Reality Check',
              onPressed: widget.analysisId == null ? null : _submit,
              loading: _busy,
            ),
          ],
        ),
      ),
    );
  }
}
