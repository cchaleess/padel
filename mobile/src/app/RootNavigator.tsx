import { useState } from 'react';
import { ActivityIndicator, StyleSheet, View } from 'react-native';
import { colors } from '../theme';
import { useAuth } from '../features/auth/AuthContext';
import LoginScreen from '../features/auth/LoginScreen';
import ProfileScreen from '../features/players/ProfileScreen';
import LevelSurveyScreen from '../features/players/LevelSurveyScreen';

type Route = 'profile' | 'levelSurvey';

export default function RootNavigator() {
  const { status } = useAuth();
  const [route, setRoute] = useState<Route>('profile');

  if (status === 'loading') {
    return (
      <View style={styles.loading}>
        <ActivityIndicator color={colors.brandDark} />
      </View>
    );
  }

  if (status === 'unauthenticated') {
    return <LoginScreen />;
  }

  if (route === 'levelSurvey') {
    return <LevelSurveyScreen onDone={() => setRoute('profile')} onCancel={() => setRoute('profile')} />;
  }

  return <ProfileScreen onStartLevelSurvey={() => setRoute('levelSurvey')} />;
}

const styles = StyleSheet.create({
  loading: { flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: colors.background },
});
