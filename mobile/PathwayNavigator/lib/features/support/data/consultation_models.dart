import 'dart:convert';

import '../../../core/utils/json_helpers.dart';

/// Consultation workflow states (`ConsultationStatus` on the backend).
abstract final class ConsultationStatus {
  static const String open = 'Open';
  static const String claimed = 'Claimed';
  static const String inProgress = 'InProgress';
  static const String awaitingStudent = 'AwaitingStudent';
  static const String answered = 'Answered';
  static const String resolved = 'Resolved';
  static const String closed = 'Closed';
  static const String escalated = 'Escalated';

  static const List<String> filterOptions = ['', open, answered, closed];

  static String labelFor(String status) {
    switch (status) {
      case open:
        return 'Waiting for a consultant';
      case claimed:
        return 'A consultant picked this up';
      case inProgress:
        return 'Your consultant is working on it';
      case awaitingStudent:
        return 'Waiting for your reply';
      case answered:
        return 'Answered';
      case resolved:
        return 'Resolved';
      case closed:
        return 'Closed';
      case escalated:
        return 'Escalated for an approval review';
      default:
        return status;
    }
  }
}

/// Where a question is anchored - the same component the answer is written back into.
abstract final class ConsultationContext {
  static const String careerDiscovery = 'CareerDiscovery';
  static const String pathwayPlan = 'PathwayPlan';
  static const String realityCheck = 'RealityCheck';
  static const String general = 'General';

  static const List<String> all = [careerDiscovery, pathwayPlan, realityCheck, general];

  static String labelFor(String context) {
    switch (context) {
      case careerDiscovery:
        return 'Career Discovery';
      case pathwayPlan:
        return 'Pathway plan';
      case realityCheck:
        return 'Reality Check';
      default:
        return 'General question';
    }
  }
}

abstract final class ConsultationPriority {
  static const String p1 = 'P1';
  static const String p2 = 'P2';
  static const String p3 = 'P3';

  static const List<String> all = [p1, p2, p3];
}

class ConsultationResource {
  const ConsultationResource({required this.label, required this.url, this.kind = 'guide'});

  final String label;
  final String url;
  final String kind;

  factory ConsultationResource.fromJson(Map<String, dynamic> json) => ConsultationResource(
        label: asString(json['label']),
        url: asString(json['url']),
        kind: asString(json['kind'], 'guide'),
      );

  Map<String, Object?> toJson() => {'label': label, 'url': url, 'kind': kind};
}

class ConsultationMessage {
  const ConsultationMessage({
    required this.id,
    required this.authorName,
    required this.authorRole,
    required this.body,
    required this.isInternal,
    required this.createdAt,
    this.resources = const <ConsultationResource>[],
  });

  final String id;
  final String authorName;
  final String authorRole;
  final String body;
  final bool isInternal;
  final DateTime createdAt;
  final List<ConsultationResource> resources;

  bool get isFromConsultant => authorRole == 'Consultant' || authorRole == 'Admin';

  factory ConsultationMessage.fromJson(Map<String, dynamic> json) => ConsultationMessage(
        id: asString(json['id']),
        authorName: asString(json['authorName']),
        authorRole: asString(json['authorRole']),
        body: asString(json['body']),
        isInternal: asBool(json['isInternal']),
        createdAt: asDate(json['createdAt'], fallback: DateTime.now().toUtc()),
        resources: asMapList(json['resources']).map(ConsultationResource.fromJson).toList(),
      );
}

/// The structured answer a consultant attaches to a pathway milestone or a review.
class ConsultationGuidance {
  const ConsultationGuidance({
    this.stageKey,
    this.note,
    this.resources = const <ConsultationResource>[],
    this.checklist = const <String>[],
    this.nextSteps = const <String>[],
    this.consultantName,
    this.createdAt,
  });

