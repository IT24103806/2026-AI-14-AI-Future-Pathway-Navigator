import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/constants/routes.dart';
import '../../../core/error/app_exception.dart';
import '../../../core/utils/validators.dart';
import '../../../core/widgets/common_widgets.dart';
import '../data/auth_repository.dart';
import 'auth_layout.dart';

enum _ResetStep { email, code, password, done }

/// Three-step password reset: e-mail -> 6-digit code -> new password (same flow as the web app).
class ForgotPasswordScreen extends StatefulWidget {
  const ForgotPasswordScreen({super.key});

  @override
  State<ForgotPasswordScreen> createState() => _ForgotPasswordScreenState();
}

class _ForgotPasswordScreenState extends State<ForgotPasswordScreen> {
  final _formKey = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _code = TextEditingController();
  final _password = TextEditingController();
  final _confirm = TextEditingController();
  _ResetStep _step = _ResetStep.email;
  bool _loading = false;
  String? _error;
  String? _info;

  @override
  void dispose() {
    _email.dispose();
    _code.dispose();
    _password.dispose();
    _confirm.dispose();
    super.dispose();
  }

  Future<void> _run(Future<void> Function(AuthRepository repo) action, _ResetStep next, {String? info}) async {
    if (!_formKey.currentState!.validate()) return;
    final repo = context.read<AuthRepository>();
    setState(() {
      _loading = true;
      _error = null;
      _info = null;
    });
    try {
      await action(repo);
      if (mounted) {
        setState(() {
          _step = next;
          _info = info;
        });
      }
    } catch (error) {
      if (mounted) setState(() => _error = describeError(error));
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _sendCode() => _run(
        (repo) => repo.requestPasswordReset(_email.text),
        _ResetStep.code,
        info: 'If the address is registered, a 6-digit code has been sent.',
      );

  Future<void> _verifyCode() =>
      _run((repo) => repo.verifyResetCode(email: _email.text, code: _code.text), _ResetStep.password);

  Future<void> _resetPassword() => _run(
        (repo) => repo.resetPassword(
          email: _email.text,
          code: _code.text,
          newPassword: _password.text,
          confirmPassword: _confirm.text,
        ),
        _ResetStep.done,
      );

  @override
  Widget build(BuildContext context) {
    if (_step == _ResetStep.done) {
      return AuthLayout(
        title: 'Password updated',
        showBack: true,
        children: [
          const Text('Your password was changed. You can now sign in with the new password.', textAlign: TextAlign.center),
          const SizedBox(height: 20),
          FilledButton(onPressed: () => context.go(AppRoutes.login), child: const Text('Back to sign in')),
        ],
      );
    }

    final fields = <Widget>[];
    late final String buttonLabel;
    late final Future<void> Function() onSubmit;

    switch (_step) {
      case _ResetStep.email:
        buttonLabel = 'Send code';
        onSubmit = _sendCode;
        fields.add(
          TextFormField(
            controller: _email,
            keyboardType: TextInputType.emailAddress,
            decoration: fieldDecoration('Registered email'),
            validator: Validators.email,
          ),
        );
      case _ResetStep.code:
        buttonLabel = 'Verify code';
        onSubmit = _verifyCode;
        fields.add(
          TextFormField(
            controller: _code,
            keyboardType: TextInputType.number,
            maxLength: 6,
            decoration: fieldDecoration('6-digit code'),
            validator: Validators.otp,
          ),
        );
      case _ResetStep.password:
      case _ResetStep.done:
        buttonLabel = 'Reset password';
        onSubmit = _resetPassword;
        fields.addAll([
          TextFormField(
            controller: _password,
            obscureText: true,
            decoration: fieldDecoration('New password', helper: 'At least 8 characters'),
            validator: Validators.newPassword,
          ),
          const SizedBox(height: 16),
          TextFormField(
            controller: _confirm,
            obscureText: true,
            decoration: fieldDecoration('Confirm new password'),
            validator: Validators.matches(() => _password.text),
          ),
        ]);
    }

    return AuthLayout(
      title: 'Reset your password',
      showBack: true,
      children: [
        if (_info != null) ...[
          Text(_info!, textAlign: TextAlign.center),
          const SizedBox(height: 12),
        ],
        Form(key: _formKey, child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: fields)),
        if (_error != null) ...[
          const SizedBox(height: 12),
          ErrorBanner(message: _error!),
        ],
        const SizedBox(height: 16),
        LoadingButton(label: buttonLabel, onPressed: onSubmit, loading: _loading),
      ],
    );
  }
}
