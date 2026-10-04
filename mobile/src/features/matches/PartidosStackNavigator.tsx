import { createNativeStackNavigator } from '@react-navigation/native-stack';
import FeedScreen from './FeedScreen';
import MatchDetailScreen from './MatchDetailScreen';
import SeatPaymentScreen from './SeatPaymentScreen';

export type PartidosStackParamList = {
  Feed: undefined;
  MatchDetail: { matchId: string };
  SeatPayment: { matchId: string; heldUntilUtc: string };
};

const Stack = createNativeStackNavigator<PartidosStackParamList>();

export default function PartidosStackNavigator() {
  return (
    <Stack.Navigator>
      <Stack.Screen name="Feed" component={FeedScreen} options={{ title: 'Partidos' }} />
      <Stack.Screen name="MatchDetail" component={MatchDetailScreen} options={{ title: 'Partido' }} />
      <Stack.Screen name="SeatPayment" component={SeatPaymentScreen} options={{ title: 'Confirmar plaza' }} />
    </Stack.Navigator>
  );
}
