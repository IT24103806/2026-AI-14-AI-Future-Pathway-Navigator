import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/constants/routes.dart';
import '../../../core/error/app_exception.dart';
import '../../../core/utils/validators.dart';
import '../../../core/widgets/common_widgets.dart';
import '../data/onboarding_models.dart';
import '../data/onboarding_repository.dart';

/// Non-conversational alternative to the chat (same result, no AI involved).
class OnboardingFormView extends StatefulWidget {
  const OnboardingFormView({super.key});

  @override
  State<OnboardingFormView> createState() => _OnboardingFormViewState();
}

class _OnboardingFormViewState extends State<OnboardingFormView> {
  final _formKey = GlobalKey<FormState>();
  final _ambition = TextEditingController();
  final _skillInput = TextEditingController();
  final _interestInput = TextEditingController();
  final List<String> _skills = [];
  final List<String> _interests = [];
  String _stage = academicStages[2];
  bool _loading = false;
  String? _error;

  @override
  void dispose() {
    _ambition.dispose();
    _skillInput.dispose();
    _interestInput.dispose();
    super.dispose();
  }

  void _addTag(TextEditingController input, List<String> target) {
    final value = input.text.trim();
    if (value.isEmpty) return;
    if (!target.any((item) => item.toLowerCase() == value.toLowerCase())) {
      setState(() => target.add(value));
    }
    input.clear();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (_skills.isEmpty || _interests.isEmpty) {
      setState(() => _error = 'Add at least one skill and one interest.');
      return;
    }
    final repository = context.read<OnboardingRepository>();
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      await repository.submitStandardForm(
        OnboardingForm(
          academicStage: _stage,
          coreSkills: List.of(_skills),
          hobbiesInterests: List.of(_interests),
          careerAmbitions: _ambition.text,
        ),
      );
      if (mounted) context.go(AppRoutes.student);
    } catch (error) {
      if (mounted) setState(() => _error = describeError(error, fallback: 'Could not save your profile.'));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Widget _tagEditor({
    required String label,
    required TextEditingController input,
    required List<String> values,
  }) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        TextField(
          controller: input,
          textInputAction: TextInputAction.done,
          onSubmitted: (_) => _addTag(input, values),
          decoration: fieldDecoration(
            label,
            suffixIcon: IconButton(
              tooltip: 'Add $label',
              onPressed: () => _addTag(input, values),
              icon: const Icon(Icons.add),
            ),
          ),
        ),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          children: [
            for (final value in values)
              InputChip(label: Text(value), onDeleted: () => setState(() => values.remove(value))),
          ],
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return Form(
      key: _formKey,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          DropdownButtonFormField<String>(
            initialValue: _stage,
            decoration: fieldDecoration('Academic stage'),
            items: [for (final stage in academicStages) DropdownMenuItem(value: stage, child: Text(stage))],
            onChanged: (value) => setState(() => _stage = value ?? _stage),
          ),
          const SizedBox(height: 16),
          _tagEditor(label: 'Core skill (e.g. Python)', input: _skillInput, values: _skills),
          const SizedBox(height: 16),
          _tagEditor(label: 'Interest (e.g. Robotics)', input: _interestInput, values: _interests),
          const SizedBox(height: 16),
          TextFormField(
            controller: _ambition,
            decoration: fieldDecoration('Career ambition', hint: 'e.g. AI Engineer'),
            validator: (value) => Validators.length(value, min: 2, max: 120, label: 'Career ambition'),
          ),
          if (_error != null) ...[
            const SizedBox(height: 16),
            ErrorBanner(message: _error!),
          ],
          const SizedBox(height: 20),
          LoadingButton(label: 'Save profile', onPressed: _submit, loading: _loading),
        ],
      ),
    );
  }
}
