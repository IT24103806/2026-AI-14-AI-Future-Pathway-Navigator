import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:pathway_navigator/features/privacy/data/consent_store.dart';
import 'package:pathway_navigator/core/storage/key_value_store.dart';
import 'package:pathway_navigator/features/privacy/presentation/privacy_notice.dart';

void main() {
  testWidgets('the notice explains AI use and shows the configured contact', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(
        home: Scaffold(body: SingleChildScrollView(child: PrivacyNotice(contact: 'privacy@example.com'))),
      ),
    );

    expect(find.text('How AI is used'), findsOneWidget);
    expect(find.textContaining('privacy@example.com'), findsOneWidget);
  });

  test('consent is versioned: a stale version asks again', () async {
    final store = InMemoryKeyValueStore();
    final consent = ConsentStore(store);
    expect(await consent.hasAccepted(), isFalse);

    await consent.accept();
    expect(await consent.hasAccepted(), isTrue);

    await store.write('privacy_consent_version', '0');
    expect(await consent.hasAccepted(), isFalse);

    await consent.accept();
    await consent.withdraw();
    expect(await consent.hasAccepted(), isFalse);
  });
}
