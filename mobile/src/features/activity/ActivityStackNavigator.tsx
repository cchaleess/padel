import { createNativeStackNavigator } from '@react-navigation/native-stack';
import MatchDetailScreen from '../matches/MatchDetailScreen';
import SeatPaymentScreen from '../matches/SeatPaymentScreen';
import ActivityScreen from './ActivityScreen';

export type ActivityStackParamList = {
  Activity: undefined;
  MatchDetail: { matchId: string };
  SeatPayment: { matchId: string; heldUntilUtc: string };
};

const Stack = createNativeStackNavigator<ActivityStackParamList>();

export default function ActivityStackNavigator() {
  return (
    <Stack.Navigator>
      <Stack.Screen name="Activity" component={ActivityScreen} options={{ title: 'Actividad' }} />
      <Stack.Screen name="MatchDetail" component={MatchDetailScreen} options={{ title: 'Partido' }} />
      <Stack.Screen name="SeatPayment" component={SeatPaymentScreen} options={{ title: 'Confirmar plaza' }} />
    </Stack.Navigator>
  );
}
