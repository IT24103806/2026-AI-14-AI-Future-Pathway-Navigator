import 'package:flutter/foundation.dart';

import '../../profile/data/profile_models.dart';
import '../../profile/data/profile_repository.dart';
import '../data/review_models.dart';
import '../data/review_repository.dart';

/// Loads the student's saved profile + latest Reality Check so the form can be pre-filled.
/// Pre-fill is a convenience: any failure silently falls back to an empty form.
class RealityCheckPrefillController extends ChangeNotifier {
  RealityCheckPrefillController(this._profiles, this._reviews);

  final ProfileRepository _profiles;
  final ReviewRepository _reviews;

  RealityCheckPrefill _prefill = RealityCheckPrefill.empty;
  bool _loading = true;
  bool _disposed = false;

  RealityCheckPrefill get prefill => _prefill;
  bool get isLoading => _loading;

  Future<T?> _optional<T>(Future<T?> future) async {
    try {
      return await future;
    } catch (_) {
      return null;
    }
  }

  Future<void> load() async {
    _loading = true;
    notifyListeners();
    final results = await Future.wait<Object?>([
      _optional<StudentProfile>(_profiles.getProfile()),
      _optional<PathwayReview>(_reviews.getMyStatus()),
    ]);
    final profile = results[0] as StudentProfile?;
    final review = results[1] as PathwayReview?;
    final fromProfile = profile == null
        ? RealityCheckPrefill.empty
        : RealityCheckPrefill(
            alStream: profile.alStream,
            alResults: profile.alResults,
            budgetLevel: profile.budgetLevel,
            currentSkills: profile.coreSkills,
          );
    final fromReview = review == null ? RealityCheckPrefill.empty : RealityCheckPrefill.fromReview(review);
    if (_disposed) return;
    _prefill = RealityCheckPrefill.merge(fromReview, fromProfile);
    _loading = false;
    notifyListeners();
  }

  @override
  void dispose() {
    _disposed = true;
    super.dispose();
  }
}
