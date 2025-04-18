﻿using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MoMediatoR
{
    /// <summary>
    /// Extension methods for registering Mediator-related services in an IServiceCollection.
    /// </summary>
    public static class MediatorServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all request and notification handlers from the specified assembly,
        /// as well as the Mediator service itself.
        /// </summary>
        /// <param name="services">The IServiceCollection to register services into.</param>
        /// <param name="assembly">The assembly to scan for request and notification handlers.</param>
        /// <returns>The updated IServiceCollection.</returns>
        public static IServiceCollection AddMoMediatoR(this IServiceCollection services, Assembly assembly)
        {
            // Register all request handlers
            var requestHandlerTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
                .ToList();

            // Register each request handler
            foreach (var type in requestHandlerTypes)
            {
                var interfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

                foreach (var interfaceType in interfaces)
                {
                    // Register the handler as a transient service
                    services.AddTransient(interfaceType, type);
                }
            }

            // Register all notification handlers
            var notificationHandlerTypes = assembly.GetTypes()
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
                .ToList();

            // Register each notification handler
            foreach (var type in notificationHandlerTypes)
            {
                var interfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>));

                foreach (var interfaceType in interfaces)
                {
                    // Register the handler as a transient service
                    services.AddTransient(interfaceType, type);
                }
            }

            // Register the Mediator itself
            services.AddTransient<IMoMediatoR, MoMediatoR>();

            return services;
        }
    }

}
