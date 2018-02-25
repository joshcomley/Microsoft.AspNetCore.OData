// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OData.UriParser;

namespace Brandless.AspNetCore.OData.Extensions
{
	/// <summary>
	/// Captures synchronous and asynchronous exceptions from the pipeline and generates HTML error responses.
	/// </summary>
	public class ODataExceptionMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataExceptionMiddleware"/> class
        /// </summary>
        /// <param name="next"></param>
        /// <param name="options"></param>
        /// <param name="loggerFactory"></param>
        public ODataExceptionMiddleware(
			RequestDelegate next,
			IOptions<DeveloperExceptionPageOptions> options,
			ILoggerFactory loggerFactory)
		{
			if (next == null)
			{
				throw new ArgumentNullException(nameof(next));
			}

			if (options == null)
			{
				throw new ArgumentNullException(nameof(options));
			}

			_next = next;
			_logger = loggerFactory.CreateLogger<DeveloperExceptionPageMiddleware>();
		}

		/// <summary>
		/// Process an individual request.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		public async Task Invoke(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (ODataUnrecognizedPathException ex)
			{
				_logger.LogError(0, ex, "An unhandled exception has occurred while executing the request");

				if (context.Response.HasStarted)
				{
					_logger.LogWarning("The response has already started, the error page middleware will not be executed.");
					throw;
				}

				try
				{
					context.Response.Clear();
					context.Response.StatusCode = 500;
					await context.Response.WriteAsync("Invalid path");

					return;
				}
				catch (Exception ex2)
				{
					// If there's a Exception while generating the error page, re-throw the original exception.
					_logger.LogError(0, ex2, "An exception was thrown attempting to display the error page.");
				}
				throw;
			}
		}

	}
}
