import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import Layout from '../../components/layout/Layout';
import HomePage from '../../features/home/pages/HomePage';
import ProductsPage from '../../features/products/pages/ProductsPage';
import ProductDetailPage from '../../features/products/pages/ProductDetailPage';
import CategoriesPage from '../../features/categories/pages/CategoriesPage';
import CartPage from '../../features/cart/pages/CartPage';
import CheckoutPage from '../../features/orders/pages/CheckoutPage';
import OrdersPage from '../../features/orders/pages/OrdersPage';
import LoginPage from '../../features/auth/pages/LoginPage';
import RegisterPage from '../../features/auth/pages/RegisterPage';

export default function AppRouter() {
  return (
    <Routes>
      <Route element={<Layout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/products" element={<ProductsPage />} />
        <Route path="/products/:slug" element={<ProductDetailPage />} />
        <Route path="/categories" element={<CategoriesPage />} />
        <Route path="/categories/:slug" element={<CategoriesPage />} />
        <Route path="/cart" element={<CartPage />} />
        <Route path="/checkout" element={<CheckoutPage />} />
        <Route path="/orders" element={<OrdersPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}
