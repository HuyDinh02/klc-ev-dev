import * as SecureStore from 'expo-secure-store';
import { useAuthStore } from '../authStore';
import type { UserProfile } from '../../types';

// Reset store state before each test
beforeEach(() => {
  useAuthStore.setState({
    isAuthenticated: false,
    isLoading: true,
    user: null,
    token: null,
  });
  jest.clearAllMocks();
});

const mockUser: UserProfile = {
  id: '1',
  email: 'driver@klc.vn',
  fullName: 'Test Driver',
  isPhoneVerified: true,
  isEmailVerified: true,
};

describe('useAuthStore', () => {
  describe('initial state', () => {
    it('should have correct default values', () => {
      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(false);
      expect(state.isLoading).toBe(true);
      expect(state.user).toBeNull();
      expect(state.token).toBeNull();
    });
  });

  describe('login', () => {
    it('should set token, user, and isAuthenticated on login', async () => {
      await useAuthStore.getState().login('test-token-123', mockUser);

      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(true);
      expect(state.isLoading).toBe(false);
      expect(state.token).toBe('test-token-123');
      expect(state.user).toEqual(mockUser);
    });

    it('should persist token to SecureStore on login', async () => {
      await useAuthStore.getState().login('test-token-123', mockUser);

      expect(SecureStore.setItemAsync).toHaveBeenCalledWith(
        'authToken',
        'test-token-123'
      );
    });
  });

  describe('logout', () => {
    it('should clear auth state on logout', async () => {
      // Login first
      await useAuthStore.getState().login('test-token-123', mockUser);
      expect(useAuthStore.getState().isAuthenticated).toBe(true);

      // Logout
      await useAuthStore.getState().logout();

      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(false);
      expect(state.token).toBeNull();
      expect(state.user).toBeNull();
    });

    it('should remove token from SecureStore on logout', async () => {
      await useAuthStore.getState().login('test-token-123', mockUser);
      await useAuthStore.getState().logout();

      expect(SecureStore.deleteItemAsync).toHaveBeenCalledWith('authToken');
    });
  });

  describe('checkAuth', () => {
    it('should set isAuthenticated to true when token exists in SecureStore', async () => {
      (SecureStore.getItemAsync as jest.Mock).mockResolvedValueOnce(
        'stored-token'
      );

      await useAuthStore.getState().checkAuth();

      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(true);
      expect(state.token).toBe('stored-token');
      expect(state.isLoading).toBe(false);
    });

    it('should set isAuthenticated to false when no token in SecureStore', async () => {
      (SecureStore.getItemAsync as jest.Mock).mockResolvedValueOnce(null);

      await useAuthStore.getState().checkAuth();

      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(false);
      expect(state.isLoading).toBe(false);
    });

    it('should handle SecureStore errors gracefully', async () => {
      (SecureStore.getItemAsync as jest.Mock).mockRejectedValueOnce(
        new Error('SecureStore error')
      );

      await useAuthStore.getState().checkAuth();

      const state = useAuthStore.getState();
      expect(state.isAuthenticated).toBe(false);
      expect(state.isLoading).toBe(false);
    });
  });

  describe('setUser', () => {
    it('should update user', () => {
      useAuthStore.getState().setUser(mockUser);
      expect(useAuthStore.getState().user).toEqual(mockUser);
    });

    it('should allow setting user to null', () => {
      useAuthStore.getState().setUser(mockUser);
      useAuthStore.getState().setUser(null);
      expect(useAuthStore.getState().user).toBeNull();
    });
  });

  describe('setToken', () => {
    it('should update token and set isAuthenticated to true', () => {
      useAuthStore.getState().setToken('new-token');
      const state = useAuthStore.getState();
      expect(state.token).toBe('new-token');
      expect(state.isAuthenticated).toBe(true);
    });

    it('should set isAuthenticated to false when token is null', () => {
      useAuthStore.getState().setToken('some-token');
      useAuthStore.getState().setToken(null);
      const state = useAuthStore.getState();
      expect(state.token).toBeNull();
      expect(state.isAuthenticated).toBe(false);
    });
  });
});
