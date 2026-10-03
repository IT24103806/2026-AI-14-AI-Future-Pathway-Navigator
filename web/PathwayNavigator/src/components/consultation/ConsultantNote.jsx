import React from 'react';

/**
 * Rendered inside the journey component a consultant wrote back to (a pathway card, a roadmap
 * milestone, the Reality Check panel).
 *
 * This is the payoff of the whole feature: the student does not have to go to a separate inbox and
 * reconstruct what they were doing - the answer is attached to the thing they were looking at.
 */
const ConsultantNote = ({ guidance, title = 'Consultant guidance' }) => {
  if (!guidance) return null;

  // Accepts either the raw JSON string stored on PathwayPlan/PathwayReview, or an already-parsed object.
  let items = [];
  if (typeof guidance === 'string') {
    try {
      const parsed = JSON.parse(guidance || '[]');
      items = Array.isArray(parsed) ? parsed : [parsed];
    } catch {
      items = [];
    }
  } else if (Array.isArray(guidance)) {
    items = guidance;
  } else if (typeof guidance === 'object') {
    items = [guidance];
  }

  const visible = items.filter((item) => item && (item.note || (item.resources ?? []).length > 0));
  if (visible.length === 0) return null;

  return (
    <section className="consultant-note" aria-label={title}>
      <h4>🤝 {title}</h4>
      {visible.map((item, index) => (
        <div key={item.consultationId ?? index}>
          {item.note && <p>{item.note}</p>}
          {(item.checklist ?? []).length > 0 && (
            <ul>
              {item.checklist.map((entry) => <li key={entry}>{entry}</li>)}
            </ul>
          )}
          {(item.resources ?? []).length > 0 && (
            <ul>
              {item.resources.map((resource) => (
                <li key={resource.url}>
                  <a href={resource.url} target="_blank" rel="noreferrer noopener">🔗 {resource.label}</a>
                </li>
              ))}
            </ul>
          )}
          <span className="consultant-note__meta">
            {item.consultantName ? `${item.consultantName} · ` : ''}
            {item.createdAt ? new Date(item.createdAt).toLocaleDateString() : ''}
          </span>
        </div>
      ))}
    </section>
  );
};

export default ConsultantNote;
