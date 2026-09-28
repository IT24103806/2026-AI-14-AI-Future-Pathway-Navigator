import 'package:flutter/material.dart';
import 'member4_api.dart';

class RealityCheckSubmissionScreen extends StatefulWidget {
  final Member4Api api;
  const RealityCheckSubmissionScreen({super.key, required this.api});
  @override State<RealityCheckSubmissionScreen> createState() => _RealityCheckSubmissionScreenState();
}

class _RealityCheckSubmissionScreenState extends State<RealityCheckSubmissionScreen> {
  final _form = GlobalKey<FormState>();
  final _analysis = TextEditingController(), _career = TextEditingController(), _stream = TextEditingController(),
      _results = TextEditingController(), _skills = TextEditingController();
  String _budget = 'Medium'; bool _loading = false; String? _error;
  @override void dispose() { for (final c in [_analysis, _career, _stream, _results, _skills]) { c.dispose(); } super.dispose(); }
  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      await widget.api.startRealityCheck(analysisId: _analysis.text.trim(), targetCareer: _career.text.trim(),
        alStream: _stream.text.trim(), alResults: _results.text.trim(), budgetLevel: _budget,
        skills: _skills.text.split(',').map((s) => s.trim()).where((s) => s.isNotEmpty).toList());
      if (mounted) Navigator.pop(context, true);
    } catch (error) { if (mounted) setState(() => _error = error.toString()); }
    finally { if (mounted) setState(() => _loading = false); }
  }
  @override Widget build(BuildContext context) => Scaffold(appBar: AppBar(title: const Text('Run reality check')), body: Form(key: _form, child: ListView(padding: const EdgeInsets.all(20), children: [
    const Text('Submit the saved Career Discovery analysis for feasibility and safety validation.', style: TextStyle(fontSize: 16)), const SizedBox(height: 16),
    _field(_analysis, 'Pathway analysis ID', (v) => RegExp(r'^[0-9a-fA-F-]{36}$').hasMatch(v ?? '') ? null : 'Enter the saved analysis UUID.'),
    _field(_career, 'Target career', _required), _field(_stream, 'A/L stream', _required),
    _field(_results, 'A/L results (example: A,B,C)', (v) => RegExp(r'^[ABCFSabcfs](\s*[,/]\s*[ABCFSabcfs])*$').hasMatch(v ?? '') ? null : 'Use grades A, B, C, S or F.'),
    DropdownButtonFormField<String>(initialValue: _budget, decoration: const InputDecoration(labelText: 'Budget level', border: OutlineInputBorder()), items: ['Low','Medium','High'].map((v) => DropdownMenuItem(value: v, child: Text(v))).toList(), onChanged: (v) => _budget = v!), const SizedBox(height: 12),
    _field(_skills, 'Current skills (comma separated)', (_) => null),
    if (_error != null) Text(_error!, style: const TextStyle(color: Colors.red)), const SizedBox(height: 12),
    FilledButton.icon(onPressed: _loading ? null : _submit, icon: const Icon(Icons.fact_check), label: Text(_loading ? 'Checking…' : 'Run Agent 4')),
  ])));
  String? _required(String? value) => (value?.trim().length ?? 0) >= 2 ? null : 'This field is required.';
  Widget _field(TextEditingController c, String label, String? Function(String?) validator) => Padding(padding: const EdgeInsets.only(bottom: 12), child: TextFormField(controller: c, decoration: InputDecoration(labelText: label, border: const OutlineInputBorder()), validator: validator));
}
