import type { AccessRequestStatus, AccessShortfall } from '../../api/types';

/** Why the player can't join directly, worded for the player themselves ("tu nivel...") or for voters looking
 * at someone else's request ("su nivel..."). */
export function describeShortfall(
  shortfall: AccessShortfall,
  range: { minLevel: number | null; maxLevel: number | null; minMatchesRequired: number | null },
  who: 'me' | 'them',
): string {
  const rangeText = `${range.minLevel?.toFixed(1)}–${range.maxLevel?.toFixed(1)}`;
  switch (shortfall) {
    case 'NoLevel':
      return who === 'me' ? 'Aún no tienes nivel (completa la encuesta en Perfil)' : 'Sin nivel todavía';
    case 'LevelBelowRange':
      return `${who === 'me' ? 'Tu' : 'Su'} nivel está por debajo del rango ${rangeText}`;
    case 'LevelAboveRange':
      return `${who === 'me' ? 'Tu' : 'Su'} nivel está por encima del rango ${rangeText}`;
    case 'NotEnoughMatches':
      return `Mínimo ${range.minMatchesRequired} partidos jugados`;
  }
}

export const requestStatusLabels: Record<AccessRequestStatus, string> = {
  Pending: 'Pendiente de votación',
  Approved: 'Aprobada',
  Rejected: 'Rechazada',
  Expired: 'Expirada: el partido se completó',
};
