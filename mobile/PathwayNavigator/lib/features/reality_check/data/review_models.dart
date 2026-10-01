import '../../../core/utils/json_helpers.dart';

/// Review workflow states (`PathwayReviewStatus` on the backend).
abstract final class ReviewStatus {
  static const String pending = 'Pending';
  static const String approved = 'Approved';
  static const String rejected = 'Rejected';
  static const String needsRevision = 'NeedsRevision';
  static const String all = 'All';

  static const List<String> filterOptions = [pending, approved, rejected, needsRevision, all];
}

/// A controlled-tool invocation or workflow step recorded by Agent 4.
/// The AI service persists these snake_case, so both spellings are accepted.
class AuditEntry {
  const AuditEntry({required this.name, required this.status, this.durationMs, this.summary});

  final String name;
  final String status;
  final double? durationMs;
  final String? summary;

  factory AuditEntry.fromJson(Map<String, dynamic> json) {
    String pick(String snake, String camel) => asString(json[snake] ?? json[camel]);
    final duration = json['duration_ms'] ?? json['durationMs'];
    final name = pick('tool_name', 'toolName');
    return AuditEntry(
      name: name.isNotEmpty ? name : asString(json['step'] ?? json['node']),
      status: asString(json['status']),
      durationMs: duration is num ? duration.toDouble() : null,
      summary: asNullableString(json['result_summary'] ?? json['resultSummary']),
    );
  }
}

/// Mirrors `PathwayReviewResponseDto`.
class PathwayReview {
  const PathwayReview({
    required this.id,
    required this.pathwayAnalysisId,
    required this.status,
    required this.isHighRisk,
    required this.riskReason,
    required this.missingSkills,
    required this.feasibilitySummary,
    required this.degreeRequirement,
    required this.subjectRequirements,
    required this.entryRequirements,
    required this.costGuidance,
    required this.gapClosurePlan,
    required this.evidenceSources,
    required this.feasibilityScore,
    required this.targetCareer,
    required this.workflowId,
    required this.validationResults,
    required this.toolCalls,
    required this.executionTrace,
    required this.agentError,
    required this.counsellorFeedback,
    required this.createdAt,
    required this.reviewedAt,
  });

  final String id;
  final String pathwayAnalysisId;
  final String status;
  final bool isHighRisk;
  final String? riskReason;
  final List<String> missingSkills;
  final String? feasibilitySummary;
  final String degreeRequirement;
  final List<String> subjectRequirements;
  final List<String> entryRequirements;
  final String costGuidance;
  final List<String> gapClosurePlan;
  final List<String> evidenceSources;
  final int feasibilityScore;
  final String targetCareer;
  final String workflowId;
  final List<String> validationResults;
  final List<AuditEntry> toolCalls;
  final List<AuditEntry> executionTrace;
  final String? agentError;
  final String? counsellorFeedback;
  final DateTime createdAt;
  final DateTime? reviewedAt;

  bool get isPending => status == ReviewStatus.pending;

  factory PathwayReview.fromJson(Map<String, dynamic> json) => PathwayReview(
        id: asString(json['id']),
        pathwayAnalysisId: asString(json['pathwayAnalysisId']),
        status: asString(json['status']),
        isHighRisk: asBool(json['isHighRisk']),
        riskReason: asNullableString(json['riskReason']),
        missingSkills: asStringList(json['missingSkillsJson']),
        feasibilitySummary: asNullableString(json['feasibilitySummary']),
        degreeRequirement: asString(json['degreeRequirement']),
        subjectRequirements: asStringList(json['subjectRequirementsJson']),
        entryRequirements: asStringList(json['entryRequirementsJson']),
        costGuidance: asString(json['costGuidance']),
        gapClosurePlan: asStringList(json['gapClosurePlanJson']),
        evidenceSources: asStringList(json['evidenceSourcesJson']),
        feasibilityScore: asInt(json['feasibilityScore']),
        targetCareer: asString(json['targetCareer']),
        workflowId: asString(json['workflowId']),
        validationResults: asStringList(json['validationResultsJson']),
        toolCalls: asMapList(json['toolCallsJson']).map(AuditEntry.fromJson).toList(),
        executionTrace: asMapList(json['executionTraceJson']).map(AuditEntry.fromJson).toList(),
        agentError: asNullableString(json['agentError']),
        counsellorFeedback: asNullableString(json['counsellorFeedback']),
        createdAt: asDate(json['createdAt']),
        reviewedAt: asNullableDate(json['reviewedAt']),
      );
}

/// Mirrors `PagedReviewsDto`.
class ReviewPage {
  const ReviewPage({required this.items, required this.page, required this.totalCount, required this.totalPages});

  final List<PathwayReview> items;
  final int page;
  final int totalCount;
  final int totalPages;

  static const ReviewPage empty = ReviewPage(items: [], page: 1, totalCount: 0, totalPages: 0);

  factory ReviewPage.fromJson(Map<String, dynamic> json) => ReviewPage(
        items: asMapList(json['items']).map(PathwayReview.fromJson).toList(),
        page: asInt(json['page'], 1),
        totalCount: asInt(json['totalCount']),
        totalPages: asInt(json['totalPages']),
      );
}

class ReviewQuery {
  const ReviewQuery({
    this.status = ReviewStatus.pending,
    this.search = '',
    this.sort = 'newest',
    this.page = 1,
    this.pageSize = 10,
  });

  final String status;
  final String search;
  final String sort;
  final int page;
  final int pageSize;

  ReviewQuery copyWith({String? status, String? search, String? sort, int? page}) => ReviewQuery(
        status: status ?? this.status,
        search: search ?? this.search,
        sort: sort ?? this.sort,
        page: page ?? this.page,
        pageSize: pageSize,
      );

  Map<String, Object?> toQuery() => {
        'status': status,
        'search': search.trim().isEmpty ? null : search.trim(),
        'sort': sort,
        'page': page,
        'pageSize': pageSize,
      };
}

/// Input for `POST /counsellor-review/analysis/{id}/evaluate` (`StartRealityCheckDto`).
class RealityCheckInput {
  const RealityCheckInput({
    required this.targetCareer,
    required this.alStream,
    required this.alResults,
    required this.budgetLevel,
    required this.currentSkills,
  });

  static const List<String> budgetLevels = ['Low', 'Medium', 'High'];

  final String targetCareer;
  final String alStream;
  final String alResults;
  final String budgetLevel;
  final List<String> currentSkills;

  Map<String, dynamic> toJson() => {
        'targetCareer': targetCareer,
        'alStream': alStream.trim(),
        'alResults': alResults.trim().toUpperCase(),
        'budgetLevel': budgetLevel,
        'currentSkills': currentSkills,
      };

  /// Splits "Python, communication" into trimmed, non-empty entries.
  static List<String> parseSkills(String raw) =>
      raw.split(',').map((skill) => skill.trim()).where((skill) => skill.isNotEmpty).toList();
}
