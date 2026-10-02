import '../../../core/utils/json_helpers.dart';

/// Mirrors `ProfileStatusDto`.
class ProfileStatus {
  const ProfileStatus({required this.hasProfile, required this.isOnboardingCompleted, this.academicStage});

  final bool hasProfile;
  final bool isOnboardingCompleted;
  final String? academicStage;

  factory ProfileStatus.fromJson(Map<String, dynamic> json) => ProfileStatus(
        hasProfile: asBool(json['hasProfile']),
        isOnboardingCompleted: asBool(json['isOnboardingCompleted']),
        academicStage: asNullableString(json['academicStage']),
      );
}

/// Mirrors `StudentProfileDto`.
class StudentProfile {
  const StudentProfile({
    required this.academicStage,
    required this.coreSkills,
    required this.hobbiesInterests,
    required this.careerAmbitions,
    this.alStream = '',
    this.alResults = '',
    this.budgetLevel = 'Medium',
    required this.onboardingMethod,
    required this.isOnboardingCompleted,
  });

  final String academicStage;
  final List<String> coreSkills;
  final List<String> hobbiesInterests;
  final String careerAmbitions;
  final String alStream;
  final String alResults;
  final String budgetLevel;
  final String onboardingMethod;
  final bool isOnboardingCompleted;

  factory StudentProfile.fromJson(Map<String, dynamic> json) => StudentProfile(
        academicStage: asString(json['academicStage']),
        coreSkills: asStringList(json['coreSkills']),
        hobbiesInterests: asStringList(json['hobbiesInterests']),
        careerAmbitions: asString(json['careerAmbitions']),
        alStream: asString(json['alStream']),
        alResults: asString(json['alResults']),
        budgetLevel: asString(json['budgetLevel']).isNotEmpty ? asString(json['budgetLevel']) : 'Medium',
        onboardingMethod: asString(json['onboardingMethod']),
        isOnboardingCompleted: asBool(json['isOnboardingCompleted']),
      );
}
