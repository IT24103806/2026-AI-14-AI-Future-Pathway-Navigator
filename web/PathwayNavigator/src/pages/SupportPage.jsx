import React, { useCallback, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import consultationApi from '../api/consultationApi';
import { getApiErrorMessage } from '../utils/apiError';
import AskConsultantDialog from '../components/consultation/AskConsultantDialog';
import ConsultationThread from '../components/consultation/ConsultationThread';
import JourneySteps from '../components/common/JourneySteps';

const STATUS_FILTERS = ['', 'Open', 'Answered', 'Closed'];

/**
 * "My questions" - the student's own list of consultant conversations.
 *
 * This is the fallback home for a deep link whose original page had no anchor (a general question),
 * and the place a student lands when they simply want to check on something they asked.
 */
const SupportPage = () => {
  const [searchParams] = useSearchParams();
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const [result, setResult] = useState({ items: [], totalCount: 0, totalPages: 0 });
  const [selected, setSelected] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [alert, setAlert] = useState(null);
  const [isAsking, setIsAsking] = useState(false);

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      setResult(await consultationApi.getMine({ status, page, pageSize: 10 }));
    } catch (error) {
      setAlert({ type: 'error', text: getApiErrorMessage(error, 'Failed to load your questions.') });
    } finally {
      setIsLoading(false);
    }
  }, [status, page]);

  useEffect(() => { load(); }, [load]);

  const openThread = useCallback(async (id) => {
    setBusy(true);
    try {
      setSelected(await consultationApi.getById(id));
    } catch (error) {
      setAlert({ type: 'error', text: getApiErrorMessage(error, 'That conversation could not be opened.') });
    } finally {
      setBusy(false);
    }
  }, []);

  // Notification deep link: open the referenced thread immediately.
  useEffect(() => {
    const id = searchParams.get('consultation');
    if (id) openThread(id);
  }, [searchParams, openThread]);

  const sendMessage = async (body) => {
    setBusy(true);
    try {
      const updated = await consultationApi.addMessage(selected.id, body);
      setSelected(updated);
      await load();
      return true;
    } catch (error) {
      setAlert({ type: 'error', text: getApiErrorMessage(error, 'Your message could not be sent.') });
      return false;
    } finally {
      setBusy(false);
    }
  };

  const closeRequest = async (id, payload) => {
    setBusy(true);
    try {
      setSelected(await consultationApi.close(id, payload));
      await load();
      setAlert({ type: 'success', text: 'Marked as resolved. Thank you — this helps us measure the service.' });
    } catch (error) {
      setAlert({ type: 'error', text: getApiErrorMessage(error, 'That could not be closed.') });
    } finally {
      setBusy(false);
    }
  };

  const reopenRequest = async (id) => {
    setBusy(true);
    try {
      setSelected(await consultationApi.reopen(id));
      await load();
      setAlert({ type: 'success', text: 'Reopened — your consultant can pick it up again.' });
    } catch (error) {
      setAlert({ type: 'error', text: getApiErrorMessage(error, 'That could not be reopened.') });
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="support-page">
      <header className="support-header">
        <div>
          <h1>🙋 My consultant questions</h1>
          <p>Ask about a pathway, a roadmap step or a Reality Check — a real consultant replies here.</p>
          <JourneySteps current={3} className="mt-2" />
        </div>
        <button type="button" className="btn btn-primary" onClick={() => setIsAsking(true)}>Ask a general question</button>
      </header>

      {alert && <div role="alert" className={`review-alert ${alert.type}`}>{alert.text}</div>}

      <section className="review-filters" aria-label="Filter your questions">
        <select aria-label="Filter by status" value={status} onChange={(event) => { setStatus(event.target.value); setPage(1); }}>
          {STATUS_FILTERS.map((value) => <option key={value} value={value}>{value || 'All questions'}</option>)}
        </select>
        <button type="button" onClick={load}>Refresh</button>
      </section>

      {isLoading ? (
        <p className="review-state">Loading your questions…</p>
      ) : result.items.length === 0 ? (
        <p className="review-state">
          No questions yet. Whenever a pathway, a cost figure or a review confuses you, ask here —
          a real consultant answers.
        </p>
      ) : (
        <div className="review-layout">
          <section className="review-list" aria-label="Your questions">
            {result.items.map((item) => (
              <button
                key={item.id}
                type="button"
                className={`review-card ${selected?.id === item.id ? 'selected' : ''}`}
                onClick={() => openThread(item.id)}
              >
                <span>
                  <strong>{item.subject}</strong>
                  <small>{item.contextSummary}</small>
                </span>
                <span className={`status ${item.status.toLowerCase()}`}>{item.status}</span>
                <small>{item.assignedConsultantName || 'Waiting for a consultant'}</small>
              </button>
            ))}
            <nav className="pagination" aria-label="Question pages">
              <button type="button" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>Previous</button>
              <span>Page {page} of {Math.max(1, result.totalPages)}</span>
              <button type="button" disabled={page >= result.totalPages} onClick={() => setPage((current) => current + 1)}>Next</button>
            </nav>
          </section>

          <section className="review-detail">
            {!selected ? (
              <p className="review-state">
                Select a question to read the conversation, or ask a new one.
                {' '}
                Looking for something specific? <Link to="/career-discovery">Go to Career Discovery</Link>.
              </p>
            ) : (
              <ConsultationThread
                consultation={selected}
                variant="student"
                busy={busy}
                onSend={sendMessage}
                onClose={closeRequest}
                onReopen={reopenRequest}
              />
            )}
          </section>
        </div>
      )}

      {isAsking && (
        <AskConsultantDialog
          contextType="General"
          onClose={() => setIsAsking(false)}
          onCreated={(created) => { setAlert({ type: 'success', text: 'Sent. A consultant will reply — you will get a notification.' }); load(); openThread(created.id); }}
        />
      )}
    </div>
  );
};

export default SupportPage;
