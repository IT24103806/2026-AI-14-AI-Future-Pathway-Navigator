import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import consultationApi from '../../api/consultationApi';

/**
 * Context-aware request dialog used from every journey anchor (pathway card, roadmap milestone,
 * Reality Check panel, dashboard).
 *
 * Two product rules are baked in here:
 *   1. The student always keeps a way to reach a human - FAQ suggestions are offered *alongside*
 *      the form, never instead of it.
 *   2. If an open request already exists for this exact context, the student is pointed at that
 *      thread instead of silently creating a duplicate.
 */
const AskConsultantDialog = ({
  contextType = 'General',
  contextRefId = null,
  contextLabel = '',
  prefillSubject = '',
  prefillBody = '',
  onClose,
  onCreated,
}) => {
  const [subject, setSubject] = useState(prefillSubject);
  const [body, setBody] = useState(prefillBody);
  const [faqMatches, setFaqMatches] = useState([]);
  const [existing, setExisting] = useState(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    let cancelled = false;
    const loadContext = async () => {
      if (contextRefId) {
        const status = await consultationApi.getContextStatus(contextType, contextRefId);
        if (!cancelled && status?.hasOpenRequest) setExisting(status);
      }
    };
    loadContext();
    return () => { cancelled = true; };
  }, [contextType, contextRefId]);

  // FAQ lookup runs as the student types (debounced) so it feels like a suggestion, not a step.
  useEffect(() => {
    if (subject.trim().length < 8) {
      setFaqMatches([]);
      return undefined;
    }
    const timer = setTimeout(async () => {
      const result = await consultationApi.matchFaq(subject.trim());
      setFaqMatches((result?.matches ?? []).slice(0, 3));
    }, 600);
    return () => clearTimeout(timer);
  }, [subject]);

  const submit = async (event) => {
    event.preventDefault();
    if (subject.trim().length < 4) return setError('Please summarise your question in a few words.');
    if (body.trim().length < 10) return setError('Please add a little more detail so the consultant can help.');
    setIsSubmitting(true);
    setError('');
    try {
      const created = await consultationApi.create({
        contextType,
        contextRefId,
        subject: subject.trim(),
        body: body.trim(),
      });
      onCreated?.(created);
      onClose?.();
    } catch (submitError) {
      setError(consultationApi.describeError(submitError, 'Your question could not be submitted.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="consult-modal" role="dialog" aria-modal="true" aria-label="Ask a consultant">
      <div className="consult-modal__card">
        <header className="consult-modal__head">
          <div>
            <h2>🙋 Ask a consultant</h2>
            <p>
              A real consultant answers here.
              {contextLabel ? <span className="consult-modal__context"> · about: {contextLabel}</span> : null}
            </p>
          </div>
          <button type="button" className="consult-modal__close" onClick={onClose} aria-label="Close">✕</button>
        </header>

        {existing && (
          <div className="review-alert" role="note">
            You already have an open question here: “{existing.subject}”.
            {' '}
            <Link to={`/student/support?consultation=${existing.consultationId}`}>Open that conversation</Link>
            {' '}— you can also send this as a new question.
          </div>
        )}

        {faqMatches.length > 0 && (
          <section className="consult-modal__faq" aria-label="Similar answered questions">
            <h3>Someone already asked something similar</h3>
            {faqMatches.map((match) => (
              <details key={match.question}>
                <summary>{match.question}</summary>
                <p>{match.answer}</p>
              </details>
            ))}
            <p className="consult-modal__faq-note">Still unsure? Send your question below — a consultant will reply.</p>
          </section>
        )}

        <form onSubmit={submit} className="consult-modal__form">
          <label htmlFor="consult-subject">Your question in one line</label>
          <input
            id="consult-subject"
            value={subject}
            maxLength={140}
            onChange={(event) => setSubject(event.target.value)}
            placeholder="e.g. Which of these two pathways fits my budget better?"
          />

          <label htmlFor="consult-body">Details</label>
          <textarea
            id="consult-body"
            rows={5}
            maxLength={4000}
            value={body}
            onChange={(event) => setBody(event.target.value)}
            placeholder="Explain what you are unsure about. The consultant can already see your pathway and results."
          />

          {error && <p className="consult-modal__error" role="alert">{error}</p>}

          <footer className="consult-modal__actions">
            <button type="button" className="btn btn-glass" onClick={onClose}>Cancel</button>
            <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
              {isSubmitting ? 'Sending…' : 'Send to a consultant'}
            </button>
          </footer>
        </form>
      </div>
    </div>
  );
};

export default AskConsultantDialog;
