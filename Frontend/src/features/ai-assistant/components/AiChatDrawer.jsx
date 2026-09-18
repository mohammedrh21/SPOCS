import React, { useState, useRef, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Sparkles, Send, Trash2, ArrowRight } from 'lucide-react';
import Drawer from '../../../components/ui/Drawer';
import Button from '../../../components/ui/Button';
import { useAiChat } from '../../../app/providers/AiChatContext';
import AiChatMessage from './AiChatMessage';
import AiTypingIndicator from './AiTypingIndicator';
import SuggestedPromptChips from './SuggestedPromptChips';

export default function AiChatDrawer() {
  const { isAiOpen, closeAi, messages, isLoading, sendMessage, clearConversation } = useAiChat();
  const [inputMessage, setInputMessage] = useState('');
  const messagesEndRef = useRef(null);
  const inputRef = useRef(null);
  const navigate = useNavigate();

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    if (isAiOpen) {
      setTimeout(() => inputRef.current?.focus(), 100);
      scrollToBottom();
    }
  }, [isAiOpen, messages, isLoading]);

  const handleSubmit = (e) => {
    e.preventDefault();
    if (!inputMessage.trim() || isLoading) return;
    sendMessage(inputMessage);
    setInputMessage('');
  };

  const handleSelectPrompt = (prompt) => {
    sendMessage(prompt);
  };

  const handleProductClick = (product) => {
    closeAi();
    navigate(`/products/${product.slug || product.id}`);
  };

  return (
    <Drawer
      isOpen={isAiOpen}
      onClose={closeAi}
      title="AI Shopping Assistant"
      subtitle="Grounded in live catalog data"
      icon={Sparkles}
      width="max-w-lg"
      footer={
        <form onSubmit={handleSubmit} className="flex items-center gap-2">
          <input
            ref={inputRef}
            type="text"
            value={inputMessage}
            onChange={(e) => setInputMessage(e.target.value)}
            placeholder="Ask about products, specs, comparisons..."
            disabled={isLoading}
            className="flex-1 bg-white border border-surface-subtle text-ink text-xs sm:text-sm rounded-xl px-4 py-2.5 placeholder:text-ink-subtle focus:outline-none focus:border-ink focus:ring-1 focus:ring-ink"
          />
          <Button
            type="submit"
            variant="nike-dark"
            size="md"
            disabled={!inputMessage.trim() || isLoading}
            className="shrink-0 rounded-xl"
            aria-label="Send message"
          >
            <Send className="w-4 h-4" />
          </Button>
        </form>
      }
    >
      <div className="space-y-4 pb-4">
        {/* Top actions & note */}
        <div className="flex items-center justify-between pb-3 border-b border-surface-subtle text-xs text-ink-muted">
          <span className="flex items-center gap-1.5 font-medium text-[11px] text-accent-indigo">
            <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
            GPT-4o Mini + Semantic RAG
          </span>
          <button
            type="button"
            onClick={clearConversation}
            className="flex items-center gap-1 text-ink-subtle hover:text-rose-600 transition-colors text-[11px] font-semibold"
          >
            <Trash2 className="w-3 h-3" />
            Clear Chat
          </button>
        </div>

        {/* Message Feed */}
        <div className="space-y-4">
          {messages.map((msg) => (
            <AiChatMessage
              key={msg.id}
              message={msg}
              onProductClick={handleProductClick}
            />
          ))}

          {isLoading && <AiTypingIndicator />}

          {/* Suggested Prompts when conversation is fresh */}
          {messages.length <= 2 && !isLoading && (
            <div className="pt-2">
              <SuggestedPromptChips onSelectPrompt={handleSelectPrompt} />
            </div>
          )}

          <div ref={messagesEndRef} />
        </div>
      </div>
    </Drawer>
  );
}
