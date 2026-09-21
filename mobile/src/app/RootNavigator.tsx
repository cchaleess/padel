import { ActivityIndicator, StyleSheet, View } from 'react-native';
import { NavigationContainer } from '@react-navigation/native';
import { colors } from '../theme';
import { useAuth } from '../features/auth/AuthContext';
import LoginScreen from '../features/auth/LoginScreen';
import AppTabs from './AppTabs';

export default function RootNavigator() {
  const { status } = useAuth();

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

  return (
    <NavigationContainer>
      <AppTabs />
    </NavigationContainer>
  );
}

const styles = StyleSheet.create({
  loading: { flex: 1, justifyContent: 'center', alignItems: 'center', backgroundColor: colors.background },
});
