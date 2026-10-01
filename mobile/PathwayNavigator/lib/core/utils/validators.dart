/// Client-side validation mirrors the backend rules so users get instant feedback; the server
/// remains the source of truth.
abstract final class Validators {
  static final RegExp _email = RegExp(r'^[^\s@]+@[^\s@]+\.[^\s@]+$');
  static final RegExp _alResults = RegExp(r'^[ABCFSabcfs](\s*[,/]\s*[ABCFSabcfs])*$');

  static String? notEmpty(String? value, {String label = 'This field'}) =>
      (value == null || value.trim().isEmpty) ? '$label is required.' : null;

  static String? email(String? value) {
    if (value == null || value.trim().isEmpty) return 'Email address is required.';
    return _email.hasMatch(value.trim()) ? null : 'Enter a valid email address.';
  }

  /// Backend rule (`RegisterRequestDto`): at least 8 characters.
  static String? newPassword(String? value) {
    if (value == null || value.isEmpty) return 'Password is required.';
    return value.length >= 8 ? null : 'Password must be at least 8 characters long.';
  }

  static String? password(String? value) =>
      (value == null || value.isEmpty) ? 'Password is required.' : null;

  static String? Function(String?) matches(String Function() other) =>
      (value) => value == other() ? null : 'Passwords do not match.';

  static String? otp(String? value) =>
      RegExp(r'^\d{6}$').hasMatch(value?.trim() ?? '') ? null : 'Enter the 6-digit code.';

  static String? alResults(String? value) =>
      _alResults.hasMatch(value?.trim() ?? '') ? null : 'Use grades A, B, C, S or F, e.g. A,B,C.';

  static String? length(String? value, {required int min, required int max, String label = 'This field'}) {
    final length = value?.trim().length ?? 0;
    if (length < min) return '$label must be at least $min characters.';
    if (length > max) return '$label must be at most $max characters.';
    return null;
  }
}
