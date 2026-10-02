import { useContext } from 'react';
import { ThemeContext } from '../context/ThemeContextDefinition';

export const useTheme = () => useContext(ThemeContext);

export default useTheme;
