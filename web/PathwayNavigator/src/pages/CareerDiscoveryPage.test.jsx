// @vitest-environment jsdom
import React from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import '@testing-library/jest-dom/vitest';
import CareerDiscoveryPage from './CareerDiscoveryPage';
import * as pathwayApi from '../api/pathwayApi';
import * as profileApi from '../api/profileApi';
import { counsellorReviewApi } from '../api/counsellorReviewApi';

vi.mock('../api/pathwayApi', () => ({
  analyzeCareerPathsApi: vi.fn(),
  approvePathwayAnalysisApi: vi.fn(),
  rejectPathwayAnalysisApi: vi.fn(),
  buildPathwayPlanApi: vi.fn(),
  getLatestPathwayAnalysisApi: vi.fn(),
  getLatestPathwayPlanApi: vi.fn(),
  getMyPathwayPlansApi: vi.fn(),
  updatePathwayPlanProgressApi: vi.fn(),
}));

vi.mock('../api/profileApi', () => ({
  getStudentProfileApi: vi.fn(),
}));

vi.mock('../api/counsellorReviewApi', () => ({
  counsellorReviewApi: { startRealityCheck: vi.fn(), getMyStatus: vi.fn() },
}));

const sampleAnalysis = {
  id: 'analysis-1',
  workflow_id: 'wf-agent2-1',
  status: 'pending_approval',
  created_at: '2026-10-02T08:00:00Z',
  recommendations: [
    {
      label: 'Path A',
      pathway_name: 'AI / Machine Learning Engineer',
      match_score: 88,
      demand_score: 82,
      competition_score: 60,
      trend: 'rising',
      data_source: 'simulated_fallback',
      reasoning: 'Strong Python and AI alignment.',
      missing_skills: ['deep learning', 'sql'],
      recommended_courses: ['Machine Learning Specialization', 'Deep Learning with PyTorch', 'MLOps Fundamentals'],
      roadmap: [{ phase: 'Foundation', course: 'Machine Learning Specialization' }],
      day_in_the_life: 'Collaborates with data teams to train and deploy monitored AI microservices.',
      salary_range_lkr: 'LKR 180,000 – 450,000/mo (Entry–Mid SL)',
      industry_tools: ['PyTorch', 'FastAPI', 'Docker'],
      portfolio_projects: ['End-to-End Sinhala/English Document Q&A Assistant using RAG'],
      recommended_certifications: ['DeepLearning.AI Machine Learning Specialization'],
      sri_lankan_education_routes: ['SLIIT BSc (Hons) in IT Specializing in Artificial Intelligence'],
    },
  ],
  validation_errors: [],
};

const samplePlan = {
  id: 'plan-1',
  workflow_id: 'wf-agent3-1',
  status: 'ready',
  selected_pathway: 'AI / Machine Learning Engineer',
  missing_skills: ['deep learning', 'sql'],
  completed_phases: ['education'],
  next_action: 'Study Machine Learning Specialization.',
  roadmap: [
    {
      order: 1,
      stage: 'education',
      title: 'Build the education foundation',
      outcome: 'Reach the education milestone.',
      actions: ['Compare accredited degree options.'],
      estimated_duration: '3-4 years',
      status: 'completed',
    },
    {
      order: 2,
      stage: 'skills',
      title: 'Close the core skill gaps',
      outcome: 'Reach the skills milestone.',
      actions: ['Study Machine Learning Specialization.'],
      estimated_duration: '3-6 months',
      status: 'not_started',
    },
  ],
};

