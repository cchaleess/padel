import type { MatchType } from '../../api/types';

export function getMatchTypeLabel(type: MatchType): string {
  return type === 'Competitive' ? 'Competitivo' : 'Amistoso';
}
