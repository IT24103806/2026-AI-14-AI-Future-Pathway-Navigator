import React, { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import {
  analyzeCareerPathsApi,
  approvePathwayAnalysisApi,
  rejectPathwayAnalysisApi,
  buildPathwayPlanApi,
  getLatestPathwayAnalysisApi,
  getLatestPathwayPlanApi,
  getMyPathwayPlansApi,
  updatePathwayPlanProgressApi,
} from '../api/pathwayApi';
import { counsellorReviewApi } from '../api/counsellorReviewApi';
import { getStudentProfileApi } from '../api/profileApi';
import { getApiErrorMessage } from '../utils/apiError';
import PrimaryButton from '../components/common/PrimaryButton';
import AlertBanner from '../components/common/AlertBanner';
import PathwayRecommendationCard from '../components/pathway/PathwayRecommendationCard';
import JourneySteps from '../components/common/JourneySteps';
import Reveal from '../components/common/Reveal';

const parseJsonList = (value) => {
  if (Array.isArray(value)) return value;
  try {
    return JSON.parse(value || '[]');
  } catch {
    return [];
  }
};

const HOW_IT_WORKS = [
  { icon: '🎯', title: 'Match', text: 'Scores your skills, interests & ambitions against a curated career knowledge base.' },
  { icon: '📊', title: 'Market Data', text: 'Pulls real job-market demand & competition signals for each candidate pathway.' },
  { icon: '🤖', title: 'AI Reasoning', text: 'Explains each match in plain language, then validates every result before showing it to you.' },
];

const DECISION_CONFIG = {
  approved: { icon: '✅', label: 'Approved', className: 'decision-approved' },
  rejected: { icon: '❌', label: 'Rejected', className: 'decision-rejected' },
};

const CareerDiscoveryPage = () => {
  const [result, setResult] = useState(null);
  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [isLoading, setIsLoading] = useState(false);
  const [isDeciding, setIsDeciding] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [hasRun, setHasRun] = useState(false);
  const [plannerResult, setPlannerResult] = useState(null);
  const [savedPlans, setSavedPlans] = useState([]);
  const [planningPathway, setPlanningPathway] = useState('');
  const [updatingStage, setUpdatingStage] = useState('');
  const [reviewCareer, setReviewCareer] = useState('');
  const [reviewBusy, setReviewBusy] = useState(false);
  const [reviewMessage, setReviewMessage] = useState('');
  const [isPrefilled, setIsPrefilled] = useState(false);
  const [reviewInput, setReviewInput] = useState({ alStream: '', alResults: '', budgetLevel: 'Medium', currentSkills: '' });

  const upsertSavedPlan = useCallback((plan) => {
    if (!plan?.selected_pathway) return;
    setSavedPlans((prev) => {
      const filtered = prev.filter(
        (item) => item.selected_pathway?.toLowerCase() !== plan.selected_pathway.toLowerCase()
      );
      return [plan, ...filtered];
    });
  }, []);

  useEffect(() => {
    let isMounted = true;
    const loadSavedState = async () => {
      setIsInitialLoading(true);
      try {
        const [latestAnalysis, latestPlan, allPlans, profile, latestReview] = await Promise.all([
          getLatestPathwayAnalysisApi ? getLatestPathwayAnalysisApi().catch(() => null) : Promise.resolve(null),
          getLatestPathwayPlanApi ? getLatestPathwayPlanApi().catch(() => null) : Promise.resolve(null),
          getMyPathwayPlansApi ? getMyPathwayPlansApi().catch(() => []) : Promise.resolve([]),
          getStudentProfileApi ? getStudentProfileApi().catch(() => null) : Promise.resolve(null),
          counsellorReviewApi?.getMyStatus ? counsellorReviewApi.getMyStatus().catch(() => null) : Promise.resolve(null),
        ]);

        if (!isMounted) return;

        if (latestAnalysis) {
          setResult(latestAnalysis);
          setHasRun(true);
          if (latestAnalysis.recommendations?.length > 0) {
            setReviewCareer((prev) => prev || latestReview?.targetCareer || latestAnalysis.recommendations[0].pathway_name);
          }
        }

        const savedStream = profile?.alStream || latestReview?.alStream || '';
        const savedResults = profile?.alResults || latestReview?.alResults || '';
        const savedBudget = profile?.budgetLevel || latestReview?.budgetLevel || 'Medium';
        const profileSkills = Array.isArray(profile?.coreSkills) ? profile.coreSkills : [];
        const reviewSkills = parseJsonList(latestReview?.currentSkillsJson);
        const mergedSkills = Array.from(new Set([...profileSkills, ...reviewSkills].filter(Boolean)));

        if (savedStream || savedResults || mergedSkills.length > 0) {
          setReviewInput((prev) => ({
            alStream: prev.alStream || savedStream,
            alResults: prev.alResults || savedResults,
            budgetLevel: prev.budgetLevel !== 'Medium' ? prev.budgetLevel : savedBudget,
            currentSkills: prev.currentSkills || mergedSkills.join(', '),
          }));
          setIsPrefilled(true);
        }

        if (Array.isArray(allPlans) && allPlans.length > 0) {
          setSavedPlans(allPlans);
        }

        if (latestPlan && latestPlan.status === 'ready') {
          setPlannerResult(latestPlan);
          upsertSavedPlan(latestPlan);
        } else if (Array.isArray(allPlans) && allPlans.length > 0) {
          setPlannerResult(allPlans[0]);
        }
      } finally {
        if (isMounted) {
          setIsInitialLoading(false);
        }
      }
    };

    loadSavedState();
    return () => {
      isMounted = false;
    };
  }, [upsertSavedPlan]);

  const startRealityCheck = async (event) => {
    event.preventDefault();
    if (!result?.id || !reviewCareer) return;
    setReviewBusy(true);
    setReviewMessage('');
    try {
      const review = await counsellorReviewApi.startRealityCheck(result.id, {
        targetCareer: reviewCareer,
        alStream: reviewInput.alStream.trim(),
        alResults: reviewInput.alResults.trim().toUpperCase(),
        budgetLevel: reviewInput.budgetLevel,
        currentSkills: reviewInput.currentSkills.split(',').map((skill) => skill.trim()).filter(Boolean),
      });
      setReviewMessage(`Reality Check saved: ${review.status}. View the result on your Reality Check page.`);
    } catch (error) {
      setReviewMessage(getApiErrorMessage(error, 'Reality Check failed. Check the API and AI service, then try again.'));
    } finally {
      setReviewBusy(false);
    }
  };

  const handleAnalyze = async () => {
    setIsLoading(true);
    setErrorMessage('');
    try {
      const data = await analyzeCareerPathsApi();
      setResult(data);
      setHasRun(true);
      if (data?.recommendations?.length > 0) {
        setReviewCareer((prev) => prev || data.recommendations[0].pathway_name);
      }
    } catch (err) {
      setErrorMessage(err.message || 'Something went wrong while analyzing your pathways.');
    } finally {
      setIsLoading(false);
    }
  };

  const handleBuildPlan = async (pathwayName) => {
    setPlanningPathway(pathwayName);
    setErrorMessage('');
    try {
      const existingSaved = savedPlans.find(
        (p) => p.selected_pathway?.toLowerCase() === pathwayName.toLowerCase()
      );
      const existingCompleted = existingSaved?.completed_phases || [];
      const plan = await buildPathwayPlanApi(pathwayName, existingCompleted);
      setPlannerResult(plan);
      if (plan?.status === 'ready') {
        upsertSavedPlan(plan);
      } else {
        setErrorMessage(`Roadmap could not be built: ${(plan?.validation_errors || []).join(' ') || 'unknown error.'}`);
      }
    } catch (err) {
      setErrorMessage(err.message || 'Something went wrong while building the roadmap.');
    } finally {
      setPlanningPathway('');
    }
  };

  const handleToggleStage = async (stageKey) => {
    if (!plannerResult || updatingStage) return;
    setUpdatingStage(stageKey);
    setErrorMessage('');

    const currentCompleted =
      plannerResult.completed_phases?.length > 0
        ? plannerResult.completed_phases
        : (plannerResult.roadmap || []).filter((s) => s.status === 'completed').map((s) => s.stage);

    const exists = currentCompleted.some((phase) => phase.toLowerCase() === stageKey.toLowerCase());
    const nextCompleted = exists
      ? currentCompleted.filter((phase) => phase.toLowerCase() !== stageKey.toLowerCase())
      : [...currentCompleted, stageKey];

    try {
      let updatedPlan;
      if (plannerResult.id && updatePathwayPlanProgressApi) {
        updatedPlan = await updatePathwayPlanProgressApi(plannerResult.id, nextCompleted);
      } else {
        updatedPlan = await buildPathwayPlanApi(plannerResult.selected_pathway, nextCompleted);
      }

      if (updatedPlan) {
        setPlannerResult(updatedPlan);
        upsertSavedPlan(updatedPlan);
      }
    } catch (err) {
      setErrorMessage(err.message || 'Failed to save roadmap milestone progress.');
    } finally {
      setUpdatingStage('');
    }
  };

  const handleDecision = async (decisionFn) => {
    if (!result?.id) return;
    setIsDeciding(true);
    setErrorMessage('');
    try {
      const data = await decisionFn(result.id);
      setResult(data);
    } catch (err) {
      setErrorMessage(err.message || 'Failed to record your decision.');
    } finally {
      setIsDeciding(false);
    }
  };

  const decision = result ? DECISION_CONFIG[result.status] : null;
  const roadmapStages = plannerResult?.roadmap || [];
  const completedStagesCount = roadmapStages.filter((step) => step.status === 'completed').length;
  const totalStagesCount = roadmapStages.length;
  const progressPercent = totalStagesCount > 0 ? Math.round((completedStagesCount / totalStagesCount) * 100) : 0;

  return (
    <div className="career-page-container">
      <div className="onboarding-hero">
        <div className="orb-field" aria-hidden="true">
          <span className="orb orb--cyan" style={{ width: 220, height: 220, top: -80, left: '8%' }} />
          <span className="orb orb--violet" style={{ width: 240, height: 240, bottom: -110, right: '6%' }} />
        </div>
        <div className="onboarding-hero-content">
          <span className="hero-pill-badge">✨ Step 2: Career Discovery</span>
          <h1>Discover Your Career Pathways</h1>
          <p>
            Agent 2 (Career Discovery) analyzes your saved profile against real career data to surface your
            top 3 ranked pathways — Path A, B, and C — each explained and market-checked.
          </p>
          <JourneySteps current={2} className="mt-4" />

          {!isLoading && !isInitialLoading && (
            <PrimaryButton onClick={handleAnalyze} isLoading={isLoading} className="hero-cta-btn">
              {hasRun ? '🔄 Re-run Career Discovery' : '🔮 Discover My Pathways'}
            </PrimaryButton>
          )}
        </div>
      </div>

      <AlertBanner type="error" message={errorMessage} onClose={() => setErrorMessage('')} />

      {errorMessage && errorMessage.toLowerCase().includes('complete onboarding') && (
        <div className="career-onboarding-nudge">
          <Link to="/onboarding" className="btn btn-sm btn-primary">
            🤖 Complete Onboarding First →
          </Link>
        </div>
      )}

      {(isLoading || isInitialLoading) && (
        <div className="career-loading-state">
          <div className="spinner large-spinner" />
          <h3>
            {isInitialLoading
              ? 'Loading your saved pathways and roadmap progress...'
              : 'Analyzing your profile against live market data...'}
          </h3>
          <p className="text-dim">
            {isInitialLoading
              ? 'Checking PostgreSQL for your latest Agent 2 analysis and Agent 3 roadmap'
              : 'Matching skills → fetching job-market signals → generating explanations → validating results'}
          </p>

          {!isInitialLoading && (
            <ul className="analysis-steps" aria-hidden="true">
              {[
                'Matching skills to the career knowledge base',
                'Fetching job-market demand & competition signals',
                'Generating explanations and validating results',
              ].map((label, index) => (
                <li className="analysis-step" key={label} style={{ animationDelay: `${index * 0.9}s` }}>
                  <span className="analysis-step__dot" />
                  {label}
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      {!isLoading && !isInitialLoading && !hasRun && (
        <Reveal className="section-head" variant="up">
          <span className="eyebrow">Before you begin</span>
          <h2>How Agent 2 builds your shortlist</h2>
        </Reveal>
      )}
      {!isLoading && !isInitialLoading && !hasRun && (
        <div className="how-it-works-grid">
          {HOW_IT_WORKS.map((step, idx) => (
            <div className="how-it-works-card" key={idx} style={{ animationDelay: `${idx * 0.1}s` }}>
              <div className="how-it-works-icon">{step.icon}</div>
              <h4>{idx + 1}. {step.title}</h4>
              <p>{step.text}</p>
            </div>
          ))}
        </div>
      )}

      {!isLoading && !isInitialLoading && result && (
        <div className="career-results-section">
          {result.status === 'failed' ? (
            <AlertBanner
              type="error"
              message={`Analysis could not be validated: ${result.validation_errors.join(' ')}`}
            />
          ) : (
            <>
              <div className="career-results-header">
                <div>
                  <h2>Your Top {result.recommendations.length} Pathways</h2>
                  {result.created_at && (
                    <small className="text-dim">
                      Saved analysis from {new Date(result.created_at).toLocaleString()}
                    </small>
                  )}
                </div>
                <span className="workflow-id-tag code-font">Workflow: {result.workflow_id}</span>
              </div>

              <div className="decision-panel">
                {decision ? (
                  <span className={`decision-badge ${decision.className}`}>
                    {decision.icon} You {decision.label} this analysis
                  </span>
                ) : (
                  <>
                    <p className="decision-prompt">Reviewed all three paths? Record your decision:</p>
                    <div className="decision-actions">
                      <button
                        type="button"
                        className="btn btn-sm decision-btn-approve"
                        disabled={isDeciding}
                        onClick={() => handleDecision(approvePathwayAnalysisApi)}
                      >
                        ✅ Approve
                      </button>
                      <button
                        type="button"
                        className="btn btn-sm decision-btn-reject"
                        disabled={isDeciding}
                        onClick={() => handleDecision(rejectPathwayAnalysisApi)}
                      >
                        ❌ Reject
                      </button>
                    </div>
                  </>
                )}
              </div>

              <div className="pathway-cards-grid">
                {result.recommendations.map((rec, idx) => (
                  <PathwayRecommendationCard
                    key={rec.pathway_name}
                    recommendation={rec}
                    rank={idx}
                    onBuildPlan={handleBuildPlan}
                    isBuilding={planningPathway === rec.pathway_name}
                  />
                ))}
              </div>

              <section className="reality-panel" aria-label="Agent 4 Reality Check">
                <div className="career-results-header" style={{ marginBottom: '0.5rem' }}>
                  <h2>Agent 4 · Reality Check</h2>
                  {isPrefilled && (
                    <span className="data-source-badge data-source-live">
                      ✨ Pre-filled from your saved profile
                    </span>
                  )}
                </div>
                <p>Select a recommended career and enter your actual A/L results, budget and skills. A counsellor reviews any flagged risks.</p>
                <form onSubmit={startRealityCheck}>
                  <label>Recommended career <select required value={reviewCareer} onChange={(event) => setReviewCareer(event.target.value)}>
                    <option value="">Select a career</option>
                    {result.recommendations.map((rec) => <option key={rec.pathway_name} value={rec.pathway_name}>{rec.pathway_name}</option>)}
                  </select></label>
                  <label>A/L stream <input required minLength={2} maxLength={80} value={reviewInput.alStream} onChange={(event) => setReviewInput({ ...reviewInput, alStream: event.target.value })} placeholder="Physical Science" /></label>
                  <label>A/L grades <input required pattern="[ABCFSabcfs](\s*[,/]\s*[ABCFSabcfs])*" value={reviewInput.alResults} onChange={(event) => setReviewInput({ ...reviewInput, alResults: event.target.value })} placeholder="A,B,C" /></label>
                  <label>Budget <select value={reviewInput.budgetLevel} onChange={(event) => setReviewInput({ ...reviewInput, budgetLevel: event.target.value })}>{['Low', 'Medium', 'High'].map((v) => <option key={v}>{v}</option>)}</select></label>
                  <label>Current skills (comma separated) <input value={reviewInput.currentSkills} onChange={(event) => setReviewInput({ ...reviewInput, currentSkills: event.target.value })} placeholder="Python, communication" /></label>
                  <button className="btn btn-primary" disabled={reviewBusy || !result.id} type="submit">{reviewBusy ? 'Checking…' : 'Run Reality Check'}</button>
                </form>
                {reviewMessage && <p role="status">{reviewMessage} <Link to="/student/reality-check">View Reality Check</Link></p>}
              </section>
            </>
          )}
        </div>
      )}

      {!isLoading && !isInitialLoading && plannerResult?.status === 'ready' && (
        <section className="career-results-section roadmap-progress-section" aria-label="Agent 3 Pathway Roadmap">
          <div className="career-results-header">
            <div>
              <span className="hero-pill-badge">🗺️ Agent 3 · Interactive Roadmap Tracker</span>
              <h2 style={{ marginTop: '0.5rem' }}>{plannerResult.selected_pathway} roadmap</h2>
            </div>
            <span className="workflow-id-tag code-font">Workflow: {plannerResult.workflow_id}</span>
          </div>

          {savedPlans.length > 1 && (
            <div className="saved-plans-switcher" role="group" aria-label="Saved pathway roadmaps">
              <span className="text-dim">Saved roadmaps:</span>
              {savedPlans.map((planItem) => {
                const isActive =
                  planItem.selected_pathway?.toLowerCase() === plannerResult.selected_pathway?.toLowerCase();
                return (
                  <button
                    key={planItem.id || planItem.selected_pathway}
                    type="button"
                    className={`btn btn-sm ${isActive ? 'btn-primary' : 'btn-outline-primary'}`}
                    onClick={() => setPlannerResult(planItem)}
                  >
                    {planItem.selected_pathway}
                  </button>
                );
              })}
            </div>
          )}

          <div className="roadmap-progress-summary-card">
            <div className="roadmap-progress-meta">
              <div>
                <strong>
                  Milestone Progress: {completedStagesCount} / {totalStagesCount} completed ({progressPercent}%)
                </strong>
                <p className="pathway-reasoning" style={{ margin: '0.35rem 0 0' }}>
                  Next action: {plannerResult.next_action}
                </p>
              </div>
              <span className="data-source-badge data-source-live">
                {progressPercent === 100 ? '🎉 All Milestones Completed' : '💾 Progress Auto-Saved'}
              </span>
            </div>
            <progress
              className="roadmap-progress-bar"
              max={totalStagesCount || 1}
              value={completedStagesCount}
              aria-label="Roadmap milestone completion progress"
            />
          </div>

          <div className="pathway-cards-grid">
            {roadmapStages.map((step) => {
              const isCompleted = step.status === 'completed';
              const isSavingThis = updatingStage === step.stage;
              return (
                <div
                  className={`how-it-works-card roadmap-stage-card ${isCompleted ? 'roadmap-stage-completed' : ''}`}
                  key={step.stage}
                >
                  <div className="roadmap-stage-top">
                    <span className="pathway-label-pill">
                      {step.order}. {step.stage.replace(/_/g, ' ')}
                    </span>
                    <span className={`stage-status-pill ${isCompleted ? 'completed' : 'pending'}`}>
                      {isCompleted ? '✅ Completed' : '⏳ To Do'}
                    </span>
                  </div>
                  <h3>{step.title}</h3>
                  <p>{step.outcome}</p>
                  <p className="text-dim">{step.actions.join(' ')}</p>
                  <div className="roadmap-stage-footer">
                    <span className="data-source-badge data-source-simulated">
                      Estimated: {step.estimated_duration}
                    </span>
                    <label className="stage-checkbox-label">
                      <input
                        type="checkbox"
                        checked={isCompleted}
                        disabled={Boolean(updatingStage)}
                        onChange={() => handleToggleStage(step.stage)}
                        aria-label={`Mark ${step.title} as completed`}
                      />
                      <span>{isSavingThis ? 'Saving…' : isCompleted ? 'Completed' : 'Mark complete'}</span>
                    </label>
                  </div>
                </div>
              );
            })}
          </div>
        </section>
      )}
    </div>
  );
};

export default CareerDiscoveryPage;
