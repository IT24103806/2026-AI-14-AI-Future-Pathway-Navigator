import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import notificationApi from '../api/notificationApi';
import { NotificationContext } from './NotificationContextDefinition';
import { useAuth } from '../hooks/useAuth';

/** Poll interval. Long enough to be cheap, short enough that a reply feels noticed. */
const POLL_INTERVAL_MS = 45000;

/**
 * Notifications are delivered by polling a single indexed count endpoint.
 *
 * Two deliberate choices:
 *   * the timer is paused while the tab is hidden (`document.visibilityState`), so a background tab
 *     costs nothing, and an immediate refresh happens when the user comes back;
 *   * failures are swallowed - a notification outage must never surface as an app error, because the
 *     underlying data is still reachable from the Support page.
 *
 * This is the one place to swap in SSE/SignalR/FCM later: the components below only consume
 * `unreadCount` and `refresh()`.
 */
export const NotificationProvider = ({ children }) => {
  const { isAuthenticated } = useAuth();
  const [unreadCount, setUnreadCount] = useState(0);
  const [items, setItems] = useState([]);
  const [isLoading, setIsLoading] = useState(false);
  const timerRef = useRef(null);

  const refreshCount = useCallback(async () => {
    if (!isAuthenticated) {
      setUnreadCount(0);
      return;
    }
    try {
      setUnreadCount(await notificationApi.getUnreadCount());
    } catch {
      // Silent by design: the badge is an enhancement, not a dependency.
    }
  }, [isAuthenticated]);

  const refresh = useCallback(async () => {
    if (!isAuthenticated) {
      setItems([]);
      setUnreadCount(0);
      return;
    }
    setIsLoading(true);
    try {
      const result = await notificationApi.getMine({ pageSize: 20 });
      setItems(result?.items ?? []);
      setUnreadCount(result?.unreadCount ?? 0);
    } catch {
      setItems([]);
    } finally {
      setIsLoading(false);
    }
  }, [isAuthenticated]);

  const markRead = useCallback(async (id) => {
    setItems((current) => current.map((item) => (item.id === id ? { ...item, isRead: true } : item)));
    setUnreadCount((current) => Math.max(0, current - 1));
    try {
      await notificationApi.markRead(id);
      await refreshCount();
    } catch {
      await refresh();
    }
  }, [refresh, refreshCount]);

  const markAllRead = useCallback(async () => {
    setItems((current) => current.map((item) => ({ ...item, isRead: true })));
    setUnreadCount(0);
    try {
      await notificationApi.markAllRead();
    } catch {
      await refresh();
    }
  }, [refresh]);

  useEffect(() => {
    if (!isAuthenticated) {
      setUnreadCount(0);
      setItems([]);
      return undefined;
    }

    refreshCount();

    const start = () => {
      if (timerRef.current) return;
      timerRef.current = setInterval(refreshCount, POLL_INTERVAL_MS);
    };
    const stop = () => {
      if (timerRef.current) {
        clearInterval(timerRef.current);
        timerRef.current = null;
      }
    };
    const onVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        refreshCount();
        start();
      } else {
        stop();
      }
    };

    if (document.visibilityState === 'visible') start();
    document.addEventListener('visibilitychange', onVisibilityChange);

    return () => {
      stop();
      document.removeEventListener('visibilitychange', onVisibilityChange);
    };
  }, [isAuthenticated, refreshCount]);

  const value = useMemo(
    () => ({ unreadCount, items, isLoading, refresh, refreshCount, markRead, markAllRead }),
    [unreadCount, items, isLoading, refresh, refreshCount, markRead, markAllRead],
  );

  return <NotificationContext.Provider value={value}>{children}</NotificationContext.Provider>;
};

export default NotificationProvider;
