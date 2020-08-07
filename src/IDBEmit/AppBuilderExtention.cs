using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;
using AutoController;

namespace IDBEmit
{
    /// <summary>
    /// IApplicationBuilder extention for creating IDBEmit service
    ///
    /// </summary>
    public static class AutoControllerExtention
    {
        private const string LogCategoryName = "IDBEmit";
        /// <summary>
        /// Adds IndexedDB projection of current DBContext.
        /// </summary>
        /// <typeparam name="T">The DBContext derived type</typeparam>
        /// <param name="appBuilder">The instance of ApplicationBuilder</param>
        /// <param name="indexedDBname">The name of IndexedDB</param>
        /// <param name="path">path</param>
        /// <param name="options">Autocontroller options</param>
        public static void UseIDBEmitter<T>(
            this IApplicationBuilder appBuilder,
            string indexedDBname,
            string path,
            IAutoControllerOptions options
            ) where T: DbContext
        {
            if (appBuilder == null)
            {
                throw new ArgumentNullException(nameof(appBuilder));
            }
            var logger = GetOrCreateLogger(appBuilder, LogCategoryName);
            // This is not nessesary register it as service
            // because it should run once
            DBEmitService<T> emitter = new DBEmitService<T>();
            emitter.Initialize(path, indexedDBname, options);
            //emitter.Dispose();
        }
        private static ILogger GetOrCreateLogger(
            IApplicationBuilder appBuilder,
            string logCategoryName)
        {
            // If the DI system gives us a logger, use it. Otherwise, set up a default one
            var loggerFactory = appBuilder.ApplicationServices.GetService<ILoggerFactory>();
            var logger = loggerFactory != null
                ? loggerFactory.CreateLogger(logCategoryName)
                : NullLogger.Instance;
            return logger;
        }
    }
}