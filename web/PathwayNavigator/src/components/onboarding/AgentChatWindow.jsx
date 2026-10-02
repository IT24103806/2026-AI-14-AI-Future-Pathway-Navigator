import React, { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { sendAgentChatMessageApi } from '../../api/onboardingApi';
import AlertBanner from '../common/AlertBanner';

const QUICK_SUGGESTIONS_FIRST_RUN = [
  '🎓 I am an Undergraduate student at SLIIT',
  '🎓 Just completed my A/Ls',
  '💻 I know Python, JavaScript, and Problem Solving',
  '🤖 I am interested in Robotics, Gaming, and Machine Learning',
  '🚀 My dream is to become an AI Engineer',
];

const QUICK_SUGGESTIONS_UPDATE = [
  '🙋 My name is …',
  '🎓 I am a graduate now',
  '💻 Please add Docker to my skills',
  '🎨 I have started learning UI/UX design',
  '🚀 I want to become a Data Scientist',
];

const buildGreeting = (isUpdateMode, savedProfile) => {
  if (!isUpdateMode) {
    return {
      role: 'assistant',
      content:
        "Hello! 👋 I'm **Pathway Guide**, your AI Career & Onboarding Guide for PathwayNavigator.\n\n" +
        "I'd love to learn a little about you so we can customize your learning roadmap! To start, " +
        'could you tell me your **current academic stage**? (e.g. After O/L, After A/L, Undergraduate, or Graduated)',
    };
  }

  const known = [
    savedProfile?.fullName ? `• **Name:** ${savedProfile.fullName}` : null,
    savedProfile?.academicStage ? `• **Academic Stage:** ${savedProfile.academicStage}` : null,
    savedProfile?.coreSkills?.length ? `• **Core Skills:** ${savedProfile.coreSkills.join(', ')}` : null,
    savedProfile?.hobbiesInterests?.length ? `• **Interests:** ${savedProfile.hobbiesInterests.join(', ')}` : null,
    savedProfile?.careerAmbitions ? `• **Career Ambition:** ${savedProfile.careerAmbitions}` : null,
  ].filter(Boolean);

  const intro = known.length
    ? `Welcome back! 👋 Here is the profile I have on file for you:\n\n${known.join('\n')}\n\n`
    : "Welcome back! 👋 I don't have a saved profile for you yet, so we'll start fresh.\n\n";

  return {
    role: 'assistant',
    content:
      `${intro}Tell me what you would like to **update** — your name, academic stage, skills, ` +
      'interests or career ambition — and I will save the change for you. Nothing changes until you say so.',
  };
};

const AgentChatWindow = ({
  onSlotsUpdate,
  currentSlots,
  baselineSlots,
  savedProfile,
  mode = 'create',
}) => {
  const isUpdateMode = mode === 'update';
  const quickSuggestions = isUpdateMode ? QUICK_SUGGESTIONS_UPDATE : QUICK_SUGGESTIONS_FIRST_RUN;

  const [messages, setMessages] = useState([
    { ...buildGreeting(isUpdateMode, savedProfile), timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) },
  ]);
  const [inputMessage, setInputMessage] = useState('');
  const [isTyping, setIsTyping] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [isComplete, setIsComplete] = useState(false);
  const [countdown, setCountdown] = useState(3);

  const messagesEndRef = useRef(null);
  const inputRef = useRef(null);
  const navigate = useNavigate();

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    scrollToBottom();
  }, [messages, isTyping]);

  useEffect(() => {
    let timer = null;
    if (isComplete && countdown > 0) {
      timer = setInterval(() => {
        setCountdown((prev) => prev - 1);
      }, 1000);
    } else if (isComplete && countdown === 0) {
      navigate('/dashboard', { replace: true });
    }
    return () => clearInterval(timer);
  }, [isComplete, countdown, navigate]);

  const handleSendMessage = async (textToSend) => {
    const text = (textToSend || inputMessage).trim();
    if (!text || isTyping || isComplete) return;

    setErrorMessage('');

    const userMessage = {
      role: 'user',
      content: text,
      timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
    };

    const updatedMessages = [...messages, userMessage];
    setMessages(updatedMessages);
    setInputMessage('');
    setIsTyping(true);

    try {
      // Prior turns only: the current user turn is sent separately as `message`, and the agent
      // appends it to the prompt itself (sending it in both places duplicated it for the LLM).
      const history = messages
        .filter((m) => m.role === 'user' || m.role === 'assistant')
        .map((m) => ({ role: m.role, content: m.content }));

      const response = await sendAgentChatMessageApi({
        message: text,
        history,
        current_slots: currentSlots,
        // Update sessions compare against the saved profile, so the agent only finishes once
        // something has actually changed.
        baseline_slots: baselineSlots || currentSlots,
        update_mode: isUpdateMode,
      });

      const assistantMessage = {
        role: 'assistant',
        content: response.reply_message,
        timestamp: new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }),
      };

      setMessages((prev) => [...prev, assistantMessage]);

      if (response.extracted_slots) {
        onSlotsUpdate(response.extracted_slots);
      }

      if (response.is_complete) {
        setIsComplete(true);
      }
    } catch (err) {
      setErrorMessage(err.message || 'Failed to send message to Pathway Guide. Please try again.');
    } finally {
      setIsTyping(false);
      setTimeout(() => inputRef.current?.focus(), 100);
    }
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  return (
    <div className="chat-window-container">
      <div className="chat-header">
        <div className="chat-agent-badge">
          <div className="agent-avatar-glow">🤖</div>
          <div>
            <h4>Pathway Guide (Agent 1)</h4>
            <span className="online-indicator">
              <span className="online-dot" /> Online & Active
            </span>
          </div>
        </div>
        {isComplete && (
          <span className="complete-badge">
            {isUpdateMode ? '✅ Profile Updated!' : '🎉 Onboarding Completed!'}
          </span>
        )}
      </div>

      <AlertBanner
        type="error"
        message={errorMessage}
        onClose={() => setErrorMessage('')}
      />

      <div className="chat-messages-stream">
        {messages.map((msg, index) => (
          <div
            key={index}
            className={`chat-message-row ${msg.role === 'user' ? 'user-row' : 'assistant-row'}`}
          >
            {msg.role === 'assistant' && (
              <div className="message-avatar">🤖</div>
            )}
            <div className={`message-bubble ${msg.role === 'user' ? 'user-bubble' : 'assistant-bubble'}`}>
              <div className="message-text">
                {msg.content.split('\n').map((paragraph, i) => (
                  <p key={i}>{paragraph}</p>
                ))}
              </div>
              <span className="message-timestamp">{msg.timestamp}</span>
            </div>
            {msg.role === 'user' && (
              <div className="message-avatar user-avatar-bubble">👤</div>
            )}
          </div>
        ))}

        {isTyping && (
          <div className="chat-message-row assistant-row">
            <div className="message-avatar">🤖</div>
            <div className="message-bubble assistant-bubble typing-bubble">
              <div className="typing-dots">
                <span />
                <span />
                <span />
              </div>
            </div>
          </div>
        )}

        {isComplete && (
          <div className="celebration-card">
            <div className="celebration-icon">🚀</div>
            <h3>{isUpdateMode ? 'Profile Updated!' : 'All Profile Data Collected!'}</h3>
            <p>
              {isUpdateMode
                ? 'Your saved details and pathway recommendations now reflect these changes.'
                : 'Your student profile is saved.'}{' '}
              Redirecting to your personalized AI dashboard in <strong>{countdown}s</strong>...
            </p>
            <button
              className="btn btn-sm btn-primary mt-2"
              onClick={() => navigate('/dashboard', { replace: true })}
            >
              Go to Dashboard Now →
            </button>
          </div>
        )}

        <div ref={messagesEndRef} />
      </div>

      {/* Quick Suggestions Chips */}
      {!isComplete && (
        <div className="quick-suggestions-bar">
          <span className="suggestions-label">💡 Suggestions:</span>
          <div className="suggestions-chips-scroll">
            {quickSuggestions.map((chip, idx) => (
              <button
                key={idx}
                type="button"
                className="suggestion-chip"
                // Chips ending in "…" need the student to complete them, so they pre-fill the
                // input box instead of being sent as-is.
                onClick={() => {
                  const text = chip.replace(/^[^\s]+ /, '');
                  if (chip.endsWith('…')) {
                    setInputMessage(`${text} `);
                    inputRef.current?.focus();
                  } else {
                    handleSendMessage(text);
                  }
                }}
                disabled={isTyping}
              >
                {chip}
              </button>
            ))}
          </div>
        </div>
      )}

      {/* Input Form */}
      <div className="chat-input-container">
        <textarea
          ref={inputRef}
          value={inputMessage}
          onChange={(e) => setInputMessage(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={isComplete ? 'Profile saved!' : 'Type your message here... (Press Enter to send)'}
          disabled={isTyping || isComplete}
          rows={1}
          className="chat-textarea"
        />
        <button
          type="button"
          onClick={() => handleSendMessage()}
          disabled={!inputMessage.trim() || isTyping || isComplete}
          className="chat-send-btn"
          aria-label="Send message"
        >
          ➤
        </button>
      </div>
    </div>
  );
};

export default AgentChatWindow;