  final String? stageKey;
  final String? note;
  final List<ConsultationResource> resources;
  final List<String> checklist;
  final List<String> nextSteps;
  final String? consultantName;
  final DateTime? createdAt;

  bool get isEmpty => (note ?? '').trim().isEmpty && resources.isEmpty && checklist.isEmpty && nextSteps.isEmpty;

  factory ConsultationGuidance.fromJson(Map<String, dynamic> json) => ConsultationGuidance(
        stageKey: asNullableString(json['stageKey'] ?? json['stage_key']),
        note: asNullableString(json['note'] ?? json['advice']),
        resources: asMapList(json['resources']).map(ConsultationResource.fromJson).toList(),
        checklist: asStringList(json['checklist']),
        nextSteps: asStringList(json['nextSteps'] ?? json['next_steps']),
        consultantName: asNullableString(json['consultantName'] ?? json['consultant_name']),
        createdAt: asNullableDate(json['createdAt'] ?? json['created_at']),
      );

  Map<String, Object?> toJson() => {
        if (stageKey != null && stageKey!.isNotEmpty) 'stageKey': stageKey,
        if (note != null && note!.isNotEmpty) 'note': note,
        'resources': resources.map((resource) => resource.toJson()).toList(),
        'checklist': checklist,
        'nextSteps': nextSteps,
      };

  /// Parses a stored guidance column.
  ///
  /// The backend writes `{}` when there is no guidance, a single object after a reply, and the
  /// pathway-plan read model keeps a JSON *array* when several replies touched different milestones -
  /// so all three shapes are accepted and empty entries are dropped.
  static List<ConsultationGuidance> parseList(dynamic raw) {
    dynamic decoded = raw;
    if (decoded is String) {
      final text = decoded.trim();
      if (text.isEmpty || text == '{}' || text == '[]') return const <ConsultationGuidance>[];
      try {
        decoded = jsonDecode(text);
      } on FormatException {
        return const <ConsultationGuidance>[];
      }
    }

    final items = <ConsultationGuidance>[];
    if (decoded is List) {
      items.addAll(decoded.whereType<Map>().map((item) => ConsultationGuidance.fromJson(Map<String, dynamic>.from(item))));
    } else if (decoded is Map) {
      items.add(ConsultationGuidance.fromJson(Map<String, dynamic>.from(decoded)));
    }
    return items.where((item) => !item.isEmpty).toList();
  }
}

class ConsultationAuditEvent {
  const ConsultationAuditEvent({required this.action, required this.fromStatus, required this.toStatus, required this.createdAt, this.details});

  final String action;
  final String fromStatus;
  final String toStatus;
  final String? details;
  final DateTime createdAt;

  factory ConsultationAuditEvent.fromJson(Map<String, dynamic> json) => ConsultationAuditEvent(
        action: asString(json['action']),
        fromStatus: asString(json['fromStatus']),
        toStatus: asString(json['toStatus']),
        details: asNullableString(json['details']),
        createdAt: asDate(json['createdAt'], fallback: DateTime.now().toUtc()),
      );
}

/// Agent 5 triage evidence. Consultant/Admin only - the API never sends this to a student.
class ConsultationTriage {
  const ConsultationTriage({
    required this.category,
    required this.priority,
    required this.language,
    required this.sentiment,
    required this.confidence,
    this.expertiseTags = const <String>[],
    this.safetyFlags = const <String>[],
    this.suggestedSlaHours = 48,
  });

  final String category;
  final String priority;
  final String language;
  final String sentiment;
  final double confidence;
  final List<String> expertiseTags;
  final List<String> safetyFlags;
  final int suggestedSlaHours;

