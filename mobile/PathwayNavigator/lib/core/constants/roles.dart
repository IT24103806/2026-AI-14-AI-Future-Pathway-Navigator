/// Role names issued by the backend (`AuthResponseDto.role` / JWT role claim).
abstract final class Roles {
  static const String student = 'Student';
  static const String consultant = 'Consultant';
  static const String counsellor = 'Counsellor';
  static const String admin = 'Admin';

  static bool isKnown(String? role) =>
      role == student || role == consultant || role == counsellor || role == admin;

  /// Counsellor + Admin: the roles that can approve or reject a pathway (ADR-004).
  ///
  /// A Consultant deliberately is NOT staff here - guidance and approval authority are separate desks.
  static bool isStaff(String? role) => role == counsellor || role == admin;

  /// Who may open the Consultant Desk. Mirrors `canAccessConsultantDesk` in the web app.
  static bool canAccessConsultantDesk(String? role) => role == consultant || role == admin;

  /// Who may answer student questions (used by the desk screens' entry points).
  static bool isConsultant(String? role) => role == consultant;
}

/// Landing route for each role (mirrors `dashboardPathForRole` in the web app).
String homeRouteForRole(String? role) {
  switch (role) {
    case Roles.consultant:
      return '/consultant';
    case Roles.counsellor:
      return '/counsellor';
    case Roles.admin:
      return '/admin';
    default:
      return '/student';
  }
}
