import React from 'react';
import { ToastProvider } from './ToastContext';
import { AuthProvider } from './AuthContext';
import { CartProvider } from './CartContext';
import { AiChatProvider } from './AiChatContext';

export default function AppProviders({ children }) {
  return (
    <ToastProvider>
      <AuthProvider>
        <CartProvider>
          <AiChatProvider>
            {children}
          </AiChatProvider>
        </CartProvider>
      </AuthProvider>
    </ToastProvider>
  );
}
