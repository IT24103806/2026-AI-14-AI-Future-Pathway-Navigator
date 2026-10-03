import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';

import '../core/config/app_config.dart';
import '../features/auth/data/auth_repository.dart';
import '../features/auth/presentation/auth_controller.dart';
import '../features/career_discovery/data/career_repository.dart';
import '../features/onboarding/data/onboarding_repository.dart';
import '../features/privacy/presentation/consent_controller.dart';
import '../features/profile/data/profile_repository.dart';
import '../features/reality_check/data/review_repository.dart';
import '../features/support/data/consultation_repository.dart';
import '../features/support/presentation/notification_controller.dart';
import 'app_theme.dart';
import 'dependencies.dart';
import 'router.dart';

class PathwayNavigatorApp extends StatefulWidget {
  const PathwayNavigatorApp({super.key, required this.dependencies});

  final AppDependencies dependencies;

  @override
  State<PathwayNavigatorApp> createState() => _PathwayNavigatorAppState();
}

class _PathwayNavigatorAppState extends State<PathwayNavigatorApp> {
  late final GoRouter _router;

  @override
  void initState() {
    super.initState();
    final deps = widget.dependencies;
    _router = buildRouter(auth: deps.auth, consent: deps.consent);
    // Read persisted state; the router shows a splash until both have loaded.
    deps.auth.restore();
    deps.consent.load();
  }

  @override
  void dispose() {
    _router.dispose();
    widget.dependencies.auth.dispose();
    widget.dependencies.consent.dispose();
    widget.dependencies.dispose?.call();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final deps = widget.dependencies;
    return MultiProvider(
      providers: [
        Provider<AppConfig>.value(value: deps.config),
        ChangeNotifierProvider<AuthController>.value(value: deps.auth),
        ChangeNotifierProvider<ConsentController>.value(value: deps.consent),
        Provider<AuthRepository>.value(value: deps.authRepository),
        Provider<ProfileRepository>.value(value: deps.profileRepository),
        Provider<OnboardingRepository>.value(value: deps.onboardingRepository),
        Provider<CareerRepository>.value(value: deps.careerRepository),
        Provider<ReviewRepository>.value(value: deps.reviewRepository),
        Provider<ConsultationRepository>.value(value: deps.consultationRepository),
        // One notification controller for the whole app: the badge in an app bar and the inbox
        // screen must show the same unread count.
        ChangeNotifierProxyProvider<AuthController, NotificationController>(
          create: (_) => NotificationController(deps.consultationRepository),
          update: (_, auth, controller) {
            // `create` always runs before the first update, so the notifier is non-null here.
            final notifications = controller!;
            if (auth.isAuthenticated) {
              notifications.start();
            } else {
              notifications.stop();
            }
            return notifications;
          },
        ),
      ],
      child: MaterialApp.router(
        title: 'Pathway Navigator',
        debugShowCheckedModeBanner: false,
        theme: AppTheme.light(),
        darkTheme: AppTheme.dark(),
        routerConfig: _router,
      ),
    );
  }
}
