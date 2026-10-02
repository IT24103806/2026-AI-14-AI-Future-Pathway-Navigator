import apiClient from './apiClient';
import { getApiErrorMessage } from '../utils/apiError';
import { API_ROUTES } from '../config/constants';

const handleError = (error, fallbackMessage) => {
  throw new Error(getApiErrorMessage(error, fallbackMessage));
};

/**
 * Runs Agent 2 (Career Discovery) against the current user's saved profile
 * and persists the result. @returns {Promise<Object>} PathwayAnalysisResponseDto
 * ({ id, workflow_id, status, recommendations, validation_errors, execution_trace, created_at })
 */
export const analyzeCareerPathsApi = async () => {
  try {
    const response = await apiClient.post(API_ROUTES.CAREER_DISCOVERY_ANALYZE);
    return response.data;
  } catch (error) {
    handleError(error, 'Failed to analyze career pathways.');
  }
};

/**
 * Fetches the signed-in student's most recent pathway analysis, or null when none exists yet (404).
 */
export const getLatestPathwayAnalysisApi = async () => {
  try {
    const response = await apiClient.get(API_ROUTES.CAREER_DISCOVERY_LATEST);
    return response.data;
  } catch (error) {
    if (error?.response?.status === 404) {
      return null;
    }
    handleError(error, 'Failed to load your latest career pathway analysis.');
  }
};

/**
 * Fetches all previously generated pathway analyses for the signed-in student.
 */
export const getPathwayAnalysisHistoryApi = async () => {
  try {
    const response = await apiClient.get(API_ROUTES.CAREER_DISCOVERY_HISTORY);
    return response.data;
  } catch (error) {
    handleError(error, 'Failed to load your career pathway history.');
  }
};

/** Fetches a previously generated pathway analysis by id. */
export const getPathwayAnalysisApi = async (id) => {
  try {
    const response = await apiClient.get(`${API_ROUTES.CAREER_DISCOVERY_BASE}/${id}`);
    return response.data;
  } catch (error) {
    handleError(error, 'Failed to load the saved pathway analysis.');
  }
};

/** Approves a pending_approval analysis (human sign-off). */
export const approvePathwayAnalysisApi = async (id) => {
  try {
    const response = await apiClient.patch(`${API_ROUTES.CAREER_DISCOVERY_BASE}/${id}/approve`);
    return response.data;
  } catch (error) {
    handleError(error, 'Failed to approve this pathway analysis.');
  }
};

/** Rejects a pending_approval analysis. */
export const rejectPathwayAnalysisApi = async (id) => {
  try {
    const response = await apiClient.patch(`${API_ROUTES.CAREER_DISCOVERY_BASE}/${id}/reject`);
    return response.data;
  } catch (error) {
    handleError(error, 'Failed to reject this pathway analysis.');
  }
};

/** Builds and persists the detailed Agent 3 roadmap for a selected recommendation. */
export const buildPathwayPlanApi = async (selectedPathway, completedPhases = []) => {
  try {
    const response = await apiClient.post(API_ROUTES.PATHWAY_PLANNER_PLAN, {
      selected_pathway: selectedPathway,
      completed_phases: completedPhases,
    });
    return response.data;
  } catch (error) {
    handleError(error, 'Failed to build the pathway roadmap.');
  }
};

/**
 * Fetches the signed-in student's most recently updated Agent 3 roadmap, or null when none exists yet (404).
 */
export const getLatestPathwayPlanApi = async () => {
  try {
    const response = await apiClient.get(API_ROUTES.PATHWAY_PLANNER_LATEST);
    return response.data;
  } catch (error) {
    if (error?.response?.status === 404) {
      return null;
    }
    handleError(error, 'Failed to load your saved pathway roadmap.');
  }
};

/**
 * Fetches all saved Agent 3 roadmaps for the signed-in student.
 */
export const getMyPathwayPlansApi = async () => {
  try {
    const response = await apiClient.get(API_ROUTES.PATHWAY_PLANNER_MY_PLANS);
    return response.data;
  } catch (error) {
    handleError(error, 'Failed to load your saved roadmaps.');
  }
};

/**
 * Updates the completed phases for a persisted Agent 3 roadmap.
 */
export const updatePathwayPlanProgressApi = async (planId, completedPhases = []) => {
  try {
    const response = await apiClient.patch(`${API_ROUTES.PATHWAY_PLANNER_BASE}/${planId}/progress`, {
      completed_phases: completedPhases,
    });
    return response.data;
  } catch (error) {
    handleError(error, 'Failed to update roadmap milestone progress.');
  }
};
