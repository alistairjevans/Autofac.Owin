﻿// This software is part of the Autofac IoC container
// Copyright © 2014 Autofac Contributors
// http://autofac.org
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
using System;
using System.ComponentModel;
using System.Linq;
using Autofac;
using Autofac.Core;
using Autofac.Core.Lifetime;
using Autofac.Integration.Owin;
using Microsoft.Owin;

namespace Owin
{
    /// <summary>
    /// Extension methods for configuring Autofac within the OWIN pipeline.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class AutofacAppBuilderExtensions
    {
        /// <summary>
        /// Unique key used to indicate the middleware for injecting the request lifetime scope has been registered with the application.
        /// </summary>
        private static readonly string InjectorRegisteredKey = "AutofacLifetimeScopeInjectorRegistered:" + Constants.AutofacMiddlewareBoundary;

        /// <summary>
        /// Determines if the Autofac lifetime scope injector middleware is
        /// registered with the application pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>
        /// <see langword="true" /> if the Autofac lifetime scope injector has been registered
        /// with the <paramref name="app" />; <see langword="false" /> if not.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="app" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This method is useful when composing an application where you may
        /// accidentally register more than one Autofac lifetime scope injector
        /// with the pipeline - for example, accidentally calling both
        /// <see cref="UseAutofacMiddleware(IAppBuilder, ILifetimeScope)"/>
        /// and <see cref="UseAutofacLifetimeScopeInjector(IAppBuilder, ILifetimeScope)"/>
        /// on the same <see cref="IAppBuilder"/>. This allows you to check
        /// an <see cref="IAppBuilder"/> and only add Autofac to the pipeline
        /// if it hasn't already been registered.
        /// </para>
        /// </remarks>
        public static bool IsAutofacLifetimeScopeInjectorRegistered(this IAppBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }

            return app.Properties.ContainsKey(InjectorRegisteredKey);
        }

