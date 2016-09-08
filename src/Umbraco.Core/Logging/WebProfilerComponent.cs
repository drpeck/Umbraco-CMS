﻿using System;
using System.Web;
using Umbraco.Core.Components;

namespace Umbraco.Core.Logging
{
    internal class WebProfilerComponent : UmbracoComponentBase, IUmbracoCoreComponent
    {
        // the profiler is too important to be composed in a component,
        // it is composed first thing in WebRuntime.Compose - this component
        // only initializes it if needed.
        //
        //public override void Compose(ServiceContainer container)
        //{
        //    container.RegisterSingleton<IProfiler, WebProfiler>();
        //}

        private WebProfiler _profiler;

        public void Initialize(IProfiler profiler, IRuntimeState runtime)
        {
            // although registered in WebRuntime.Compose, ensure that we have
            // not been replaced by another component, and we are still "the" profiler
            _profiler = profiler as WebProfiler;
            if (_profiler == null) return;

            if (SystemUtilities.GetCurrentTrustLevel() < AspNetHostingPermissionLevel.High)
            {
                // if we don't have a high enough trust level we cannot bind to the events
                LogHelper.Info<WebProfilerComponent>("Cannot install when the application is running in Medium trust.");
            }
            else if (runtime.Debug == false)
            {
                // only when debugging
                LogHelper.Info<WebProfilerComponent>("Cannot install when the application is not running in debug mode.");
            }
            else
            {
                // bind to ApplicationInit - ie execute the application initialization for *each* application
                // it would be a mistake to try and bind to the current application events
                UmbracoApplicationBase.ApplicationInit += InitializeApplication;
            }
        }

        private void InitializeApplication(object sender, EventArgs args)
        {
            var app = sender as HttpApplication;
            if (app == null) return;

            // for *each* application (this will run more than once)
            app.BeginRequest += (s, a) => _profiler.UmbracoApplicationBeginRequest(s, a);
            app.EndRequest += (s, a) => _profiler.UmbracoApplicationEndRequest(s, a);
        }
    }
}