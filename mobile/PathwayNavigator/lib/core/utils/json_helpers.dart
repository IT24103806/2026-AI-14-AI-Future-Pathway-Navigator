import 'dart:convert';

/// Defensive JSON readers: the API mixes camelCase DTOs, snake_case agent payloads and
/// JSON-encoded string columns (`missingSkillsJson`), so parsing must never throw on shape drift.

String asString(dynamic value, [String fallback = '']) => value is String ? value : fallback;

String? asNullableString(dynamic value) => value is String ? value : null;

int asInt(dynamic value, [int fallback = 0]) => value is num ? value.toInt() : fallback;

double asDouble(dynamic value, [double fallback = 0]) => value is num ? value.toDouble() : fallback;

bool asBool(dynamic value, [bool fallback = false]) => value is bool ? value : fallback;

Map<String, dynamic> asMap(dynamic value) =>
    value is Map ? Map<String, dynamic>.from(value) : <String, dynamic>{};

List<Map<String, dynamic>> asMapList(dynamic value) {
  final decoded = _decodeIfString(value);
  if (decoded is! List) return <Map<String, dynamic>>[];
  return decoded.whereType<Map>().map((item) => Map<String, dynamic>.from(item)).toList();
}

/// Accepts a real JSON array or a JSON-encoded string containing an array.
List<String> asStringList(dynamic value) {
  final decoded = _decodeIfString(value);
  if (decoded is! List) return <String>[];
  return decoded.whereType<String>().toList();
}

DateTime asDate(dynamic value, {DateTime? fallback}) {
  if (value is String) {
    final parsed = DateTime.tryParse(value);
    if (parsed != null) return parsed;
  }
  return fallback ?? DateTime.fromMillisecondsSinceEpoch(0, isUtc: true);
}

DateTime? asNullableDate(dynamic value) => value is String ? DateTime.tryParse(value) : null;

dynamic _decodeIfString(dynamic value) {
  if (value is! String) return value;
  if (value.trim().isEmpty) return <dynamic>[];
  try {
    return jsonDecode(value);
  } on FormatException {
    return <dynamic>[];
  }
}
