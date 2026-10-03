import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;

import '../core/config/app_config.dart';
import '../core/network/api_client.dart';
import '../core/storage/key_value_store.dart';
import '../features/auth/data/auth_repository.dart';
import '../features/auth/data/session_store.dart';
import '../features/auth/presentation/auth_controller.dart';
import '../features/career_discovery/data/career_repository.dart';
import '../features/onboarding/data/onboarding_repository.dart';
import '../features/privacy/data/consent_store.dart';
import '../features/privacy/presentation/consent_controller.dart';
import '../features/profile/data/profile_repository.dart';
import '../features/reality_check/data/review_repository.dart';
import '../features/support/data/consultation_repository.dart';

/// Composition root: the one place where concrete classes are chosen and wired together.
///
/// Tests build the same graph with fakes by passing their own [KeyValueStore], [http.Client] or
/// by constructing [AppDependencies] directly with fake repositories.
class AppDependencies {
  AppDependencies({
    required this.config,
    required this.auth,
    required this.consent,
    required this.authRepository,
    required this.profileRepository,
    required this.onboardingRepository,
    required this.careerRepository,
    required this.reviewRepository,
    required this.consultationRepository,
    this.dispose,
  });

  factory AppDependencies.create({
    required AppConfig config,
    KeyValueStore? store,
    http.Client? httpClient,
  }) {
    final keyValueStore = store ?? SecureKeyValueStore();
    final sessionStore = SessionStore(keyValueStore);

    late final AuthController auth;
    final api = ApiClient(
      baseUrl: config.apiBaseUrl,
      timeout: config.requestTimeout,
      httpClient: httpClient,
      readToken: sessionStore.readToken,
      // Late-bound: the controller is created right below and only called at request time.
      onUnauthorized: () => auth.expireSession(),
    );

    final authRepository = RemoteAuthRepository(api);
    auth = AuthController(repository: authRepository, sessionStore: sessionStore);

    return AppDependencies(
      config: config,
      auth: auth,
      consent: ConsentController(ConsentStore(keyValueStore)),
      authRepository: authRepository,
      profileRepository: RemoteProfileRepository(api),
      onboardingRepository: RemoteOnboardingRepository(api),
      careerRepository: RemoteCareerRepository(api),
      reviewRepository: RemoteReviewRepository(api),
      consultationRepository: RemoteConsultationRepository(api),
      dispose: api.close,
    );
  }

  final AppConfig config;
  final AuthController auth;
  final ConsentController consent;
  final AuthRepository authRepository;
  final ProfileRepository profileRepository;
  final OnboardingRepository onboardingRepository;
  final CareerRepository careerRepository;
  final ReviewRepository reviewRepository;
  final ConsultationRepository consultationRepository;
  final VoidCallback? dispose;
}
