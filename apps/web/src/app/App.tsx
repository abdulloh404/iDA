import { QueryClientProvider } from '@tanstack/react-query';
import { RouterProvider } from 'react-router-dom';
import { AuthProvider } from '../features/auth/AuthContext';
import { ToastProvider } from '../components/feedback/Toast';
import { PreferencesProvider } from './PreferencesProvider';
import { queryClient } from './queryClient';
import { router } from './router';
export default function App() {
    return (<PreferencesProvider>
      <QueryClientProvider client={queryClient}>
        <AuthProvider onSessionChange={() => queryClient.clear()}>
          <ToastProvider>
            <RouterProvider router={router}/>
          </ToastProvider>
        </AuthProvider>
      </QueryClientProvider>
    </PreferencesProvider>);
}
