import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { analyzeCareerPathsApi, approvePathwayAnalysisApi, rejectPathwayAnalysisApi, buildPathwayPlanApi } from '../api/pathwayApi';
import { counsellorReviewApi } from '../api/counsellorReviewApi';
import PrimaryButton from '../components/common/PrimaryButton';
import AlertBanner from '../components/common/AlertBanner';
import PathwayRecommendationCard from '../components/pathway/PathwayRecommendationCard';

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
  const [isLoading, setIsLoading] = useState(false);
  const [isDeciding, setIsDeciding] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [hasRun, setHasRun] = useState(false);
  const [plannerResult, setPlannerResult] = useState(null);
  const [planningPathway, setPlanningPathway] = useState('');
  const [reviewCareer, setReviewCareer] = useState('');
  const [reviewBusy, setReviewBusy] = useState(false);
  const [reviewMessage, setReviewMessage] = useState('');
  const [reviewInput, setReviewInput] = useState({ alStream: '', alResults: '', budgetLevel: 'Medium', currentSkills: '' });

  const startRealityCheck = async (event) => {
    event.preventDefault();
    if (!result?.id || !reviewCareer) return;
    setReviewBusy(true); setReviewMessage('');
    try {
      const review = await counsellorReviewApi.startRealityCheck(result.id, {
        targetCareer: reviewCareer, alStream: reviewInput.alStream.trim(),
        alResults: reviewInput.alResults.trim().toUpperCase(), budgetLevel: reviewInput.budgetLevel,
        currentSkills: reviewInput.currentSkills.split(',').map((skill) => skill.trim()).filter(Boolean),
      });
      setReviewMessage(`Reality Check saved: ${review.status}. View the result on your Reality Check page.`);
    } catch (error) {
      setReviewMessage(error.response?.data?.message || 'Reality Check failed. Check the API and AI service, then try again.');
    } finally { setReviewBusy(false); }
  };

  const handleAnalyze = async () => {
    setIsLoading(true);
    setErrorMessage('');
    try {
      const data = await analyzeCareerPathsApi();
      setResult(data);
      setHasRun(true);
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
      setPlannerResult(await buildPathwayPlanApi(pathwayName));
    } catch (err) {
      setErrorMessage(err.message || 'Something went wrong while building the roadmap.');
    } finally {
      setPlanningPathway('');
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

  return (
    <div className="career-page-container">
      <div className="onboarding-hero">
        <span className="hero-pill-badge">✨ Step 2: Career Discovery</span>
        <h1>Discover Your Career Pathways</h1>
        <p>
          Agent 2 (Career Discovery) analyzes your saved profile against real career data to surface your
          top 3 ranked pathways — Path A, B, and C — each explained and market-checked.
        </p>

        {!isLoading && (
          <PrimaryButton onClick={handleAnalyze} isLoading={isLoading} className="hero-cta-btn">
            {hasRun ? '🔄 Re-run Career Discovery' : '🔮 Discover My Pathways'}
          </PrimaryButton>
        )}
      </div>

      <AlertBanner type="error" message={errorMessage} onClose={() => setErrorMessage('')} />

      {errorMessage && errorMessage.toLowerCase().includes('complete onboarding') && (
        <div className="career-onboarding-nudge">
          <Link to="/onboarding" className="btn btn-sm btn-primary">
            🤖 Complete Onboarding First →
          </Link>
        </div>
      )}

      {isLoading && (
        <div className="career-loading-state">
          <div className="spinner large-spinner" />
          <h3>Analyzing your profile against live market data...</h3>
          <p className="text-dim">Matching skills → fetching job-market signals → generating explanations → validating results</p>
        </div>
      )}

      {!isLoading && !hasRun && (
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

      {!isLoading && result && (
        <div className="career-results-section">
          {result.status === 'failed' ? (
            <AlertBanner
              type="error"
              message={`Analysis could not be validated: ${result.validation_errors.join(' ')}`}
            />
          ) : (
            <>
              <div className="career-results-header">
                <h2>Your Top {result.recommendations.length} Pathways</h2>
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
                <h2>Agent 4 · Reality Check</h2>
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

              {plannerResult?.status === 'ready' && (
                <section className="career-results-section">
                  <div className="career-results-header">
                    <h2>{plannerResult.selected_pathway} roadmap</h2>
                    <span className="workflow-id-tag code-font">Workflow: {plannerResult.workflow_id}</span>
                  </div>
                  <p className="pathway-reasoning">Next action: {plannerResult.next_action}</p>
                  <div className="pathway-cards-grid">
                    {plannerResult.roadmap.map((step) => (
                      <div className="how-it-works-card" key={step.stage}>
                        <span className="pathway-label-pill">{step.order}. {step.stage.replace('_', ' ')}</span>
                        <h3>{step.title}</h3>
                        <p>{step.outcome}</p>
                        <p className="text-dim">{step.actions.join(' ')}</p>
                        <span className="data-source-badge data-source-simulated">Estimated: {step.estimated_duration}</span>
                      </div>
                    ))}
                  </div>
                </section>
              )}
            </>
          )}
        </div>
      )}
    </div>
  );
};

export default CareerDiscoveryPage;
