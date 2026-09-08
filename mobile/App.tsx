import { AuthProvider } from './src/features/auth/AuthContext';
import RootNavigator from './src/app/RootNavigator';

export default function App() {
  return (
    <AuthProvider>
      <RootNavigator />
    </AuthProvider>
  );
}
