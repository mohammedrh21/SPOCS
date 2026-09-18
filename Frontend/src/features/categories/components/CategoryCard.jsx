import React from 'react';
import { Link } from 'react-router-dom';
import { ArrowUpRight } from 'lucide-react';
import { CATEGORY_HERO_IMAGES } from '../../../constants/config';

export default function CategoryCard({ category }) {
  const bgImage = category.imageUrl || CATEGORY_HERO_IMAGES[category.slug] || 'https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=800&auto=format&fit=crop&q=80';

  return (
    <Link
      to={`/categories/${category.slug}`}
      className="group relative h-80 rounded-3xl overflow-hidden block shadow-card hover:shadow-card-hover transition-all duration-300"
    >
      {/* Background Image */}
      <img
        src={bgImage}
        alt={category.name}
        className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-700 brightness-[0.85] group-hover:brightness-[0.75]"
      />

      {/* Gradient Overlay */}
      <div className="absolute inset-0 bg-gradient-to-t from-ink/90 via-ink/30 to-transparent" />

      {/* Content */}
      <div className="absolute inset-0 p-6 flex flex-col justify-between text-white">
        <div className="flex justify-end">
          <div className="w-10 h-10 rounded-full bg-white/20 backdrop-blur-md flex items-center justify-center text-white group-hover:bg-white group-hover:text-ink transition-all">
            <ArrowUpRight className="w-5 h-5 group-hover:translate-x-0.5 group-hover:-translate-y-0.5 transition-transform" />
          </div>
        </div>

        <div className="space-y-1">
          {category.productCount !== undefined && (
            <p className="text-xs font-bold text-accent-orange uppercase tracking-wider">
              {category.productCount} {category.productCount === 1 ? 'Product' : 'Products'}
            </p>
          )}
          <h3 className="nike-heading text-2xl sm:text-3xl text-white tracking-tight">
            {category.name}
          </h3>
          {category.description && (
            <p className="text-xs text-slate-300 line-clamp-1 max-w-xs font-normal">
              {category.description}
            </p>
          )}
        </div>
      </div>
    </Link>
  );
}
