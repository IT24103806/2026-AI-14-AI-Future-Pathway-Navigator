import 'package:flutter/foundation.dart';

import '../../../core/error/app_exception.dart';
import '../data/review_models.dart';
import '../data/review_repository.dart';

class StudentRealityController extends ChangeNotifier {
  StudentRealityController(this._repository);

  final ReviewRepository _repository;

  PathwayReview? _latest;
  List<PathwayReview> _history = const [];
  bool _loading = true;
  String? _error;
  bool _revisionOpen = false;
  String? _resubmitNotice;

  PathwayReview? get latest => _latest;
  List<PathwayReview> get history => _history;
  bool get isLoading => _loading;
  String? get error => _error;

  /// True while the revise-and-resubmit form is visible.
  bool get revisionOpen => _revisionOpen;
  String? get resubmitNotice => _resubmitNotice;

  /// A counsellor decision other than "Pending" can be followed by a revised run.
  bool get canRevise => _latest != null && !_latest!.isPending;

  void openRevision() {
    if (!canRevise) return;
    _revisionOpen = true;
    notifyListeners();
  }

  void closeRevision() {
    _revisionOpen = false;
    notifyListeners();
  }

  Future<void> load() async {
    _loading = true;
    _error = null;
    notifyListeners();
    try {
      final results = await Future.wait<Object?>([_repository.getMyStatus(), _repository.getMyHistory()]);
      final status = results[0] as PathwayReview?;
      final history = results[1]! as List<PathwayReview>;
      _history = history;
      _latest = status ?? (history.isEmpty ? null : history.first);
      // "Needs revision" / "Rejected" drops the student straight into the pre-filled revision form.
      _revisionOpen = _latest?.canResubmit ?? false;
    } catch (error) {
      _error = describeError(error, fallback: 'Unable to load your Reality Check result.');
    } finally {
      _loading = false;
      notifyListeners();
    }
  }

  /// Re-runs Agent 4 for the current review. Throws on failure so the form can show the message.
  Future<PathwayReview> resubmit(RealityCheckInput input) async {
    final current = _latest;
    if (current == null) throw StateError('There is no Reality Check to resubmit.');
    final updated = await _repository.resubmitRealityCheck(current.id, input);
    _latest = updated;
    _history = [updated, ..._history.where((item) => item.id != updated.id)];
    _revisionOpen = updated.canResubmit;
    _resubmitNotice = 'Updated Reality Check submitted. New status: ${updated.status} (score ${updated.feasibilityScore}/100).';
    notifyListeners();
    return updated;
  }
}
