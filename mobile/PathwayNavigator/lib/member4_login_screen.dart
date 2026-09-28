import 'package:flutter/material.dart';
import 'member4_api.dart';
import 'pathway_status_screen.dart';

class Member4LoginScreen extends StatefulWidget {
  final Member4Api api;
  const Member4LoginScreen({super.key, required this.api});
  @override State<Member4LoginScreen> createState() => _Member4LoginScreenState();
}

class _Member4LoginScreenState extends State<Member4LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController(), _password = TextEditingController();
  bool _loading = false; String? _error;
  @override void dispose() { _email.dispose(); _password.dispose(); super.dispose(); }

  Future<void> _login() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });
    try {
      await widget.api.login(_email.text.trim(), _password.text);
      if (mounted) Navigator.of(context).pushReplacement(MaterialPageRoute(builder: (_) => PathwayStatusScreen(api: widget.api)));
    } catch (error) { if (mounted) setState(() => _error = error.toString()); }
    finally { if (mounted) setState(() => _loading = false); }
  }

  @override Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Student sign in')),
    body: Center(child: ConstrainedBox(constraints: const BoxConstraints(maxWidth: 480), child: Padding(
      padding: const EdgeInsets.all(24), child: Form(key: _formKey, child: Column(mainAxisSize: MainAxisSize.min, children: [
        const Icon(Icons.route, size: 58, color: Color(0xFF2563EB)), const SizedBox(height: 16),
        const Text('AI Future Pathway Navigator', style: TextStyle(fontSize: 22, fontWeight: FontWeight.bold)),
        const SizedBox(height: 24),
        TextFormField(controller: _email, keyboardType: TextInputType.emailAddress, decoration: const InputDecoration(labelText: 'Email', border: OutlineInputBorder()), validator: (v) => v != null && v.contains('@') ? null : 'Enter a valid email.'),
        const SizedBox(height: 14),
        TextFormField(controller: _password, obscureText: true, decoration: const InputDecoration(labelText: 'Password', border: OutlineInputBorder()), validator: (v) => (v?.length ?? 0) >= 6 ? null : 'Password must contain at least 6 characters.'),
        if (_error != null) Padding(padding: const EdgeInsets.only(top: 12), child: Text(_error!, style: const TextStyle(color: Colors.red))),
        const SizedBox(height: 18), FilledButton(onPressed: _loading ? null : _login, child: Text(_loading ? 'Signing in…' : 'Sign in')),
      ]))),
    )),
  );
}
