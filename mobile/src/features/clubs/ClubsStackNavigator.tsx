import { createNativeStackNavigator } from '@react-navigation/native-stack';
import ClubsListScreen from './ClubsListScreen';
import ClubDetailScreen from './ClubDetailScreen';
import SubmitClubScreen from './SubmitClubScreen';
import CreateMatchScreen from '../matches/CreateMatchScreen';
import MatchDetailScreen from '../matches/MatchDetailScreen';
import SeatPaymentScreen from '../matches/SeatPaymentScreen';

export type ClubsStackParamList = {
  ClubsList: undefined;
  ClubDetail: { clubId: string };
  SubmitClub: undefined;
  CreateMatch: { courtSlotId: string; courtName: string; startsAt: string; endsAt: string; durationMinutes: number };
  MatchDetail: { matchId: string };
  SeatPayment: { matchId: string; heldUntilUtc: string };
};

const Stack = createNativeStackNavigator<ClubsStackParamList>();

export default function ClubsStackNavigator() {
  return (
    <Stack.Navigator>
      <Stack.Screen name="ClubsList" component={ClubsListScreen} options={{ title: 'Clubes' }} />
      <Stack.Screen name="ClubDetail" component={ClubDetailScreen} options={{ title: 'Club' }} />
      <Stack.Screen name="SubmitClub" component={SubmitClubScreen} options={{ title: 'Aportar un club' }} />
      <Stack.Screen name="CreateMatch" component={CreateMatchScreen} options={{ title: 'Crear partido' }} />
      <Stack.Screen name="MatchDetail" component={MatchDetailScreen} options={{ title: 'Partido' }} />
      <Stack.Screen name="SeatPayment" component={SeatPaymentScreen} options={{ title: 'Confirmar plaza' }} />
    </Stack.Navigator>
  );
}
