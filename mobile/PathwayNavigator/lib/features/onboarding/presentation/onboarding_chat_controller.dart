import 'package:flutter/foundation.dart';

import '../../../core/error/app_exception.dart';
import '../data/onboarding_models.dart';
import '../data/onboarding_repository.dart';

/// State for the Agent 1 conversational onboarding.
class OnboardingChatController extends ChangeNotifier {
  OnboardingChatController(this._repository);

  static const String greeting =
      "Hello! I'm Pathway Guide, your AI onboarding assistant. To start, what is your current "
      'academic stage? (After O/L, After A/L, Undergraduate or Graduated)';

  final OnboardingRepository _repository;

  final List<ChatMessage> _messages = [const ChatMessage(role: ChatRole.assistant, content: greeting)];
  ProfileSlots _slots = ProfileSlots.empty;
  bool _isTyping = false;
  bool _isComplete = false;
  String? _error;

  List<ChatMessage> get messages => List.unmodifiable(_messages);
  ProfileSlots get slots => _slots;
  bool get isTyping => _isTyping;
  bool get isComplete => _isComplete;
  String? get error => _error;

  void clearError() {
    _error = null;
    notifyListeners();
  }

  Future<void> send(String text) async {
    final message = text.trim();
    if (message.isEmpty || _isTyping || _isComplete) return;

    // History = turns BEFORE this message (the agent appends the current one itself).
    final history = List<ChatMessage>.of(_messages);
    _messages.add(ChatMessage(role: ChatRole.user, content: message));
    _isTyping = true;
    _error = null;
    notifyListeners();

    try {
      final reply = await _repository.sendChatMessage(message: message, history: history, currentSlots: _slots);
      _messages.add(ChatMessage(role: ChatRole.assistant, content: reply.replyMessage));
      _slots = reply.slots;
      _isComplete = reply.isComplete;
    } catch (error) {
      _error = describeError(error, fallback: 'Could not reach Pathway Guide. Please try again.');
    } finally {
      _isTyping = false;
      notifyListeners();
    }
  }
}
