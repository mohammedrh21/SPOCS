
using Microsoft.Extensions.DependencyInjection;
using SPOCS.Application.Contracts;
using SPOCS.Application.Contracts.AI;
using SPOCS.Application.Contracts.Identity;
using SPOCS.Application.Contracts.Services;
using SPOCS.Application.Services;


namespace SPOCS.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            AddServices(services);
            return services;
        }

        private static void AddServices(IServiceCollection services)
        {
            services.AddScoped<ICategoryService, CategoryService>();
            services.AddScoped<IProductService, ProductService>();
            services.AddScoped<ICartService, CartService>();
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<IProductEmbeddingService, ProductEmbeddingService>();
            services.AddScoped<IShoppingToolService, ShoppingToolService>();
            services.AddScoped<IAiChatService, AiChatService>();
        }
    }
}
