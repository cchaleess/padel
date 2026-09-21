import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import ComingSoonScreen from '../components/ui/ComingSoonScreen';
import PartidosTabScreen from './PartidosTabScreen';
import ProfileTab from './ProfileTab';
import { colors } from '../theme';

const Tab = createBottomTabNavigator();

function CrearTab() {
  return <ComingSoonScreen title="Crear" />;
}

function ActividadTab() {
  return <ComingSoonScreen title="Actividad" />;
}

export default function AppTabs() {
  return (
    <Tab.Navigator
      screenOptions={{
        headerShown: false,
        tabBarActiveTintColor: colors.brandDark,
        tabBarInactiveTintColor: colors.inkMuted,
      }}
    >
      <Tab.Screen name="Partidos" component={PartidosTabScreen} />
      <Tab.Screen name="Crear" component={CrearTab} />
      <Tab.Screen name="Actividad" component={ActividadTab} />
      <Tab.Screen name="Perfil" component={ProfileTab} />
    </Tab.Navigator>
  );
}
