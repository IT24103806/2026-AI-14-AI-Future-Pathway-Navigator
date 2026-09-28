import React, { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { counsellorReviewApi } from '../api/counsellorReviewApi';
import GapClosureTasks from '../components/GapClosureTasks';

const parseList = (value) => { if (Array.isArray(value)) return value; try { return JSON.parse(value || '[]'); } catch { return []; } };
const statusCopy = {
  Pending: ['Waiting for counsellor', 'Your high-impact pathway is paused until an authorized counsellor reviews it.'],
  Approved: ['Approved', 'The counsellor approved this Reality Check result.'],
  Rejected: ['Rejected', 'Review the counsellor feedback before choosing another path.'],
  NeedsRevision: ['Revision requested', 'Update the pathway using the counsellor feedback and run the Reality Check again.'],
  Failed: ['Reality Check failed', 'This attempt was recorded safely. Return to Career Discovery and try again.'],
};

export default function StudentRealityCheckPage() {
  const [status, setStatus] = useState(null); const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  const load = useCallback(async () => {
    setLoading(true); setError('');
    try { const [latest, past] = await Promise.all([counsellorReviewApi.getMyStatus().catch((err) => { if (err.response?.status === 404) return null; throw err; }), counsellorReviewApi.getMyHistory()]); setHistory(past); setStatus(latest || past[0] || null); }
    catch (err) { if (err.response?.status === 404) setStatus(null); else setError(err.response?.data?.message || 'Unable to load your Reality Check result.'); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { load(); }, [load]);
  if (loading) return <div className="reality-student-page"><p className="review-state">Loading Agent 4 evidence…</p></div>;
  if (error) return <div className="reality-student-page"><div className="review-alert error" role="alert">{error}</div><button className="btn btn-primary" onClick={load}>Retry</button></div>;
  if (!status) return <div className="reality-student-page"><section className="reality-empty"><span>🧭</span><h1>No Reality Check yet</h1><p>Choose a career pathway first. Agent 4 will then check whether it is achievable for your results, subjects, budget and current skills.</p><Link className="btn btn-primary" to="/career-discovery">Choose a career pathway</Link></section></div>;
  const missing = parseList(status.missingSkillsJson), subjects = parseList(status.subjectRequirementsJson), entries = parseList(status.entryRequirementsJson), plan = parseList(status.gapClosurePlanJson);
  const [statusTitle, statusText] = statusCopy[status.status] || [status.status, 'Your pathway status has been updated.'];
  return <div className="reality-student-page">
    <header className="student-reality-header"><div><span className="agent-badge">🛡️ Agent 4 · Reality Check</span><h1>{status.targetCareer}</h1><p>Workflow <code>{status.workflowId}</code></p></div><button className="btn btn-outline-primary" onClick={load}>Refresh status</button></header>
    <section className={`student-status-banner ${status.status.toLowerCase()}`}><div><strong>{statusTitle}</strong><p>{statusText}</p></div><span>{status.status}</span></section>
    <div className="student-reality-grid">
      <section className="reality-panel score-panel"><h2>Feasibility score</h2><div className="large-score">{status.feasibilityScore}<small>/100</small></div><progress max="100" value={status.feasibilityScore} /></section>
      <section className="reality-panel"><h2>Agent 4 summary</h2><p>{status.feasibilitySummary || 'The pathway was checked against available evidence.'}</p>{status.isHighRisk && <div className="student-risk"><strong>Risk requiring review</strong><p>{status.riskReason}</p></div>}</section>
      <section className="reality-panel"><h2>Degree or qualification</h2><p>{status.degreeRequirement || 'Counsellor verification is required.'}</p><h3>Required subjects</h3>{subjects.length ? <ul>{subjects.map((x) => <li key={x}>{x}</li>)}</ul> : <p className="muted">No verified subjects listed.</p>}</section>
      <section className="reality-panel"><h2>Entry requirements</h2>{entries.length ? <ul>{entries.map((x) => <li key={x}>{x}</li>)}</ul> : <p className="muted">No verified entry requirements listed.</p>}<h3>Cost guidance</h3><p>{status.costGuidance || 'Verify the total cost before making a commitment.'}</p></section>
      <section className="reality-panel"><h2>Missing skills</h2>{missing.length ? <div className="chip-row">{missing.map((x) => <span key={x}>{x}</span>)}</div> : <p>No major prerequisite skill gap was identified.</p>}</section>
      <section className="reality-panel"><h2>Shortest gap-closing plan</h2>{plan.length ? <ol>{plan.map((x) => <li key={x}>{x}</li>)}</ol> : <p className="muted">The plan will appear after the next Reality Check.</p>}</section>
    </div>
    <GapClosureTasks reviewId={status.id} />
    <section className="reality-panel counsellor-feedback"><h2>👩‍🏫 Counsellor feedback</h2><p>{status.counsellorFeedback || (status.status === 'Pending' ? 'Waiting for an authorized counsellor decision.' : 'No feedback was provided.')}</p></section>
    <section className="reality-panel"><h2>Review history</h2>{history.length === 0 ? <p className="muted">No previous reviews.</p> : <div className="student-history">{history.map((item) => <div key={item.id}><span className={`history-dot ${item.status.toLowerCase()}`} /><div><strong>{item.targetCareer}</strong><small>{new Date(item.createdAt).toLocaleString()}</small></div><b>{item.status}</b></div>)}</div>}</section>
  </div>;
}
