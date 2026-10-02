import 'dart:async';

import 'package:flutter/foundation.dart';

import '../../../core/error/app_exception.dart';
import '../data/review_models.dart';
import '../data/review_repository.dart';

class CounsellorQueueController extends ChangeNotifier {
  CounsellorQueueController(this._repository);

  final ReviewRepository _repository;

  ReviewQuery _query = const ReviewQuery();
  ReviewPage _page = ReviewPage.empty;
  bool _loading = true;
  String? _error;
  Timer? _debounce;
  int _requestId = 0;
  bool _disposed = false;

  ReviewQuery get query => _query;
  ReviewPage get page => _page;
  bool get isLoading => _loading;
  String? get error => _error;

  Future<void> load() async {
    final requestId = ++_requestId;
    _loading = true;
    _error = null;
    notifyListeners();
    try {
      final result = await _repository.getReviews(_query);
      if (requestId != _requestId) return; // a newer request superseded this one
      _page = result;
    } catch (error) {
      if (requestId != _requestId) return;
      _error = describeError(error, fallback: 'Failed to load reviews.');
    } finally {
      if (requestId == _requestId && !_disposed) {
        _loading = false;
        notifyListeners();
      }
    }
  }

  void setStatus(String status) {
    _query = _query.copyWith(status: status, page: 1);
    load();
  }

  void setSort(String sort) {
    _query = _query.copyWith(sort: sort, page: 1);
    load();
  }

  /// Debounced so each keystroke does not hit the API.
  void setSearch(String search) {
    _query = _query.copyWith(search: search, page: 1);
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 400), load);
  }

  void goToPage(int page) {
    if (page < 1 || (_page.totalPages > 0 && page > _page.totalPages)) return;
    _query = _query.copyWith(page: page);
    load();
  }

  @override
  void notifyListeners() {
    if (!_disposed) super.notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    _debounce?.cancel();
    super.dispose();
  }
}
