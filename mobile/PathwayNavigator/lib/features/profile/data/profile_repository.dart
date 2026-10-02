import '../../../core/error/app_exception.dart';
import '../../../core/network/api_client.dart';
import '../../../core/utils/json_helpers.dart';
import 'profile_models.dart';

abstract interface class ProfileRepository {
  Future<ProfileStatus> getStatus();

  /// Returns null when the student has not created a profile yet (HTTP 404).
  Future<StudentProfile?> getProfile();
}

class RemoteProfileRepository implements ProfileRepository {
  RemoteProfileRepository(this._api);

  final ApiClient _api;

  @override
  Future<ProfileStatus> getStatus() async => ProfileStatus.fromJson(asMap(await _api.get('/profile/status')));

  @override
  Future<StudentProfile?> getProfile() async {
    try {
      return StudentProfile.fromJson(asMap(await _api.get('/profile')));
    } on AppException catch (error) {
      // Only "not found" means "no profile yet"; everything else must surface.
      if (error.isNotFound) return null;
      rethrow;
    }
  }
}
