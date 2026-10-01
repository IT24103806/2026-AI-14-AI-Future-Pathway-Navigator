/// Role names issued by the backend (`AuthResponseDto.role` / JWT role claim).
abstract final class Roles {
  static const String student = 'Student';
  static const String counsellor = 'Counsellor';
  static const String admin = 'Admin';

  static bool isKnown(String? role) => role == student || role == counsellor || role == admin;

  static bool isStaff(String? role) => role == counsellor || role == admin;
}

/// Landing route for each role (mirrors `dashboardPathForRole` in the web app).
String homeRouteForRole(String? role) {
  switch (role) {
    case Roles.counsellor:
      return '/counsellor';
    case Roles.admin:
      return '/admin';
    default:
      return '/student';
  }
}
