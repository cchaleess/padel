import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import ActivityStackNavigator from '../features/activity/ActivityStackNavigator';
import { PendingVotesProvider, usePendingVotes } from '../features/activity/PendingVotesContext';
import ClubsStackNavigator from '../features/clubs/ClubsStackNavigator';
import PartidosTabScreen from './PartidosTabScreen';
import ProfileTab from './ProfileTab';
import { colors } from '../theme';

const Tab = createBottomTabNavigator();

export default function AppTabs() {
  return (
    <PendingVotesProvider>
      <Tabs />
    </PendingVotesProvider>
  );
}

function Tabs() {
  const { count, refresh } = usePendingVotes();

  return (
    <Tab.Navigator
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: colors.brandDark,
        tabBarInactiveTintColor: colors.inkMuted,
      }}
      // Any tab change is a cheap moment to check for new requests to vote on.
      screenListeners={{ focus: () => void refresh() }}
    >
      <Tab.Screen name="Partidos" component={PartidosTabScreen} />
      <Tab.Screen name="Crear" component={ClubsStackNavigator} />
      <Tab.Screen
        name="Actividad"
        component={ActivityStackNavigator}
        options={{ tabBarBadge: count > 0 ? count : undefined }}
      />
      <Tab.Screen name="Perfil" component={ProfileTab} />
    </Tab.Navigator>
  );
}
