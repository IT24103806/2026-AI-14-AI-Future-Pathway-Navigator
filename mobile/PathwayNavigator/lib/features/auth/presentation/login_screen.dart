import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/constants/routes.dart';
import '../../../core/error/app_exception.dart';
import '../../../core/utils/validators.dart';
import '../../../core/widgets/common_widgets.dart';
import 'auth_controller.dart';
import 'auth_layout.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _loading = false;
  bool _obscure = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    if (context.read<AuthController>().consumeSessionExpired()) {
      _error = 'Your session has expired. Please sign in again.';
    }
  }

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
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
      // On success the router redirects to the role's home screen.
      await auth.login(_email.text, _password.text);
    } catch (error) {
      if (mounted) setState(() => _error = describeError(error, fallback: 'Sign-in failed. Please try again.'));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthLayout(
      title: 'Sign in',
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
                  textInputAction: TextInputAction.done,
                  autofillHints: const [AutofillHints.password],
                  onFieldSubmitted: (_) => _submit(),
                  decoration: fieldDecoration(
                    'Password',
                    suffixIcon: IconButton(
                      tooltip: _obscure ? 'Show password' : 'Hide password',
                      onPressed: () => setState(() => _obscure = !_obscure),
                      icon: Icon(_obscure ? Icons.visibility : Icons.visibility_off),
                    ),
                  ),
                  validator: Validators.password,
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
        LoadingButton(label: 'Sign in', onPressed: _submit, loading: _loading),
        const SizedBox(height: 8),
        TextButton(
          onPressed: _loading ? null : () => context.push(AppRoutes.forgotPassword),
          child: const Text('Forgot password?'),
        ),
        TextButton(
          onPressed: _loading ? null : () => context.push(AppRoutes.register),
          child: const Text('New student? Create an account'),
        ),
      ],
    );
  }
}
