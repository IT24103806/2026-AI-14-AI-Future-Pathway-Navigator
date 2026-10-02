import React, { useCallback, useEffect, useState } from 'react';
import { gapClosureTaskApi } from '../api/gapClosureTaskApi';
export default function GapClosureTasks({ reviewId }) {
  const [tasks, setTasks] = useState([]); const [title, setTitle] = useState('');
  const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  const refresh = useCallback(async () => {
    try { setTasks(await gapClosureTaskApi.list(reviewId)); setError(''); }
    catch (err) { setError(err.response?.data?.message || 'Unable to load tasks.'); }
  }, [reviewId]);
  useEffect(() => { refresh(); }, [refresh]);
  const run = async (action) => {
    setBusy(true); setError('');
    try { await action(); await refresh(); }
    catch (err) { setError(err.response?.data?.message || 'Unable to save task.'); }
    finally { setBusy(false); }
  };
  return <section className="reality-panel gap-tasks"><h2>My gap-closing tasks</h2>
    <p>Track the actions you plan to take after this Reality Check.</p>
    <form onSubmit={(event) => { event.preventDefault(); const value = title.trim(); if (value) run(async () => { await gapClosureTaskApi.create(reviewId, value); setTitle(''); }); }}>
      <input aria-label="New gap task" maxLength={160} required value={title} onChange={(event) => setTitle(event.target.value)} placeholder="e.g. Complete Python basics" />
      <button className="btn btn-primary" type="submit" disabled={busy}>Add task</button>
    </form>
    {error && <p role="alert" className="review-alert error">{error}</p>}
    {tasks.length === 0 && <p className="muted">No tasks yet.</p>}
    <ul>{tasks.map((task) => <li key={task.id}>
      <input aria-label={`Title for ${task.id}`} maxLength={160} value={task.title}
        onChange={(event) => setTasks((old) => old.map((item) => item.id === task.id ? { ...item, title: event.target.value } : item))} />
      <select aria-label={`Status for ${task.id}`} value={task.status}
        onChange={(event) => run(() => gapClosureTaskApi.update(task.id, { ...task, status: event.target.value }))}>
        <option value="ToDo">To do</option><option value="InProgress">In progress</option><option value="Done">Done</option>
      </select>
      <button type="button" disabled={busy || !task.title.trim()} onClick={() => run(() => gapClosureTaskApi.update(task.id, task))}>Save</button>
      <button type="button" disabled={busy} onClick={() => { if (window.confirm('Delete this task?')) run(() => gapClosureTaskApi.remove(task.id)); }}>Delete</button>
    </li>)}</ul>
  </section>;
}
