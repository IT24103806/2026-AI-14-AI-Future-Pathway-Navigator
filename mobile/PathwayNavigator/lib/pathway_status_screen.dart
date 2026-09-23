import 'package:flutter/material.dart';
import 'member4_api.dart';
import 'member4_login_screen.dart';
import 'reality_check_submission_screen.dart';

class PathwayStatusScreen extends StatefulWidget {
  final Member4Api api;
  const PathwayStatusScreen({super.key, required this.api});

  @override
  State<PathwayStatusScreen> createState() => _PathwayStatusScreenState();
}

class _PathwayStatusScreenState extends State<PathwayStatusScreen> {
  PathwayReviewStatus? _status;
  List<PathwayReviewStatus> _history = [];
  bool _loading = true;
  String? _error;
  DateTime? _bookedDate;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final values = await Future.wait([
        widget.api.getMyStatus(),
        widget.api.getMyHistory(),
      ]);
      if (mounted) {
        setState(() {
          _status = values[0] as PathwayReviewStatus;
          _history = values[1] as List<PathwayReviewStatus>;
        });
      }
    } on Member4ApiException catch (error) {
      if (error.statusCode == 401) {
        await widget.api.logout();
        if (mounted) {
          Navigator.of(context).pushReplacement(
            MaterialPageRoute(builder: (_) => Member4LoginScreen(api: widget.api)),
          );
        }
      } else if (mounted) {
        setState(() => _error = error.message);
      }
    } catch (_) {
      if (mounted) {
        setState(() => _error = 'Unexpected error. Check your connection and retry.');
      }
    } finally {
      if (mounted) {
        setState(() => _loading = false);
      }
    }
  }

  Future<void> _selectConsultationDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: DateTime.now().add(const Duration(days: 1)),
      firstDate: DateTime.now(),
      lastDate: DateTime.now().add(const Duration(days: 60)),
    );
    if (picked != null && mounted) {
      setState(() => _bookedDate = picked);
    }
  }

  Color _statusColor(String status) => switch (status) {
        'Approved' => Colors.green,
        'Rejected' => Colors.red,
        'NeedsRevision' => Colors.orange,
        _ => Colors.blue,
      };

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(
          title: const Text('Feasibility & approval'),
          actions: [
            IconButton(
              tooltip: 'New reality check',
              onPressed: () async {
                final changed = await Navigator.of(context).push<bool>(
                  MaterialPageRoute(builder: (_) => RealityCheckSubmissionScreen(api: widget.api)),
                );
                if (changed == true) {
                  _load();
                }
              },
              icon: const Icon(Icons.add_task),
            ),
            IconButton(
              tooltip: 'Refresh',
              onPressed: _load,
              icon: const Icon(Icons.refresh),
            ),
            IconButton(
              tooltip: 'Sign out',
              onPressed: () async {
                await widget.api.logout();
                if (context.mounted) {
                  Navigator.of(context).pushReplacement(
                    MaterialPageRoute(builder: (_) => Member4LoginScreen(api: widget.api)),
                  );
                }
              },
              icon: const Icon(Icons.logout),
            ),
          ],
        ),
        body: RefreshIndicator(
          onRefresh: _load,
          child: _loading
              ? ListView(
                  children: const [
                    SizedBox(height: 260),
                    Center(child: CircularProgressIndicator()),
                  ],
                )
              : _error != null
                  ? ListView(
                      padding: const EdgeInsets.all(24),
                      children: [
                        const SizedBox(height: 160),
                        const Icon(Icons.info_outline, size: 52),
                        const SizedBox(height: 12),
                        Text(_error!, textAlign: TextAlign.center),
                        const SizedBox(height: 12),
                        FilledButton(onPressed: _load, child: const Text('Retry')),
                      ],
                    )
                  : _content(),
        ),
      );

  Widget _content() {
    final status = _status!;
    final color = _statusColor(status.status);
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        Card(
          color: color.withValues(alpha: .1),
          child: ListTile(
            leading: Icon(Icons.verified_outlined, color: color, size: 36),
            title: Text(status.targetCareer, style: const TextStyle(fontWeight: FontWeight.bold)),
            subtitle: Text('Workflow ${status.workflowId}\nSubmitted ${status.createdAt.toLocal()}'),
            trailing: Chip(label: Text(status.status)),
          ),
        ),
        const SizedBox(height: 12),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text('Reality match', style: TextStyle(fontWeight: FontWeight.bold)),
                    Text('${status.feasibilityScore}%'),
                  ],
                ),
                const SizedBox(height: 10),
                LinearProgressIndicator(value: status.feasibilityScore / 100),
              ],
            ),
          ),
        ),
        if (status.isHighRisk)
          Card(
            color: Colors.red.shade50,
            child: ListTile(
              leading: const Icon(Icons.warning_amber, color: Colors.red),
              title: const Text('Risk requiring human review'),
              subtitle: Text(status.riskReason ?? 'Risk details unavailable.'),
            ),
          ),
        const SizedBox(height: 12),
        const Text('Identified skill gaps', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 16)),
        const SizedBox(height: 8),
        status.missingSkills.isEmpty
            ? const Text('No major prerequisite gaps identified.')
            : Wrap(
                spacing: 8,
                children: status.missingSkills.map((skill) => Chip(label: Text(skill))).toList(),
              ),
        const SizedBox(height: 12),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Qualification & entry reality', style: TextStyle(fontWeight: FontWeight.bold)),
                const SizedBox(height: 8),
                Text(status.degreeRequirement),
                if (status.subjectRequirements.isNotEmpty) ...[
                  const SizedBox(height: 8),
                  Text('Subjects: ${status.subjectRequirements.join(', ')}'),
                ],
                if (status.entryRequirements.isNotEmpty) ...[
                  const SizedBox(height: 8),
                  ...status.entryRequirements.map((item) => Text('• $item')),
                ],
                const SizedBox(height: 8),
                Text('Cost: ${status.costGuidance}'),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Shortest gap-closing plan', style: TextStyle(fontWeight: FontWeight.bold)),
                const SizedBox(height: 8),
                ...status.gapClosurePlan.asMap().entries.map((entry) => Text('${entry.key + 1}. ${entry.value}')),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        Card(
          child: ListTile(
            leading: const Icon(Icons.support_agent),
            title: const Text('Counsellor feedback'),
            subtitle: Text(
              status.feedback ??
                  (status.status == 'Pending'
                      ? 'Waiting for an authorized counsellor decision.'
                      : 'No feedback provided.'),
            ),
          ),
        ),
        if (status.status == 'NeedsRevision')
          Padding(
            padding: const EdgeInsets.only(top: 12),
            child: FilledButton.icon(
              onPressed: _selectConsultationDate,
              icon: const Icon(Icons.calendar_month),
              label: Text(
                _bookedDate == null
                    ? 'Choose consultation date'
                    : 'Consultation: ${_bookedDate!.toLocal().toString().split(' ').first}',
              ),
            ),
          ),
        const SizedBox(height: 24),
        const Text('Review history', style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
        ..._history.map(
          (item) => ListTile(
            contentPadding: EdgeInsets.zero,
            leading: Icon(Icons.circle, size: 12, color: _statusColor(item.status)),
            title: Text(item.targetCareer),
            subtitle: Text(item.createdAt.toLocal().toString()),
            trailing: Text(item.status),
          ),
        ),
      ],
    );
  }
}