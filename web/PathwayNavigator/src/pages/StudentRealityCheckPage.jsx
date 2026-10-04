import React, { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { counsellorReviewApi } from '../api/counsellorReviewApi';
import { getStudentProfileApi } from '../api/profileApi';
import { getLatestPathwayAnalysisApi } from '../api/pathwayApi';
import { getApiErrorMessage } from '../utils/apiError';
import JourneySteps from '../components/common/JourneySteps';
import AskConsultantDialog from '../components/consultation/AskConsultantDialog';
import ConsultationThread from '../components/consultation/ConsultationThread';
import ConsultantNote from '../components/consultation/ConsultantNote';
import consultationApi from '../api/consultationApi';

const parseList = (value) => {
  if (Array.isArray(value)) return value;
  try {
    return JSON.parse(value || '[]');
  } catch {
    return [];
  }
};

const statusCopy = {
  Pending: [
    'Waiting for counsellor',
    'Your high-impact pathway is paused until an authorized counsellor reviews it.',
  ],
  Approved: ['Approved', 'The counsellor approved this Reality Check result.'],
  Rejected: ['Rejected', 'Review the counsellor feedback before choosing another path.'],
  NeedsRevision: [
    'Revision requested',
    'Update the pathway using the counsellor feedback and run the Reality Check again.',
  ],
};

export default function StudentRealityCheckPage() {
  const [status, setStatus] = useState(null);
  // Consultant channel: which anchor the student is asking from, the deep-linked thread, and notices.
  const [askContext, setAskContext] = useState(null);
  const [focusedConsultation, setFocusedConsultation] = useState(null);
  const [consultationNotice, setConsultationNotice] = useState('');
  const [history, setHistory] = useState([]);
  const [careerOptions, setCareerOptions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [showRevisionForm, setShowRevisionForm] = useState(false);
  const [resubmitBusy, setResubmitBusy] = useState(false);
  const [resubmitMessage, setResubmitMessage] = useState('');
  const [resubmitError, setResubmitError] = useState('');
  const [revisionInput, setRevisionInput] = useState({
    targetCareer: '',
    alStream: '',
    alResults: '',
    budgetLevel: 'Medium',
    currentSkills: '',
  });

  const hydrateRevisionForm = useCallback((review, profileData, analysisData) => {
    if (!review) return;
    const reviewSkills = parseList(review.currentSkillsJson);
    const profileSkills = Array.isArray(profileData?.coreSkills) ? profileData.coreSkills : [];
    const mergedSkills = Array.from(new Set([...reviewSkills, ...profileSkills].filter(Boolean)));

    const recNames = (analysisData?.recommendations || [])
      .map((rec) => rec.pathway_name)
      .filter(Boolean);
    const allCareers = Array.from(new Set([review.targetCareer, ...recNames].filter(Boolean)));
    setCareerOptions(allCareers);

    setRevisionInput({
      targetCareer: review.targetCareer || allCareers[0] || '',
      alStream: review.alStream || profileData?.alStream || '',
      alResults: review.alResults || profileData?.alResults || '',
      budgetLevel: review.budgetLevel || profileData?.budgetLevel || 'Medium',
      currentSkills: mergedSkills.join(', '),
    });

    const requiresRevision =
      review.status === 'NeedsRevision' || review.status === 'Rejected';
    setShowRevisionForm(requiresRevision);
  }, []);

  const load = useCallback(async () => {
    setLoading(true);
    setError('');
    try {
      const [latest, past, profileData, analysisData] = await Promise.all([
        counsellorReviewApi.getMyStatus().catch((err) => {
          if (err.response?.status === 404) return null;
          throw err;
        }),
        counsellorReviewApi.getMyHistory(),
        getStudentProfileApi ? getStudentProfileApi().catch(() => null) : Promise.resolve(null),
        getLatestPathwayAnalysisApi
          ? getLatestPathwayAnalysisApi().catch(() => null)
          : Promise.resolve(null),
      ]);
      const safeHistory = Array.isArray(past) ? past : [];
      const activeReview = latest || safeHistory[0] || null;
      setHistory(safeHistory);
      setStatus(activeReview);
      if (activeReview) {
        hydrateRevisionForm(activeReview, profileData, analysisData);
      }
    } catch (err) {
      if (err.response?.status === 404) {
        setStatus(null);
      } else {
        setError(getApiErrorMessage(err, 'Unable to load your Reality Check result.'));
      }
    } finally {
      setLoading(false);
    }
  }, [hydrateRevisionForm]);

  useEffect(() => {
    load();
  }, [load]);

  // Notification deep link (`?consultation=<id>`): the consultant's answer opens over this page, so the
  // student can read it in the context of the review they were stuck on.
  useEffect(() => {
    const consultationId = new URLSearchParams(window.location.search).get('consultation');
    if (!consultationId) return;
    consultationApi.getById(consultationId)
      .then(setFocusedConsultation)
      .catch(() => setConsultationNotice(''));
  }, []);

  const handleQuickAddSkill = (skillToAdd) => {
    const existing = revisionInput.currentSkills
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean);
    const alreadyPresent = existing.some(
      (s) => s.toLowerCase() === skillToAdd.toLowerCase()
    );
    if (alreadyPresent) return;
    const nextSkills = [...existing, skillToAdd].join(', ');
    setRevisionInput((prev) => ({ ...prev, currentSkills: nextSkills }));
  };

  const handleResubmit = async (event) => {
    event.preventDefault();
    if (!status) return;
    setResubmitBusy(true);
    setResubmitMessage('');
    setResubmitError('');

    const payload = {
      targetCareer: revisionInput.targetCareer.trim() || status.targetCareer,
      alStream: revisionInput.alStream.trim(),
      alResults: revisionInput.alResults.trim().toUpperCase(),
      budgetLevel: revisionInput.budgetLevel,
      currentSkills: revisionInput.currentSkills
        .split(',')
        .map((skill) => skill.trim())
        .filter(Boolean),
    };

    try {
      const submitFn = counsellorReviewApi.resubmitRealityCheck
        ? () => counsellorReviewApi.resubmitRealityCheck(status.id, payload)
        : () => counsellorReviewApi.startRealityCheck(status.pathwayAnalysisId, payload);

      const updated = await submitFn();
      setStatus(updated);
      setHistory((prev) => [updated, ...prev.filter((item) => item.id !== updated.id)]);
      setResubmitMessage(
        `Updated Reality Check submitted! New status: ${updated.status} (Score: ${updated.feasibilityScore}/100).`
      );
      if (updated.status !== 'NeedsRevision' && updated.status !== 'Rejected') {
        setShowRevisionForm(false);
      }
    } catch (err) {
      setResubmitError(
        getApiErrorMessage(err, 'Could not resubmit your Reality Check. Please verify your inputs and try again.')
      );
    } finally {
      setResubmitBusy(false);
    }
  };

  if (loading) {
    return (
      <div className="reality-student-page">
        <p className="review-state">Loading Agent 4 evidence…</p>
      </div>
    );
  }
  if (error) {
    return (
      <div className="reality-student-page">
        <div className="review-alert error" role="alert">
          {error}
        </div>
        <button className="btn btn-primary" onClick={load}>
          Retry
        </button>
      </div>
    );
  }
  if (!status) {
    return (
      <div className="reality-student-page">
        <section className="reality-empty">
          <span>🧭</span>
          <h1>No Reality Check yet</h1>
          <p>
            Choose a career pathway first. Agent 4 will then check whether it is achievable for your
            results, subjects, budget and current skills.
          </p>
          <Link className="btn btn-primary" to="/career-discovery">
            Choose a career pathway
          </Link>
        </section>
      </div>
    );
  }

  const missing = parseList(status.missingSkillsJson);
  const subjects = parseList(status.subjectRequirementsJson);
  const entries = parseList(status.entryRequirementsJson);
  const plan = parseList(status.gapClosurePlanJson);
  const submittedSkills = parseList(status.currentSkillsJson);
  const activeFormSkills = revisionInput.currentSkills
    .split(',')
    .map((s) => s.trim().toLowerCase())
    .filter(Boolean);
  const canResubmit = status.status !== 'Pending';
  const [statusTitle, statusText] = statusCopy[status.status] || [
    status.status,
    'Your pathway status has been updated.',
  ];

  return (
    <div className="reality-student-page">
      <header className="student-reality-header">
        <div>
          <span className="agent-badge">🛡️ Agent 4 · Reality Check</span>
          <h1>{status.targetCareer}</h1>
          <p>
            Workflow <code>{status.workflowId}</code>
          </p>
          <JourneySteps current={3} className="mt-2" />
        </div>
        <div className="student-reality-header-actions">
          <button
            type="button"
            className="ask-consultant-trigger"
            onClick={() => setAskContext({
              contextType: 'RealityCheck',
              contextRefId: status.id,
              contextLabel: `${status.targetCareer} · ${status.status}`,
              prefillSubject: `Question about my ${status.targetCareer} Reality Check`,
              prefillBody: status.status === 'NeedsRevision' || status.status === 'Rejected'
                ? `I do not understand the feedback on my Reality Check: "${status.counsellorFeedback ?? ''}". Can you explain what I need to change?`
                : `I would like to understand my Reality Check result (${status.status}, feasibility ${status.feasibilityScore}%). Can you explain what it means for me?`,
            })}
          >
            🙋 Ask a consultant about this review
          </button>
          {canResubmit && !showRevisionForm && (
            <button
              type="button"
              className="btn btn-primary"
              onClick={() => setShowRevisionForm(true)}
            >
              🔄 Revise & Resubmit
            </button>
          )}
          <button className="btn btn-outline-primary" onClick={load}>
            Refresh status
          </button>
        </div>
      </header>

      <section className={`student-status-banner ${status.status.toLowerCase()}`}>
        <div>
          <strong>{statusTitle}</strong>
          <p>{statusText}</p>
        </div>
        <span>{status.status}</span>
      </section>

      {resubmitMessage && (
        <div className="review-alert success" role="status">
          {resubmitMessage}
        </div>
      )}

      {(status.alStream || status.alResults || submittedSkills.length > 0) && (
        <section className="reality-panel submitted-context-panel" aria-label="Submitted academic context">
          <h2>Submitted academic & skill profile</h2>
          <div className="chip-row">
            {status.alStream && <span>🎓 Stream: {status.alStream}</span>}
            {status.alResults && <span>📝 A/L Grades: {status.alResults}</span>}
            {status.budgetLevel && <span>💳 Budget: {status.budgetLevel}</span>}
            {submittedSkills.map((skill) => (
              <span key={skill}>🛠️ {skill}</span>
            ))}
          </div>
        </section>
      )}

      <div className="student-reality-grid">
        <section className="reality-panel score-panel">
          <h2>Feasibility score</h2>
          <div className="large-score">
            {status.feasibilityScore}
            <small>/100</small>
          </div>
          <progress max="100" value={status.feasibilityScore} />
        </section>
        <section className="reality-panel">
          <h2>Agent 4 summary</h2>
          <p>{status.feasibilitySummary || 'The pathway was checked against available evidence.'}</p>
          {status.isHighRisk && (
            <div className="student-risk">
              <strong>Risk requiring review</strong>
              <p>{status.riskReason}</p>
            </div>
          )}
        </section>
        <section className="reality-panel">
          <h2>Degree or qualification</h2>
          <p>{status.degreeRequirement || 'Counsellor verification is required.'}</p>
          <h3>Required subjects</h3>
          {subjects.length ? (
            <ul>
              {subjects.map((x) => (
                <li key={x}>{x}</li>
              ))}
            </ul>
          ) : (
            <p className="muted">No verified subjects listed.</p>
          )}
        </section>
        <section className="reality-panel">
          <h2>Entry requirements</h2>
          {entries.length ? (
            <ul>
              {entries.map((x) => (
                <li key={x}>{x}</li>
              ))}
            </ul>
          ) : (
            <p className="muted">No verified entry requirements listed.</p>
          )}
          <h3>Cost guidance</h3>
          <p>{status.costGuidance || 'Verify the total cost before making a commitment.'}</p>
        </section>
        <section className="reality-panel">
          <h2>Missing skills</h2>
          {missing.length ? (
            <div className="chip-row">
              {missing.map((x) => (
                <span key={x}>{x}</span>
              ))}
            </div>
          ) : (
            <p>No major prerequisite skill gap was identified.</p>
          )}
        </section>
        <section className="reality-panel">
          <h2>Shortest gap-closing plan</h2>
          {plan.length ? (
            <ol>
              {plan.map((x) => (
                <li key={x}>{x}</li>
              ))}
            </ol>
          ) : (
            <p className="muted">The plan will appear after the next Reality Check.</p>
          )}
        </section>
      </div>

      <section className="reality-panel counsellor-feedback">
        <h2>👩‍🏫 Counsellor feedback</h2>
        <p>
          {status.counsellorFeedback ||
            (status.status === 'Pending'
              ? 'Waiting for an authorized counsellor decision.'
              : 'No feedback was provided.')}
        </p>
        {/* Answers from a consultant are written back onto the review and shown right here. */}
        <ConsultantNote guidance={status.consultantAdviceJson} title="Consultant help with this review" />
      </section>

      {focusedConsultation && (
        <div className="consult-modal" role="dialog" aria-modal="true" aria-label="Consultant conversation">
          <div className="consult-modal__card">
            <header className="consult-modal__head">
              <div>
                <h2>🤝 Your consultant conversation</h2>
                <p>Reply here and keep working through your pathway.</p>
              </div>
              <button type="button" className="consult-modal__close" onClick={() => setFocusedConsultation(null)} aria-label="Close">✕</button>
            </header>
            <ConsultationThread
              consultation={focusedConsultation}
              variant="student"
              onSend={async (body) => {
                try {
                  setFocusedConsultation(await consultationApi.addMessage(focusedConsultation.id, body));
                  return true;
                } catch {
                  setConsultationNotice('Your message could not be sent.');
                  return false;
                }
              }}
            />
          </div>
        </div>
      )}

      {consultationNotice && <p className="review-alert success" role="status">{consultationNotice}</p>}

      {canResubmit && showRevisionForm && (
        <section className="reality-panel revision-loop-panel" aria-label="Revise and resubmit Reality Check">
          <div className="career-results-header" style={{ marginBottom: '0.5rem' }}>
            <div>
              <h2>🔁 Revise & Resubmit Reality Check</h2>
              <p className="muted" style={{ margin: '0.25rem 0 0' }}>
                Update your target pathway, academic details, or newly acquired skills to re-run Agent 4
                and submit a revised evaluation.
              </p>
            </div>
            {status.status !== 'NeedsRevision' && status.status !== 'Rejected' && (
              <button
                type="button"
                className="btn btn-sm btn-outline-primary"
                onClick={() => setShowRevisionForm(false)}
              >
                Close
              </button>
            )}
          </div>

          {missing.length > 0 && (
            <div className="revision-skill-chips-box">
              <small className="text-dim">
                Completed a gap-closing step? Click a missing skill below to add it to your profile:
              </small>
              <div className="chip-row" style={{ marginTop: '0.4rem' }}>
                {missing.map((skill) => {
                  const isAdded = activeFormSkills.includes(skill.toLowerCase());
                  return (
                    <button
                      key={skill}
                      type="button"
                      className={`skill-quick-add-chip ${isAdded ? 'added' : ''}`}
                      onClick={() => handleQuickAddSkill(skill)}
                      disabled={isAdded}
                    >
                      {isAdded ? `✓ Added ${skill}` : `+ Add ${skill}`}
                    </button>
                  );
                })}
              </div>
            </div>
          )}

          {resubmitError && (
            <div className="review-alert error" role="alert">
              {resubmitError}
            </div>
          )}

          <form onSubmit={handleResubmit} className="revision-form">
            <label>
              Target career
              {careerOptions.length > 1 ? (
                <select
                  required
                  value={revisionInput.targetCareer}
                  onChange={(e) =>
                    setRevisionInput({ ...revisionInput, targetCareer: e.target.value })
                  }
                >
                  {careerOptions.map((c) => (
                    <option key={c} value={c}>
                      {c}
                    </option>
                  ))}
                </select>
              ) : (
                <input
                  required
                  value={revisionInput.targetCareer}
                  onChange={(e) =>
                    setRevisionInput({ ...revisionInput, targetCareer: e.target.value })
                  }
                  placeholder="Data Science & AI"
                />
              )}
            </label>
            <label>
              A/L stream
              <input
                required
                minLength={2}
                maxLength={80}
                value={revisionInput.alStream}
                onChange={(e) => setRevisionInput({ ...revisionInput, alStream: e.target.value })}
                placeholder="Physical Science"
              />
            </label>
            <label>
              A/L grades
              <input
                required
                pattern="[ABCFSabcfs](\s*[,/]\s*[ABCFSabcfs])*"
                value={revisionInput.alResults}
                onChange={(e) => setRevisionInput({ ...revisionInput, alResults: e.target.value })}
                placeholder="A,B,C"
              />
            </label>
            <label>
              Budget
              <select
                value={revisionInput.budgetLevel}
                onChange={(e) =>
                  setRevisionInput({ ...revisionInput, budgetLevel: e.target.value })
                }
              >
                {['Low', 'Medium', 'High'].map((v) => (
                  <option key={v} value={v}>
                    {v}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Current skills (comma separated)
              <input
                value={revisionInput.currentSkills}
                onChange={(e) =>
                  setRevisionInput({ ...revisionInput, currentSkills: e.target.value })
                }
                placeholder="Python, SQL, Statistics"
              />
            </label>
            <button className="btn btn-primary" disabled={resubmitBusy} type="submit">
              {resubmitBusy ? 'Resubmitting…' : 'Resubmit Reality Check'}
            </button>
          </form>
        </section>
      )}

      <section className="reality-panel">
        <h2>Review history</h2>
        {history.length === 0 ? (
          <p className="muted">No previous reviews.</p>
        ) : (
          <div className="student-history">
            {history.map((item) => (
              <div key={item.id}>
                <span className={`history-dot ${item.status.toLowerCase()}`} />
                <div>
                  <strong>{item.targetCareer}</strong>
                  <small>{new Date(item.createdAt).toLocaleString()}</small>
                </div>
                <b>{item.status}</b>
              </div>
            ))}
          </div>
        )}
      </section>

      {askContext && (
        <AskConsultantDialog
          contextType={askContext.contextType}
          contextRefId={askContext.contextRefId}
          contextLabel={askContext.contextLabel}
          prefillSubject={askContext.prefillSubject}
          prefillBody={askContext.prefillBody}
          onClose={() => setAskContext(null)}
          onCreated={(created) => {
            setConsultationNotice('Sent. A consultant will reply — you will get a notification.');
            setFocusedConsultation(created);
          }}
        />
      )}
    </div>
  );
}
