import 'package:flutter/foundation.dart';

/// Debug-only logging. Never pass tokens, passwords, e-mail addresses, A/L results or any
/// other personal data - release builds drop these calls entirely.
void appLog(String message) {
  if (kDebugMode) debugPrint('[PathwayNavigator] $message');
}
