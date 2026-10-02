import '../../../core/error/app_exception.dart';
import '../../../core/network/api_client.dart';
import '../../../core/utils/json_helpers.dart';
import 'career_models.dart';

abstract interface class CareerRepository {
  /// Runs Agent 2 against the student's saved profile and persists the result.
  Future<PathwayAnalysis> analyze();

  /// Latest saved Agent 2 analysis for the signed-in student, or null when none exists (HTTP 404).
  Future<PathwayAnalysis?> getLatestAnalysis();

  Future<PathwayAnalysis> approve(String analysisId);

  Future<PathwayAnalysis> reject(String analysisId);

  /// Builds and persists the Agent 3 roadmap for [pathwayName].
  Future<PathwayPlan> buildPlan(String pathwayName, {List<String> completedPhases = const <String>[]});

  /// Latest saved Agent 3 roadmap for the signed-in student, or null when none exists (HTTP 404).
  Future<PathwayPlan?> getLatestPlan();

  /// Updates the completed milestone phases for a persisted Agent 3 roadmap.
  Future<PathwayPlan> updatePlanProgress(String planId, List<String> completedPhases);
}

class RemoteCareerRepository implements CareerRepository {
  RemoteCareerRepository(this._api);

  final ApiClient _api;

  @override
  Future<PathwayAnalysis> analyze() async =>
      PathwayAnalysis.fromJson(asMap(await _api.post('/career-discovery/analyze')));

  @override
  Future<PathwayAnalysis?> getLatestAnalysis() async {
    try {
      return PathwayAnalysis.fromJson(asMap(await _api.get('/career-discovery/me/latest')));
    } on AppException catch (error) {
      if (error.isNotFound) return null;
      rethrow;
    }
  }

  @override
  Future<PathwayAnalysis> approve(String analysisId) async =>
      PathwayAnalysis.fromJson(asMap(await _api.patch('/career-discovery/$analysisId/approve')));

  @override
  Future<PathwayAnalysis> reject(String analysisId) async =>
      PathwayAnalysis.fromJson(asMap(await _api.patch('/career-discovery/$analysisId/reject')));

  @override
  Future<PathwayPlan> buildPlan(String pathwayName, {List<String> completedPhases = const <String>[]}) async =>
      PathwayPlan.fromJson(
        asMap(
          await _api.post(
            '/pathway-planner/plan',
            body: {'selected_pathway': pathwayName, 'completed_phases': completedPhases},
          ),
        ),
      );

  @override
  Future<PathwayPlan?> getLatestPlan() async {
    try {
      return PathwayPlan.fromJson(asMap(await _api.get('/pathway-planner/me/latest')));
    } on AppException catch (error) {
      if (error.isNotFound) return null;
      rethrow;
    }
  }

  @override
  Future<PathwayPlan> updatePlanProgress(String planId, List<String> completedPhases) async => PathwayPlan.fromJson(
        asMap(
          await _api.patch(
            '/pathway-planner/$planId/progress',
            body: {'completed_phases': completedPhases},
          ),
        ),
      );
}
