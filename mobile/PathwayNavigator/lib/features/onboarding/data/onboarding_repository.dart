import '../../../core/network/api_client.dart';
import '../../../core/utils/json_helpers.dart';
import 'onboarding_models.dart';

abstract interface class OnboardingRepository {
  /// One turn with Agent 1. [history] must contain the **previous** turns only - the current
  /// [message] is sent separately and the agent appends it itself.
  Future<AgentChatReply> sendChatMessage({
    required String message,
    required List<ChatMessage> history,
    required ProfileSlots currentSlots,
  });

  Future<void> submitStandardForm(OnboardingForm form);
}

class RemoteOnboardingRepository implements OnboardingRepository {
  RemoteOnboardingRepository(this._api);

  final ApiClient _api;

  @override
  Future<AgentChatReply> sendChatMessage({
    required String message,
    required List<ChatMessage> history,
    required ProfileSlots currentSlots,
  }) async {
    final data = await _api.post(
      '/onboarding/chat',
      body: {
        'message': message,
        'history': history.map((m) => m.toJson()).toList(),
        'current_slots': currentSlots.toJson(),
      },
    );
    return AgentChatReply.fromJson(asMap(data));
  }

  @override
  Future<void> submitStandardForm(OnboardingForm form) async {
    await _api.post('/onboarding/standard-form', body: form.toJson());
  }
}
