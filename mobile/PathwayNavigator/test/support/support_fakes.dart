import 'dart:convert';

import 'package:pathway_navigator/features/support/data/consultation_models.dart';
import 'package:pathway_navigator/features/support/data/consultation_repository.dart';

/// In-memory stand-in for the consultation API, shared by the support unit and widget tests.
class FakeConsultationRepository implements ConsultationRepository {
  ConsultationPage myConsultations = ConsultationPage.empty;
  ConsultationPage queue = ConsultationPage.empty;
  Consultation? current;
  NotificationPage notifications = NotificationPage.empty;
  int unreadCount = 0;
  Object? error;
  ConsultantBrief? brief;
  ConsultantDraft? draft;

  final List<String> calls = [];
  final List<ConsultantQueueQuery> queueQueries = [];
  ({String id, String message, bool usedAgentDraft})? lastReply;
  ({String id, String body})? lastMessage;
  Map<String, Object?>? lastCreate;

  void _guard() {
    final failure = error;
    if (failure != null) throw failure;
  }

  @override
  Future<Consultation> create({
    required String subject,
    required String body,
    String contextType = ConsultationContext.general,
    String? contextRefId,
    String? category,
    String? priority,
  }) async {
    _guard();
    calls.add('create:$contextType');
    lastCreate = {
      'subject': subject,
      'body': body,
      'contextType': contextType,
      'contextRefId': contextRefId,
      'category': category,
      'priority': priority,
    };
    return current ?? fakeConsultation(subject: subject, contextType: contextType);
  }

  @override
  Future<ConsultationPage> getMine({String status = '', int page = 1, int pageSize = 10}) async {
    _guard();
    calls.add('getMine:$status');
    return myConsultations;
  }

  @override
  Future<Consultation> getById(String id) async {
    _guard();
    calls.add('getById:$id');
    return current ?? fakeConsultation(id: id);
  }

  @override
  Future<Consultation> addMessage(String id, String body) async {
    _guard();
    calls.add('addMessage:$id');
    lastMessage = (id: id, body: body);
    return current ?? fakeConsultation(id: id);
  }

  @override
  Future<Consultation> close(String id, {int? rating, String? feedback}) async {
    _guard();
    calls.add('close:$id');
    return fakeConsultation(id: id, status: ConsultationStatus.closed);
  }

  @override
  Future<Consultation> reopen(String id) async {
    _guard();
    calls.add('reopen:$id');
    return fakeConsultation(id: id, status: ConsultationStatus.inProgress);
  }

  @override
  Future<bool> hasOpenRequest(String contextType, String? contextRefId) async => false;

  @override
  Future<ConsultationPage> getQueue(ConsultantQueueQuery query) async {
    _guard();
    queueQueries.add(query);
    return queue;
  }

  @override
  Future<Consultation> getCase(String id) async {
    _guard();
    calls.add('getCase:$id');
    return current ?? fakeConsultation(id: id);
  }

  @override
  Future<Consultation> claim(String id) async {
    _guard();
    calls.add('claim:$id');
    return fakeConsultation(id: id, status: ConsultationStatus.claimed);
  }

  @override
  Future<Consultation> release(String id, String reason) async {
    _guard();
    calls.add('release:$id');
    return fakeConsultation(id: id);
  }

  @override
  Future<Consultation> reply(
    String id, {
    required String message,
    List<ConsultationResource> resources = const <ConsultationResource>[],
    ConsultationGuidance? guidance,
    String? resolutionSummary,
    bool closeAfterReply = false,
    bool usedAgentDraft = false,
  }) async {
    _guard();
    calls.add('reply:$id');
    lastReply = (id: id, message: message, usedAgentDraft: usedAgentDraft);
    return fakeConsultation(
      id: id,
      status: closeAfterReply ? ConsultationStatus.closed : ConsultationStatus.answered,
      guidanceJson: guidance == null ? '{}' : jsonEncode(guidance.toJson()),
    );
  }

