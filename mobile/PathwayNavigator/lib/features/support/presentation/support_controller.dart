import 'package:flutter/foundation.dart';

import '../../../core/error/app_exception.dart';
import '../data/consultation_models.dart';
import '../data/consultation_repository.dart';

/// Student side of the consultation channel: my questions, the open thread, and asking a new one.
class SupportController extends ChangeNotifier {
  SupportController(this._repository);

  final ConsultationRepository _repository;

  String _status = '';
  int _page = 1;
  ConsultationPage _pageResult = ConsultationPage.empty;
  Consultation? _selected;
  bool _loading = true;
  bool _busy = false;
  String? _error;
  String? _notice;
  int _requestId = 0;
  bool _disposed = false;

  String get status => _status;
  int get page => _page;
  ConsultationPage get pageResult => _pageResult;
  Consultation? get selected => _selected;
  bool get isLoading => _loading;
  bool get isBusy => _busy;
  String? get error => _error;
  String? get notice => _notice;

  Future<void> load() async {
    final requestId = ++_requestId;
    _loading = true;
    _error = null;
    notifyListeners();
    try {
      final result = await _repository.getMine(status: _status, page: _page);
      if (requestId != _requestId) return;
      _pageResult = result;
    } catch (error) {
      if (requestId != _requestId) return;
      _error = describeError(error, fallback: 'Failed to load your questions.');
    } finally {
      if (requestId == _requestId && !_disposed) {
        _loading = false;
        notifyListeners();
      }
    }
  }

  void setStatus(String status) {
    _status = status;
    _page = 1;
    load();
  }

  void goToPage(int page) {
    if (page < 1 || (_pageResult.totalPages > 0 && page > _pageResult.totalPages)) return;
    _page = page;
    load();
  }

  /// Opens a thread - used by the list, and by a notification deep link.
  Future<void> open(String id) async {
    _busy = true;
    _error = null;
    notifyListeners();
    try {
      _selected = await _repository.getById(id);
    } catch (error) {
      _error = describeError(error, fallback: 'That conversation could not be opened.');
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  void closeThread() {
    _selected = null;
    notifyListeners();
  }

  Future<Consultation?> ask({
    required String subject,
    required String body,
    String contextType = ConsultationContext.general,
    String? contextRefId,
    String? category,
    String? priority,
  }) async {
    _busy = true;
    _error = null;
    notifyListeners();
    try {
      final created = await _repository.create(
        subject: subject,
        body: body,
        contextType: contextType,
        contextRefId: contextRefId,
        category: category,
        priority: priority,
      );
      _notice = 'Sent. A consultant will reply - you will get a notification.';
      _selected = created;
      await load();
      return created;
    } catch (error) {
      _error = describeError(error, fallback: 'Your question could not be sent.');
      return null;
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  /// Returns true when the student's reply went through (the thread keeps their text otherwise).
  Future<bool> sendMessage(String body) async {
    final current = _selected;
    if (current == null || body.trim().length < 2) return false;
    _busy = true;
    notifyListeners();
    try {
      _selected = await _repository.addMessage(current.id, body);
      await load();
      return true;
    } catch (error) {
      _error = describeError(error, fallback: 'Your message could not be sent.');
      return false;
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  Future<void> closeRequest({int? rating, String? feedback}) async {
    final current = _selected;
    if (current == null) return;
    _busy = true;
    notifyListeners();
    try {
      _selected = await _repository.close(current.id, rating: rating, feedback: feedback);
      _notice = 'Marked as resolved. Thank you - this helps us measure the service.';
      await load();
    } catch (error) {
      _error = describeError(error, fallback: 'That could not be closed.');
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  Future<void> reopen() async {
    final current = _selected;
    if (current == null) return;
    _busy = true;
    notifyListeners();
    try {
      _selected = await _repository.reopen(current.id);
      _notice = 'Reopened - your consultant can pick it up again.';
      await load();
    } catch (error) {
      _error = describeError(error, fallback: 'That could not be reopened.');
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  void clearMessages() {
    _error = null;
    _notice = null;
    notifyListeners();
  }

  @override
  void notifyListeners() {
    if (!_disposed) super.notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    super.dispose();
  }
}
