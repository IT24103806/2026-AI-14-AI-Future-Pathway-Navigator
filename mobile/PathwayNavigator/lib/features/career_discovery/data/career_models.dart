import '../../../core/utils/json_helpers.dart';

class RoadmapPhase {
  const RoadmapPhase({required this.phase, required this.course});

  final String phase;
  final String course;

  factory RoadmapPhase.fromJson(Map<String, dynamic> json) =>
      RoadmapPhase(phase: asString(json['phase']), course: asString(json['course']));
}

/// One of the (up to three) ranked pathways produced by Agent 2.
class PathwayRecommendation {
  const PathwayRecommendation({
    required this.label,
    required this.pathwayName,
    required this.matchScore,
    required this.demandScore,
    required this.competitionScore,
    required this.trend,
    required this.dataSource,
    required this.reasoning,
    required this.missingSkills,
    required this.recommendedCourses,
    required this.roadmap,
  });

  final String label;
  final String pathwayName;
  final int matchScore;
  final int demandScore;
  final int competitionScore;
  final String trend;
  final String dataSource;
  final String reasoning;
  final List<String> missingSkills;
  final List<String> recommendedCourses;
  final List<RoadmapPhase> roadmap;

  bool get isLiveData => dataSource == 'adzuna_live';

  factory PathwayRecommendation.fromJson(Map<String, dynamic> json) => PathwayRecommendation(
        label: asString(json['label']),
        pathwayName: asString(json['pathway_name']),
        matchScore: asInt(json['match_score']),
        demandScore: asInt(json['demand_score']),
        competitionScore: asInt(json['competition_score']),
        trend: asString(json['trend']),
        dataSource: asString(json['data_source']),
        reasoning: asString(json['reasoning']),
        missingSkills: asStringList(json['missing_skills']),
        recommendedCourses: asStringList(json['recommended_courses']),
        roadmap: asMapList(json['roadmap']).map(RoadmapPhase.fromJson).toList(),
      );
}

/// Status values the backend uses for a pathway analysis.
abstract final class AnalysisStatus {
  static const String pendingApproval = 'pending_approval';
  static const String approved = 'approved';
  static const String rejected = 'rejected';
  static const String failed = 'failed';
}

class PathwayAnalysis {
  const PathwayAnalysis({
    required this.id,
    required this.workflowId,
    required this.status,
    required this.recommendations,
    required this.validationErrors,
  });

  final String? id;
  final String workflowId;
  final String status;
  final List<PathwayRecommendation> recommendations;
  final List<String> validationErrors;

  bool get failed => status == AnalysisStatus.failed;
  bool get isDecided => status == AnalysisStatus.approved || status == AnalysisStatus.rejected;

  factory PathwayAnalysis.fromJson(Map<String, dynamic> json) => PathwayAnalysis(
        id: asNullableString(json['id']),
        workflowId: asString(json['workflow_id']),
        status: asString(json['status']),
        recommendations: asMapList(json['recommendations']).map(PathwayRecommendation.fromJson).toList(),
        validationErrors: asStringList(json['validation_errors']),
      );
}

class RoadmapStage {
  const RoadmapStage({
    required this.order,
    required this.stage,
    required this.title,
    required this.outcome,
    required this.actions,
    required this.estimatedDuration,
  });

  final int order;
  final String stage;
  final String title;
  final String outcome;
  final List<String> actions;
  final String estimatedDuration;

  factory RoadmapStage.fromJson(Map<String, dynamic> json) => RoadmapStage(
        order: asInt(json['order']),
        stage: asString(json['stage']),
        title: asString(json['title']),
        outcome: asString(json['outcome']),
        actions: asStringList(json['actions']),
        estimatedDuration: asString(json['estimated_duration']),
      );
}

/// Agent 3 output. `status == 'ready'` means the roadmap passed validation.
class PathwayPlan {
  const PathwayPlan({
    required this.workflowId,
    required this.status,
    required this.selectedPathway,
    required this.roadmap,
    required this.missingSkills,
    required this.nextAction,
    required this.validationErrors,
  });

  final String workflowId;
  final String status;
  final String selectedPathway;
  final List<RoadmapStage> roadmap;
  final List<String> missingSkills;
  final String nextAction;
  final List<String> validationErrors;

  bool get isReady => status == 'ready';

  factory PathwayPlan.fromJson(Map<String, dynamic> json) => PathwayPlan(
        workflowId: asString(json['workflow_id']),
        status: asString(json['status']),
        selectedPathway: asString(json['selected_pathway']),
        roadmap: asMapList(json['roadmap']).map(RoadmapStage.fromJson).toList(),
        missingSkills: asStringList(json['missing_skills']),
        nextAction: asString(json['next_action']),
        validationErrors: asStringList(json['validation_errors']),
      );
}