        /// <summary>
        /// Adds middleware to inject a request-scoped Autofac lifetime scope into the OWIN pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="container">The root Autofac application lifetime scope/container.</param>
        /// <returns>The application builder.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="app" /> or <paramref name="container" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This extension is used when separating the notions of injecting the
        /// lifetime scope and adding middleware to the pipeline from the container.
        /// </para>
        /// <para>
        /// Since middleware registration order matters, generally you want the
        /// Autofac request lifetime scope registered early in the pipeline, but
        /// you may not want the middleware registered with Autofac added to the
        /// pipeline until later.
        /// </para>
        /// <para>
        /// This method gets used in conjunction with <see cref="UseMiddlewareFromContainer{T}(IAppBuilder)" />.
        /// Do not use this with <see cref="UseAutofacMiddleware(IAppBuilder, ILifetimeScope)"/>
        /// or you'll get unexpected results!
        /// </para>
        /// </remarks>
        /// <example>
        /// <code lang="C#">
        /// app
        ///   .UseAutofacLifetimeScopeInjector(container)
        ///   .UseBasicAuthentication()
        ///   .Use((c, next) =&gt;
        ///   {
        ///     //authorization
        ///     return next();
        ///   })
        ///   .UseMiddlewareFromContainer&lt;PathRewriter&gt;()
        ///   .UseSendFileFallback()
        ///   .UseStaticFiles();
        /// </code>
        /// </example>
        /// <seealso cref="UseMiddlewareFromContainer{T}(IAppBuilder)" />
        public static IAppBuilder UseAutofacLifetimeScopeInjector(this IAppBuilder app, ILifetimeScope container)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }

            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            return app.RegisterAutofacLifetimeScopeInjector(container);
        }

        /// <summary>
        /// Adds middleware to both inject a request-scoped Autofac lifetime scope into the OWIN pipeline
        /// as well as add all middleware components registered with Autofac.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="container">The root Autofac application lifetime scope/container.</param>
        /// <returns>The application builder.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="app" /> or <paramref name="container" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This extension registers the Autofac lifetime scope and all Autofac-registered
        /// middleware into the application at the same time. This is the simplest
        /// way to integrate Autofac into OWIN but has the least control over
        /// pipeline construction.
        /// </para>
        /// <para>
        /// Do not use this with <see cref="UseAutofacLifetimeScopeInjector(IAppBuilder, ILifetimeScope)"/>
        /// or <see cref="UseMiddlewareFromContainer{T}(IAppBuilder)"/>
        /// or you'll get unexpected results!
        /// </para>
        /// </remarks>
        /// <example>
        /// <code lang="C#">
        /// app
        ///   .UseAutofacMiddleware(container)
        ///   .UseBasicAuthentication()
        ///   .Use((c, next) =&gt;
        ///   {
        ///     //authorization
        ///     return next();
        ///   })
        ///   .UseSendFileFallback()
        ///   .UseStaticFiles();
        /// </code>
        /// </example>
        public static IAppBuilder UseAutofacMiddleware(this IAppBuilder app, ILifetimeScope container)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }

            if (container == null)
            {
                throw new ArgumentNullException("container");
            }

            return app
                .RegisterAutofacLifetimeScopeInjector(container)
                .UseAllMiddlewareRegisteredInContainer(container);
        }

        /// <summary>
        /// Adds a middleware to the OWIN pipeline that will be constructed using Autofac
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The application builder.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// Thrown if <paramref name="app" /> is <see langword="null" />.
        /// </exception>
        /// <remarks>
        /// <para>
        /// This extension is used when separating the notions of injecting the
        /// lifetime scope and adding middleware to the pipeline from the container.
        /// </para>
        /// <para>
        /// Since middleware registration order matters, generally you want the
        /// Autofac request lifetime scope registered early in the pipeline, but
        /// you may not want the middleware registered with Autofac added to the
        /// pipeline until later.
        /// </para>
        /// <para>
        /// This method gets used in conjunction with <see cref="UseAutofacLifetimeScopeInjector(IAppBuilder, ILifetimeScope)" />.
        /// Do not use this with <see cref="UseAutofacMiddleware(IAppBuilder, ILifetimeScope)"/>
        /// or you'll get unexpected results!
        /// </para>
        /// </remarks>
        /// <example>
        /// <code lang="C#">
        /// app
        ///   .UseAutofacLifetimeScopeInjector(container)
        ///   .UseBasicAuthentication()
        ///   .Use((c, next) =&gt;
        ///   {
        ///     //authorization
        ///     return next();
        ///   })
        ///   .UseMiddlewareFromContainer&lt;PathRewriter&gt;()
        ///   .UseSendFileFallback()
        ///   .UseStaticFiles();
        /// </code>
        /// </example>
        /// <seealso cref="UseAutofacLifetimeScopeInjector(IAppBuilder, ILifetimeScope)" />
        public static IAppBuilder UseMiddlewareFromContainer<T>(this IAppBuilder app)
            where T : OwinMiddleware
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }

            return app.Use<AutofacMiddleware<T>>();
        }

        private static IAppBuilder RegisterAutofacLifetimeScopeInjector(this IAppBuilder app, ILifetimeScope container)
        {
            app.Use(async (context, next) =>
            {
                using (var lifetimeScope = container.BeginLifetimeScope(MatchingScopeLifetimeTags.RequestLifetimeScopeTag,
                    b => b.RegisterInstance(context).As<IOwinContext>()))
                {
                    context.Set(Constants.OwinLifetimeScopeKey, lifetimeScope);
                    await next();
                }
            });

            app.Properties[InjectorRegisteredKey] = true;
            return app;
        }

        private static IAppBuilder UseAllMiddlewareRegisteredInContainer(this IAppBuilder app, IComponentContext container)
        {
            var services = container.ComponentRegistry.Registrations.SelectMany(r => r.Services)
                .OfType<TypedService>()
                .Where(s => s.ServiceType.IsAssignableTo<OwinMiddleware>() && !s.ServiceType.IsAbstract)
                .Select(service => typeof(AutofacMiddleware<>).MakeGenericType(service.ServiceType))
                .Where(serviceType => !container.IsRegistered(serviceType));

            var typedServices = services.ToArray();
            if (!typedServices.Any())
            {
                return app;
            }

            foreach (var typedService in typedServices)
            {
                app.Use(typedService);
            }

            return app;
        }
    }
}
