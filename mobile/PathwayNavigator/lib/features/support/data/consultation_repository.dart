import '../../../core/network/api_client.dart';
import '../../../core/utils/json_helpers.dart';
import 'consultation_models.dart';

/// The consultation channel, as seen from all three sides: the student asking, the consultant
/// answering, and the notification inbox that tells either of them something changed.
///
/// Every method maps 1:1 onto a REST endpoint - the split of *who* may call what is enforced by the
/// API, not by constructing different clients.
abstract interface class ConsultationRepository {
  // ---- student ----
  Future<Consultation> create({
    required String subject,
    required String body,
    String contextType,
    String? contextRefId,
    String? category,
    String? priority,
  });

  Future<ConsultationPage> getMine({String status = '', int page = 1, int pageSize = 10});

  Future<Consultation> getById(String id);

  Future<Consultation> addMessage(String id, String body);

  Future<Consultation> close(String id, {int? rating, String? feedback});

  Future<Consultation> reopen(String id);

  /// Whether the student already has an open question about the thing they are looking at.
  Future<bool> hasOpenRequest(String contextType, String? contextRefId);

  // ---- consultant ----
  Future<ConsultationPage> getQueue(ConsultantQueueQuery query);

  Future<Consultation> getCase(String id);

  Future<Consultation> claim(String id);

  Future<Consultation> release(String id, String reason);

  Future<Consultation> reply(
    String id, {
    required String message,
    List<ConsultationResource> resources = const <ConsultationResource>[],
    ConsultationGuidance? guidance,
    String? resolutionSummary,
    bool closeAfterReply = false,
    bool usedAgentDraft = false,
  });

  Future<Consultation> addNote(String id, String body);

  Future<Consultation> updatePriority(String id, String priority);

  Future<Consultation> escalate(String id, String reason);

  /// Agent 5 case brief, or null when the copilot is unavailable (never an error for the desk).
  Future<ConsultantBrief?> getBrief(String id);

  /// Agent 5 draft reply. The consultant edits it; nothing is sent automatically.
  Future<ConsultantDraft?> getDraft(String id, {String tone = 'Supportive'});

  Future<ConsultantStats> getStats();

  // ---- notifications ----
  Future<NotificationPage> getNotifications({bool unreadOnly = false, int page = 1, int pageSize = 20});

  Future<int> getUnreadCount();

  Future<void> markRead(String id);

  Future<void> markAllRead();
}

class RemoteConsultationRepository implements ConsultationRepository {
  RemoteConsultationRepository(this._api);

  final ApiClient _api;

  @override
  Future<Consultation> create({
    required String subject,
    required String body,
    String contextType = ConsultationContext.general,
    String? contextRefId,
    String? category,
    String? priority,
  }) async =>
      Consultation.fromJson(asMap(await _api.post('/consultations', body: {
        'contextType': contextType,
        'contextRefId': contextRefId,
        'category': category,
        'priority': priority,
        'subject': subject.trim(),
        'body': body.trim(),
      })));

  @override
  Future<ConsultationPage> getMine({String status = '', int page = 1, int pageSize = 10}) async =>
      ConsultationPage.fromJson(asMap(await _api.get('/consultations/me', query: {
        'status': status,
        'page': page,
        'pageSize': pageSize,
      })));

  @override
  Future<Consultation> getById(String id) async => Consultation.fromJson(asMap(await _api.get('/consultations/$id')));

  @override
  Future<Consultation> addMessage(String id, String body) async =>
      Consultation.fromJson(asMap(await _api.post('/consultations/$id/messages', body: {'body': body.trim()})));

  @override
  Future<Consultation> close(String id, {int? rating, String? feedback}) async =>
      Consultation.fromJson(asMap(await _api.post('/consultations/$id/close', body: {'rating': rating, 'feedback': feedback})));

  @override
  Future<Consultation> reopen(String id) async =>
      Consultation.fromJson(asMap(await _api.post('/consultations/$id/reopen')));

  @override
  Future<bool> hasOpenRequest(String contextType, String? contextRefId) async {
    if (contextRefId == null || contextRefId.isEmpty) return false;
    try {
      final json = asMap(await _api.get('/consultations/context/$contextType/$contextRefId'));
      return asBool(json['hasOpenRequest']);
    } catch (_) {
      return false; // a badge is never worth failing a page load for
    }
  }

  @override
  Future<ConsultationPage> getQueue(ConsultantQueueQuery query) async =>
      ConsultationPage.fromJson(asMap(await _api.get('/consultant/queue', query: query.toQuery())));

  @override
  Future<Consultation> getCase(String id) async => Consultation.fromJson(asMap(await _api.get('/consultant/queue/$id')));

  @override
  Future<Consultation> claim(String id) async =>
      Consultation.fromJson(asMap(await _api.post('/consultant/queue/$id/claim')));

  @override
  Future<Consultation> release(String id, String reason) async =>
      Consultation.fromJson(asMap(await _api.post('/consultant/queue/$id/release', body: {'reason': reason})));

  @override
  Future<Consultation> reply(
    String id, {
    required String message,
    List<ConsultationResource> resources = const <ConsultationResource>[],
    ConsultationGuidance? guidance,
    String? resolutionSummary,
    bool closeAfterReply = false,
    bool usedAgentDraft = false,
  }) async =>
      Consultation.fromJson(asMap(await _api.post('/consultant/queue/$id/reply', body: {
        'message': message.trim(),
        'resources': resources.map((resource) => resource.toJson()).toList(),
        'guidance': guidance?.toJson(),
        'resolutionSummary': resolutionSummary,
        'closeAfterReply': closeAfterReply,
        'usedAgentDraft': usedAgentDraft,
      })));

  @override
  Future<Consultation> addNote(String id, String body) async =>
      Consultation.fromJson(asMap(await _api.post('/consultant/queue/$id/note', body: {'body': body.trim()})));

  @override
  Future<Consultation> updatePriority(String id, String priority) async =>
      Consultation.fromJson(asMap(await _api.post('/consultant/queue/$id/priority', body: {'priority': priority})));

  @override
  Future<Consultation> escalate(String id, String reason) async =>
      Consultation.fromJson(asMap(await _api.post('/consultant/queue/$id/escalate', body: {'reason': reason})));

  @override
  Future<ConsultantBrief?> getBrief(String id) async {
    try {
      return ConsultantBrief.fromJson(asMap(await _api.get('/consultant/queue/$id/brief')));
    } catch (_) {
      return null; // the desk must keep working when the copilot is offline
    }
  }

  @override
  Future<ConsultantDraft?> getDraft(String id, {String tone = 'Supportive'}) async {
    try {
      return ConsultantDraft.fromJson(asMap(await _api.post('/consultant/queue/$id/draft', body: {'tone': tone})));
    } catch (_) {
      return null;
    }
  }

  @override
  Future<ConsultantStats> getStats() async => ConsultantStats.fromJson(asMap(await _api.get('/consultant/me/stats')));

  @override
  Future<NotificationPage> getNotifications({bool unreadOnly = false, int page = 1, int pageSize = 20}) async =>
      NotificationPage.fromJson(asMap(await _api.get('/notifications', query: {
        'unreadOnly': unreadOnly,
        'page': page,
        'pageSize': pageSize,
      })));

  @override
  Future<int> getUnreadCount() async {
    final json = asMap(await _api.get('/notifications/unread-count'));
    return asInt(json['unreadCount']);
  }

  @override
  Future<void> markRead(String id) async => _api.post('/notifications/$id/read');

  @override
  Future<void> markAllRead() async => _api.post('/notifications/read-all');
}
