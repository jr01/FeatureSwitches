﻿using System.Security.Claims;
using System.Threading;
using FeatureSwitches.Caching;

namespace FeatureSwitches.Test.IntegrationTest
{
    public class FeatureCacheContextAccessor : IFeatureCacheContextAccessor
    {
        private readonly CurrentCustomer currentCustomer;

        public FeatureCacheContextAccessor(CurrentCustomer currentCustomer)
        {
            this.currentCustomer = currentCustomer;
        }

        public object? GetContext()
        {
            var name = this.currentCustomer.Name;
            if (name is null)
            {
                var identity = Thread.CurrentPrincipal?.Identity as ClaimsIdentity;
                name = identity?.Name;
            }

            return name;
        }
    }
}
