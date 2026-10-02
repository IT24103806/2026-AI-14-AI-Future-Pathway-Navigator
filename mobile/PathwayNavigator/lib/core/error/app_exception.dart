/// Coarse classification so the UI can react (e.g. redirect on 401) without parsing messages.
enum AppErrorKind {
  network,
  timeout,
  unauthorized,
  forbidden,
  notFound,
  conflict,
  validation,
  server,
  unknown,
}

/// The only exception type that leaves the data layer. Messages are user-presentable.
class AppException implements Exception {
  const AppException(this.message, {this.kind = AppErrorKind.unknown, this.statusCode});

  final String message;
  final AppErrorKind kind;
  final int? statusCode;

  bool get isNotFound => kind == AppErrorKind.notFound;
  bool get isUnauthorized => kind == AppErrorKind.unauthorized;

  @override
  String toString() => message;
}

/// Turns any thrown object into text that is safe to show to the user.
String describeError(Object error, {String fallback = 'Something went wrong. Please try again.'}) {
  if (error is AppException) return error.message;
  return fallback;
}
