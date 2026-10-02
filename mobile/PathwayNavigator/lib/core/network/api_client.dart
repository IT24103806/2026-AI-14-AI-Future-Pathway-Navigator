import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:http/http.dart' as http;

import '../error/app_exception.dart';

typedef TokenReader = Future<String?> Function();

/// The single gateway to the ASP.NET Core API.
///
/// Responsibilities: base URL, bearer token, JSON encoding, timeouts, and translating every
/// failure into an [AppException]. Repositories depend on this class, never on `package:http`.
class ApiClient {
  ApiClient({
    required this._baseUrl,
    required this._readToken,
    http.Client? httpClient,
    this.timeout = const Duration(seconds: 120),
    this.onUnauthorized,
  })  : _client = httpClient ?? http.Client();

  final String _baseUrl;
  final TokenReader _readToken;
  final http.Client _client;
  final Duration timeout;

  /// Invoked when an authenticated request is rejected with 401 (expired / revoked token).
  void Function()? onUnauthorized;

  Future<dynamic> get(String path, {Map<String, Object?>? query, bool authenticated = true}) =>
      _send('GET', path, query: query, authenticated: authenticated);

  Future<dynamic> post(String path, {Object? body, bool authenticated = true}) =>
      _send('POST', path, body: body, authenticated: authenticated);

  Future<dynamic> put(String path, {Object? body, bool authenticated = true}) =>
      _send('PUT', path, body: body, authenticated: authenticated);

  Future<dynamic> patch(String path, {Object? body, bool authenticated = true}) =>
      _send('PATCH', path, body: body, authenticated: authenticated);

  void close() => _client.close();

  Uri _buildUri(String path, Map<String, Object?>? query) {
    final uri = Uri.parse('$_baseUrl$path');
    if (query == null) return uri;
    final params = <String, String>{
      for (final entry in query.entries)
        if (entry.value != null && entry.value.toString().isNotEmpty) entry.key: entry.value.toString(),
    };
    return params.isEmpty ? uri : uri.replace(queryParameters: params);
  }

  Future<dynamic> _send(
    String method,
    String path, {
    required bool authenticated,
    Object? body,
    Map<String, Object?>? query,
  }) async {
    final headers = <String, String>{'Accept': 'application/json'};
    if (body != null) headers['Content-Type'] = 'application/json';

    if (authenticated) {
      final token = await _readToken();
      if (token == null || token.isEmpty) {
        throw const AppException(
          'Please sign in to continue.',
          kind: AppErrorKind.unauthorized,
          statusCode: 401,
        );
      }
      headers['Authorization'] = 'Bearer $token';
    }

    final request = http.Request(method, _buildUri(path, query))..headers.addAll(headers);
    if (body != null) request.body = jsonEncode(body);

    final http.Response response;
    try {
      final streamed = await _client.send(request).timeout(timeout);
      response = await http.Response.fromStream(streamed).timeout(timeout);
    } on TimeoutException {
      throw const AppException(
        'The server took too long to respond. The AI service may be busy - please try again.',
        kind: AppErrorKind.timeout,
      );
    } on SocketException {
      throw const AppException(_networkMessage, kind: AppErrorKind.network);
    } on http.ClientException {
      throw const AppException(_networkMessage, kind: AppErrorKind.network);
    }

    return _handle(response, authenticated);
  }

  static const String _networkMessage = 'Network error. Unable to reach the Pathway Navigator API.';

  dynamic _handle(http.Response response, bool authenticated) {
    final status = response.statusCode;
    dynamic data;
    if (response.bodyBytes.isNotEmpty) {
      try {
        data = jsonDecode(utf8.decode(response.bodyBytes));
      } on FormatException {
        data = null;
      }
    }

    if (status >= 200 && status < 300) return data;

    final kind = _kindFor(status);
    if (kind == AppErrorKind.unauthorized && authenticated) onUnauthorized?.call();
    throw AppException(extractErrorMessage(data, status), kind: kind, statusCode: status);
  }

  static AppErrorKind _kindFor(int status) {
    if (status == 401) return AppErrorKind.unauthorized;
    if (status == 403) return AppErrorKind.forbidden;
    if (status == 404) return AppErrorKind.notFound;
    if (status == 409) return AppErrorKind.conflict;
    if (status == 400 || status == 422) return AppErrorKind.validation;
    if (status >= 500) return AppErrorKind.server;
    return AppErrorKind.unknown;
  }
}

/// Signatures of an ASP.NET Core developer exception page, a .NET stack trace or raw HTML. The API
/// never sends these on purpose, but an unhandled server-side exception used to leak through and
/// the app rendered the whole dump inside its error banner.
final RegExp _serverDumpPattern = RegExp(
  r'<!doctype html|<html[\s>]|<head>|<body>'
  r'|\bat\s+[\w.<>`+]+\(|HEADERS\s*={2,}'
  r'|Npgsql|Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore',
  caseSensitive: false,
);

/// Trims a candidate message, drops server dumps and caps how much text the UI can receive.
String? _cleanMessage(dynamic value) {
  if (value is! String) return null;
  final text = value.trim();
  if (text.isEmpty || _serverDumpPattern.hasMatch(text)) return null;
  return text.length > 300 ? '${text.substring(0, 297)}…' : text;
}

/// Mirrors `getApiErrorMessage` in the web app. The API answers with either
/// `{ "message": "..." }` or ASP.NET ProblemDetails `{ "title", "errors": { "Field": ["msg"] } }`.
String extractErrorMessage(dynamic data, int status) {
  final textBody = _cleanMessage(data);
  if (textBody != null) return textBody;
  if (data is Map) {
    final message = _cleanMessage(data['message']);
    if (message != null) return message;

    final errors = data['errors'];
    if (errors is Map) {
      final messages = <String>[];
      for (final value in errors.values) {
        if (value is List) {
          for (final entry in value) {
            final cleaned = _cleanMessage(entry);
            if (cleaned != null) messages.add(cleaned);
          }
        } else {
          final cleaned = _cleanMessage(value);
          if (cleaned != null) messages.add(cleaned);
        }
      }
      if (messages.isNotEmpty) return messages.join(' ');
    }
  }
  switch (status) {
    case 401:
      return 'Your session has expired. Please sign in again.';
    case 403:
      return 'You do not have permission to perform this action.';
    case 404:
      return 'The requested item was not found.';
  }
  // A 5xx means the server failed, not that the user did something wrong. The API itself retries
  // dropped database connections before they ever get this far.
  if (status >= 500) return 'The server ran into a problem. Please try again shortly.';
  final title = data is Map ? _cleanMessage(data['title']) : null;
  if (title != null) return title;
  return 'Request failed (HTTP $status).';
}
