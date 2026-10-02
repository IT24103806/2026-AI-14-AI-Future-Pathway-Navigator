import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import Reveal from '../components/common/Reveal';
import FormInput from '../components/common/FormInput';
import PrimaryButton from '../components/common/PrimaryButton';
import AlertBanner from '../components/common/AlertBanner';

const TOPICS = [
  'Student support',
  'School / counsellor partnership',
  'Technical issue',
  'Data & privacy question',
  'Something else',
];

const INFO_CARDS = [
  {
    icon: '✉️',
    title: 'Email the team',
    text: 'We reply to student, parent and counsellor enquiries within two working days.',
    action: { label: 'Pathway Navigator support inbox', href: '/contact' },
  },
  {
    icon: '🏫',
    title: 'For schools & counsellors',
    text: 'Onboarding a cohort, or want a walkthrough of the approval centre? Ask for a guided demo.',
    action: { label: 'Request a demo', href: '/contact' },
  },
  {
    icon: '🔐',
    title: 'Privacy & your data',
    text: 'Your profile and Reality Check evidence are tied to your account and never sold or shared.',
    action: { label: 'Read the principles', href: '/about' },
  },
];

const ContactPage = () => {
  const [form, setForm] = useState({ name: '', email: '', topic: TOPICS[0], message: '' });
  const [errors, setErrors] = useState({});
  const [submitted, setSubmitted] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleChange = (event) => {
    const { name, value } = event.target;
    setForm((previous) => ({ ...previous, [name]: value }));
    setErrors((previous) => (previous[name] ? { ...previous, [name]: '' } : previous));
  };

  const validate = () => {
    const nextErrors = {};
    if (!form.name.trim()) nextErrors.name = 'Please tell us your name.';
    if (!form.email.trim()) {
      nextErrors.email = 'An email address is required so we can reply.';
    } else if (!/\S+@\S+\.\S+/.test(form.email)) {
      nextErrors.email = 'Please enter a valid email address.';
    }
    if (form.message.trim().length < 20) {
      nextErrors.message = 'Please add at least a sentence or two (20+ characters).';
    }
    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!validate()) return;

    setIsSubmitting(true);
    // No messaging endpoint is wired in this build yet — the handler validates and
    // acknowledges locally so the UI flow is complete without pretending to send.
    await new Promise((resolve) => setTimeout(resolve, 550));
    setIsSubmitting(false);
    setSubmitted(true);
  };

  const resetForm = () => {
    setForm({ name: '', email: '', topic: TOPICS[0], message: '' });
    setErrors({});
    setSubmitted(false);
  };

  return (
    <div className="home-container">
      <section className="section section-tight" style={{ paddingBottom: 0 }}>
        <Reveal className="section-head">
          <span className="eyebrow">Contact us</span>
          <h2>Let&rsquo;s talk about your next step</h2>
          <p>
            Questions about a pathway, a school rollout or the counsellor workflow? Send a
            message and the right person will get back to you.
          </p>
        </Reveal>
      </section>

      <section className="contact-layout">
        <div className="contact-aside">
          {INFO_CARDS.map((card, index) => (
            <Reveal className="contact-info-card" key={card.title} delay={index * 80} variant="left">
              <span className="icon-chip" aria-hidden="true">{card.icon}</span>
              <div>
                <h3>{card.title}</h3>
                <p>{card.text}</p>
              </div>
            </Reveal>
          ))}

          <Reveal className="glass-panel" delay={240} variant="left">
            <h3 style={{ fontSize: 'var(--fs-lg)', marginBottom: '0.4rem' }}>While you wait</h3>
            <p className="text-muted" style={{ fontSize: 'var(--fs-sm)' }}>
              The fastest way to see what PathwayNavigator does is to answer the AI guide&rsquo;s
              first few questions.
            </p>
            <div className="row" style={{ marginTop: '0.9rem' }}>
              <Link to="/register" className="btn btn-primary btn-sm">Create an account</Link>
              <Link to="/about" className="btn btn-glass btn-sm">How it works</Link>
            </div>
          </Reveal>
        </div>

        <Reveal className="contact-form-card" variant="right">
          {submitted ? (
            <div className="contact-success" role="status">
              <span className="contact-success__icon" aria-hidden="true">✅</span>
              <h2>Thank you, {form.name.split(' ')[0]}!</h2>
              <p className="text-muted">
                Your message about <strong>{form.topic.toLowerCase()}</strong> has been captured.
                Replies are sent to <strong>{form.email}</strong>.
              </p>
              <p className="text-dim" style={{ fontSize: 'var(--fs-xs)' }}>
                Demo build: the form validates and acknowledges locally — no message is sent yet.
              </p>
              <button type="button" className="btn btn-glass" onClick={resetForm}>
                Send another message
              </button>
            </div>
          ) : (
            <>
              <h2>Send us a message</h2>
              <p>All fields marked with * are required.</p>

              <AlertBanner type="error" message={errors.form} />

              <form onSubmit={handleSubmit} noValidate>
                <div className="form-row">
                  <FormInput
                    id="contact-name"
                    label="Your name"
                    name="name"
                    value={form.name}
                    onChange={handleChange}
                    placeholder="e.g. Nimal Perera"
                    error={errors.name}
                    required
                    autoComplete="name"
                  />
                  <FormInput
                    id="contact-email"
                    label="Email address"
                    type="email"
                    name="email"
                    value={form.email}
                    onChange={handleChange}
                    placeholder="you@example.com"
                    error={errors.email}
                    required
                    autoComplete="email"
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="contact-topic" className="form-label">What is this about?</label>
                  <select
                    id="contact-topic"
                    name="topic"
                    className="form-select"
                    value={form.topic}
                    onChange={handleChange}
                  >
                    {TOPICS.map((topic) => (
                      <option key={topic} value={topic}>{topic}</option>
                    ))}
                  </select>
                </div>

                <FormInput
                  id="contact-message"
                  label="Message"
                  name="message"
                  value={form.message}
                  onChange={handleChange}
                  placeholder="Tell us a little about your situation or question…"
                  error={errors.message}
                  required
                />

                <PrimaryButton type="submit" isLoading={isSubmitting} disabled={isSubmitting} className="w-full">
                  Send message
                </PrimaryButton>

                <p className="text-dim" style={{ fontSize: 'var(--fs-xs)', marginTop: '0.75rem', textAlign: 'center' }}>
                  Demo build: this form validates locally and does not send yet.
                </p>
              </form>
            </>
          )}
        </Reveal>
      </section>
    </div>
  );
};

export default ContactPage;
