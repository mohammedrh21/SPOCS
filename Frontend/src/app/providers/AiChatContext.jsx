import React, { createContext, useContext, useState, useCallback } from 'react';
import { chatApi } from '../../features/ai-assistant/api/chatApi';
import { useToast } from './ToastContext';

const AiChatContext = createContext(null);

const INITIAL_MESSAGE = {
  id: 'welcome-1',
  role: 'assistant',
  content: "Hi! I'm your SPOCS AI Shopping Assistant. I'm connected directly to our live product catalog. Tell me what you're looking for (e.g. 'I need a fast laptop for programming under $1500' or 'Best headphones for gym') and I'll find the perfect match for you!",
  referencedProducts: [],
  timestamp: new Date().toISOString(),
};

export function AiChatProvider({ children }) {
  const [isAiOpen, setIsAiOpen] = useState(false);
  const [messages, setMessages] = useState([INITIAL_MESSAGE]);
  const [isLoading, setIsLoading] = useState(false);
  const toast = useToast();

  const openAi = () => setIsAiOpen(true);
  const closeAi = () => setIsAiOpen(false);
  const toggleAi = () => setIsAiOpen((prev) => !prev);

  const sendMessage = useCallback(async (text) => {
    if (!text || !text.trim() || isLoading) return;

    const userMessage = {
      id: Date.now().toString(),
      role: 'user',
      content: text.trim(),
      timestamp: new Date().toISOString(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setIsLoading(true);

    try {
      // Build conversation history (excluding initial greeting to keep prompt focused)
      const history = messages
        .filter((m) => m.id !== 'welcome-1')
        .map((m) => ({ role: m.role, content: m.content }));

      const response = await chatApi.sendMessage({
        message: text.trim(),
        history,
        topK: 6,
      });

      const assistantMessage = {
        id: (Date.now() + 1).toString(),
        role: 'assistant',
        content: response.reply || "I couldn't find exact matches for your request.",
        referencedProducts: response.referencedProducts || [],
        timestamp: new Date().toISOString(),
      };

      setMessages((prev) => [...prev, assistantMessage]);
    } catch (err) {
      console.error('AI chat failed', err);
      const errorMsg = err.response?.data?.message || 'Sorry, I ran into an issue finding products. Please try again.';
      toast.error('AI Assistant error');
      setMessages((prev) => [
        ...prev,
        {
          id: (Date.now() + 1).toString(),
          role: 'assistant',
          content: errorMsg,
          referencedProducts: [],
          timestamp: new Date().toISOString(),
        },
      ]);
    } finally {
      setIsLoading(false);
    }
  }, [messages, isLoading, toast]);

  const askAboutProduct = useCallback((product) => {
    openAi();
    sendMessage(`Tell me more about the "${product.name}" and who it is best suited for.`);
  }, [sendMessage]);

  const clearConversation = () => {
    setMessages([INITIAL_MESSAGE]);
  };

  return (
    <AiChatContext.Provider
      value={{
        isAiOpen,
        messages,
        isLoading,
        openAi,
        closeAi,
        toggleAi,
        sendMessage,
        askAboutProduct,
        clearConversation,
      }}
    >
      {children}
    </AiChatContext.Provider>
  );
}

export function useAiChat() {
  const context = useContext(AiChatContext);
  if (!context) {
    throw new Error('useAiChat must be used within an AiChatProvider');
  }
  return context;
}
