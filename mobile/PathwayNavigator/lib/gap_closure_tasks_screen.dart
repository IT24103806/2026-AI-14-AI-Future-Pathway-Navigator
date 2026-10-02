import 'package:flutter/material.dart';
import 'member4_api.dart';

class GapClosureTasksScreen extends StatefulWidget {
  final Member4Api api;
  final String reviewId;
  const GapClosureTasksScreen({super.key, required this.api, required this.reviewId});
  @override State<GapClosureTasksScreen> createState() => _GapClosureTasksScreenState();
}
class _GapClosureTasksScreenState extends State<GapClosureTasksScreen> {
  List<GapClosureTask> tasks = [];
  final title = TextEditingController();
  bool busy = false;
  String? error;
  @override void initState() { super.initState(); refresh(); }
  @override void dispose() { title.dispose(); super.dispose(); }
  Future<void> refresh() async {
    try { final result = await widget.api.getGapTasks(widget.reviewId);
      if (mounted) setState(() { tasks = result; error = null; });
    } catch (e) { if (mounted) setState(() => error = e.toString()); }
  }
  Future<void> run(Future<void> Function() operation) async {
    setState(() { busy = true; error = null; });
    try { await operation(); await refresh(); }
    catch (e) { if (mounted) setState(() => error = e.toString()); }
    finally { if (mounted) setState(() => busy = false); }
  }
  Future<void> edit(GapClosureTask task) async {
    final input = TextEditingController(text: task.title);
    var status = task.status;
    final value = await showDialog<(String, String)>(context: context, builder: (ctx) => StatefulBuilder(
      builder: (ctx, setDialogState) => AlertDialog(title: const Text('Edit task'),
        content: Column(mainAxisSize: MainAxisSize.min, children: [
          TextField(controller: input, maxLength: 160, decoration: const InputDecoration(labelText: 'Task title')),
          DropdownButton<String>(value: status, isExpanded: true, items: const [
            DropdownMenuItem(value: 'ToDo', child: Text('To do')),
            DropdownMenuItem(value: 'InProgress', child: Text('In progress')),
            DropdownMenuItem(value: 'Done', child: Text('Done')),
          ], onChanged: (value) { if (value != null) setDialogState(() => status = value); }),
        ]), actions: [TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Cancel')),
          FilledButton(onPressed: () => Navigator.pop(ctx, (input.text.trim(), status)), child: const Text('Save'))])));
    if (value != null && value.$1.isNotEmpty) await run(() => widget.api.updateGapTask(task, value.$1, value.$2));
    input.dispose();
  }
  @override Widget build(BuildContext context) => Scaffold(appBar: AppBar(title: const Text('My gap-closing tasks')),
    body: ListView(padding: const EdgeInsets.all(16), children: [
      Text('Create actions for this Reality Check. Only you can change your tasks.', style: Theme.of(context).textTheme.bodyMedium),
      Row(children: [Expanded(child: TextField(controller: title, maxLength: 160, decoration: const InputDecoration(labelText: 'New task'))),
        IconButton(tooltip: 'Add task', onPressed: busy ? null : () { final value = title.text.trim();
          if (value.isNotEmpty) run(() async { await widget.api.addGapTask(widget.reviewId, value); title.clear(); }); }, icon: const Icon(Icons.add))]),
      if (error != null) Text(error!, style: const TextStyle(color: Colors.red)),
      if (tasks.isEmpty) const Text('No tasks yet.'),
      ...tasks.map((task) => Card(
        child: ListTile(
          title: Text(task.title),
          subtitle: Text(task.status),
          onTap: busy ? null : () => edit(task),
          trailing: IconButton(
            tooltip: 'Delete ${task.title}',
            icon: const Icon(Icons.delete_outline),
            onPressed: busy ? null : () async {
              final confirm = await showDialog<bool>(
                context: context,
                builder: (ctx) => AlertDialog(
                  title: const Text('Delete task?'),
                  actions: [
                    TextButton(
                      onPressed: () => Navigator.pop(ctx, false),
                      child: const Text('Cancel'),
                    ),
                    TextButton(
                      onPressed: () => Navigator.pop(ctx, true),
                      child: const Text('Delete'),
                    ),
                  ],
                ),
              );
              if (confirm == true) {
                await run(() => widget.api.deleteGapTask(task.id));
              }
            },
          ),
        ),
      )),
    ]));
}
