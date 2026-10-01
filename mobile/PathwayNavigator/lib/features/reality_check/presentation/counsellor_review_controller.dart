import 'package:flutter/foundation.dart';

import '../../../core/error/app_exception.dart';
import '../data/review_models.dart';
import '../data/review_repository.dart';

class CounsellorReviewController extends ChangeNotifier {
  CounsellorReviewController(this._repository, this.reviewId);

  static const int minFeedbackLength = 5;
  static const int maxFeedbackLength = 1000;

  final ReviewRepository _repository;
  final String reviewId;

  PathwayReview? _review;
  bool _loading = true;
  bool _submitting = false;
  bool _decided = false;
  String? _error;

  PathwayReview? get review => _review;
  bool get isLoading => _loading;
  bool get isSubmitting => _submitting;

  /// True once a decision was recorded - the caller pops with this so the queue reloads.
  bool get decided => _decided;
  String? get error => _error;

  Future<void> load() async {
    _loading = true;
    _error = null;
    notifyListeners();
    try {
      _review = await _repository.getReview(reviewId);
    } catch (error) {
      _error = describeError(error, fallback: 'Failed to load review details.');
    } finally {
      _loading = false;
      notifyListeners();
    }
  }

  /// Returns true on success. [decision] is one of Approved / Rejected / NeedsRevision.
  Future<bool> submit(String decision, String feedback) async {
    final text = feedback.trim();
    if (text.length < minFeedbackLength) {
      _error = 'Enter at least $minFeedbackLength characters of professional feedback.';
      notifyListeners();
      return false;
    }
    if (_submitting) return false;
    _submitting = true;
    _error = null;
    notifyListeners();
    try {
      _review = await _repository.submitDecision(id: reviewId, decision: decision, feedback: text);
      _decided = true;
      return true;
    } catch (error) {
      _error = describeError(error, fallback: 'Decision could not be recorded.');
      return false;
    } finally {
      _submitting = false;
      notifyListeners();
    }
  }
}
