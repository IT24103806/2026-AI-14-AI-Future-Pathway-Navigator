import '../../../core/error/app_exception.dart';
import '../../../core/network/api_client.dart';
import '../../../core/utils/json_helpers.dart';
import 'auth_models.dart';

abstract interface class AuthRepository {
  Future<AuthSession> login({required String email, required String password});

  /// Self-service registration is limited to the Student role on mobile. Staff accounts are
  /// provisioned by an administrator.
  Future<AuthSession> register({required String email, required String password});

  Future<void> requestPasswordReset(String email);

  Future<void> verifyResetCode({required String email, required String code});

  Future<void> resetPassword({
    required String email,
    required String code,
    required String newPassword,
    required String confirmPassword,
  });
}

class RemoteAuthRepository implements AuthRepository {
  RemoteAuthRepository(this._api);

  final ApiClient _api;

  AuthSession _parseSession(dynamic data) {
    final session = AuthSession.fromJson(asMap(data));
    if (session.token.isEmpty) {
      throw const AppException('The server returned no access token.');
    }
    return session;
  }

  @override
  Future<AuthSession> login({required String email, required String password}) async {
    final data = await _api.post(
      '/auth/login',
      body: {'email': email.trim(), 'password': password},
      authenticated: false,
    );
    return _parseSession(data);
  }

  @override
  Future<AuthSession> register({required String email, required String password}) async {
    final data = await _api.post(
      '/auth/register',
      body: {'email': email.trim(), 'password': password, 'roleName': 'Student'},
      authenticated: false,
    );
    return _parseSession(data);
  }

  @override
  Future<void> requestPasswordReset(String email) async {
    await _api.post('/auth/forgot-password', body: {'email': email.trim()}, authenticated: false);
  }

  @override
  Future<void> verifyResetCode({required String email, required String code}) async {
    await _api.post(
      '/auth/verify-reset-code',
      body: {'email': email.trim(), 'code': code.trim()},
      authenticated: false,
    );
  }

  @override
  Future<void> resetPassword({
    required String email,
    required String code,
    required String newPassword,
    required String confirmPassword,
  }) async {
    await _api.post(
      '/auth/reset-password',
      body: {
        'email': email.trim(),
        'code': code.trim(),
        'newPassword': newPassword,
        'confirmPassword': confirmPassword,
      },
      authenticated: false,
    );
  }
}
