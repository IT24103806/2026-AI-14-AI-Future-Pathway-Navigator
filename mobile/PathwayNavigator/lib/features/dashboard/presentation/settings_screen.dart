import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/constants/routes.dart';
import '../../../core/config/app_config.dart';
import '../../../core/utils/date_format.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../privacy/presentation/consent_controller.dart';
import '../../privacy/presentation/privacy_notice.dart';

/// Account, privacy and about information. Reachable by every role.
class SettingsScreen extends StatelessWidget {
  const SettingsScreen({super.key});

  Future<void> _withdraw(BuildContext context) async {
    final consent = context.read<ConsentController>();
    final auth = context.read<AuthController>();
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Withdraw consent?'),
        content: const Text('You will be signed out and asked to review the privacy notice again before using the app.'),
        actions: [
          TextButton(onPressed: () => Navigator.of(dialogContext).pop(false), child: const Text('Cancel')),
          FilledButton(onPressed: () => Navigator.of(dialogContext).pop(true), child: const Text('Withdraw')),
        ],
      ),
    );
    if (confirmed != true) return;
    await auth.logout();
    await consent.withdraw();
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthController>();
    final config = context.read<AppConfig>();
    final session = auth.session;

    return Scaffold(
      appBar: AppBar(title: const Text('Settings')),
      body: ListView(
        children: [
          ListTile(
            leading: const Icon(Icons.person_outline),
            title: Text(session?.email ?? 'Not signed in'),
            subtitle: Text('Role: ${session?.role ?? '-'}'
                '${session == null ? '' : '\nSession valid until ${formatDateTime(session.expiresAt)}'}'),
            isThreeLine: session != null,
          ),
          const Divider(),
          ListTile(
            leading: const Icon(Icons.privacy_tip_outlined),
            title: const Text('Privacy & AI use'),
            onTap: () => showModalBottomSheet<void>(
              context: context,
              isScrollControlled: true,
              builder: (_) => DraggableScrollableSheet(
                expand: false,
                initialChildSize: 0.8,
                builder: (_, scrollController) => SingleChildScrollView(
                  controller: scrollController,
                  padding: const EdgeInsets.all(24),
                  child: PrivacyNotice(contact: config.privacyContact),
                ),
              ),
            ),
          ),
          ListTile(
            leading: const Icon(Icons.gpp_bad_outlined),
            title: const Text('Withdraw consent'),
            subtitle: const Text('Signs you out and shows the privacy notice again.'),
            onTap: () => _withdraw(context),
          ),
          ListTile(
            leading: const Icon(Icons.description_outlined),
            title: const Text('Open-source licences'),
            onTap: () => showLicensePage(context: context, applicationName: 'Pathway Navigator'),
          ),
          if (auth.isAuthenticated)
            ListTile(
              leading: const Icon(Icons.logout),
              title: const Text('Sign out'),
              onTap: () async {
                final router = GoRouter.of(context);
                await auth.logout();
                router.go(AppRoutes.login);
              },
            ),
          const Divider(),
          const ListTile(
            leading: Icon(Icons.info_outline),
            title: Text('Pathway Navigator'),
            subtitle: Text('AI guidance is advice, not a decision. Confirm important choices with a counsellor.'),
          ),
          if (kDebugMode)
            ListTile(
              leading: const Icon(Icons.bug_report_outlined),
              title: const Text('API base URL (debug only)'),
              subtitle: Text(config.apiBaseUrl),
            ),
        ],
      ),
    );
  }
}
