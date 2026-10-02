import 'package:flutter_test/flutter_test.dart';
import 'package:pathway_navigator/core/error/app_exception.dart';
import 'package:pathway_navigator/core/storage/key_value_store.dart';
import 'package:pathway_navigator/features/auth/data/session_store.dart';
import 'package:pathway_navigator/features/auth/presentation/auth_controller.dart';

import '../support/fakes.dart';

void main() {
  late InMemoryKeyValueStore store;
  late SessionStore sessions;
  late FakeAuthRepository repository;
  late AuthController auth;

  setUp(() {
    store = InMemoryKeyValueStore();
    sessions = SessionStore(store);
    repository = FakeAuthRepository();
    auth = AuthController(repository: repository, sessionStore: sessions);
  });

  test('starts unknown and becomes unauthenticated when nothing is stored', () async {
    expect(auth.status, AuthStatus.unknown);
    await auth.restore();
    expect(auth.status, AuthStatus.unauthenticated);
  });

  test('login persists the session and exposes the role', () async {
    repository.session = fakeSession(role: 'Counsellor');

    await auth.login('c@example.com', 'password123');

    expect(auth.isAuthenticated, isTrue);
    expect(auth.role, 'Counsellor');
    expect((await sessions.read())?.email, 'student@example.com');
  });

  test('failed login leaves the user signed out', () async {
    repository.error = const AppException('Invalid credentials');

    await expectLater(auth.login('a@b.co', 'wrongwrong'), throwsA(isA<AppException>()));

    expect(auth.status, AuthStatus.unknown);
    expect(await sessions.read(), isNull);
  });

  test('rejects accounts with an unknown role', () async {
    repository.session = fakeSession(role: 'Superuser');

    await expectLater(auth.login('a@b.co', 'password123'), throwsA(isA<AppException>()));

    expect(auth.isAuthenticated, isFalse);
    expect(await sessions.read(), isNull);
  });

  test('restore keeps a valid stored session', () async {
    await sessions.save(fakeSession());
    await auth.restore();
    expect(auth.isAuthenticated, isTrue);
  });

  test('restore discards an expired session', () async {
    await sessions.save(fakeSession(expiresAt: DateTime.now().toUtc().subtract(const Duration(hours: 1))));

    await auth.restore();

    expect(auth.status, AuthStatus.unauthenticated);
    expect(await sessions.read(), isNull);
  });

  test('logout clears the stored session', () async {
    await auth.login('a@b.co', 'password123');
    await auth.logout();
    expect(auth.status, AuthStatus.unauthenticated);
    expect(await sessions.readToken(), isNull);
  });

  test('expireSession signs out and flags the message exactly once', () async {
    await auth.login('a@b.co', 'password123');

    auth.expireSession();
    await Future<void>.delayed(Duration.zero);

    expect(auth.status, AuthStatus.unauthenticated);
    expect(auth.consumeSessionExpired(), isTrue);
    expect(auth.consumeSessionExpired(), isFalse);
  });

  test('corrupt stored data is treated as signed out', () async {
    await store.write('auth_session_v1', '{not json');
    await auth.restore();
    expect(auth.status, AuthStatus.unauthenticated);
  });
}