  factory ConsultationTriage.fromJson(Map<String, dynamic> json) => ConsultationTriage(
        category: asString(json['category'], 'Unclassified'),
        priority: asString(json['priority'], ConsultationPriority.p2),
        language: asString(json['language'], 'English'),
        sentiment: asString(json['sentiment'], 'neutral'),
        confidence: asDouble(json['confidence']),
        expertiseTags: asStringList(json['expertise_tags'] ?? json['expertiseTags']),
        safetyFlags: asStringList(json['safety_flags'] ?? json['safetyFlags']),
        suggestedSlaHours: asInt(json['suggested_sla_hours'] ?? json['suggestedSlaHours'], 48),
      );
}

class Consultation {
  const Consultation({
    required this.id,
    required this.subject,
    required this.body,
    required this.contextType,
    required this.contextSummary,
    required this.category,
    required this.priority,
    required this.status,
    required this.createdAt,
    this.studentName = '',
    this.assignedConsultantId,
    this.assignedConsultantName,
    this.contextSnapshotJson = '{}',
    this.slaDueAt,
    this.isSlaBreached = false,
    this.closedAt,
    this.resolutionSummary,
    this.consultantGuidanceJson = '{}',
    this.studentRating,
    this.studentFeedback,
    this.messages = const <ConsultationMessage>[],
    this.auditEvents = const <ConsultationAuditEvent>[],
    this.agentTriage,
  });

  final String id;
  final String subject;
  final String body;
  final String contextType;
  final String contextSummary;
  final String category;
  final String priority;
  final String status;
  final DateTime createdAt;
  final String studentName;
  final String? assignedConsultantId;
  final String? assignedConsultantName;
  final String contextSnapshotJson;
  final DateTime? slaDueAt;
  final bool isSlaBreached;
  final DateTime? closedAt;
  final String? resolutionSummary;
  final String consultantGuidanceJson;
  final int? studentRating;
  final String? studentFeedback;
  final List<ConsultationMessage> messages;
  final List<ConsultationAuditEvent> auditEvents;
  final ConsultationTriage? agentTriage;

  bool get isClosed => status == ConsultationStatus.closed;
  bool get isClaimed => (assignedConsultantId ?? '').isNotEmpty;

  /// Messages a student is allowed to see (the API already filters these, this is presentation).
  List<ConsultationMessage> get visibleMessages => messages.where((message) => !message.isInternal).toList();

  List<ConsultationMessage> get internalNotes => messages.where((message) => message.isInternal).toList();

  List<ConsultationGuidance> get guidance => ConsultationGuidance.parseList(consultantGuidanceJson);

  /// "3h left" / "2d overdue" - the consultant queue sorts by this, the student sees a reassurance.
  String get slaLabel {
    final due = slaDueAt;
    if (due == null) return '';
    if (isClosed) return 'closed';
    final hours = due.difference(DateTime.now().toUtc()).inHours;
    if (hours < 0) return '${hours.abs()}h overdue';
    if (hours < 24) return '${hours}h left';
    return '${(hours / 24).round()}d left';
  }

  factory Consultation.fromJson(Map<String, dynamic> json) => Consultation(
        id: asString(json['id']),
        subject: asString(json['subject']),
        body: asString(json['body']),
        contextType: asString(json['contextType'], ConsultationContext.general),
        contextSummary: asString(json['contextSummary']),
        category: asString(json['category'], 'Unclassified'),
        priority: asString(json['priority'], ConsultationPriority.p2),
        status: asString(json['status']),
        createdAt: asDate(json['createdAt'], fallback: DateTime.now().toUtc()),
        studentName: asString(json['studentName']),
        assignedConsultantId: asNullableString(json['assignedConsultantId']),
        assignedConsultantName: asNullableString(json['assignedConsultantName']),
        contextSnapshotJson: asString(json['contextSnapshotJson'], '{}'),
        slaDueAt: asNullableDate(json['slaDueAt']),
        isSlaBreached: asBool(json['isSlaBreached']),
        closedAt: asNullableDate(json['closedAt']),
        resolutionSummary: asNullableString(json['resolutionSummary']),
        consultantGuidanceJson: asString(json['consultantGuidanceJson'], '{}'),
        studentRating: json['studentRating'] is num ? asInt(json['studentRating']) : null,
        studentFeedback: asNullableString(json['studentFeedback']),
        messages: asMapList(json['messages']).map(ConsultationMessage.fromJson).toList(),
        auditEvents: asMapList(json['auditEvents']).map(ConsultationAuditEvent.fromJson).toList(),
        agentTriage: json['agentTriage'] is Map ? ConsultationTriage.fromJson(asMap(json['agentTriage'])) : null,
      );
}

