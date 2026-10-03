import 'package:flutter_test/flutter_test.dart';
import 'package:pathway_navigator/core/constants/roles.dart';
import 'package:pathway_navigator/features/support/data/consultation_models.dart';
import 'package:pathway_navigator/features/support/presentation/consultant_desk_controller.dart';
import 'package:pathway_navigator/features/support/presentation/notification_controller.dart';
import 'package:pathway_navigator/features/support/presentation/support_controller.dart';

import '../support/support_fakes.dart';

void main() {
  group('roles', () {
    test('a consultant is a known role but is not staff (no approval authority)', () {
      expect(Roles.isKnown(Roles.consultant), isTrue);
      expect(Roles.isStaff(Roles.consultant), isFalse);
      expect(Roles.isStaff(Roles.counsellor), isTrue);
      expect(Roles.canAccessConsultantDesk(Roles.consultant), isTrue);
      expect(Roles.canAccessConsultantDesk(Roles.admin), isTrue);
      expect(Roles.canAccessConsultantDesk(Roles.student), isFalse);
    });

    test('a consultant logs in straight to their own desk', () {
      expect(homeRouteForRole(Roles.consultant), '/consultant');
      expect(homeRouteForRole(Roles.student), '/student');
    });
  });

  group('ConsultationGuidance.parseList', () {
    test('treats the empty stored shapes as no guidance at all', () {
      expect(ConsultationGuidance.parseList('{}'), isEmpty);
      expect(ConsultationGuidance.parseList('[]'), isEmpty);
      expect(ConsultationGuidance.parseList(''), isEmpty);
      expect(ConsultationGuidance.parseList(null), isEmpty);
      expect(ConsultationGuidance.parseList('not json'), isEmpty);
    });

    test('accepts a single object, an array and an already-parsed list', () {
      expect(ConsultationGuidance.parseList('{"note":"Apply for the scholarship first."}').single.note,
          'Apply for the scholarship first.');
      expect(ConsultationGuidance.parseList('[{"note":"A"},{"note":"B"}]'), hasLength(2));
      expect(ConsultationGuidance.parseList([{'note': 'C'}]).single.note, 'C');
    });

    test('drops entries that carry nothing the student could read', () {
      expect(ConsultationGuidance.parseList('[{"stageKey":"skills"},{"note":null,"resources":[]}]'), isEmpty);
    });

    test('keeps the checklist, resources and milestone key', () {
      final guidance = ConsultationGuidance.parseList(
        '{"stageKey":"skills","note":"Start free.","checklist":["Module 1"],'
        '"resources":[{"label":"Guide","url":"https://example.edu/g","kind":"guide"}]}',
      ).single;

      expect(guidance.stageKey, 'skills');
      expect(guidance.checklist, ['Module 1']);
      expect(guidance.resources.single.url, 'https://example.edu/g');
      expect(guidance.isEmpty, isFalse);
    });
  });

  group('Consultation', () {
    test('parses the API payload and hides internal notes from the student view', () {
      final consultation = fakeConsultation();

      expect(consultation.subject, 'Which pathway fits my budget?');
      expect(consultation.contextType, ConsultationContext.careerDiscovery);
      expect(consultation.visibleMessages, hasLength(1));
      expect(consultation.internalNotes, hasLength(1));
      expect(consultation.isClosed, isFalse);
    });

    test('labels the SLA window the consultant queue sorts by', () {
      expect(fakeConsultation().slaLabel, endsWith('h left'));
      expect(fakeConsultation(status: ConsultationStatus.closed).slaLabel, 'closed');
    });

    test('maps a stored deep link onto a mobile route', () {
      expect(notificationRoute('/student/support?consultation=c1', Roles.student), '/student/support?consultation=c1');
      expect(notificationRoute('/consultant/dashboard?consultation=c1', Roles.consultant), '/consultant?consultation=c1');
      expect(notificationRoute('/career-discovery', Roles.student), '/student/career-discovery');
      expect(notificationRoute('/student/reality-check?career=AI', Roles.student), '/student/reality-check');
      expect(notificationRoute(null, Roles.student), '');
    });
  });

  group('SupportController', () {
    test('asks with the context of the component the student was looking at', () async {
      final repository = FakeConsultationRepository();
      final controller = SupportController(repository);

      final created = await controller.ask(
        subject: 'Cheaper route?',
        body: 'Is there an affordable way into AI engineering?',
        contextType: ConsultationContext.pathwayPlan,
        contextRefId: 'p1',
        category: 'PathwayAdvice',
      );

      expect(created, isNotNull);
      expect(repository.lastCreate?['contextType'], ConsultationContext.pathwayPlan);
      expect(repository.lastCreate?['contextRefId'], 'p1');
      expect(controller.notice, contains('notification'));
      controller.dispose();
    });

    test('refuses an empty reply and keeps the student on the thread after a real one', () async {
      final repository = FakeConsultationRepository()
        ..current = fakeConsultation(status: ConsultationStatus.answered);
      final controller = SupportController(repository);
      await controller.open('c1');

      expect(await controller.sendMessage('x'), isFalse);
      expect(repository.lastMessage, isNull);

      expect(await controller.sendMessage('Thank you, I submitted it.'), isTrue);
      expect(repository.lastMessage?.body, 'Thank you, I submitted it.');
      controller.dispose();
    });

    test('does not set a stale selected thread when opening fails', () async {
      final repository = FakeConsultationRepository()..error = Exception('boom');
      final controller = SupportController(repository);

      await controller.open('c1');

      expect(controller.selected, isNull);
      expect(controller.error, isNotNull);
      controller.dispose();
    });
  });

  group('ConsultantDeskController', () {
    test('loads the queue and opens a case with its brief', () async {
      final repository = FakeConsultationRepository()
        ..queue = ConsultationPage(
          items: [fakeConsultation()],
          page: 1,
          totalCount: 1,
          totalPages: 1,
        )
        ..brief = const ConsultantBrief(headline: 'PathwayAdvice', sections: [], confidence: 0.7);
      final controller = ConsultantDeskController(repository);

      await controller.loadQueue();
      expect(controller.queue.items, hasLength(1));
      expect(controller.stats?.unclaimedPool, 1);

      await controller.openCase('c1');
      expect(controller.selected?.id, 'c1');
      expect(controller.brief?.headline, 'PathwayAdvice');
      controller.dispose();
    });

    test('inserting an Agent 5 draft never sends anything by itself', () async {
      final repository = FakeConsultationRepository()
        ..current = fakeConsultation()
        ..draft = const ConsultantDraft(body: 'Here is a cheaper route...', confidence: 0.8);
      final controller = ConsultantDeskController(repository);
      await controller.openCase('c1');

      final draft = await controller.loadDraft(tone: 'Direct');

      expect(draft?.body, contains('cheaper route'));
      expect(repository.calls.any((call) => call.startsWith('reply:')), isFalse);
      controller.dispose();
    });

    test('sends the reply with the draft flag and refreshes the queue', () async {
      final repository = FakeConsultationRepository()..current = fakeConsultation();
      final controller = ConsultantDeskController(repository);
      await controller.openCase('c1');

      final ok = await controller.sendReply(
        message: 'Apply for the merit scholarship first.',
        guidance: const ConsultationGuidance(note: 'Do this before paying for a course.'),
        usedAgentDraft: true,
      );

      expect(ok, isTrue);
      expect(repository.lastReply?.usedAgentDraft, isTrue);
      expect(repository.calls, contains('reply:c1'));
      expect(controller.selected?.status, ConsultationStatus.answered);
      controller.dispose();
    });

    test('an escalated case keeps the counsellor in charge of approvals', () async {
      final repository = FakeConsultationRepository()..current = fakeConsultation();
      final controller = ConsultantDeskController(repository);
      await controller.openCase('c1');

      await controller.escalate();

      expect(repository.calls, contains('escalate:c1'));
      expect(controller.notice, contains('counsellor'));
      controller.dispose();
    });

    test('refuses to send an empty reply', () async {
      final repository = FakeConsultationRepository()..current = fakeConsultation();
      final controller = ConsultantDeskController(repository);
      await controller.openCase('c1');

      expect(await controller.sendReply(message: '  '), isFalse);
      expect(controller.error, contains('Write a reply'));
      expect(repository.calls.any((call) => call.startsWith('reply:')), isFalse);
      controller.dispose();
    });
  });

  group('NotificationController', () {
    test('polls the unread count and keeps the badge in step after a read', () async {
      final repository = FakeConsultationRepository()
        ..unreadCount = 2
        ..notifications = NotificationPage(
          items: [
            AppNotification(
              id: 'n1',
              type: 'ConsultationAnswered',
              title: 'Your consultant replied',
              body: 'Ms. Perera answered your question.',
              createdAt: DateTime.utc(2026, 10, 3),
            ),
          ],
          unreadCount: 2,
          page: 1,
          totalCount: 1,
        );
      final controller = NotificationController(repository, pollInterval: const Duration(hours: 1));

      controller.start();
      await Future<void>.delayed(Duration.zero);
      expect(controller.unreadCount, 2);

      await controller.markAllRead();
      expect(controller.unreadCount, 0);
      expect(repository.calls, contains('readAll'));
      controller.dispose();
    });
  });
}
