import { useState } from 'react';
import ProfileScreen from '../features/players/ProfileScreen';
import LevelSurveyScreen from '../features/players/LevelSurveyScreen';

type Route = 'profile' | 'levelSurvey';

export default function ProfileTab() {
  const [route, setRoute] = useState<Route>('profile');

  if (route === 'levelSurvey') {
    return <LevelSurveyScreen onDone={() => setRoute('profile')} onCancel={() => setRoute('profile')} />;
  }

  return <ProfileScreen onStartLevelSurvey={() => setRoute('levelSurvey')} />;
}
