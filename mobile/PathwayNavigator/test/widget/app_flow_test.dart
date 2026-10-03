import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:pathway_navigator/app/app.dart';
import 'package:pathway_navigator/app/dependencies.dart';
import 'package:pathway_navigator/core/error/app_exception.dart';
import 'package:pathway_navigator/core/storage/key_value_store.dart';
import 'package:pathway_navigator/features/auth/data/auth_models.dart';
import 'package:pathway_navigator/features/auth/data/session_store.dart';
import 'package:pathway_navigator/features/auth/presentation/auth_controller.dart';
import 'package:pathway_navigator/features/privacy/data/consent_store.dart';
import 'package:pathway_navigator/features/privacy/presentation/consent_controller.dart';

import '../support/fakes.dart';
import '../support/support_fakes.dart';

Future<AppDependencies> buildDeps({
  bool consentAccepted = true,
  AuthSession? storedSession,
  FakeAuthRepository? authRepository,
  FakeReviewRepository? reviewRepository,
}) async {
  final store = InMemoryKeyValueStore();
  final sessions = SessionStore(store);
  final consentStore = ConsentStore(store);
  if (consentAccepted) await consentStore.accept();
  if (storedSession != null) await sessions.save(storedSession);

  final authRepo = authRepository ?? FakeAuthRepository();
  return AppDependencies(
    config: testConfig,
    auth: AuthController(repository: authRepo, sessionStore: sessions),
    consent: ConsentController(consentStore),
    authRepository: authRepo,
    profileRepository: FakeProfileRepository(),
    onboardingRepository: FakeOnboardingRepository(),
    careerRepository: FakeCareerRepository(),
    reviewRepository: reviewRepository ?? FakeReviewRepository(),
    consultationRepository: FakeConsultationRepository(),
  );
}

Future<void> pumpApp(WidgetTester tester, AppDependencies deps) async {
  await tester.pumpWidget(PathwayNavigatorApp(dependencies: deps));
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('first run shows the privacy notice and blocks until it is accepted', (tester) async {
    final deps = await buildDeps(consentAccepted: false);
    await pumpApp(tester, deps);

    expect(find.text('Before you start'), findsOneWidget);
    FilledButton continueButton() => tester.widget<FilledButton>(find.widgetWithText(FilledButton, 'Continue'));
    expect(continueButton().onPressed, isNull);

    await tester.tap(find.byType(CheckboxListTile));
    await tester.pump();
    expect(continueButton().onPressed, isNotNull);

    await tester.tap(find.widgetWithText(FilledButton, 'Continue'));
    await tester.pumpAndSettle();

    expect(find.text('Before you start'), findsNothing);
    expect(find.widgetWithText(FilledButton, 'Sign in'), findsOneWidget);
  });

  testWidgets('login validates input before calling the API', (tester) async {
    final authRepository = FakeAuthRepository();
    await pumpApp(tester, await buildDeps(authRepository: authRepository));

    await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
    await tester.pumpAndSettle();

    expect(find.text('Email address is required.'), findsOneWidget);
    expect(authRepository.calls, isEmpty);
  });

  testWidgets('a student who signs in lands on the student dashboard', (tester) async {
    final authRepository = FakeAuthRepository();
    await pumpApp(tester, await buildDeps(authRepository: authRepository));

    await tester.enterText(find.widgetWithText(TextFormField, 'Email'), 'student@example.com');
    await tester.enterText(find.widgetWithText(TextFormField, 'Password'), 'password123');
    await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
    await tester.pumpAndSettle();

    expect(authRepository.calls, ['login:student@example.com']);
    expect(find.text('Welcome'), findsOneWidget);
    expect(find.text('Finish setting up'), findsOneWidget);
  });

  testWidgets('a failed sign-in shows the server message and stays on the login screen', (tester) async {
    final authRepository = FakeAuthRepository(error: const AppException('Invalid credentials'));
    await pumpApp(tester, await buildDeps(authRepository: authRepository));

    await tester.enterText(find.widgetWithText(TextFormField, 'Email'), 'student@example.com');
    await tester.enterText(find.widgetWithText(TextFormField, 'Password'), 'wrongpassword');
    await tester.tap(find.widgetWithText(FilledButton, 'Sign in'));
    await tester.pumpAndSettle();

    expect(find.text('Invalid credentials'), findsOneWidget);
    expect(find.text('Welcome'), findsNothing);
  });

  testWidgets('a stored counsellor session opens the approval centre', (tester) async {
    final reviews = FakeReviewRepository();
    final deps = await buildDeps(storedSession: fakeSession(role: 'Counsellor'), reviewRepository: reviews);
    await pumpApp(tester, deps);

    expect(find.text('Approval Centre'), findsOneWidget);
    expect(find.text('No reviews match these filters.'), findsOneWidget);
    expect(reviews.queries, isNotEmpty);
  });

  testWidgets('signing out returns to the login screen', (tester) async {
    final deps = await buildDeps(storedSession: fakeSession());
    await pumpApp(tester, deps);
    expect(find.text('Welcome'), findsOneWidget);

    await tester.tap(find.byTooltip('Sign out'));
    await tester.pumpAndSettle();

    expect(find.widgetWithText(FilledButton, 'Sign in'), findsOneWidget);
  });
}
