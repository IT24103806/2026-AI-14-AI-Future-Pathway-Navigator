import 'dart:async';

import 'package:flutter/foundation.dart';

import '../../../core/error/app_exception.dart';
import '../data/consultation_models.dart';
import '../data/consultation_repository.dart';

/// The Consultant Desk.
///
/// Holds the queue (ordered by SLA), the open case, and the Agent 5 brief/draft. The AI is assistive
/// throughout: the brief is guidance for the human, the draft is inserted into the composer and the
/// consultant still presses send. Nothing here can answer a student on its own.
class ConsultantDeskController extends ChangeNotifier {
  ConsultantDeskController(this._repository);

  final ConsultationRepository _repository;

  ConsultantQueueQuery _query = const ConsultantQueueQuery();
  ConsultationPage _queue = ConsultationPage.empty;
  Consultation? _selected;
  ConsultantBrief? _brief;
  ConsultantStats? _stats;
  bool _loading = true;
  bool _busy = false;
  String? _error;
  String? _notice;
  Timer? _debounce;
  int _requestId = 0;
  bool _disposed = false;

  ConsultantQueueQuery get query => _query;
  ConsultationPage get queue => _queue;
  Consultation? get selected => _selected;
  ConsultantBrief? get brief => _brief;
  ConsultantStats? get stats => _stats;
  bool get isLoading => _loading;
  bool get isBusy => _busy;
  String? get error => _error;
  String? get notice => _notice;

  Future<void> loadQueue() async {
    final requestId = ++_requestId;
    _loading = true;
    _error = null;
    notifyListeners();
    try {
      final result = await _repository.getQueue(_query);
      if (requestId != _requestId) return;
      _queue = result;
      _stats = await _repository.getStats();
    } catch (error) {
      if (requestId != _requestId) return;
      _error = describeError(error, fallback: 'Failed to load the consultant queue.');
    } finally {
      if (requestId == _requestId && !_disposed) {
        _loading = false;
        notifyListeners();
      }
    }
  }

  void setScope(String scope) {
    _query = _query.copyWith(scope: scope, page: 1);
    loadQueue();
  }

  void setPriorityFilter(String priority) {
    _query = _query.copyWith(priority: priority, page: 1);
    loadQueue();
  }

  void setContextFilter(String contextType) {
    _query = _query.copyWith(contextType: contextType, page: 1);
    loadQueue();
  }

  /// Debounced so each keystroke does not hit the API.
  void setSearch(String search) {
    _query = _query.copyWith(search: search, page: 1);
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 400), loadQueue);
  }

  void goToPage(int page) {
    if (page < 1 || (_queue.totalPages > 0 && page > _queue.totalPages)) return;
    _query = _query.copyWith(page: page);
    loadQueue();
  }

  Future<void> openCase(String id) async {
    _busy = true;
    _brief = null;
    _error = null;
    notifyListeners();
    try {
      _selected = await _repository.getCase(id);
      _brief = await _repository.getBrief(id);
    } catch (error) {
      _error = describeError(error, fallback: 'Failed to load the case.');
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  void closeCase() {
    _selected = null;
    _brief = null;
    notifyListeners();
  }

  Future<bool> claim() => _runCaseAction((id) => _repository.claim(id), 'Case claimed.');

  Future<bool> release({String reason = ''}) =>
      _runCaseAction((id) => _repository.release(id, reason), 'Case returned to the pool.');

  Future<bool> updatePriority(String priority) =>
      _runCaseAction((id) => _repository.updatePriority(id, priority), 'Priority updated.');

  Future<bool> escalate({String reason = 'Needs a counsellor decision.'}) =>
      _runCaseAction((id) => _repository.escalate(id, reason), 'Escalated to the counsellor queue.');

  Future<bool> addNote(String body) => _runCaseAction((id) => _repository.addNote(id, body), 'Internal note saved.');

  /// Sends the reply. `usedAgentDraft` must be passed by the screen, which is the only place that
  /// knows whether the text started from an Agent 5 draft.
  Future<bool> sendReply({
    required String message,
    List<ConsultationResource> resources = const <ConsultationResource>[],
    ConsultationGuidance? guidance,
    String? resolutionSummary,
    bool closeAfterReply = false,
    bool usedAgentDraft = false,
  }) async {
    final id = _selected?.id;
    if (id == null) return false;
    if (message.trim().length < 2) {
      _error = 'Write a reply before sending.';
      notifyListeners();
      return false;
    }
    _busy = true;
    notifyListeners();
    try {
      _selected = await _repository.reply(
        id,
        message: message,
        resources: resources,
        guidance: guidance,
        resolutionSummary: resolutionSummary,
        closeAfterReply: closeAfterReply,
        usedAgentDraft: usedAgentDraft,
      );
      _notice = closeAfterReply ? 'Reply sent and the case is closed.' : 'Reply sent - the student has been notified.';
      await loadQueue();
      return true;
    } catch (error) {
      _error = describeError(error, fallback: 'That action could not be completed.');
      return false;
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  /// Fetches a draft. Returns null (and sets [error]) when the copilot has nothing to offer.
  Future<ConsultantDraft?> loadDraft({String tone = 'Supportive'}) async {
    final id = _selected?.id;
    if (id == null) return null;
    _busy = true;
    notifyListeners();
    try {
      final draft = await _repository.getDraft(id, tone: tone);
      if (draft == null || draft.body.isEmpty) {
        _error = 'No draft is available right now - please write your reply.';
        return null;
      }
      _notice = draft.mustEscalate
          ? 'Review before sending: ${draft.safetyNotes.isEmpty ? 'this case must be escalated.' : draft.safetyNotes.join(' ')}'
          : 'Draft inserted. Edit it in your own words before sending.';
      return draft;
    } catch (error) {
      _error = describeError(error, fallback: 'That action could not be completed.');
      return null;
    } finally {
      _busy = false;
      notifyListeners();
    }
  }

  Future<bool> _runCaseAction(
    Future<Consultation> Function(String id) action,
    String successText,
  ) async {
    final id = _selected?.id;
    if (id == null) return false;
    _busy = true;
    _error = null;
    notifyListeners();
    try {
      _selected = await action(id);
      _notice = successText;
      await loadQueue();
      return true;
    } catch (error) {
      _error = describeError(error, fallback: 'That action could not be completed.');
      return false;
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
    _debounce?.cancel();
    super.dispose();
  }
}
