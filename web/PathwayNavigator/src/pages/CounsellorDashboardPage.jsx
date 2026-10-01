import React, { useCallback, useEffect, useState } from 'react';
import { counsellorReviewApi } from '../api/counsellorReviewApi';
import { getApiErrorMessage } from '../utils/apiError';

const parseJson = (value) => { try { return JSON.parse(value || '[]'); } catch { return []; } };
// Tool-call and execution-trace evidence is persisted exactly as emitted by the AI service
// (snake_case: tool_name, duration_ms), while the rest of the review DTO is camelCase.
const pick = (item, snake, camel) => item?.[snake] ?? item?.[camel];

export default function CounsellorDashboardPage() {
  const [result, setResult] = useState({ items: [], totalCount: 0, totalPages: 0 });
  const [filters, setFilters] = useState({ status: 'Pending', search: '', sort: 'newest', page: 1, pageSize: 10 });
  const [selected, setSelected] = useState(null);
  const [feedback, setFeedback] = useState('');
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  const [alert, setAlert] = useState(null);

  const loadReviews = useCallback(async () => {
    setLoading(true);
    try { setResult(await counsellorReviewApi.getReviews(filters)); }
    catch (error) { setAlert({ type: 'error', text: getApiErrorMessage(error, 'Failed to load reviews.') }); }
    finally { setLoading(false); }
  }, [filters]);

  useEffect(() => { loadReviews(); }, [loadReviews]);

  const updateFilter = (key, value) => setFilters((old) => ({ ...old, [key]: value, page: key === 'page' ? value : 1 }));
  const chooseReview = async (review) => {
    try { setSelected(await counsellorReviewApi.getReviewById(review.id)); setFeedback(''); }
    catch { setAlert({ type: 'error', text: 'Failed to load review details.' }); }
  };

  const decide = async (decision) => {
    if (feedback.trim().length < 5) return setAlert({ type: 'error', text: 'Enter at least 5 characters of professional feedback.' });
    setActionLoading(true);
    try {
      await counsellorReviewApi.submitDecision(selected.id, decision, feedback.trim());
      setAlert({ type: 'success', text: `Decision recorded: ${decision}. The student status is updated.` });
      setSelected(null); setFeedback(''); await loadReviews();
    } catch (error) { setAlert({ type: 'error', text: getApiErrorMessage(error, 'Decision could not be recorded.') }); }
    finally { setActionLoading(false); }
  };

  const skills = parseJson(selected?.missingSkillsJson);
  const validations = parseJson(selected?.validationResultsJson);
  const tools = parseJson(selected?.toolCallsJson);
  const trace = parseJson(selected?.executionTraceJson);
  const subjects = parseJson(selected?.subjectRequirementsJson);
  const entryRequirements = parseJson(selected?.entryRequirementsJson);
  const gapPlan = parseJson(selected?.gapClosurePlanJson);
  const evidenceSources = parseJson(selected?.evidenceSourcesJson);

  return (
    <div className="review-page">
      <header className="review-header"><div><h1>Counsellor Approval Centre</h1><p>Review Agent 4 feasibility evidence before a high-impact pathway is released.</p></div><span className="queue-badge">{result.totalCount} results</span></header>
      {alert && <div role="alert" className={`review-alert ${alert.type}`}>{alert.text}</div>}

      <section className="review-filters" aria-label="Review filters">
        <input aria-label="Search reviews" placeholder="Search career or workflow ID" value={filters.search} onChange={(e) => updateFilter('search', e.target.value)} />
        <select aria-label="Filter status" value={filters.status} onChange={(e) => updateFilter('status', e.target.value)}>
          {['Pending', 'Approved', 'Rejected', 'NeedsRevision', 'All'].map((s) => <option key={s}>{s}</option>)}
        </select>
        <select aria-label="Sort reviews" value={filters.sort} onChange={(e) => updateFilter('sort', e.target.value)}>
          <option value="newest">Newest first</option><option value="oldest">Oldest first</option><option value="score">Lowest feasibility</option>
        </select>
        <button onClick={loadReviews}>Refresh</button>
      </section>

      {loading ? <p className="review-state">Loading verified workflow records…</p> : result.items.length === 0 ? <p className="review-state">No reviews match these filters.</p> : (
        <div className="review-layout">
          <section className="review-list" aria-label="Review queue">
            {result.items.map((review) => <button key={review.id} className={`review-card ${selected?.id === review.id ? 'selected' : ''}`} onClick={() => chooseReview(review)}>
              <span><strong>{review.targetCareer}</strong><small>{review.workflowId}</small></span>
              <span className={`status ${review.status.toLowerCase()}`}>{review.status}</span>
              <span className="score">Feasibility {review.feasibilityScore}%</span>
              <small>{new Date(review.createdAt).toLocaleString()}</small>
            </button>)}
            <nav className="pagination" aria-label="Review pages"><button disabled={filters.page <= 1} onClick={() => updateFilter('page', filters.page - 1)}>Previous</button><span>Page {filters.page} of {Math.max(1, result.totalPages)}</span><button disabled={filters.page >= result.totalPages} onClick={() => updateFilter('page', filters.page + 1)}>Next</button></nav>
          </section>

          <section className="review-detail">
            {!selected ? <p className="review-state">Select a workflow to inspect evidence.</p> : <>
              <div className="detail-title"><div><h2>{selected.targetCareer}</h2><code>{selected.workflowId}</code></div><strong>{selected.feasibilityScore}%</strong></div>
              {selected.isHighRisk && <div className="risk-box"><strong>High-risk flag</strong><p>{selected.riskReason}</p></div>}
              <h3>Feasibility summary</h3><p>{selected.feasibilitySummary}</p>
              <h3>Degree or qualification requirement</h3><p>{selected.degreeRequirement || 'Requires counsellor verification.'}</p>
              <h3>Subject requirements</h3><div className="chip-row">{subjects.length ? subjects.map((s) => <span key={s}>{s}</span>) : <em>Not verified</em>}</div>
              <h3>Entry requirements</h3><ul>{entryRequirements.map((item) => <li key={item}>{item}</li>)}</ul>
              <h3>Cost guidance</h3><p>{selected.costGuidance || 'Cost evidence requires verification.'}</p>
              <h3>Missing prerequisites</h3><div className="chip-row">{skills.length ? skills.map((s) => <span key={s}>{s}</span>) : <em>No major gap</em>}</div>
              <h3>Shortest gap-closing plan</h3><ol>{gapPlan.map((item) => <li key={item}>{item}</li>)}</ol>
              <details><summary>Agent 4 audit evidence</summary><h4>Evidence sources</h4><ul>{evidenceSources.map((v) => <li key={v}>{v}</li>)}</ul><h4>Validation</h4><ul>{validations.map((v) => <li key={v}>{v}</li>)}</ul><h4>Controlled tools</h4><ul>{tools.map((t, i) => <li key={`${pick(t, 'tool_name', 'toolName')}-${i}`}>{pick(t, 'tool_name', 'toolName')}: {t.status} ({pick(t, 'duration_ms', 'durationMs')} ms)</li>)}</ul><h4>Execution trace</h4><ol>{trace.map((t, i) => <li key={`${t.step}-${i}`}>{t.step}: {t.status} ({pick(t, 'duration_ms', 'durationMs')} ms)</li>)}</ol></details>
              {selected.status === 'Pending' && <div className="decision-panel"><label htmlFor="feedback">Required counsellor feedback</label><textarea id="feedback" rows="4" maxLength="1000" value={feedback} onChange={(e) => setFeedback(e.target.value)} /><small>{feedback.length}/1000</small><div className="decision-buttons"><button disabled={actionLoading} className="approve" onClick={() => decide('Approved')}>Approve</button><button disabled={actionLoading} className="revise" onClick={() => decide('NeedsRevision')}>Request revision</button><button disabled={actionLoading} className="reject" onClick={() => decide('Rejected')}>Reject</button></div></div>}
            </>}
          </section>
        </div>
      )}
    </div>
  );
}
