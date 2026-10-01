import 'package:flutter_test/flutter_test.dart';
import 'package:pathway_navigator/app/router.dart';
import 'package:pathway_navigator/core/constants/roles.dart';
import 'package:pathway_navigator/core/constants/routes.dart';
import 'package:pathway_navigator/features/auth/presentation/auth_controller.dart';

String? redirect(
  String location, {
  AuthStatus auth = AuthStatus.authenticated,
  String? role = Roles.student,
  bool consentLoaded = true,
  bool consent = true,
}) =>
    resolveRedirect(
      location: location,
      authStatus: auth,
      role: role,
      consentLoaded: consentLoaded,
      consentAccepted: consent,
    );

void main() {
  group('startup', () {
    test('shows the splash until auth and consent have loaded', () {
      expect(redirect('/login', auth: AuthStatus.unknown), AppRoutes.splash);
      expect(redirect('/login', consentLoaded: false), AppRoutes.splash);
      expect(redirect(AppRoutes.splash, auth: AuthStatus.unknown), isNull);
    });
  });

  group('consent gate', () {
    test('blocks everything until consent is given', () {
      expect(redirect('/student', consent: false), AppRoutes.consent);
      expect(redirect('/login', auth: AuthStatus.unauthenticated, role: null, consent: false), AppRoutes.consent);
      expect(redirect(AppRoutes.consent, consent: false), isNull);
    });

    test('consent screen is skipped once consent exists', () {
      expect(redirect(AppRoutes.consent), '/student');
      expect(redirect(AppRoutes.splash, role: Roles.counsellor), '/counsellor');
    });
  });

  group('unauthenticated', () {
    String? signedOut(String location) => redirect(location, auth: AuthStatus.unauthenticated, role: null);

    test('can reach the public auth screens', () {
      for (final route in AppRoutes.publicRoutes) {
        expect(signedOut(route), isNull, reason: route);
      }
    });

    test('is sent to login from protected screens', () {
      expect(signedOut('/student'), AppRoutes.login);
      expect(signedOut('/counsellor/review/abc'), AppRoutes.login);
      expect(signedOut('/admin'), AppRoutes.login);
      expect(signedOut('/settings'), AppRoutes.login);
    });
  });

  group('role guards', () {
    test('signed-in users bounce off the auth screens to their home', () {
      expect(redirect('/login', role: Roles.admin), '/admin');
      expect(redirect('/register'), '/student');
    });

    test('students stay in /student', () {
      expect(redirect('/student/career-discovery'), isNull);
      expect(redirect('/counsellor'), '/student');
      expect(redirect('/admin'), '/student');
    });

    test('counsellors cannot open student or admin areas', () {
      expect(redirect('/counsellor', role: Roles.counsellor), isNull);
      expect(redirect('/counsellor/review/1', role: Roles.counsellor), isNull);
      expect(redirect('/student', role: Roles.counsellor), '/counsellor');
      expect(redirect('/admin', role: Roles.counsellor), '/counsellor');
    });

    test('admins may use the approval centre but not student screens', () {
      expect(redirect('/counsellor', role: Roles.admin), isNull);
      expect(redirect('/admin', role: Roles.admin), isNull);
      expect(redirect('/student/reality-check', role: Roles.admin), '/admin');
    });

    test('a prefix lookalike is not treated as a protected area', () {
      expect(redirect('/studentx', role: Roles.counsellor), isNull);
    });

    test('settings is open to every signed-in role', () {
      for (final role in [Roles.student, Roles.counsellor, Roles.admin]) {
        expect(redirect('/settings', role: role), isNull);
      }
    });
  });
}
