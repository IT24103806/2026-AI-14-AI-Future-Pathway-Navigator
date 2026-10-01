import 'package:flutter_test/flutter_test.dart';
import 'package:pathway_navigator/core/config/app_config.dart';
import 'package:pathway_navigator/core/constants/roles.dart';
import 'package:pathway_navigator/core/utils/json_helpers.dart';
import 'package:pathway_navigator/core/utils/jwt_utils.dart';
import 'package:pathway_navigator/core/utils/validators.dart';

import '../support/fakes.dart';

void main() {
  group('json_helpers', () {
    test('asStringList accepts arrays and JSON-encoded strings', () {
      expect(asStringList(['a', 'b']), ['a', 'b']);
      expect(asStringList('["a","b"]'), ['a', 'b']);
      expect(asStringList('not json'), isEmpty);
      expect(asStringList(null), isEmpty);
      expect(asStringList(''), isEmpty);
    });

    test('scalar helpers fall back instead of throwing', () {
      expect(asString(5), '');
      expect(asInt('x', 7), 7);
      expect(asInt(3.9), 3);
      expect(asBool(null), isFalse);
      expect(asMap(null), isEmpty);
      expect(asNullableDate('garbage'), isNull);
      expect(asNullableDate('2026-01-02T03:04:05Z'), isNotNull);
    });
  });

  group('JwtUtils', () {
    test('reads expiry and detects expiry', () {
      final future = DateTime.now().toUtc().add(const Duration(hours: 1));
      final past = DateTime.now().toUtc().subtract(const Duration(hours: 1));
      expect(JwtUtils.isExpired(fakeJwt(future)), isFalse);
      expect(JwtUtils.isExpired(fakeJwt(past)), isTrue);
    });

    test('fails closed on malformed tokens', () {
      expect(JwtUtils.isExpired('garbage'), isTrue);
      expect(JwtUtils.isExpired('a.b.c'), isTrue);
    });
  });

  group('Validators', () {
    test('email', () {
      expect(Validators.email('a@b.co'), isNull);
      expect(Validators.email('nope'), isNotNull);
      expect(Validators.email(''), isNotNull);
    });

    test('new password needs 8 characters (backend rule)', () {
      expect(Validators.newPassword('1234567'), isNotNull);
      expect(Validators.newPassword('12345678'), isNull);
    });

    test('A/L results mirror the backend regex', () {
      expect(Validators.alResults('A,B,C'), isNull);
      expect(Validators.alResults('a / b'), isNull);
      expect(Validators.alResults('A,Z'), isNotNull);
      expect(Validators.alResults(''), isNotNull);
    });

    test('otp is exactly six digits', () {
      expect(Validators.otp('123456'), isNull);
      expect(Validators.otp('12345'), isNotNull);
      expect(Validators.otp('12345a'), isNotNull);
    });

    test('matches compares lazily', () {
      var other = 'x';
      final validator = Validators.matches(() => other);
      expect(validator('x'), isNull);
      other = 'y';
      expect(validator('x'), isNotNull);
    });
  });

  group('AppConfig', () {
    test('rejects plain HTTP in release builds', () {
      const config = AppConfig(apiBaseUrl: 'http://example.com/api');
      expect(() => config.validate(isRelease: true), throwsStateError);
      expect(() => config.validate(isRelease: false), returnsNormally);
    });

    test('accepts HTTPS in release builds and rejects garbage', () {
      expect(() => const AppConfig(apiBaseUrl: 'https://example.com/api').validate(isRelease: true), returnsNormally);
      expect(() => const AppConfig(apiBaseUrl: 'not a url').validate(isRelease: false), throwsStateError);
    });

    test('normalizeUrl strips one trailing slash', () {
      expect(AppConfig.normalizeUrl(' https://x.io/api/ '), 'https://x.io/api');
    });
  });

  group('Roles', () {
    test('home routes', () {
      expect(homeRouteForRole(Roles.student), '/student');
      expect(homeRouteForRole(Roles.counsellor), '/counsellor');
      expect(homeRouteForRole(Roles.admin), '/admin');
    });

    test('only the three backend roles are known', () {
      expect(Roles.isKnown('Student'), isTrue);
      expect(Roles.isKnown('Hacker'), isFalse);
      expect(Roles.isKnown(null), isFalse);
    });
  });
}
