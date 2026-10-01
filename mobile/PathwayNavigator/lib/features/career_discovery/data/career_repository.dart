import '../../../core/network/api_client.dart';
import '../../../core/utils/json_helpers.dart';
import 'career_models.dart';

abstract interface class CareerRepository {
  /// Runs Agent 2 against the student's saved profile and persists the result.
  Future<PathwayAnalysis> analyze();

  Future<PathwayAnalysis> approve(String analysisId);

  Future<PathwayAnalysis> reject(String analysisId);

  /// Builds the Agent 3 roadmap for [pathwayName].
  Future<PathwayPlan> buildPlan(String pathwayName);
}

class RemoteCareerRepository implements CareerRepository {
  RemoteCareerRepository(this._api);

  final ApiClient _api;

  @override
  Future<PathwayAnalysis> analyze() async =>
      PathwayAnalysis.fromJson(asMap(await _api.post('/career-discovery/analyze')));

  @override
  Future<PathwayAnalysis> approve(String analysisId) async =>
      PathwayAnalysis.fromJson(asMap(await _api.patch('/career-discovery/$analysisId/approve')));

  @override
  Future<PathwayAnalysis> reject(String analysisId) async =>
      PathwayAnalysis.fromJson(asMap(await _api.patch('/career-discovery/$analysisId/reject')));

  @override
  Future<PathwayPlan> buildPlan(String pathwayName) async => PathwayPlan.fromJson(
        asMap(
          await _api.post(
            '/pathway-planner/plan',
            body: {'selected_pathway': pathwayName, 'completed_phases': <String>[]},
          ),
        ),
      );
}
