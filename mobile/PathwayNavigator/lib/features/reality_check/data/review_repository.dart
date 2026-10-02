import '../../../core/error/app_exception.dart';
import '../../../core/network/api_client.dart';
import '../../../core/utils/json_helpers.dart';
import 'review_models.dart';

abstract interface class ReviewRepository {
  Future<PathwayReview> startRealityCheck(String analysisId, RealityCheckInput input);

  Future<PathwayReview> resubmitRealityCheck(String reviewId, RealityCheckInput input);

  /// Latest review for the signed-in student, or null when none exists (HTTP 404).
  Future<PathwayReview?> getMyStatus();

  Future<List<PathwayReview>> getMyHistory();

  // Counsellor / Admin
  Future<ReviewPage> getReviews(ReviewQuery query);

  Future<PathwayReview> getReview(String id);

  Future<PathwayReview> submitDecision({required String id, required String decision, required String feedback});
}

class RemoteReviewRepository implements ReviewRepository {
  RemoteReviewRepository(this._api);

  final ApiClient _api;

  @override
  Future<PathwayReview> startRealityCheck(String analysisId, RealityCheckInput input) async =>
      PathwayReview.fromJson(asMap(await _api.post('/counsellor-review/analysis/$analysisId/evaluate', body: input.toJson())));

  @override
  Future<PathwayReview> resubmitRealityCheck(String reviewId, RealityCheckInput input) async =>
      PathwayReview.fromJson(asMap(await _api.post('/counsellor-review/$reviewId/resubmit', body: input.toJson())));

  @override
  Future<PathwayReview?> getMyStatus() async {
    try {
      return PathwayReview.fromJson(asMap(await _api.get('/counsellor-review/me/status')));
    } on AppException catch (error) {
      if (error.isNotFound) return null; // "no review yet" is a normal state, not an error
      rethrow;
    }
  }

  @override
  Future<List<PathwayReview>> getMyHistory() async =>
      asMapList(await _api.get('/counsellor-review/me/history')).map(PathwayReview.fromJson).toList();

  @override
  Future<ReviewPage> getReviews(ReviewQuery query) async =>
      ReviewPage.fromJson(asMap(await _api.get('/counsellor-review', query: query.toQuery())));

  @override
  Future<PathwayReview> getReview(String id) async =>
      PathwayReview.fromJson(asMap(await _api.get('/counsellor-review/$id')));

  @override
  Future<PathwayReview> submitDecision({
    required String id,
    required String decision,
    required String feedback,
  }) async =>
      PathwayReview.fromJson(
        asMap(await _api.post('/counsellor-review/$id/decision', body: {'decision': decision, 'feedback': feedback.trim()})),
      );
}
