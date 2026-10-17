import { useColorScheme } from 'react-native';

const light = {
  background: '#F6F7FB',
  surface: '#FFFFFF',
  text: '#14161F',
  textMuted: '#636B7E',
  border: '#E2E5EE',
  primary: '#2F6FEB',
  onPrimary: '#FFFFFF',
  danger: '#C8372D',
  success: '#2E8B57',
};

const dark: typeof light = {
  background: '#0E1016',
  surface: '#191C25',
  text: '#F2F3F7',
  textMuted: '#9BA3B6',
  border: '#2A2F3C',
  primary: '#6C9BFF',
  onPrimary: '#0E1016',
  danger: '#FF7A70',
  success: '#5DD39E',
};

export type Theme = typeof light;

export function useTheme(): Theme {
  return useColorScheme() === 'dark' ? dark : light;
}

export const spacing = { xs: 4, sm: 8, md: 12, lg: 16, xl: 24 };
