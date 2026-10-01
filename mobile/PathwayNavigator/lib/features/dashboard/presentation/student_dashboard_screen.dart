import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../../../core/constants/routes.dart';
import '../../../core/widgets/common_widgets.dart';
import '../../auth/presentation/auth_controller.dart';
import '../../profile/data/profile_repository.dart';
import 'student_dashboard_controller.dart';

class StudentDashboardScreen extends StatelessWidget {
  const StudentDashboardScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider<StudentDashboardController>(
      create: (context) => StudentDashboardController(context.read<ProfileRepository>())..load(),
      child: const _DashboardBody(),
    );
  }
}

class _DashboardBody extends StatelessWidget {
  const _DashboardBody();

  /// Opens a pushed screen and refreshes the profile when the user comes back
  /// (onboarding may have changed it).
  Future<void> _open(BuildContext context, StudentDashboardController controller, String route) async {
    await context.push<Object?>(route);
    controller.load();
  }

  @override
  Widget build(BuildContext context) {
    final controller = context.watch<StudentDashboardController>();
    final auth = context.watch<AuthController>();
    final profile = controller.profile;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Pathway Navigator'),
        actions: [
          IconButton(
            tooltip: 'Settings',
            onPressed: () => context.push(AppRoutes.settings),
            icon: const Icon(Icons.settings_outlined),
          ),
          IconButton(tooltip: 'Sign out', onPressed: auth.logout, icon: const Icon(Icons.logout)),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: controller.load,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text('Welcome', style: Theme.of(context).textTheme.headlineSmall),
            Text(auth.session?.email ?? '', style: Theme.of(context).textTheme.bodyMedium),
            const SizedBox(height: 16),
            if (controller.isLoading && profile == null)
              const Padding(padding: EdgeInsets.all(24), child: LoadingView())
            else if (controller.error != null)
              ErrorBanner(message: controller.error!, onRetry: controller.load)
            else if (!controller.isOnboarded)
              SectionCard(
                title: 'Finish setting up',
                icon: Icons.person_outline,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Tell us about your stage, skills and interests to get personalised pathways.'),
                    const SizedBox(height: 12),
                    FilledButton(
                      onPressed: () => _open(context, controller, AppRoutes.onboarding),
                      child: const Text('Start onboarding'),
                    ),
                  ],
                ),
              )
            else if (profile != null)
              SectionCard(
                title: profile.careerAmbitions.isEmpty ? 'Your profile' : 'Target career: ${profile.careerAmbitions}',
                icon: Icons.badge_outlined,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(profile.academicStage),
                    const SizedBox(height: 12),
                    Text('Core skills', style: Theme.of(context).textTheme.titleSmall),
                    const SizedBox(height: 4),
                    TagWrap(items: profile.coreSkills, emptyText: 'No skills added'),
                    const SizedBox(height: 12),
                    Text('Hobbies & interests', style: Theme.of(context).textTheme.titleSmall),
                    const SizedBox(height: 4),
                    TagWrap(items: profile.hobbiesInterests, emptyText: 'No interests added'),
                    const SizedBox(height: 8),
                    TextButton.icon(
                      onPressed: () => _open(context, controller, AppRoutes.onboarding),
                      icon: const Icon(Icons.smart_toy_outlined),
                      label: const Text('Re-run onboarding'),
                    ),
                  ],
                ),
              ),
            _FeatureTile(
              icon: Icons.explore_outlined,
              title: 'AI pathway recommender',
              body: 'Explore career pathways tailored to your profile.',
              onTap: () => context.push(AppRoutes.careerDiscovery),
            ),
            _FeatureTile(
              icon: Icons.verified_user_outlined,
              title: 'Reality Check & approval',
              body: 'See feasibility evidence, skill gaps, counsellor decision and your gap-closing plan.',
              onTap: () => context.push(AppRoutes.realityCheck),
            ),
          ],
        ),
      ),
    );
  }
}

class _FeatureTile extends StatelessWidget {
  const _FeatureTile({required this.icon, required this.title, required this.body, required this.onTap});

  final IconData icon;
  final String title;
  final String body;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        onTap: onTap,
        leading: Icon(icon),
        title: Text(title),
        subtitle: Text(body),
        trailing: const Icon(Icons.chevron_right),
      ),
    );
  }
}
