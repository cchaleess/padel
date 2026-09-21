import { createNativeStackNavigator } from '@react-navigation/native-stack';
import ClubsListScreen from './ClubsListScreen';
import ClubDetailScreen from './ClubDetailScreen';
import CourtSlotsScreen from './CourtSlotsScreen';
import SubmitClubScreen from './SubmitClubScreen';

export type ClubsStackParamList = {
  ClubsList: undefined;
  ClubDetail: { clubId: string };
  CourtSlots: { clubId: string; courtId: string; courtName: string };
  SubmitClub: undefined;
};

const Stack = createNativeStackNavigator<ClubsStackParamList>();

export default function ClubsStackNavigator() {
  return (
    <Stack.Navigator>
      <Stack.Screen name="ClubsList" component={ClubsListScreen} options={{ title: 'Clubes' }} />
      <Stack.Screen name="ClubDetail" component={ClubDetailScreen} options={{ title: 'Club' }} />
      <Stack.Screen name="CourtSlots" component={CourtSlotsScreen} options={{ title: 'Huecos disponibles' }} />
      <Stack.Screen name="SubmitClub" component={SubmitClubScreen} options={{ title: 'Aportar un club' }} />
    </Stack.Navigator>
  );
}
