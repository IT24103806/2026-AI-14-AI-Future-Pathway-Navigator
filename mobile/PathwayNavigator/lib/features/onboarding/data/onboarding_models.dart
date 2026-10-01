import '../../../core/utils/json_helpers.dart';

/// Fixed vocabulary shared with the web form and Agent 1.
const List<String> academicStages = [
  'After O/L',
  'After A/L',
  'Undergraduate',
  'Graduated',
  'Other',
];

/// Mirrors `ExtractedSlotsDto` (snake_case JSON, as produced by Agent 1).
class ProfileSlots {
  const ProfileSlots({
    this.academicStage,
    this.coreSkills = const [],
    this.hobbiesInterests = const [],
    this.careerAmbitions,
  });

  final String? academicStage;
  final List<String> coreSkills;
  final List<String> hobbiesInterests;
  final String? careerAmbitions;

  static const ProfileSlots empty = ProfileSlots();

  factory ProfileSlots.fromJson(Map<String, dynamic> json) => ProfileSlots(
        academicStage: asNullableString(json['academic_stage']),
        coreSkills: asStringList(json['core_skills']),
        hobbiesInterests: asStringList(json['hobbies_interests']),
        careerAmbitions: asNullableString(json['career_ambitions']),
      );

  Map<String, dynamic> toJson() => {
        'academic_stage': academicStage,
        'core_skills': coreSkills,
        'hobbies_interests': hobbiesInterests,
        'career_ambitions': careerAmbitions,
      };

  bool get hasAcademicStage => (academicStage ?? '').trim().isNotEmpty;
  bool get hasCareerAmbitions => (careerAmbitions ?? '').trim().isNotEmpty;

  /// How many of the four required slots are filled (drives the progress indicator).
  int get filledCount =>
      [hasAcademicStage, coreSkills.isNotEmpty, hobbiesInterests.isNotEmpty, hasCareerAmbitions]
          .where((filled) => filled)
          .length;

  static const int totalSlots = 4;
}

enum ChatRole { user, assistant }

class ChatMessage {
  const ChatMessage({required this.role, required this.content});

  final ChatRole role;
  final String content;

  Map<String, dynamic> toJson() => {'role': role.name, 'content': content};
}

/// Mirrors `AgentChatResponseDto`.
class AgentChatReply {
  const AgentChatReply({
    required this.replyMessage,
    required this.slots,
    required this.missingSlots,
    required this.isComplete,
    required this.isSaved,
  });

  final String replyMessage;
  final ProfileSlots slots;
  final List<String> missingSlots;
  final bool isComplete;
  final bool isSaved;

  factory AgentChatReply.fromJson(Map<String, dynamic> json) => AgentChatReply(
        replyMessage: asString(json['reply_message']),
        slots: ProfileSlots.fromJson(asMap(json['extracted_slots'])),
        missingSlots: asStringList(json['missing_slots']),
        isComplete: asBool(json['is_complete']),
        isSaved: asBool(json['is_saved']),
      );
}

/// Payload of the standard (non-conversational) onboarding form (`CompleteOnboardingDto`).
class OnboardingForm {
  const OnboardingForm({
    required this.academicStage,
    required this.coreSkills,
    required this.hobbiesInterests,
    required this.careerAmbitions,
  });

  final String academicStage;
  final List<String> coreSkills;
  final List<String> hobbiesInterests;
  final String careerAmbitions;

  Map<String, dynamic> toJson() => {
        'academicStage': academicStage,
        'coreSkills': coreSkills,
        'hobbiesInterests': hobbiesInterests,
        'careerAmbitions': careerAmbitions.trim(),
        'onboardingMethod': 'StandardForm',
      };
}