  @override
  Future<Consultation> addNote(String id, String body) async {
    _guard();
    calls.add('note:$id');
    return current ?? fakeConsultation(id: id);
  }

  @override
  Future<Consultation> updatePriority(String id, String priority) async {
    _guard();
    calls.add('priority:$id:$priority');
    return fakeConsultation(id: id, priority: priority);
  }

  @override
  Future<Consultation> escalate(String id, String reason) async {
    _guard();
    calls.add('escalate:$id');
    return fakeConsultation(id: id, status: ConsultationStatus.escalated);
  }

  @override
  Future<ConsultantBrief?> getBrief(String id) async => brief;

  @override
  Future<ConsultantDraft?> getDraft(String id, {String tone = 'Supportive'}) async {
    calls.add('draft:$id:$tone');
    return draft;
  }

  @override
  Future<ConsultantStats> getStats() async => const ConsultantStats(
        openCases: 2,
        assignedToMe: 1,
        unclaimedPool: 1,
        slaCompliancePercent: 95,
        averageRating: 4.5,
      );

  @override
  Future<NotificationPage> getNotifications({bool unreadOnly = false, int page = 1, int pageSize = 20}) async {
    _guard();
    calls.add('notifications');
    return notifications;
  }

  @override
  Future<int> getUnreadCount() async => unreadCount;

  @override
  Future<void> markRead(String id) async => calls.add('read:$id');

  @override
  Future<void> markAllRead() async => calls.add('readAll');
}

/// A consultation as the .NET API serialises it (camelCase, JSON-encoded guidance column).
Consultation fakeConsultation({
  String id = 'c1',
  String subject = 'Which pathway fits my budget?',
  String status = ConsultationStatus.open,
  String contextType = ConsultationContext.careerDiscovery,
  String priority = ConsultationPriority.p2,
  String guidanceJson = '{}',
  String contextSummary = 'Career Discovery - top matches: AI Engineer',
}) =>
    Consultation.fromJson({
      'id': id,
      'studentId': 's1',
      'studentName': 'Nimal',
      'assignedConsultantId': status == ConsultationStatus.open ? null : 'u9',
      'assignedConsultantName': status == ConsultationStatus.open ? null : 'Ms. Perera',
      'contextType': contextType,
      'contextRefId': 'a1',
      'contextSnapshotJson': '{"topCareers":["AI Engineer"]}',
      'contextSummary': contextSummary,
      'category': 'PathwayAdvice',
      'priority': priority,
      'subject': subject,
      'body': 'AI Engineer looks expensive for my family.',
      'status': status,
      'slaDueAt': DateTime.now().toUtc().add(const Duration(hours: 20)).toIso8601String(),
      'isSlaBreached': false,
      'firstRespondedAt': null,
      'answeredAt': null,
      'closedAt': null,
      'resolutionSummary': null,
      'consultantGuidanceJson': guidanceJson,
      'agentDraftUsed': false,
      'studentRating': null,
      'studentFeedback': null,
      'createdAt': '2026-10-03T08:00:00Z',
      'updatedAt': '2026-10-03T08:00:00Z',
      'messages': [
        {
          'id': 'm1',
          'authorUserId': 's1',
          'authorName': 'Nimal',
          'authorRole': 'Student',
          'body': 'AI Engineer looks expensive.',
          'isInternal': false,
          'resources': <Object?>[],
          'createdAt': '2026-10-03T08:00:00Z',
        },
        {
          'id': 'm2',
          'authorUserId': 'u9',
          'authorName': 'Ms. Perera',
          'authorRole': 'Consultant',
          'body': 'Check the scholarship page.',
          'isInternal': true,
          'resources': <Object?>[],
          'createdAt': '2026-10-03T08:10:00Z',
        },
      ],
      'auditEvents': <Object?>[],
      'agentTriage': null,
    });
