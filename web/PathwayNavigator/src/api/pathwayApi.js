import apiClient from './apiClient';
import { getApiErrorMessage } from '../utils/apiError';
import { API_ROUTES } from '../config/constants';

const handleError = (error, fallbackMessage) => {
  throw new Error(getApiErrorMessage(error, fallbackMessage));
};

/**
 * Runs Agent 2 (Career Discovery) against the current user's saved profile
 * and persists the result. @returns {Promise<Object>} PathwayAnalysisResponseDto
 * ({ id, workflow_id, status, recommendations, validation_errors, execution_trace })
 */
export const analyzeCareerPathsApi = async () => {
  try {
    const response = await apiClient.post(API_ROUTES.CAREER_DISCOVERY_ANALYZE);
    return response.data;
  } catch (error) {
    handleError(error, 'Failed to analyze career pathways.');
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

/** Builds the detailed Agent 3 roadmap for a selected recommendation. */
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
