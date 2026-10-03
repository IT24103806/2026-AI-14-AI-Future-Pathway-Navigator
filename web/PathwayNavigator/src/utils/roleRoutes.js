export const dashboardPathForRole = (role) => {
  if (role === 'Consultant') return '/consultant/dashboard';
  if (role === 'Counsellor') return '/counsellor/dashboard';
  if (role === 'Admin') return '/admin/dashboard';
  return '/student/dashboard';
};

/**
 * The Consultant Desk is a *support* surface, distinct from the counsellor approval centre: a
 * consultant answers student questions but never approves or rejects a pathway (ADR-004), so this
 * helper is intentionally separate from any "staff" grouping.
 */
export const canAccessConsultantDesk = (role) => role === 'Consultant' || role === 'Admin';
