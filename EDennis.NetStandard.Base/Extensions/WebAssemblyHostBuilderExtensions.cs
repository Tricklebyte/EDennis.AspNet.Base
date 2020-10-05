﻿using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace EDennis.NetStandard.Base.Extensions {
    public static class WebAssemblyHostBuilderExtensions {


        /// <summary>
        /// Adds ApiClients (each with HttpClient and configured HttpMethodHandler), 
        ///     using settings from Configuration ("ApiClients" section)
        /// Calls AddAuthorizationCore() and AddOidcAuthentication, 
        ///     using OIDC settings from Configuration sections (Oidc:ProviderOptions, Oidc:UserOptions)
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="apiClientTypes"></param>
        public static void AddApiClients(this WebAssemblyHostBuilder builder, params Type[] apiClientTypes) {

            var apiClients = new ApiClients();
            builder.Configuration.Bind("ApiClients", apiClients);
            builder.Services.Configure<ApiClients>(builder.Configuration.GetSection("ApiClients"));


            foreach (var apiClientType in apiClientTypes) {

                var d1 = typeof(ConfigurableAuthorizationMessageHandler<>);
                Type[] typeArgs = { apiClientType };
                var configurableAuthorizationMethodHandlerGenericType = d1.MakeGenericType(typeArgs);


                //AddTransient<>
                var addTransientMethod = typeof(ServiceCollectionServiceExtensions)
                    .GetMethod(nameof(ServiceCollectionServiceExtensions.AddTransient));
                var addTransientGenericMethod = addTransientMethod.MakeGenericMethod(configurableAuthorizationMethodHandlerGenericType);
                addTransientGenericMethod.Invoke(builder.Services, null);

                //Get ApiClient settings from Configuration.  Throw exception if key not found
                if (!apiClients.TryGetValue(apiClientType.Name, out ApiClient apiClient))
                    throw new Exception($"{apiClientType.Name} cannot be found in ApiClients section of Configuration");

                //AddHttpClient()
                builder.Services.AddHttpClient(apiClientType.Name, client => client.BaseAddress = new Uri(apiClient.TargetUrl));

                //AddHttpMessageHandler<ConfigurableAuthorizationMethodHandler<>>
                var methodInfo = typeof(HttpClientBuilderExtensions)
                    .GetMethod(nameof(HttpClientBuilderExtensions.AddHttpMessageHandler));
                var genericMethod = methodInfo.MakeGenericMethod(configurableAuthorizationMethodHandlerGenericType);
                genericMethod.Invoke(builder.Services, null);
            }

            builder.Services.AddAuthorizationCore();
            builder.Services.AddOidcAuthentication(options => {
                // see https://aka.ms/blazor-standalone-auth 
                builder.Configuration.Bind("Oidc:ProviderOptions", options.ProviderOptions);
                builder.Configuration.Bind("Oidc:UserOptions", options.UserOptions);
            });


        }
    }
}
