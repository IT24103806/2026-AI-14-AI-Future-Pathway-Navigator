import 'package:flutter/foundation.dart';

/// Build-time configuration, supplied with `--dart-define` (never hard-coded secrets).
///
/// ```
/// flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5081/api          # emulator -> host machine
/// flutter run --dart-define=API_BASE_URL=http://192.168.1.23:5081/api      # physical device on LAN (debug only)
/// flutter build apk --dart-define=API_BASE_URL=https://api.example.com/api # release: HTTPS is mandatory
/// ```
class AppConfig {
  const AppConfig({
    required this.apiBaseUrl,
    this.requestTimeout = const Duration(seconds: 120),
    this.privacyContact = '',
  });

  /// Base URL of the ASP.NET Core API **including** the `/api` prefix. The Python AI service is
  /// internal and must never be called from the app.
  final String apiBaseUrl;

  /// Agent calls run an LLM behind the API, so the timeout is deliberately generous.
  final Duration requestTimeout;

  /// Where users can send privacy / data-deletion requests (shown on the privacy screen).
  final String privacyContact;

  static const String defaultApiBaseUrl = 'http://10.0.2.2:5081/api';

  factory AppConfig.fromEnvironment() {
    const url = String.fromEnvironment('API_BASE_URL', defaultValue: defaultApiBaseUrl);
    const contact = String.fromEnvironment('PRIVACY_CONTACT');
    final config = AppConfig(apiBaseUrl: normalizeUrl(url), privacyContact: contact);
    config.validate(isRelease: kReleaseMode);
    return config;
  }

  /// Fails fast on unsafe configuration instead of leaking credentials over the network.
  void validate({required bool isRelease}) {
    final uri = Uri.tryParse(apiBaseUrl);
    if (uri == null || !uri.hasScheme || uri.host.isEmpty) {
      throw StateError('API_BASE_URL "$apiBaseUrl" is not a valid absolute URL.');
    }
    if (isRelease && uri.scheme != 'https') {
      throw StateError(
        'Release builds must use HTTPS. Pass --dart-define=API_BASE_URL=https://<host>/api '
        '(plain HTTP is only allowed in debug builds).',
      );
    }
  }

  static String normalizeUrl(String url) {
    final trimmed = url.trim();
    return trimmed.endsWith('/') ? trimmed.substring(0, trimmed.length - 1) : trimmed;
  }
}
