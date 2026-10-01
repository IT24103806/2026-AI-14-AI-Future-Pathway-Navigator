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

  PathwayReview? get latest => _latest;
  List<PathwayReview> get history => _history;
  bool get isLoading => _loading;
  String? get error => _error;

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
    } catch (error) {
      _error = describeError(error, fallback: 'Unable to load your Reality Check result.');
    } finally {
      _loading = false;
      notifyListeners();
    }
  }
}
