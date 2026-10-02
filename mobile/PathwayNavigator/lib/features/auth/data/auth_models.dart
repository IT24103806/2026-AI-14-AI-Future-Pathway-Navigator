import '../../../core/utils/json_helpers.dart';
import '../../../core/utils/jwt_utils.dart';

/// Mirrors `AuthResponseDto` ({ userId, email, role, token, expiresAt }).
class AuthSession {
  const AuthSession({
    required this.userId,
    required this.email,
    required this.role,
    required this.token,
    required this.expiresAt,
  });

  final String userId;
  final String email;
  final String role;
  final String token;
  final DateTime expiresAt;

  factory AuthSession.fromJson(Map<String, dynamic> json) => AuthSession(
        userId: asString(json['userId']),
        email: asString(json['email']),
        role: asString(json['role']),
        token: asString(json['token']),
        expiresAt: asDate(json['expiresAt']),
      );

  Map<String, dynamic> toJson() => {
        'userId': userId,
        'email': email,
        'role': role,
        'token': token,
        'expiresAt': expiresAt.toIso8601String(),
      };

  bool isExpired({DateTime? now}) => JwtUtils.isExpired(token, now: now);
}
