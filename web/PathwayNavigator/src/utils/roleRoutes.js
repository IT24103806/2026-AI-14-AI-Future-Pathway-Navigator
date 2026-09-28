export const dashboardPathForRole = (role) => {
  if (role === 'Counsellor') return '/counsellor/dashboard';
  if (role === 'Admin') return '/admin/dashboard';
  return '/student/dashboard';
};