class ConsultationPage {
  const ConsultationPage({required this.items, required this.page, required this.totalCount, required this.totalPages});

  final List<Consultation> items;
  final int page;
  final int totalCount;
  final int totalPages;

  static const ConsultationPage empty = ConsultationPage(items: [], page: 1, totalCount: 0, totalPages: 0);

  factory ConsultationPage.fromJson(Map<String, dynamic> json) => ConsultationPage(
        items: asMapList(json['items']).map(Consultation.fromJson).toList(),
        page: asInt(json['page'], 1),
        totalCount: asInt(json['totalCount']),
        totalPages: asInt(json['totalPages']),
      );
}

/// Consultant queue filters (mirrors `ConsultantQueueQuery` on the backend).
class ConsultantQueueQuery {
  const ConsultantQueueQuery({
    this.scope = 'Open',
    this.category = '',
    this.priority = '',
    this.contextType = '',
    this.search = '',
    this.sort = 'sla',
    this.page = 1,
    this.pageSize = 10,
  });

  final String scope;
  final String category;
  final String priority;
  final String contextType;
  final String search;
  final String sort;
  final int page;
  final int pageSize;

  static const List<String> scopes = ['Open', 'Mine', 'Unresolved', 'All'];

  ConsultantQueueQuery copyWith({
    String? scope,
    String? category,
    String? priority,
    String? contextType,
    String? search,
    String? sort,
    int? page,
    int? pageSize,
  }) =>
      ConsultantQueueQuery(
        scope: scope ?? this.scope,
        category: category ?? this.category,
        priority: priority ?? this.priority,
        contextType: contextType ?? this.contextType,
        search: search ?? this.search,
        sort: sort ?? this.sort,
        page: page ?? this.page,
        pageSize: pageSize ?? this.pageSize,
      );

  Map<String, Object?> toQuery() => {
        'scope': scope,
        'category': category,
        'priority': priority,
        'contextType': contextType,
        'search': search,
        'sort': sort,
        'page': page,
        'pageSize': pageSize,
      };
}

class ConsultantBriefSection {
  const ConsultantBriefSection({required this.title, required this.points});

  final String title;
  final List<String> points;

  factory ConsultantBriefSection.fromJson(Map<String, dynamic> json) =>
      ConsultantBriefSection(title: asString(json['title']), points: asStringList(json['points']));
}

/// Agent 5 case brief - always shown as guidance for the human, never sent as an answer.
class ConsultantBrief {
  const ConsultantBrief({required this.headline, required this.sections, required this.confidence, this.evidenceSources = const <String>[]});

  final String headline;
  final List<ConsultantBriefSection> sections;
  final double confidence;
  final List<String> evidenceSources;

  factory ConsultantBrief.fromJson(Map<String, dynamic> json) => ConsultantBrief(
        headline: asString(json['headline']),
        sections: asMapList(json['sections']).map(ConsultantBriefSection.fromJson).toList(),
        confidence: asDouble(json['confidence']),
        evidenceSources: asStringList(json['evidence_sources'] ?? json['evidenceSources']),
      );
}

/// Agent 5 draft reply. `mustEscalate` is a hard stop: the consultant must change the reply first.
class ConsultantDraft {
  const ConsultantDraft({
    required this.body,
    required this.confidence,
    this.mustEscalate = false,
    this.safetyNotes = const <String>[],
    this.citations = const <String>[],
  });

