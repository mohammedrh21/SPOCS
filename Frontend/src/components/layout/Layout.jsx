import React from 'react';
import { Outlet } from 'react-router-dom';
import TopAnnouncementBar from './TopAnnouncementBar';
import Header from './Header';
import Footer from './Footer';
import CartDrawer from '../../features/cart/components/CartDrawer';
import AiChatDrawer from '../../features/ai-assistant/components/AiChatDrawer';
import AiFloatingButton from '../../features/ai-assistant/components/AiFloatingButton';
import AuthModal from '../../features/auth/components/AuthModal';

export default function Layout() {
  return (
    <div className="min-h-screen flex flex-col bg-surface-soft text-ink">
      {/* Top Banner */}
      <TopAnnouncementBar />

      {/* Sticky Main Header */}
      <Header />

      {/* Main Page Body */}
      <main className="flex-1">
        <Outlet />
      </main>

      {/* Footer */}
      <Footer />

      {/* Global Slide-Over Drawers & Modals */}
      <CartDrawer />
      <AiChatDrawer />
      <AiFloatingButton />
      <AuthModal />
    </div>
  );
}
