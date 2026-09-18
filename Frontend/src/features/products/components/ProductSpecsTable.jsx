import React, { useState } from 'react';
import { CheckCircle2, Sliders, FileText } from 'lucide-react';
import Tabs from '../../../components/ui/Tabs';

export default function ProductSpecsTable({
  description = '',
  features = [],
  specifications = {},
}) {
  const [activeTab, setActiveTab] = useState('features');

  const specEntries = Object.entries(specifications || {});

  const tabs = [
    { id: 'features', label: `Key Features (${features?.length || 0})` },
    { id: 'specs', label: `Specifications (${specEntries.length})` },
    { id: 'description', label: 'Overview' },
  ];

  return (
    <div className="bg-white rounded-3xl border border-surface-subtle p-6 sm:p-8 shadow-card space-y-6">
      <Tabs
        tabs={tabs}
        activeTab={activeTab}
        onChange={setActiveTab}
      />

      {/* Features Tab */}
      {activeTab === 'features' && (
        <div className="space-y-4">
          {features && features.length > 0 ? (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3.5">
              {features.map((feature, idx) => (
                <div
                  key={idx}
                  className="flex items-start gap-3 p-3.5 rounded-2xl bg-surface-soft/60 border border-surface-subtle"
                >
                  <CheckCircle2 className="w-5 h-5 text-emerald-500 shrink-0 mt-0.5" />
                  <span className="text-xs sm:text-sm font-medium text-ink leading-relaxed">
                    {feature}
                  </span>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-xs text-ink-muted">No specific key features listed for this item.</p>
          )}
        </div>
      )}

      {/* Specifications Tab */}
      {activeTab === 'specs' && (
        <div className="space-y-4">
          {specEntries.length > 0 ? (
            <div className="divide-y divide-surface-subtle border border-surface-subtle rounded-2xl overflow-hidden">
              {specEntries.map(([key, value], idx) => (
                <div
                  key={idx}
                  className={`grid grid-cols-1 sm:grid-cols-3 p-4 gap-2 text-xs sm:text-sm ${
                    idx % 2 === 0 ? 'bg-white' : 'bg-surface-soft/60'
                  }`}
                >
                  <span className="font-bold text-ink-muted sm:col-span-1">{key}</span>
                  <span className="text-ink font-medium sm:col-span-2">{value}</span>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-xs text-ink-muted">No technical specifications provided.</p>
          )}
        </div>
      )}

      {/* Description Tab */}
      {activeTab === 'description' && (
        <div className="prose prose-sm text-ink-muted max-w-none text-xs sm:text-sm leading-relaxed whitespace-pre-line">
          {description || 'No detailed description available.'}
        </div>
      )}
    </div>
  );
}
