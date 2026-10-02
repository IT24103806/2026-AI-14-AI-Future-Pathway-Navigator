import 'dart:convert';

/// Read-only JWT helpers. The app never *trusts* these claims for authorisation - the backend
/// validates every request - they are only used to avoid sending an already-expired token.
abstract final class JwtUtils {
  static Map<String, dynamic>? decodePayload(String token) {
    final parts = token.split('.');
    if (parts.length != 3) return null;
    try {
      final normalized = base64Url.normalize(parts[1]);
      final json = jsonDecode(utf8.decode(base64Url.decode(normalized)));
      return json is Map ? Map<String, dynamic>.from(json) : null;
    } on FormatException {
      return null;
    }
  }

  static DateTime? expiry(String token) {
    final exp = decodePayload(token)?['exp'];
    if (exp is! num) return null;
    return DateTime.fromMillisecondsSinceEpoch(exp.toInt() * 1000, isUtc: true);
  }

  /// A token without a readable `exp` claim is treated as expired (fail closed).
  static bool isExpired(String token, {DateTime? now, Duration skew = const Duration(seconds: 30)}) {
    final expiresAt = expiry(token);
    if (expiresAt == null) return true;
    return !(now ?? DateTime.now().toUtc()).isBefore(expiresAt.subtract(skew));
  }
}
