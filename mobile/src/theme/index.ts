export const colors = {
  background: '#F5F4EE',
  ink: '#163D2F',
  inkMuted: '#53635B',
  brandDark: '#36584A',
  accent: '#B4CB45',
} as const;

export const typography = {
  eyebrow: {
    fontSize: 11,
    letterSpacing: 2,
    fontWeight: '700',
    color: colors.brandDark,
  },
  title: {
    fontSize: 44,
    fontWeight: '800',
    letterSpacing: -2,
    color: colors.ink,
  },
  body: {
    fontSize: 26,
    lineHeight: 34,
    fontWeight: '600',
    color: colors.ink,
  },
  note: {
    fontSize: 16,
    lineHeight: 25,
    color: colors.inkMuted,
  },
} as const;
