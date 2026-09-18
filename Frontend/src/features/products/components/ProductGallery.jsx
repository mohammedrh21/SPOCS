import React, { useState } from 'react';

export default function ProductGallery({ images = [], productName = 'Product' }) {
  const defaultImage = images.find((img) => img.isPrimary)?.imageUrl || images[0]?.imageUrl || '';
  const [selectedImage, setSelectedImage] = useState(defaultImage);

  // Sync selectedImage if images array updates
  React.useEffect(() => {
    if (images.length > 0) {
      const primary = images.find((img) => img.isPrimary)?.imageUrl || images[0]?.imageUrl || '';
      setSelectedImage(primary);
    }
  }, [images]);

  return (
    <div className="space-y-4">
      {/* Main Large Image Container */}
      <div className="relative w-full aspect-square bg-white rounded-3xl border border-surface-subtle p-8 sm:p-12 flex items-center justify-center overflow-hidden shadow-card">
        {selectedImage ? (
          <img
            src={selectedImage}
            alt={productName}
            className="w-full h-full object-contain mix-blend-multiply transition-all duration-300 hover:scale-105"
          />
        ) : (
          <div className="w-full h-full bg-surface-muted rounded-2xl flex items-center justify-center text-sm font-bold text-slate-400">
            No Image Available
          </div>
        )}
      </div>

      {/* Thumbnails Row */}
      {images.length > 1 && (
        <div className="flex items-center gap-3 overflow-x-auto pb-2 no-scrollbar">
          {images.map((img) => {
            const isSelected = selectedImage === img.imageUrl;
            return (
              <button
                key={img.id || img.imageUrl}
                type="button"
                onClick={() => setSelectedImage(img.imageUrl)}
                className={`relative w-20 h-20 rounded-2xl bg-white p-2 border shrink-0 transition-all flex items-center justify-center overflow-hidden ${
                  isSelected
                    ? 'border-ink ring-2 ring-ink ring-offset-2'
                    : 'border-surface-subtle hover:border-slate-400 opacity-70 hover:opacity-100'
                }`}
              >
                <img
                  src={img.imageUrl}
                  alt={img.altText || productName}
                  className="w-full h-full object-contain mix-blend-multiply"
                />
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
}
