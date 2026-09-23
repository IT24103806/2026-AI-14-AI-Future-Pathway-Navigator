import { useContext } from 'react';
import { AuthContext } from '../context/AuthContextDefinition';

/**
 * Custom hook to access global AuthContext.
 */
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider component.');
  }
  return context;
};

export default useAuth;
