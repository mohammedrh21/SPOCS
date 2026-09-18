import React from 'react';
import { cn } from '../../utils/cn';

export function Skeleton({ className, ...props }) {
  return (
    <div
      className={cn("animate-pulse rounded-xl bg-surface-muted", className)}
      {...props}
    />
  );
}

export function ProductCardSkeleton() {
  return (
    <div className="bg-white rounded-2xl border border-surface-subtle overflow-hidden p-4 space-y-4 shadow-card">
      <Skeleton className="w-full aspect-square rounded-xl" />
      <div className="space-y-2">
        <Skeleton className="h-4 w-1/3" />
        <Skeleton className="h-5 w-4/5" />
        <Skeleton className="h-4 w-2/5" />
      </div>
      <div className="flex items-center justify-between pt-2">
        <Skeleton className="h-6 w-1/4" />
        <Skeleton className="h-9 w-9 rounded-full" />
      </div>
    </div>
  );
}
