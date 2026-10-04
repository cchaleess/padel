import { useEffect, useRef, useState } from 'react';
import { Pressable, StyleSheet, Text, View } from 'react-native';
import { CommonActions, StackActions, useNavigation } from '@react-navigation/native';
import { ApiError, api } from '../../api/httpClient';
import { colors, typography } from '../../theme';

type Props = { route: { params: { matchId: string; heldUntilUtc: string } } };

function remainingMs(heldUntilUtc: string): number {
  return Math.max(0, new Date(heldUntilUtc).getTime() - Date.now());
}

function formatCountdown(ms: number): string {
  const totalSeconds = Math.ceil(ms / 1000);
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${String(seconds).padStart(2, '0')}`;
}

export default function SeatPaymentScreen({ route }: Props) {
  const { matchId, heldUntilUtc } = route.params;
  const navigation = useNavigation();
  const [remaining, setRemaining] = useState(() => remainingMs(heldUntilUtc));
  const [isPaying, setIsPaying] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const isConfirmed = useRef(false);

  // The countdown only displays the server's deadline; the server re-checks it on confirm (design.md).
  useEffect(() => {
    const timer = setInterval(() => setRemaining(remainingMs(heldUntilUtc)), 1000);
    return () => clearInterval(timer);
  }, [heldUntilUtc]);

  // Single release point for leaving without paying: Cancel, Android back and the back gesture all remove the
  // screen. Fire-and-forget: if the hold already expired, the backend answers 409 and there's nothing to undo.
  useEffect(
    () =>
      navigation.addListener('beforeRemove', () => {
        if (!isConfirmed.current) {
          api.releaseSeat(matchId).catch(() => undefined);
        }
      }),
    [navigation, matchId],
  );

  const hasExpired = remaining === 0;

  // Cancelling the payment drops the player on the feed (decision from manual verification, see
  // m5-mobile-confirmation proposal). The navigate bubbles up to the tab navigator from whichever stack this screen
  // lives in; popping that stack afterwards (not before, while this screen can still dispatch) removes this screen,
  // so beforeRemove releases the seat as usual and the stack is clean when the player comes back to that tab.
  const cancel = () => {
    navigation.dispatch(CommonActions.navigate('Partidos', { segment: 'partidos', requestedAt: Date.now() }));
    navigation.dispatch(StackActions.popToTop());
  };

  const pay = async () => {
    setIsPaying(true);
    setError(null);
    try {
      await api.confirmSeat(matchId);
      isConfirmed.current = true;
      navigation.goBack();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se ha podido confirmar la plaza.');
      setIsPaying(false);
    }
  };

  return (
    <View style={styles.screen}>
      <Text style={typography.eyebrow}>PAGO SIMULADO</Text>
      <Text style={[typography.note, styles.detail]}>
        Tu plaza está reservada mientras completas el pago. Si no pagas a tiempo, se libera.
      </Text>

      <Text accessibilityLabel={`Tiempo restante ${formatCountdown(remaining)}`} style={styles.countdown}>
        {formatCountdown(remaining)}
      </Text>
      {hasExpired ? <Text style={styles.error}>La retención ha expirado.</Text> : null}
      {error ? <Text style={styles.error}>{error}</Text> : null}

      <Pressable
        accessibilityRole="button"
        disabled={hasExpired || isPaying}
        style={[styles.button, (hasExpired || isPaying) && styles.buttonDisabled]}
        onPress={pay}>
        <Text style={styles.buttonLabel}>{isPaying ? 'Pagando...' : 'Pagar (simulado)'}</Text>
      </Pressable>

      <Pressable
        accessibilityRole="button"
        disabled={isPaying}
        style={styles.secondaryButton}
        onPress={cancel}>
        <Text style={styles.secondaryButtonLabel}>{hasExpired ? 'Volver al feed' : 'Cancelar'}</Text>
      </Pressable>
    </View>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.background, paddingHorizontal: 20, paddingTop: 16 },
  detail: { marginTop: 8 },
  countdown: {
    marginTop: 32,
    fontSize: 56,
    fontWeight: '800',
    color: colors.ink,
    textAlign: 'center',
    fontVariant: ['tabular-nums'],
  },
  error: { marginTop: 12, color: '#B3261E', fontSize: 14, lineHeight: 20, textAlign: 'center' },
  button: {
    marginTop: 32,
    backgroundColor: colors.brandDark,
    borderRadius: 12,
    paddingVertical: 16,
    alignItems: 'center',
  },
  buttonDisabled: { opacity: 0.5 },
  buttonLabel: { color: colors.background, fontSize: 16, fontWeight: '700' },
  secondaryButton: { marginTop: 12, paddingVertical: 14, alignItems: 'center' },
  secondaryButtonLabel: { color: colors.brandDark, fontSize: 16, fontWeight: '600' },
});
