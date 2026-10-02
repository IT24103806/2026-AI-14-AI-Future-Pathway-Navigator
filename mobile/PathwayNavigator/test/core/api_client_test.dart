import 'dart:async';
import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:pathway_navigator/core/error/app_exception.dart';
import 'package:pathway_navigator/core/network/api_client.dart';

ApiClient clientFor(
  MockClientHandler handler, {
  String? token = 'tok',
  void Function()? onUnauthorized,
  Duration timeout = const Duration(seconds: 5),
}) =>
    ApiClient(
      baseUrl: 'http://api.test/api',
      readToken: () async => token,
      httpClient: MockClient(handler),
      timeout: timeout,
      onUnauthorized: onUnauthorized,
    );

http.Response json(Object body, int status) =>
    http.Response(jsonEncode(body), status, headers: {'content-type': 'application/json'});

void main() {
  test('attaches the bearer token and decodes JSON', () async {
    late http.BaseRequest seen;
    final api = clientFor((request) async {
      seen = request;
      return json({'ok': true}, 200);
    });

    final data = await api.get('/profile', query: {'page': 2, 'search': null});

    expect(data, {'ok': true});
    expect(seen.headers['Authorization'], 'Bearer tok');
    expect(seen.url.toString(), 'http://api.test/api/profile?page=2');
  });

  test('does not attach a token to unauthenticated calls', () async {
    late http.BaseRequest seen;
    final api = clientFor((request) async {
      seen = request;
      return json({}, 200);
    });

    await api.post('/auth/login', body: {'email': 'a'}, authenticated: false);

    expect(seen.headers.containsKey('Authorization'), isFalse);
    expect(seen.headers['Content-Type'], contains('application/json'));
  });

  test('refuses authenticated calls without a token and never hits the network', () async {
    var called = false;
    final api = clientFor((request) async {
      called = true;
      return json({}, 200);
    }, token: null);

    await expectLater(api.get('/profile'), throwsA(isA<AppException>().having((e) => e.isUnauthorized, 'unauthorized', true)));
    expect(called, isFalse);
  });

  test('401 on an authenticated call fires onUnauthorized', () async {
    var fired = 0;
    final api = clientFor((request) async => json({'message': 'expired'}, 401), onUnauthorized: () => fired++);

    await expectLater(api.get('/profile'), throwsA(isA<AppException>()));
    expect(fired, 1);
  });

  test('401 on login (wrong password) does not expire a session', () async {
    var fired = 0;
    final api = clientFor((request) async => json({'message': 'Invalid credentials'}, 401), onUnauthorized: () => fired++);

    await expectLater(
      api.post('/auth/login', body: {}, authenticated: false),
      throwsA(isA<AppException>().having((e) => e.message, 'message', 'Invalid credentials')),
    );
    expect(fired, 0);
  });

  test('404 maps to notFound', () async {
    final api = clientFor((request) async => http.Response('', 404));
    await expectLater(api.get('/x'), throwsA(isA<AppException>().having((e) => e.isNotFound, 'notFound', true)));
  });

  test('timeouts become friendly AppExceptions', () async {
    final api = clientFor(
      (request) => Completer<http.Response>().future,
      timeout: const Duration(milliseconds: 50),
    );
    await expectLater(
      api.get('/slow'),
      throwsA(isA<AppException>().having((e) => e.kind, 'kind', AppErrorKind.timeout)),
    );
  });

  test('connection failures become network errors', () async {
    final api = clientFor((request) async => throw http.ClientException('boom'));
    await expectLater(
      api.get('/x'),
      throwsA(isA<AppException>().having((e) => e.kind, 'kind', AppErrorKind.network)),
    );
  });

  group('extractErrorMessage', () {
    test('prefers message, then ProblemDetails errors', () {
      expect(extractErrorMessage({'message': 'm'}, 400), 'm');
      expect(
        extractErrorMessage({'errors': {'Email': ['Bad email'], 'Password': ['Too short']}}, 400),
        'Bad email Too short',
      );
      expect(extractErrorMessage({'title': 'One or more validation errors occurred.'}, 400),
          'One or more validation errors occurred.');
    });

    test('falls back by status', () {
      expect(extractErrorMessage(null, 403), contains('permission'));
      expect(extractErrorMessage(null, 500), contains('server'));
      expect(extractErrorMessage(null, 418), contains('418'));
    });

    test('never surfaces a raw server dump', () {
      const dump = 'System.InvalidOperationException: An exception has been raised that is likely '
          'due to a transient failure.\n'
          '   at PathwayNavigator.Api.Services.AuthService.LoginAsync(LoginRequestDto request)\n'
          'HEADERS =====';
      expect(extractErrorMessage(dump, 500), contains('server'));
      expect(
        extractErrorMessage('<html><body>Internal Server Error</body></html>', 500),
        contains('server'),
      );
    });
  });
}
