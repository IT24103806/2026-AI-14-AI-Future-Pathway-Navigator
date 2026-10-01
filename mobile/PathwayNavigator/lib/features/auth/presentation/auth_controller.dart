import 'dart:async';

import 'package:flutter/foundation.dart';

import '../../../core/constants/roles.dart';
import '../../../core/error/app_exception.dart';
import '../data/auth_models.dart';
import '../data/auth_repository.dart';
import '../data/session_store.dart';

enum AuthStatus { unknown, authenticated, unauthenticated }

/// App-wide authentication state. The router listens to it to guard routes.
class AuthController extends ChangeNotifier {
  AuthController({required AuthRepository repository, required SessionStore sessionStore})
      : _repository = repository,
        _sessionStore = sessionStore;

  final AuthRepository _repository;
  final SessionStore _sessionStore;

  AuthStatus _status = AuthStatus.unknown;
  AuthSession? _session;
  bool _sessionExpired = false;

  AuthStatus get status => _status;
  AuthSession? get session => _session;
  String? get role => _session?.role;
  bool get isAuthenticated => _status == AuthStatus.authenticated;
  bool get isInitialized => _status != AuthStatus.unknown;

  /// Restores a still-valid session from secure storage at app start.
  Future<void> restore() async {
    final stored = await _sessionStore.read();
    if (stored != null && Roles.isKnown(stored.role) && !stored.isExpired()) {
      _session = stored;
      _status = AuthStatus.authenticated;
    } else {
      if (stored != null) await _sessionStore.clear();
      _session = null;
      _status = AuthStatus.unauthenticated;
    }
    notifyListeners();
  }

  Future<void> login(String email, String password) async {
    final session = await _repository.login(email: email, password: password);
    await _adopt(session);
  }

  Future<void> register(String email, String password) async {
    final session = await _repository.register(email: email, password: password);
    await _adopt(session);
  }

  Future<void> logout() async {
    await _sessionStore.clear();
    _session = null;
    _status = AuthStatus.unauthenticated;
    notifyListeners();
  }

  /// Called by the API client when the server rejects the token (401).
  void expireSession() {
    if (_status != AuthStatus.authenticated) return;
    _sessionExpired = true;
    unawaited(logout());
  }

  /// Returns (and clears) the "your session expired" flag so the login screen shows it once.
  bool consumeSessionExpired() {
    final value = _sessionExpired;
    _sessionExpired = false;
    return value;
  }

  Future<void> _adopt(AuthSession session) async {
    if (!Roles.isKnown(session.role)) {
      throw const AppException('This account type is not supported by the app.');
    }
    await _sessionStore.save(session);
    _session = session;
    _status = AuthStatus.authenticated;
    _sessionExpired = false;
    notifyListeners();
  }
}
