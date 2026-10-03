export const ROLES = {
  STUDENT: 'Student',
  // Consultant answers student questions and writes guidance back into the journey; a Counsellor owns
  // the approval decision, and an Admin provisions consultant accounts.
  CONSULTANT: 'Consultant',
  COUNSELLOR: 'Counsellor',
  ADMIN: 'Admin',
};

export const STORAGE_KEYS = {
  TOKEN: 'pn_auth_token',
  USER: 'pn_auth_user',
};

export const API_ROUTES = {
  LOGIN: '/auth/login',
  REGISTER: '/auth/register',
  GOOGLE: '/auth/google',
  FORGOT_PASSWORD: '/auth/forgot-password',
  VERIFY_RESET_CODE: '/auth/verify-reset-code',
  RESET_PASSWORD: '/auth/reset-password',
  ONBOARDING_CHAT: '/onboarding/chat',
  ONBOARDING_STANDARD: '/onboarding/standard-form',
  PROFILE_STATUS: '/profile/status',
  PROFILE: '/profile',
  CAREER_DISCOVERY_BASE: '/career-discovery',
  CAREER_DISCOVERY_ANALYZE: '/career-discovery/analyze',
  CAREER_DISCOVERY_LATEST: '/career-discovery/me/latest',
  CAREER_DISCOVERY_HISTORY: '/career-discovery/me/history',
  PATHWAY_PLANNER_BASE: '/pathway-planner',
  PATHWAY_PLANNER_PLAN: '/pathway-planner/plan',
  PATHWAY_PLANNER_LATEST: '/pathway-planner/me/latest',
  PATHWAY_PLANNER_MY_PLANS: '/pathway-planner/me',
  CONSULTATIONS: '/consultations',
  CONSULTATIONS_ME: '/consultations/me',
  CONSULTANT_QUEUE: '/consultant/queue',
  CONSULTANT_STATS: '/consultant/me/stats',
  NOTIFICATIONS: '/notifications',
  NOTIFICATIONS_UNREAD: '/notifications/unread-count',
};
