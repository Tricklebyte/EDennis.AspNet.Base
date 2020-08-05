﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EDennis.NetStandard.Base {
    public class DefaultPoliciesAuthorizationPolicyProvider : IAuthorizationPolicyProvider {

        private AuthorizationOptions _options;
        private Task<AuthorizationPolicy> _cachedPolicyTask;
        private readonly IConfiguration _configuration;
        //outerkey is the scope policy (the default policy associated with the action method)
        //inner key is the pattern that matches either negatively or positively
        public ConcurrentDictionary<string, ConcurrentDictionary<string, bool>> PolicyPatternCacheSet { get; private set; }
        private readonly ILogger _logger;


        public DefaultPoliciesAuthorizationPolicyProvider(IConfiguration configuration,
            ILogger logger) {

            _configuration = configuration;
            PolicyPatternCacheSet = new ConcurrentDictionary<string, ConcurrentDictionary<string,bool>>();
            _logger = logger;
        }

        /// <summary>
        /// Gets the default authorization policy.
        /// </summary>
        /// <returns>The default authorization policy.</returns>
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() {
            if (_options == null)
                BuildPolicyOptions();
            return GetCachedPolicy(ref _cachedPolicyTask, _options.DefaultPolicy);
        }


        private Task<AuthorizationPolicy> GetCachedPolicy(ref Task<AuthorizationPolicy> cachedPolicy, AuthorizationPolicy currentPolicy) {
            if (_options == null)
                BuildPolicyOptions();
            var local = cachedPolicy;
            if (local == null || local.Result != currentPolicy) {
                cachedPolicy = local = Task.FromResult(currentPolicy);
            }
            return local;
        }

        /// <summary>
        /// Gets a <see cref="AuthorizationPolicy"/> from the given <paramref name="policyName"/>
        /// </summary>
        /// <param name="policyName">The policy name to retrieve.</param>
        /// <returns>The named <see cref="AuthorizationPolicy"/>.</returns>
        public virtual Task<AuthorizationPolicy> GetPolicyAsync(string policyName) {
            // MVC caches policies specifically for this class, so this method MUST return the same policy per
            // policyName for every request or it could allow undesired access. It also must return synchronously.
            // A change to either of these behaviors would require shipping a patch of MVC as well.
            if (_options == null)
                BuildPolicyOptions();
            return Task.FromResult(_options.GetPolicy(policyName));
        }

        private void BuildPolicyOptions() {
            _logger.LogTrace("Building default policies");
            _options = new AuthorizationOptions();

            //***
            //*** Get the DefaultPolicies added to configuration 
            //*** by AddDefaultAuthorizationPolicyConvention
            //***
            List<string> policies = new List<string>();
            _configuration.Bind(DefaultAuthorizationPolicyConvention.DEFAULT_POLICIES_KEY, policies);

            if (policies.Count > 0) {
                foreach (var policy in policies)
                    _options.AddPolicy(policy, builder => {
                        var policyPatternCache = PolicyPatternCacheSet.GetOrAdd(policy, new ConcurrentDictionary<string, bool>());
                        builder.RequireClaimPatternMatch(policy, policyPatternCache, _logger);
                    });
            }

        }

        /// <summary>
        /// This is unnecessary because comprehensive policies are generated
        /// by this policy provider.
        /// </summary>
        /// <returns></returns>
        public Task<AuthorizationPolicy> GetFallbackPolicyAsync() {
            //per https://github.com/googleapis/google-api-dotnet-client/issues/1434#issuecomment-527104967
            var result = new AuthorizationPolicyBuilder();
            result = result.RequireAssertion(context =>
            {
                return true;
            });

            return Task.FromResult(result.Build());
        }

    }
}