describe('CareerDiscoveryPage', () => {
  afterEach(() => cleanup());

  beforeEach(() => {
    vi.clearAllMocks();
    pathwayApi.getLatestPathwayAnalysisApi.mockResolvedValue(null);
    pathwayApi.getLatestPathwayPlanApi.mockResolvedValue(null);
    pathwayApi.getMyPathwayPlansApi.mockResolvedValue([]);
    profileApi.getStudentProfileApi.mockResolvedValue(null);
    counsellorReviewApi.getMyStatus.mockResolvedValue(null);
  });

  it('auto-loads and displays the latest saved Agent 2 analysis and Agent 3 roadmap on mount', async () => {
    pathwayApi.getLatestPathwayAnalysisApi.mockResolvedValue(sampleAnalysis);
    pathwayApi.getLatestPathwayPlanApi.mockResolvedValue(samplePlan);
    pathwayApi.getMyPathwayPlansApi.mockResolvedValue([samplePlan]);

    render(
      <MemoryRouter>
        <CareerDiscoveryPage />
      </MemoryRouter>
    );

    expect(await screen.findByText('Your Top 1 Pathways')).toBeInTheDocument();
    expect(screen.getByText('🔄 Re-run Career Discovery')).toBeInTheDocument();
    expect(screen.getByText('AI / Machine Learning Engineer roadmap')).toBeInTheDocument();
    expect(screen.getByText(/Milestone Progress: 1 \/ 2 completed \(50%\)/)).toBeInTheDocument();
    expect(screen.getByText(/Next action: Study Machine Learning Specialization\./)).toBeInTheDocument();
  });

  it('persists stage checkbox toggles via updatePathwayPlanProgressApi and updates progress', async () => {
    pathwayApi.getLatestPathwayAnalysisApi.mockResolvedValue(sampleAnalysis);
    pathwayApi.getLatestPathwayPlanApi.mockResolvedValue(samplePlan);
    pathwayApi.getMyPathwayPlansApi.mockResolvedValue([samplePlan]);

    const updatedPlan = {
      ...samplePlan,
      completed_phases: ['education', 'skills'],
      next_action: 'All roadmap milestones for AI / Machine Learning Engineer are completed! Keep your portfolio and skills updated.',
      roadmap: samplePlan.roadmap.map((step) => ({ ...step, status: 'completed' })),
    };
    pathwayApi.updatePathwayPlanProgressApi.mockResolvedValue(updatedPlan);

    render(
      <MemoryRouter>
        <CareerDiscoveryPage />
      </MemoryRouter>
    );

    const skillsCheckbox = await screen.findByLabelText('Mark Close the core skill gaps as completed');
    expect(skillsCheckbox).not.toBeChecked();

    fireEvent.click(skillsCheckbox);

    await waitFor(() => {
      expect(pathwayApi.updatePathwayPlanProgressApi).toHaveBeenCalledWith('plan-1', ['education', 'skills']);
    });

    expect(await screen.findByText(/Milestone Progress: 2 \/ 2 completed \(100%\)/)).toBeInTheDocument();
    expect(screen.getByText(/All roadmap milestones for AI \/ Machine Learning Engineer are completed!/)).toBeInTheDocument();
  });

  it('shows the initial How It Works state when no saved analysis exists yet', async () => {
    render(
      <MemoryRouter>
        <CareerDiscoveryPage />
      </MemoryRouter>
    );

    expect(await screen.findByText('🔮 Discover My Pathways')).toBeInTheDocument();
    expect(screen.getByText('1. Match')).toBeInTheDocument();
  });

  it('expands the Career Deep-Dive drawer to show projects, certifications, tools, and Sri Lankan study routes', async () => {
    pathwayApi.getLatestPathwayAnalysisApi.mockResolvedValue(sampleAnalysis);

    render(
      <MemoryRouter>
        <CareerDiscoveryPage />
      </MemoryRouter>
    );

    expect(await screen.findByText(/LKR 180,000 – 450,000\/mo/)).toBeInTheDocument();

    const toggleBtn = screen.getByRole('button', { name: /Career Deep-Dive, Projects & SL Study Routes/i });
    expect(toggleBtn).toHaveAttribute('aria-expanded', 'false');

    fireEvent.click(toggleBtn);

    expect(toggleBtn).toHaveAttribute('aria-expanded', 'true');
    expect(screen.getByText(/Collaborates with data teams to train and deploy monitored AI microservices\./)).toBeInTheDocument();
    expect(screen.getByText('PyTorch')).toBeInTheDocument();
    expect(screen.getByText('End-to-End Sinhala/English Document Q&A Assistant using RAG')).toBeInTheDocument();
    expect(screen.getByText('SLIIT BSc (Hons) in IT Specializing in Artificial Intelligence')).toBeInTheDocument();
  });

  it('pre-fills the Reality Check form from the student profile and submits without re-typing', async () => {
    pathwayApi.getLatestPathwayAnalysisApi.mockResolvedValue(sampleAnalysis);
    profileApi.getStudentProfileApi.mockResolvedValue({
      alStream: 'Physical Science',
      alResults: 'A, B, C',
      budgetLevel: 'High',
      coreSkills: ['Python', 'Git'],
    });
    counsellorReviewApi.startRealityCheck.mockResolvedValue({ status: 'Approved' });

    render(
      <MemoryRouter>
        <CareerDiscoveryPage />
      </MemoryRouter>
    );

    expect(await screen.findByText('✨ Pre-filled from your saved profile')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Physical Science')).toBeInTheDocument();
    expect(screen.getByDisplayValue('A, B, C')).toBeInTheDocument();
    expect(screen.getByDisplayValue('High')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Python, Git')).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Run Reality Check' }));

    await waitFor(() => {
      expect(counsellorReviewApi.startRealityCheck).toHaveBeenCalledWith('analysis-1', {
        targetCareer: 'AI / Machine Learning Engineer',
        alStream: 'Physical Science',
        alResults: 'A, B, C',
        budgetLevel: 'High',
        currentSkills: ['Python', 'Git'],
      });
    });
  });
});
