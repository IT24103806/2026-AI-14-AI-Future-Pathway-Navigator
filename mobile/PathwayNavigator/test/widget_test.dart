import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:pathway_navigator/main.dart';

void main() {
  testWidgets('shows the login form when not authenticated', (WidgetTester tester) async {
    await tester.pumpWidget(const MainApp());

    expect(find.text('Sign in to run Career Discovery'), findsOneWidget);
    expect(find.widgetWithText(ElevatedButton, 'Login'), findsOneWidget);
  });
}
