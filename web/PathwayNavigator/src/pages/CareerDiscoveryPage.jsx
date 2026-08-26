import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { analyzeCareerPathsApi, approvePathwayAnalysisApi, rejectPathwayAnalysisApi } from '../api/pathwayApi';
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
                  <PathwayRecommendationCard key={rec.pathway_name} recommendation={rec} rank={idx} />
                ))}
              </div>
            </>
          )}
        </div>
      )}
    </div>
  );
};

export default CareerDiscoveryPage;
