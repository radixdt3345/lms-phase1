import React from 'react';
import { Provider } from 'react-redux';
import { PublicClientApplication } from '@azure/msal-browser';
import { MsalProvider } from '@azure/msal-react';
import { msalConfig } from './auth/msalConfig';
import { store } from './store/store';
import AppRouter from './router';

const msalInstance = new PublicClientApplication(msalConfig);

const App: React.FC = () => {
  return (
    <MsalProvider instance={msalInstance}>
      <Provider store={store}>
        <AppRouter />
      </Provider>
    </MsalProvider>
  );
};

export default App;
