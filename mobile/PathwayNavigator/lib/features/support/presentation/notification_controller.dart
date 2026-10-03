import 'dart:async';

import 'package:flutter/foundation.dart';

import '../../../core/error/app_exception.dart';
import '../data/consultation_models.dart';
import '../data/consultation_repository.dart';

/// The notification inbox plus the unread badge.
///
/// Polling is deliberately gentle: one indexed count every 45 seconds while the app is in the
/// foreground, paused when it is not. Notifications are the *signal*; the DB inbox is the source of
/// truth, so a missed poll only delays the badge, it never loses an answer.
class NotificationController extends ChangeNotifier {
  NotificationController(this._repository, {this.pollInterval = const Duration(seconds: 45)});

  final ConsultationRepository _repository;
  final Duration pollInterval;

  Timer? _timer;
  bool _polling = false;
  NotificationPage _page = NotificationPage.empty;
  int _unreadCount = 0;
  bool _loading = false;
  String? _error;
  int _requestId = 0;
  bool _disposed = false;

  NotificationPage get page => _page;
  int get unreadCount => _unreadCount;
  bool get isLoading => _loading;
  String? get error => _error;

  /// Called once the user is signed in (and when the app returns to the foreground).
  ///
  /// Idempotent: the app-wide proxy provider calls this on every auth change, and re-arming the timer
  /// each time would keep pushing the next poll back.
  void start() {
    if (_polling) return;
    _polling = true;
    refreshUnreadCount();
    _timer = Timer.periodic(pollInterval, (_) => refreshUnreadCount());
  }

  void stop() {
    _polling = false;
    _timer?.cancel();
    _timer = null;
  }

  Future<void> refreshUnreadCount() async {
    try {
      final count = await _repository.getUnreadCount();
      if (count == _unreadCount) return; // no change, no rebuild: keeps idle polling cheap
      _unreadCount = count;
      notifyListeners();
    } catch (_) {
      // A failed poll is not user-facing: the next one will pick it up.
    }
  }

  Future<void> load({bool unreadOnly = false}) async {
    final requestId = ++_requestId;
    _loading = true;
    _error = null;
    notifyListeners();
    try {
      final result = await _repository.getNotifications(unreadOnly: unreadOnly);
      if (requestId != _requestId) return;
      _page = result;
      _unreadCount = result.unreadCount;
    } catch (error) {
      if (requestId != _requestId) return;
      _error = describeError(error, fallback: 'Failed to load notifications.');
    } finally {
      if (requestId == _requestId && !_disposed) {
        _loading = false;
        notifyListeners();
      }
    }
  }

  Future<void> markRead(String id) async {
    try {
      await _repository.markRead(id);
      _page = NotificationPage(
        items: _page.items.map((item) => item.id == id ? _copyRead(item) : item).toList(),
        unreadCount: _unreadCount > 0 ? _unreadCount - 1 : 0,
        page: _page.page,
        totalCount: _page.totalCount,
      );
      _unreadCount = _page.unreadCount;
      notifyListeners();
    } catch (error) {
      _error = describeError(error, fallback: 'That notification could not be updated.');
      notifyListeners();
    }
  }

  Future<void> markAllRead() async {
    try {
      await _repository.markAllRead();
      _page = NotificationPage(
        items: _page.items.map(_copyRead).toList(),
        unreadCount: 0,
        page: _page.page,
        totalCount: _page.totalCount,
      );
      _unreadCount = 0;
      notifyListeners();
    } catch (error) {
      _error = describeError(error, fallback: 'Notifications could not be updated.');
      notifyListeners();
    }
  }

  /// Rewrites an optimistic "read" tick without needing a round trip for the whole page.
  AppNotification _copyRead(AppNotification item) => AppNotification(
        id: item.id,
        type: item.type,
        title: item.title,
        body: item.body,
        createdAt: item.createdAt,
        deepLink: item.deepLink,
        priority: item.priority,
        isRead: true,
      );

  @override
  void notifyListeners() {
    if (!_disposed) super.notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    stop();
    super.dispose();
  }
}
