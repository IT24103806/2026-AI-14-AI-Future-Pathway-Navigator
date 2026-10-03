/// Route locations, shared by the router and the screens that navigate.
abstract final class AppRoutes {
  static const String splash = '/splash';
  static const String consent = '/consent';
  static const String login = '/login';
  static const String register = '/register';
  static const String forgotPassword = '/forgot-password';
  static const String settings = '/settings';
  static const String student = '/student';
  static const String onboarding = '/student/onboarding';
  static const String careerDiscovery = '/student/career-discovery';
  static const String realityCheck = '/student/reality-check';
  static const String support = '/student/support';
  static const String consultant = '/consultant';
  static const String counsellor = '/counsellor';
  static const String admin = '/admin';
  static const String notifications = '/notifications';

  static String counsellorReview(String id) => '/counsellor/review/$id';

  static String supportThread(String consultationId) => '$support?consultation=$consultationId';

  static String consultantCase(String consultationId) => '$consultant?consultation=$consultationId';

  static const Set<String> publicRoutes = {login, register, forgotPassword};
}
