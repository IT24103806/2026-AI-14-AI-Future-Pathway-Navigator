import React, { useState } from 'react';

const STATUS_LABELS = {
  Open: 'Waiting for a consultant',
  Claimed: 'A consultant picked this up',
  InProgress: 'Your consultant is working on it',
  AwaitingStudent: 'Waiting for your reply',
  Answered: 'Answered',
  Resolved: 'Resolved',
  Closed: 'Closed',
  Escalated: 'Escalated for an approval review',
};

const parseGuidance = (json) => {
  try {
    return JSON.parse(json || '{}');
  } catch {
    return {};
  }
};

/**
 * Shared thread renderer. `variant` switches between the student view (reply + close) and the
 * consultant view (internal notes are rendered, but that flag can never come from the student API).
 *
 * Internal messages are rendered only when the API actually returned them - the server filters them
 * out for students, so this is presentation, not the security boundary.
 */
const ConsultationThread = ({
  consultation,
  variant = 'student',
  onSend,
  onClose,
  onReopen,
  busy = false,
}) => {
  const [draft, setDraft] = useState('');
  const [rating, setRating] = useState(0);
  const [feedback, setFeedback] = useState('');

  if (!consultation) return null;

  const guidance = parseGuidance(consultation.consultantGuidanceJson);
  const isClosed = consultation.status === 'Closed';

  const send = async (event) => {
    event.preventDefault();
    if (draft.trim().length < 2) return;
    const sent = await onSend?.(draft.trim());
    if (sent !== false) setDraft('');
  };

  return (
    <div className={`thread thread--${variant}`}>
      <header className="thread__head">
        <div>
          <h3>{consultation.subject}</h3>
          <p className="thread__meta">
            <span className={`status ${consultation.status.toLowerCase()}`}>
              {STATUS_LABELS[consultation.status] ?? consultation.status}
            </span>
            <span> · {consultation.contextSummary}</span>
            {consultation.priority ? <span> · {consultation.priority}</span> : null}
          </p>
        </div>
        {consultation.assignedConsultantName && (
          <span className="thread__owner">Consultant: {consultation.assignedConsultantName}</span>
        )}
      </header>

      {consultation.isSlaBreached && !isClosed && (
        <div className="review-alert" role="status">
          This question has passed its response target. It has been flagged to the consultant team.
        </div>
      )}

      <ol className="thread__messages">
        {(consultation.messages ?? []).map((message) => (
          <li key={message.id} className={`thread__message thread__message--${message.authorRole.toLowerCase()} ${message.isInternal ? 'is-internal' : ''}`}>
            <div className="thread__message-head">
              <strong>{message.authorName}</strong>
              <span>{new Date(message.createdAt).toLocaleString()}</span>
              {message.isInternal && <span className="thread__internal-tag">internal</span>}
            </div>
            <p>{message.body}</p>
            {(message.resources ?? []).length > 0 && (
              <ul className="thread__resources">
                {message.resources.map((resource) => (
                  <li key={resource.url}>
                    <a href={resource.url} target="_blank" rel="noreferrer noopener">🔗 {resource.label}</a>
                  </li>
                ))}
              </ul>
            )}
          </li>
        ))}
      </ol>

      {guidance.note && (
        <section className="thread__guidance" aria-label="Consultant guidance">
          <h4>Consultant guidance</h4>
          <p>{guidance.note}</p>
          {(guidance.nextSteps ?? []).length > 0 && (
            <>
              <h5>Next steps</h5>
              <ol>
                {guidance.nextSteps.map((step) => <li key={step}>{step}</li>)}
              </ol>
            </>
          )}
          {(guidance.checklist ?? []).length > 0 && (
            <ul className="thread__checklist">
              {guidance.checklist.map((item) => <li key={item}>{item}</li>)}
            </ul>
          )}
        </section>
      )}

      <footer className="thread__footer">
        {isClosed ? (
          <div className="thread__closed">
            <p>
              Closed{consultation.resolutionSummary ? `: ${consultation.resolutionSummary}` : '.'}
              {consultation.studentRating ? ` You rated this ${consultation.studentRating}/5.` : ''}
            </p>
            {variant === 'student' && onReopen && (
              <button type="button" className="btn btn-glass" onClick={() => onReopen(consultation.id)} disabled={busy}>
                Reopen within 7 days
              </button>
            )}
          </div>
        ) : (
          <>
            {variant === 'student' && (
              <form className="thread__reply" onSubmit={send}>
                <label htmlFor="thread-reply">Continue the conversation</label>
                <textarea
                  id="thread-reply"
                  rows={3}
                  maxLength={4000}
                  value={draft}
                  onChange={(event) => setDraft(event.target.value)}
                  placeholder="Add anything the consultant should know…"
                />
                <button type="submit" className="btn btn-primary" disabled={busy || draft.trim().length < 2}>
                  Send
                </button>
              </form>
            )}

            {variant === 'student' && onClose && consultation.status === 'Answered' && (
              <div className="thread__close-box">
                <p>Did this answer your question?</p>
                <div className="thread__rating" role="group" aria-label="Rate this answer">
                  {[1, 2, 3, 4, 5].map((value) => (
                    <button
                      key={value}
                      type="button"
                      className={value <= rating ? 'is-selected' : ''}
                      onClick={() => setRating(value)}
                      aria-label={`${value} out of 5`}
                    >
                      ★
                    </button>
                  ))}
                </div>
                <input
                  aria-label="Optional feedback"
                  value={feedback}
                  maxLength={1000}
                  onChange={(event) => setFeedback(event.target.value)}
                  placeholder="Optional: what helped most?"
                />
                <div className="thread__close-actions">
                  <button type="button" className="btn btn-primary" disabled={busy} onClick={() => onClose(consultation.id, { rating: rating || undefined, feedback: feedback.trim() || undefined })}>
                    Confirm resolved
                  </button>
                  <button type="button" className="btn btn-glass" disabled={busy} onClick={() => onClose(consultation.id, {})}>
                    Close without rating
                  </button>
                </div>
              </div>
            )}
          </>
        )}
      </footer>
    </div>
  );
};

export default ConsultationThread;
