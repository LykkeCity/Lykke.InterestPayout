using System;
using JetBrains.Annotations;
using Lykke.Cqrs;
using Microsoft.Extensions.DependencyInjection;

namespace InterestPayout.Common.Cqrs
{
    public class ServiceProviderDependencyResolver : IDependencyResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceProviderDependencyResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object GetService([NotNull] Type type)
        {
            return _serviceProvider.GetRequiredService(type);
        }

        public bool HasService(Type type)
        {
            var result = _serviceProvider.GetService(type);
            return result != null;
        }
    }
}
