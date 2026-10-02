import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../core/constants/roles.dart';
import '../core/constants/routes.dart';
import '../features/auth/presentation/auth_controller.dart';
import '../features/auth/presentation/forgot_password_screen.dart';
import '../features/auth/presentation/login_screen.dart';
import '../features/auth/presentation/register_screen.dart';
import '../features/career_discovery/presentation/career_discovery_screen.dart';
import '../features/dashboard/presentation/admin_dashboard_screen.dart';
import '../features/dashboard/presentation/settings_screen.dart';
import '../features/dashboard/presentation/student_dashboard_screen.dart';
import '../features/onboarding/presentation/onboarding_screen.dart';
import '../features/privacy/presentation/consent_controller.dart';
import '../features/privacy/presentation/consent_screen.dart';
import '../features/reality_check/presentation/counsellor_queue_screen.dart';
import '../features/reality_check/presentation/counsellor_review_screen.dart';
import '../features/reality_check/presentation/student_reality_screen.dart';

/// Pure routing policy (no Flutter dependency, unit-tested): returns the location the user must
/// be sent to, or null to stay where they are.
///
/// Order matters: wait for state -> privacy consent -> authentication -> role authorisation.
/// NOTE: this is a UX guard only. The API enforces authorisation server-side.
String? resolveRedirect({
  required String location,
  required AuthStatus authStatus,
  required String? role,
  required bool consentLoaded,
  required bool consentAccepted,
}) {
  if (authStatus == AuthStatus.unknown || !consentLoaded) {
    return location == AppRoutes.splash ? null : AppRoutes.splash;
  }

  if (!consentAccepted) {
    return location == AppRoutes.consent ? null : AppRoutes.consent;
  }

  final isPublic = AppRoutes.publicRoutes.contains(location);
  final isGate = location == AppRoutes.splash || location == AppRoutes.consent;

  if (authStatus == AuthStatus.unauthenticated) {
    if (isPublic) return null;
    return AppRoutes.login;
  }

  // Authenticated from here on.
  final home = homeRouteForRole(role);
  if (isPublic || isGate) return home;

  if (_isUnder(location, AppRoutes.student) && role != Roles.student) return home;
  if (_isUnder(location, AppRoutes.counsellor) && !Roles.isStaff(role)) return home;
  if (_isUnder(location, AppRoutes.admin) && role != Roles.admin) return home;
  return null;
}

bool _isUnder(String location, String base) => location == base || location.startsWith('$base/');

GoRouter buildRouter({
  required AuthController auth,
  required ConsentController consent,
  String initialLocation = AppRoutes.splash,
}) {
  return GoRouter(
    initialLocation: initialLocation,
    refreshListenable: Listenable.merge([auth, consent]),
    redirect: (context, state) => resolveRedirect(
      location: state.uri.path,
      authStatus: auth.status,
      role: auth.role,
      consentLoaded: consent.isLoaded,
      consentAccepted: consent.accepted,
    ),
    errorBuilder: (context, state) => Scaffold(
      appBar: AppBar(title: const Text('Not found')),
      body: Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Text('That page does not exist.'),
            const SizedBox(height: 12),
            FilledButton(onPressed: () => context.go(homeRouteForRole(auth.role)), child: const Text('Go home')),
          ],
        ),
      ),
    ),
    routes: [
      GoRoute(path: AppRoutes.splash, builder: (_, _) => const _SplashScreen()),
      GoRoute(path: AppRoutes.consent, builder: (_, _) => const ConsentScreen()),
      GoRoute(path: AppRoutes.login, builder: (_, _) => const LoginScreen()),
      GoRoute(path: AppRoutes.register, builder: (_, _) => const RegisterScreen()),
      GoRoute(path: AppRoutes.forgotPassword, builder: (_, _) => const ForgotPasswordScreen()),
      GoRoute(path: AppRoutes.settings, builder: (_, _) => const SettingsScreen()),
      GoRoute(path: AppRoutes.student, builder: (_, _) => const StudentDashboardScreen()),
      GoRoute(path: AppRoutes.onboarding, builder: (_, _) => const OnboardingScreen()),
      GoRoute(path: AppRoutes.careerDiscovery, builder: (_, _) => const CareerDiscoveryScreen()),
      GoRoute(path: AppRoutes.realityCheck, builder: (_, _) => const StudentRealityScreen()),
      GoRoute(path: AppRoutes.counsellor, builder: (_, _) => const CounsellorQueueScreen()),
      GoRoute(
        path: '${AppRoutes.counsellor}/review/:id',
        builder: (_, state) => CounsellorReviewScreen(reviewId: state.pathParameters['id'] ?? ''),
      ),
      GoRoute(path: AppRoutes.admin, builder: (_, _) => const AdminDashboardScreen()),
    ],
  );
}

class _SplashScreen extends StatelessWidget {
  const _SplashScreen();

  @override
  Widget build(BuildContext context) => const Scaffold(body: Center(child: CircularProgressIndicator()));
}
