import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:pathway_navigator/features/reality_check/data/review_repository.dart';
import 'package:pathway_navigator/features/reality_check/presentation/student_reality_screen.dart';

import '../support/fakes.dart';

Future<void> pumpScreen(WidgetTester tester, FakeReviewRepository repository) async {
  await tester.pumpWidget(
    Provider<ReviewRepository>.value(
      value: repository,
      child: const MaterialApp(home: StudentRealityScreen()),
    ),
  );
  await tester.pumpAndSettle();
}

void main() {
  testWidgets('shows the empty state when there is no review yet', (tester) async {
    await pumpScreen(tester, FakeReviewRepository());
    expect(find.text('No Reality Check yet'), findsOneWidget);
  });

  testWidgets('shows status, evidence and the AI disclosure for a pending review', (tester) async {
    await pumpScreen(tester, FakeReviewRepository()..myStatus = fakeReview());

    expect(find.text('AI Engineer'), findsOneWidget);
    expect(find.text('Waiting for counsellor'), findsOneWidget);
    expect(find.textContaining('Risk requiring review'), findsOneWidget);
    expect(find.text('Python'), findsWidgets);
  });
}
