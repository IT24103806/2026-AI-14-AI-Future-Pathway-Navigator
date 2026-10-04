import React, { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useNotifications } from '../../hooks/useNotifications';

/**
 * Unread badge + inbox dropdown.
 *
 * Every notification carries a server-generated deep link that returns the student to the exact panel
 * they asked from, which is what makes "see the update and continue the journey" work without the
 * student hunting through pages.
 */
const NotificationBell = () => {
  const { unreadCount, items, refresh, markRead, markAllRead } = useNotifications();
  const [isOpen, setIsOpen] = useState(false);
  const containerRef = useRef(null);
  const navigate = useNavigate();

  useEffect(() => {
    if (!isOpen) return undefined;
    refresh();
    const onClickOutside = (event) => {
      if (containerRef.current && !containerRef.current.contains(event.target)) setIsOpen(false);
    };
    const onKeyDown = (event) => {
      if (event.key === 'Escape') setIsOpen(false);
    };
    window.addEventListener('mousedown', onClickOutside);
    window.addEventListener('keydown', onKeyDown);
    return () => {
      window.removeEventListener('mousedown', onClickOutside);
      window.removeEventListener('keydown', onKeyDown);
    };
  }, [isOpen, refresh]);

  const openNotification = async (notification) => {
    if (!notification.isRead) await markRead(notification.id);
    setIsOpen(false);
    if (notification.deepLink) navigate(notification.deepLink);
  };

  return (
    <div className="notif-bell" ref={containerRef}>
      <button
        type="button"
        className="notif-bell__button"
        aria-label={unreadCount > 0 ? `Notifications, ${unreadCount} unread` : 'Notifications'}
        aria-expanded={isOpen}
        onClick={() => setIsOpen((open) => !open)}
      >
        <span aria-hidden="true">🔔</span>
        {unreadCount > 0 && <span className="notif-bell__badge">{unreadCount > 9 ? '9+' : unreadCount}</span>}
      </button>

      {isOpen && (
        <div className="notif-panel" role="dialog" aria-label="Notifications">
          <header className="notif-panel__head">
            <strong>Notifications</strong>
            {items.some((item) => !item.isRead) && (
              <button type="button" onClick={markAllRead}>Mark all read</button>
            )}
          </header>

          {items.length === 0 ? (
            <p className="notif-panel__empty">
              Nothing yet. When a consultant answers a question you asked, it appears here.
            </p>
          ) : (
            <ul className="notif-panel__list">
              {items.map((item) => (
                <li key={item.id}>
                  <button
                    type="button"
                    className={`notif-item ${item.isRead ? '' : 'is-unread'}`}
                    onClick={() => openNotification(item)}
                  >
                    <span className="notif-item__title">{item.title}</span>
                    <span className="notif-item__body">{item.body}</span>
                    <span className="notif-item__meta">
                      {new Date(item.createdAt).toLocaleString()}
                      {item.deepLink ? ' · open' : ''}
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
};

export default NotificationBell;
