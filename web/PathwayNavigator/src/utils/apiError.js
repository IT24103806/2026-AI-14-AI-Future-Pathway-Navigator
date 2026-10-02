/**
 * Extracts a human-readable message from an axios error produced by the ASP.NET Core API.
 *
 * The backend answers with one of:
 *   - { message: "..." }                       (controller-level business errors, 401/404/409/502)
 *   - ProblemDetails { title, errors: { Field: ["msg"] } }   (automatic [ApiController] model validation → 400)
 */
export const getApiErrorMessage = (error, fallback = 'Something went wrong. Please try again.') => {
  const response = error?.response;
  if (!response) {
    if (error?.code === 'ECONNABORTED') {
      return 'The server took too long to respond. The AI service may be busy — please try again.';
    }
    return 'Network error. Unable to reach the Pathway Navigator API.';
  }

  const data = response.data;
  if (typeof data === 'string' && data.trim()) return data;
  if (data?.message) return data.message;

  if (data?.errors && typeof data.errors === 'object') {
    const messages = Object.values(data.errors).flat().filter(Boolean);
    if (messages.length > 0) return messages.join(' ');
  }

  if (response.status === 401) return 'Your session has expired. Please sign in again.';
  if (response.status === 403) return 'You do not have permission to perform this action.';
  return data?.title || fallback;
};