  final String body;
  final double confidence;
  final bool mustEscalate;
  final List<String> safetyNotes;
  final List<String> citations;

  factory ConsultantDraft.fromJson(Map<String, dynamic> json) => ConsultantDraft(
        body: asString(json['body']),
        confidence: asDouble(json['confidence']),
        mustEscalate: asBool(json['must_escalate'] ?? json['mustEscalate']),
        safetyNotes: asStringList(json['safety_notes'] ?? json['safetyNotes']),
        citations: asStringList(json['citations']),
      );
}

class ConsultantStats {
  const ConsultantStats({
    required this.openCases,
    required this.assignedToMe,
    required this.unclaimedPool,
    required this.slaCompliancePercent,
    this.averageRating = 0,
  });

  final int openCases;
  final int assignedToMe;
  final int unclaimedPool;
  final double slaCompliancePercent;
  final double averageRating;

  factory ConsultantStats.fromJson(Map<String, dynamic> json) => ConsultantStats(
        openCases: asInt(json['openCases']),
        assignedToMe: asInt(json['assignedToMe']),
        unclaimedPool: asInt(json['unclaimedPool']),
        slaCompliancePercent: asDouble(json['slaCompliancePercent']),
        averageRating: asDouble(json['averageRating']),
      );
}

/// A row in the in-app notification inbox.
class AppNotification {
  const AppNotification({
    required this.id,
    required this.type,
    required this.title,
    required this.body,
    required this.createdAt,
    this.deepLink,
    this.priority = 'Info',
    this.isRead = false,
  });

  final String id;
  final String type;
  final String title;
  final String body;
  final DateTime createdAt;
  final String? deepLink;
  final String priority;
  final bool isRead;

  bool get isUrgent => priority == 'Urgent' || priority == 'Warning';

  factory AppNotification.fromJson(Map<String, dynamic> json) => AppNotification(
        id: asString(json['id']),
        type: asString(json['type']),
        title: asString(json['title']),
        body: asString(json['body']),
        createdAt: asDate(json['createdAt'], fallback: DateTime.now().toUtc()),
        deepLink: asNullableString(json['deepLink']),
        priority: asString(json['priority'], 'Info'),
        isRead: asBool(json['isRead']),
      );
}

class NotificationPage {
  const NotificationPage({required this.items, required this.unreadCount, required this.page, required this.totalCount});

  final List<AppNotification> items;
  final int unreadCount;
  final int page;
  final int totalCount;

  static const NotificationPage empty = NotificationPage(items: [], unreadCount: 0, page: 1, totalCount: 0);

  factory NotificationPage.fromJson(Map<String, dynamic> json) => NotificationPage(
        items: asMapList(json['items']).map(AppNotification.fromJson).toList(),
        unreadCount: asInt(json['unreadCount']),
        page: asInt(json['page'], 1),
        totalCount: asInt(json['totalCount']),
      );
}

/// Maps the web deep link stored on a notification onto a mobile route.
///
/// The API is the single source of truth for *where* an answer was written back to; the mobile app
/// only translates that path into its own route space. Unknown paths fall back to the inbox.
String notificationRoute(String? deepLink, String role) {
  final link = (deepLink ?? '').trim();
  if (link.isEmpty) return '';
  final uri = Uri.tryParse(link);
  final path = uri?.path ?? link;
  final consultation = uri?.queryParameters['consultation'];

  if (path.contains('support')) {
    return consultation == null || consultation.isEmpty
        ? '/student/support'
        : '/student/support?consultation=$consultation';
  }
  if (path.contains('consultant')) {
    return consultation == null || consultation.isEmpty ? '/consultant' : '/consultant?consultation=$consultation';
  }
  if (path.contains('reality-check')) return '/student/reality-check';
  if (path.contains('career-discovery')) return '/student/career-discovery';
  return '';
}
