import 'package:flutter/material.dart';
import 'package:provider/provider.dart';

import '../../../core/error/app_exception.dart';
import '../../../core/utils/validators.dart';
import '../../../core/widgets/common_widgets.dart';
import 'auth_controller.dart';
import 'auth_layout.dart';

/// Student self-registration. Counsellor/Admin accounts are not self-service on mobile.
class RegisterScreen extends StatefulWidget {
  const RegisterScreen({super.key});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();
  bool _loading = false;
  bool _obscure = true;
  String? _error;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    final auth = context.read<AuthController>();
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      await auth.register(_email.text, _password.text);
    } catch (error) {
      if (mounted) {
        setState(() => _error = describeError(error, fallback: 'Registration failed. Please try again.'));
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthLayout(
      title: 'Create your student account',
      showBack: true,
      children: [
        Form(
          key: _formKey,
          child: AutofillGroup(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                TextFormField(
                  controller: _email,
                  keyboardType: TextInputType.emailAddress,
                  textInputAction: TextInputAction.next,
                  autofillHints: const [AutofillHints.email],
                  decoration: fieldDecoration('Email'),
                  validator: Validators.email,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _password,
                  obscureText: _obscure,
                  textInputAction: TextInputAction.next,
                  autofillHints: const [AutofillHints.newPassword],
                  decoration: fieldDecoration(
                    'Password',
                    helper: 'At least 8 characters',
                    suffixIcon: IconButton(
                      tooltip: _obscure ? 'Show password' : 'Hide password',
                      onPressed: () => setState(() => _obscure = !_obscure),
                      icon: Icon(_obscure ? Icons.visibility : Icons.visibility_off),
                    ),
                  ),
                  validator: Validators.newPassword,
                ),
                const SizedBox(height: 16),
                TextFormField(
                  controller: _confirm,
                  obscureText: _obscure,
                  textInputAction: TextInputAction.done,
                  autofillHints: const [AutofillHints.newPassword],
                  onFieldSubmitted: (_) => _submit(),
                  decoration: fieldDecoration('Confirm password'),
                  validator: Validators.matches(() => _password.text),
                ),
              ],
            ),
          ),
        ),
        if (_error != null) ...[
          const SizedBox(height: 16),
          ErrorBanner(message: _error!),
        ],
        const SizedBox(height: 20),
        LoadingButton(label: 'Create account', onPressed: _submit, loading: _loading),
      ],
    );
  }
}
