import 'package:flutter/foundation.dart';

import '../../../core/error/app_exception.dart';
import '../../profile/data/profile_models.dart';
import '../../profile/data/profile_repository.dart';

class StudentDashboardController extends ChangeNotifier {
  StudentDashboardController(this._repository);

  final ProfileRepository _repository;

  StudentProfile? _profile;
  bool _loading = true;
  String? _error;

  StudentProfile? get profile => _profile;
  bool get isLoading => _loading;
  String? get error => _error;

  /// A profile exists and onboarding was finished.
  bool get isOnboarded => _profile?.isOnboardingCompleted ?? false;

  Future<void> load() async {
    _loading = true;
    _error = null;
    notifyListeners();
    try {
      _profile = await _repository.getProfile();
    } catch (error) {
      _error = describeError(error, fallback: 'Could not load your profile.');
    } finally {
      _loading = false;
      notifyListeners();
    }
  }
}
