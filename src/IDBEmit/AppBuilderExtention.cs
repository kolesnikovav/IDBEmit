using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.EntityFrameworkCore;

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
        /// Adds IDBEmit as singletone service and register it in Dependency Injection
        ///
        /// </summary>
        /// <typeparam name="T">The type of your DBContext/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        public static void AddDBEmitter<T>(this IServiceCollection services) where T: DbContext
        {
            services.AddSingleton(typeof(DBEmitService<T>));
        }
        private static DBEmitService<T> GetDBEmitService<T>(IApplicationBuilder builder) where T: DbContext
        {
            return (DBEmitService<T>)builder.ApplicationServices.GetService(typeof(DBEmitService<T>));
        }
        /// <summary>
        /// Adds IndexedDB projection of current DBContext.
        /// </summary>
        /// <typeparam name="T">The DBContext derived type</typeparam>
        /// <param name="appBuilder">The instance of ApplicationBuilder</param>
        /// <param name="indexedDBname">The name of IndexedDB</param>
        /// <param name="path">path</param>
        public static void UseIDBEmitter<T>(
            this IApplicationBuilder appBuilder,
            string indexedDBname,
            string path
            ) where T: DbContext
        {
            if (appBuilder == null)
            {
                throw new ArgumentNullException(nameof(appBuilder));
            }
            var logger = GetOrCreateLogger(appBuilder, LogCategoryName);
            DBEmitService<T> emitter = GetDBEmitService<T>(appBuilder);
            emitter.Initialize(path, indexedDBname);
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