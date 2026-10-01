import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/constants/routes.dart';
import '../../../core/widgets/common_widgets.dart';
import '../../auth/presentation/auth_controller.dart';

/// Governance overview. The web app shows the same overview; the user/role and catalogue
/// administration tools it mentions are not implemented in the backend yet, so they are
/// described, not offered.
class AdminDashboardScreen extends StatelessWidget {
  const AdminDashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthController>();
    return Scaffold(
      appBar: AppBar(
        title: const Text('Administration'),
        actions: [
          IconButton(
            tooltip: 'Settings',
            onPressed: () => context.push(AppRoutes.settings),
            icon: const Icon(Icons.settings_outlined),
          ),
          IconButton(tooltip: 'Sign out', onPressed: auth.logout, icon: const Icon(Icons.logout)),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text('Administration & governance', style: Theme.of(context).textTheme.headlineSmall),
          Text('Signed in as ${auth.session?.email ?? ''}'),
          const SizedBox(height: 16),
          Card(
            child: ListTile(
              onTap: () => context.push(AppRoutes.counsellor),
              leading: const Icon(Icons.fact_check_outlined),
              title: const Text('Approval oversight'),
              subtitle: const Text('Inspect Reality Check workflows, validation evidence, tool calls and human decisions.'),
              trailing: const Icon(Icons.chevron_right),
            ),
          ),
          const SectionCard(
            title: 'User & role governance',
            icon: Icons.group_outlined,
            child: Text('Managing user access and role assignment is an administrator responsibility.'),
          ),
          const SectionCard(
            title: 'Career catalogue governance',
            icon: Icons.menu_book_outlined,
            child: Text('Maintain the approved career, education and market-data sources used by the agents.'),
          ),
          const SectionCard(
            title: 'Audit & safe failure',
            icon: Icons.manage_search,
            child: Text('Review workflow IDs, deterministic validations, failures and approval records.'),
          ),
        ],
      ),
    );
  }
}
