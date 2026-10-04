import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { AppState } from 'react-native';
import { api } from '../../api/httpClient';

interface PendingVotesValue {
  /** Exception requests waiting for my vote (m6-quality-rules), shown as the Actividad tab's badge. */
  count: number;
  refresh: () => Promise<void>;
  /** For screens that already loaded the activity, to update the badge without a second request. */
  report: (count: number) => void;
}

const PendingVotesContext = createContext<PendingVotesValue | null>(null);

/** Stand-in for notifications until plan §21: refreshed when the app comes to the foreground, when a tab gets
 * focus (see AppTabs) and right after voting. */
export function PendingVotesProvider({ children }: { children: ReactNode }) {
  const [count, setCount] = useState(0);

  const refresh = useCallback(async () => {
    try {
      setCount((await api.getActivity()).toVote.length);
    } catch {
      // A badge that fails to refresh keeps its last value; the Actividad screen shows its own errors.
    }
  }, []);

  useEffect(() => {
    void refresh();
    const subscription = AppState.addEventListener('change', (state) => {
      if (state === 'active') {
        void refresh();
      }
    });
    return () => subscription.remove();
  }, [refresh]);

  const value = useMemo(() => ({ count, refresh, report: setCount }), [count, refresh]);
  return <PendingVotesContext.Provider value={value}>{children}</PendingVotesContext.Provider>;
}

export function usePendingVotes(): PendingVotesValue {
  const context = useContext(PendingVotesContext);
  if (!context) {
    throw new Error('usePendingVotes must be used within PendingVotesProvider');
  }
  return context;
}
