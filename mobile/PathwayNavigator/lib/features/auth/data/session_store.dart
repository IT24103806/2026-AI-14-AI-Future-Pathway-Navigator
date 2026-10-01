import 'dart:convert';

import '../../../core/storage/key_value_store.dart';
import 'auth_models.dart';

/// Persists the signed-in session in the Android Keystore-backed store.
class SessionStore {
  SessionStore(this._store);

  static const String _key = 'auth_session_v1';

  final KeyValueStore _store;

  Future<AuthSession?> read() async {
    try {
      final raw = await _store.read(_key);
      if (raw == null) return null;
      final json = jsonDecode(raw);
      if (json is! Map) return null;
      final session = AuthSession.fromJson(Map<String, dynamic>.from(json));
      return session.token.isEmpty ? null : session;
    } on Exception {
      // Corrupt data or an unreadable Keystore entry: treat as signed out rather than crash.
      return null;
    }
  }

  Future<String?> readToken() async => (await read())?.token;

  Future<void> save(AuthSession session) => _store.write(_key, jsonEncode(session.toJson()));

  Future<void> clear() => _store.delete(_key);
}
