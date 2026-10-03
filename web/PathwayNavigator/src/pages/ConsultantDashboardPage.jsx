import React, { useCallback, useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import consultantApi from '../api/consultantApi';
import { getApiErrorMessage } from '../utils/apiError';
import ConsultationThread from '../components/consultation/ConsultationThread';

const SCOPES = ['Open', 'Mine', 'Unresolved', 'All'];
const CATEGORIES = ['', 'GuideRequest', 'DoubtAnswer', 'RealityCheckClarification', 'PathwayAdvice', 'MarketCourseInfo', 'Unclassified'];
const PRIORITIES = ['', 'P1', 'P2', 'P3'];
const CONTEXTS = ['', 'CareerDiscovery', 'PathwayPlan', 'RealityCheck', 'General'];

const slaLabel = (consultation) => {
  if (consultation.closedAt) return 'closed';
  const due = new Date(consultation.slaDueAt).getTime();
  const hours = Math.round((due - Date.now()) / 36e5);
  if (hours < 0) return `${Math.abs(hours)}h overdue`;
  if (hours < 24) return `${hours}h left`;
  return `${Math.round(hours / 24)}d left`;
};

/**
 * The Consultant Desk.
 *
 * Three panes: the queue (what needs attention, ordered by SLA), the case (frozen context + Agent 5
 * case brief + thread) and the composer (Agent 5 draft, resources, structured guidance, reply).
 *
 * The AI is assistive throughout: the brief and the draft are always editable, the draft is never sent
 * automatically, and every send is a deliberate click by the consultant.
 */
const ConsultantDashboardPage = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const [filters, setFilters] = useState({ scope: 'Open', category: '', priority: '', contextType: '', search: '', sort: 'sla', page: 1, pageSize: 10 });
  const [queue, setQueue] = useState({ items: [], totalCount: 0, totalPages: 0 });
  const [selected, setSelected] = useState(null);
  const [brief, setBrief] = useState(null);
  const [stats, setStats] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [alert, setAlert] = useState(null);
  const [tab, setTab] = useState('case');
  // Guards the `?consultation=` deep link so it runs once per case: without it, opening a case would
  // re-fetch it through the effect and snap the consultant back to the Case tab mid-conversation.
  const [handledDeepLink, setHandledDeepLink] = useState(null);

  const [reply, setReply] = useState({ message: '', closeAfterReply: false, resolutionSummary: '', stageKey: '', note: '', checklist: '', resourceLabel: '', resourceUrl: '' });
  const [internalNote, setInternalNote] = useState('');

  const loadQueue = useCallback(async () => {
    setIsLoading(true);
    try {
      setQueue(await consultantApi.getQueue(filters));
    } catch (error) {
      setAlert({ type: 'error', text: getApiErrorMessage(error, 'Failed to load the consultant queue.') });
    } finally {
      setIsLoading(false);
    }
  }, [filters]);

  useEffect(() => { loadQueue(); }, [loadQueue]);
  useEffect(() => { consultantApi.getStats().then(setStats).catch(() => setStats(null)); }, []);

  // Deep link from a notification: open that exact case (once).
  useEffect(() => {
    const caseId = searchParams.get('consultation');
    if (!caseId || caseId === handledDeepLink) return;
    setHandledDeepLink(caseId);
    consultantApi.getCase(caseId)
      .then((result) => { setSelected(result); setTab('case'); })
      .catch(() => setAlert({ type: 'error', text: 'That consultation could not be opened.' }));
  }, [searchParams, handledDeepLink]);

  const openCase = async (id) => {
    setBusy(true);
    setBrief(null);
    try {
      const result = await consultantApi.getCase(id);
      setSelected(result);
      setTab('case');
      setHandledDeepLink(id);
      setSearchParams({ consultation: id }, { replace: true });
      consultantApi.getBrief(id).then(setBrief).catch(() => setBrief(null));
    } catch (error) {
      setAlert({ type: 'error', text: getApiErrorMessage(error, 'Failed to load the case.') });
    } finally {
      setBusy(false);
    }
  };

  const runAction = async (action, successText) => {
    setBusy(true);
    try {
      const updated = await action();
      if (updated) setSelected(updated);
      setAlert({ type: 'success', text: successText });
      await loadQueue();
      return true;
    } catch (error) {
      setAlert({ type: 'error', text: getApiErrorMessage(error, 'That action could not be completed.') });
      return false;
    } finally {
      setBusy(false);
    }
  };

  const insertDraft = async (tone) => {
    setBusy(true);
    try {
      const draft = await consultantApi.getDraft(selected.id, tone);
      if (!draft?.body) {
        setAlert({ type: 'error', text: 'No draft is available right now — please write your reply.' });
        return;
      }
      // `usedDraft` records that the reply *started* from an Agent 5 draft; editing it afterwards is
      // the expected behaviour and does not clear the metric.
      setReply((current) => ({ ...current, message: draft.body, usedDraft: true }));
      if (draft.must_escalate) {
        setAlert({ type: 'error', text: `Review before sending: ${draft.safety_notes?.join(' ') || 'this case must be escalated.'}` });
      } else {
        setAlert({ type: 'success', text: 'Draft inserted. Edit it in your own words before sending.' });
      }
    } finally {
      setBusy(false);
    }
  };

  const sendReply = async (event) => {
    event.preventDefault();
    if (reply.message.trim().length < 2) {
      setAlert({ type: 'error', text: 'Write a reply before sending.' });
      return;
    }
    const resources = reply.resourceUrl.trim()
      ? [{ label: reply.resourceLabel.trim() || reply.resourceUrl.trim(), url: reply.resourceUrl.trim(), kind: 'guide' }]
      : [];
    const guidance = (reply.stageKey.trim() || reply.note.trim() || reply.checklist.trim())
      ? {
          stageKey: reply.stageKey.trim() || null,
          note: reply.note.trim() || null,
          resources,
          checklist: reply.checklist.split('\n').map((line) => line.trim()).filter(Boolean),
        }
      : null;

    const ok = await runAction(
      () => consultantApi.reply(selected.id, {
        message: reply.message.trim(),
        resources,
        guidance,
        resolutionSummary: reply.resolutionSummary.trim() || null,
        closeAfterReply: reply.closeAfterReply,
        usedAgentDraft: Boolean(reply.usedDraft),
      }),
      reply.closeAfterReply ? 'Reply sent and the case is closed.' : 'Reply sent — the student has been notified.',
    );
    if (ok) setReply({ message: '', closeAfterReply: false, resolutionSummary: '', stageKey: '', note: '', checklist: '', resourceLabel: '', resourceUrl: '' });
  };

  const updateFilter = (key, value) => setFilters((current) => ({ ...current, [key]: value, page: key === 'page' ? value : 1 }));
  const isMine = selected && selected.assignedConsultantId && !selected.closedAt;

  return (
    <div className="desk-page">
      <header className="desk-header">
        <div>
          <h1>Consultant Desk</h1>
          <p>Answer student questions with their pathway, roadmap and Reality Check evidence in view.</p>
        </div>
        {stats && (
          <div className="desk-stats">
            <span><strong>{stats.unclaimedPool}</strong> unclaimed</span>
            <span><strong>{stats.assignedToMe}</strong> mine</span>
            <span><strong>{stats.slaCompliancePercent}%</strong> on time</span>
            <span><strong>{stats.averageRating || '—'}</strong> rating</span>
          </div>
        )}
      </header>

      {alert && <div role="alert" className={`review-alert ${alert.type}`}>{alert.text}</div>}

      <section className="review-filters" aria-label="Queue filters">
        <select aria-label="Queue scope" value={filters.scope} onChange={(event) => updateFilter('scope', event.target.value)}>
          {SCOPES.map((scope) => <option key={scope} value={scope}>{scope === 'Open' ? 'Unclaimed pool' : scope}</option>)}
        </select>
        <select aria-label="Filter category" value={filters.category} onChange={(event) => updateFilter('category', event.target.value)}>
          {CATEGORIES.map((category) => <option key={category} value={category}>{category || 'All categories'}</option>)}
        </select>
        <select aria-label="Filter priority" value={filters.priority} onChange={(event) => updateFilter('priority', event.target.value)}>
          {PRIORITIES.map((priority) => <option key={priority} value={priority}>{priority || 'All priorities'}</option>)}
        </select>
        <select aria-label="Filter context" value={filters.contextType} onChange={(event) => updateFilter('contextType', event.target.value)}>
          {CONTEXTS.map((context) => <option key={context} value={context}>{context || 'All contexts'}</option>)}
        </select>
        <input aria-label="Search the queue" placeholder="Search subject or student" value={filters.search} onChange={(event) => updateFilter('search', event.target.value)} />
        <button type="button" onClick={loadQueue}>Refresh</button>
      </section>

      {isLoading ? (
        <p className="review-state">Loading the queue…</p>
      ) : queue.items.length === 0 ? (
        <p className="review-state">Nothing here. The pool is clear with these filters.</p>
      ) : (
        <div className="review-layout">
          <section className="review-list" aria-label="Consultation queue">
            {queue.items.map((item) => (
              <button
                key={item.id}
                type="button"
                className={`review-card ${selected?.id === item.id ? 'selected' : ''}`}
                onClick={() => openCase(item.id)}
              >
                <span>
                  <strong>{item.subject}</strong>
                  <small>{item.studentName} · {item.contextSummary}</small>
                </span>
                <span className={`status ${item.status.toLowerCase()}`}>{item.status}</span>
                <span className={`score ${item.isSlaBreached ? 'is-breached' : ''}`}>{item.priority} · {slaLabel(item)}</span>
                <small>{item.category}</small>
              </button>
            ))}
            <nav className="pagination" aria-label="Queue pages">
              <button type="button" disabled={filters.page <= 1} onClick={() => updateFilter('page', filters.page - 1)}>Previous</button>
              <span>Page {filters.page} of {Math.max(1, queue.totalPages)}</span>
              <button type="button" disabled={filters.page >= queue.totalPages} onClick={() => updateFilter('page', filters.page + 1)}>Next</button>
            </nav>
          </section>

          <section className="review-detail">
            {!selected ? (
              <p className="review-state">Select a question to see the student&rsquo;s context and reply.</p>
            ) : (
              <>
                <div className="detail-title">
                  <div>
                    <h2>{selected.subject}</h2>
                    <code>{selected.contextSummary}</code>
                  </div>
                  <span className={`status ${selected.status.toLowerCase()}`}>{selected.status}</span>
                </div>

                <div className="desk-actions">
                  {!selected.assignedConsultantId && (
                    <button type="button" className="btn btn-primary" disabled={busy} onClick={() => runAction(() => consultantApi.claim(selected.id), 'Case claimed.')}>Claim</button>
                  )}
                  {isMine && (
                    <button type="button" className="btn btn-glass" disabled={busy} onClick={() => runAction(() => consultantApi.release(selected.id, ''), 'Case returned to the pool.')}>Release</button>
                  )}
                  <select
                    aria-label="Priority"
                    value={selected.priority}
                    disabled={busy}
                    onChange={(event) => runAction(() => consultantApi.updatePriority(selected.id, event.target.value), 'Priority updated.')}
                  >
                    {['P1', 'P2', 'P3'].map((priority) => <option key={priority} value={priority}>{priority}</option>)}
                  </select>
                  <button
                    type="button"
                    className="btn btn-outline-danger"
                    disabled={busy || selected.status === 'Escalated'}
                    onClick={() => runAction(() => consultantApi.escalate(selected.id, 'Needs a counsellor decision.'), 'Escalated to the counsellor queue.')}
                  >
                    Escalate to counsellor
                  </button>
                </div>

                <nav className="desk-tabs" aria-label="Case views">
                  <button type="button" className={tab === 'case' ? 'is-active' : ''} onClick={() => setTab('case')}>Case</button>
                  <button type="button" className={tab === 'thread' ? 'is-active' : ''} onClick={() => setTab('thread')}>Conversation</button>
                  <button type="button" className={tab === 'notes' ? 'is-active' : ''} onClick={() => setTab('notes')}>Internal notes</button>
                </nav>

                {tab === 'case' && (
                  <div className="desk-case">
                    <h3>The student&rsquo;s question</h3>
                    <p className="desk-question">{selected.body}</p>

                    <h3>Frozen context</h3>
                    <ConsultationContextSummary summary={selected.contextSummary} snapshot={selected.contextSnapshotJson} />

                    <h3>Agent 5 case brief</h3>
                    {!brief ? (
                      <p className="review-state">No brief available — the copilot may be offline. The evidence above is enough to reply.</p>
                    ) : (
                      <div className="desk-brief">
                        <p className="desk-brief__headline">{brief.headline} · confidence {(brief.confidence * 100).toFixed(0)}%</p>
                        {brief.sections.map((section) => (
                          <div key={section.title} className="desk-brief__section">
                            <h4>{section.title}</h4>
                            <ul>{section.points.map((point) => <li key={point}>{point}</li>)}</ul>
                          </div>
                        ))}
                      </div>
                    )}

                    {selected.agentTriage && (
                      <details className="desk-triage">
                        <summary>Agent 5 triage evidence</summary>
                        {/* The DTO is serialised with its snake_case [JsonPropertyName] attributes. */}
                        <ul>
                          <li>Category: {selected.agentTriage.category}</li>
                          <li>Priority: {selected.agentTriage.priority} · SLA {selected.agentTriage.suggested_sla_hours}h</li>
                          <li>Expertise: {(selected.agentTriage.expertise_tags ?? []).join(', ') || 'general'}</li>
                          <li>Language: {selected.agentTriage.language} · sentiment: {selected.agentTriage.sentiment}</li>
                          <li>Confidence: {selected.agentTriage.confidence}</li>
                          {(selected.agentTriage.safety_flags ?? []).length > 0 && (
                            <li className="is-warning">Safety flags: {selected.agentTriage.safety_flags.join(', ')}</li>
                          )}
                        </ul>
                      </details>
                    )}
                  </div>
                )}

                {tab === 'thread' && (
                  <ConsultationThread consultation={selected} variant="consultant" />
                )}

                {tab === 'notes' && (
                  <div className="desk-notes">
                    <p className="review-state">Internal notes are never shown to the student.</p>
                    <form
                      onSubmit={async (event) => {
                        event.preventDefault();
                        if (internalNote.trim().length < 2) return;
                        const ok = await runAction(() => consultantApi.addNote(selected.id, internalNote.trim()), 'Internal note saved.');
                        if (ok) setInternalNote('');
                      }}
                    >
                      <textarea aria-label="Internal note" rows={3} maxLength={4000} value={internalNote} onChange={(event) => setInternalNote(event.target.value)} />
                      <button type="submit" className="btn btn-glass" disabled={busy}>Add note</button>
                    </form>
                    <ul className="desk-notes__list">
                      {(selected.messages ?? []).filter((message) => message.isInternal).map((message) => (
                        <li key={message.id}><strong>{message.authorName}</strong> · {new Date(message.createdAt).toLocaleString()}<p>{message.body}</p></li>
                      ))}
                    </ul>
                  </div>
                )}

                {!selected.closedAt && (
                  <form className="decision-panel" onSubmit={sendReply}>
                    <label htmlFor="desk-reply">Your reply</label>
                    <div className="desk-draft">
                      <span>Draft assistance (Agent 5):</span>
                      <button type="button" className="btn btn-glass" disabled={busy} onClick={() => insertDraft('Supportive')}>Supportive</button>
                      <button type="button" className="btn btn-glass" disabled={busy} onClick={() => insertDraft('Direct')}>Direct</button>
                      <button type="button" className="btn btn-glass" disabled={busy} onClick={() => insertDraft('Detailed')}>Detailed</button>
                    </div>
                    <textarea
                      id="desk-reply"
                      rows={7}
                      maxLength={4000}
                      value={reply.message}
                      onChange={(event) => setReply((current) => ({ ...current, message: event.target.value }))}
                      placeholder="Write to the student. Answer the question, then say what to do next."
                    />

                    <fieldset className="desk-compose">
                      <legend>Attach a guide (optional)</legend>
                      <input aria-label="Resource label" placeholder="Label" value={reply.resourceLabel} onChange={(event) => setReply((current) => ({ ...current, resourceLabel: event.target.value }))} />
                      <input aria-label="Resource url" placeholder="https://…" value={reply.resourceUrl} onChange={(event) => setReply((current) => ({ ...current, resourceUrl: event.target.value }))} />
                    </fieldset>

                    <fieldset className="desk-compose">
                      <legend>Write-back guidance (optional)</legend>
                      <input aria-label="Roadmap stage key" placeholder="Stage key (e.g. skills) — attaches this to that milestone" value={reply.stageKey} onChange={(event) => setReply((current) => ({ ...current, stageKey: event.target.value }))} />
                      <textarea aria-label="Guidance note" rows={2} placeholder="Short note shown inside the student's pathway" value={reply.note} onChange={(event) => setReply((current) => ({ ...current, note: event.target.value }))} />
                      <textarea aria-label="Checklist" rows={3} placeholder="One checklist item per line" value={reply.checklist} onChange={(event) => setReply((current) => ({ ...current, checklist: event.target.value }))} />
                      <input aria-label="Resolution summary" placeholder="Resolution summary (shown when closed)" value={reply.resolutionSummary} onChange={(event) => setReply((current) => ({ ...current, resolutionSummary: event.target.value }))} />
                    </fieldset>

                    <label className="desk-close-toggle">
                      <input type="checkbox" checked={reply.closeAfterReply} onChange={(event) => setReply((current) => ({ ...current, closeAfterReply: event.target.checked }))} />
                      Send and close this question
                    </label>

                    <div className="decision-buttons">
                      <button type="submit" className="approve" disabled={busy}>Send reply to student</button>
                    </div>
                    <p className="desk-disclaimer">
                      Sending notifies the student and attaches your guidance to their pathway. Approving or rejecting a
                      pathway stays with a counsellor.
                    </p>
                  </form>
                )}
              </>
            )}
          </section>
        </div>
      )}
    </div>
  );
};

/** Renders the frozen snapshot compactly; falls back to the server-provided summary. */
const ConsultationContextSummary = ({ summary, snapshot }) => {
  let parsed = null;
  try {
    parsed = JSON.parse(snapshot || '{}');
  } catch {
    parsed = null;
  }

  if (!parsed || Object.keys(parsed).length === 0) {
    return <p>{summary || 'No context attached.'}</p>;
  }

  return (
    <div className="desk-context">
      <p>{summary}</p>
      <ul>
        {Object.entries(parsed).map(([key, value]) => (
          <li key={key}>
            <strong>{key}</strong>: {Array.isArray(value) ? value.join(', ') : String(value)}
          </li>
        ))}
      </ul>
    </div>
  );
};

export default ConsultantDashboardPage;
