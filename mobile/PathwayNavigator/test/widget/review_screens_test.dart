import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';
import 'package:pathway_navigator/features/reality_check/data/review_repository.dart';
import 'package:pathway_navigator/features/reality_check/presentation/student_reality_screen.dart';

import '../support/fakes.dart';

Future<void> pumpScreen(WidgetTester tester, FakeReviewRepository repository) async {
  tester.view.physicalSize = const Size(1000, 4000);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
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

  testWidgets('NeedsRevision opens a pre-filled form; adding a missing skill and resubmitting calls resubmit', (tester) async {
    final repository = FakeReviewRepository()
      ..myStatus = fakeReview(status: 'NeedsRevision')
      ..resubmitResult = fakeReview(id: 'r2', status: 'Approved');

    await pumpScreen(tester, repository);

    expect(find.text('Revise & resubmit Reality Check'), findsOneWidget);
    expect(find.text('Pre-filled from your saved profile'), findsOneWidget);
    expect(find.widgetWithText(TextFormField, 'Physical Science'), findsOneWidget);
    expect(find.widgetWithText(TextFormField, 'A, B, C'), findsOneWidget);

    await tester.tap(find.text('+ Maths'));
    await tester.pump();
    expect(find.widgetWithText(TextFormField, 'Python, Maths'), findsOneWidget);

    await tester.tap(find.text('Resubmit Reality Check'));
    await tester.pumpAndSettle();

    expect(repository.lastResubmission!.id, 'r1');
    expect(repository.lastResubmission!.input.currentSkills, ['Python', 'Maths']);
    expect(find.textContaining('Updated Reality Check submitted'), findsOneWidget);
  });

  testWidgets('an approved review offers an optional Revise & resubmit button', (tester) async {
    await pumpScreen(tester, FakeReviewRepository()..myStatus = fakeReview(status: 'Approved'));

    expect(find.text('Revise & resubmit'), findsOneWidget);
    expect(find.text('Revise & resubmit Reality Check'), findsNothing);
  });

  testWidgets('a pending review shows no revision controls', (tester) async {
    await pumpScreen(tester, FakeReviewRepository()..myStatus = fakeReview());

    expect(find.text('Revise & resubmit'), findsNothing);
    expect(find.text('Revise & resubmit Reality Check'), findsNothing);
  });
}
