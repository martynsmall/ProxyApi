﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.Mvc;

namespace ProxyApi
{
	 /// <summary>
	/// Validates Anti-Forgery CSRF tokens for Web API based on a cookie and a header value.
	/// </summary>
	/// <remarks>
	/// Adapted from the MVC 4 SPA template
	/// </remarks>
	public class ValidateHttpAntiForgeryTokenAttribute : AuthorizationFilterAttribute
	{
		/// <summary>
		/// Name of the header that is expected to contain the request verification token.
		/// </summary>
		public const string RequestVerificationTokenHeader = "X-RequestVerificationToken";
		
		private IContextProvider _contextProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="ValidateHttpAntiForgeryTokenAttribute" /> class.
		/// </summary>
		/// <param name="contextProvider">The context provider.</param>
		public ValidateHttpAntiForgeryTokenAttribute(IContextProvider contextProvider)
		{
			if (contextProvider == null) throw new ArgumentNullException("contextProvider");

			_contextProvider = contextProvider;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ValidateHttpAntiForgeryTokenAttribute" /> class.
		/// </summary>
		public ValidateHttpAntiForgeryTokenAttribute()
			: this(new ContextProvider())
		{}

		/// <summary>
		/// Calls when a process requests authorization.
		/// </summary>
		/// <param name="actionContext">The action context, which encapsulates information for using <see cref="T:System.Web.Http.Filters.AuthorizationFilterAttribute" />.</param>
		[ExcludeFromCodeCoverage] //requires AntiForgery access
		public override void OnAuthorization(HttpActionContext actionContext)
		{
			if (actionContext == null) throw new ArgumentNullException("actionContext");

			var request = actionContext.ControllerContext.Request;

			try
			{
				if (ShouldValidateRequest(actionContext))
					ValidateRequestHeader(request);
			}
			catch (HttpAntiForgeryException e)
			{
				actionContext.Response = request.CreateErrorResponse(HttpStatusCode.Forbidden, e);
			}
		}

		/// <summary>
		/// A list of authentication types for which the token will
		/// not be checked.  This should not be specified in conjunction
		/// with <see cref="IncludeAuthenticationTypes"/>.
		/// </summary>
		public string ExcludeAuthenticationTypes { get; set; }

		/// <summary>
		/// A list of authentication types for which the token will
		/// be checked.  This should not be specified in conjunction
		/// with <see cref="ExcludeAuthenticationTypes"/>.
		/// </summary>
		public string IncludeAuthenticationTypes { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether authentication is required
		/// </summary>
		public bool RequireAuthentication { get; set; }

		/// <summary>
		/// Determines whether or not a request should be validated
		/// </summary>
		/// <param name="actionContext">The action context.</param>
		/// <returns><c>true</c> if the request should be validated.</returns>
		public virtual bool ShouldValidateRequest(HttpActionContext actionContext)
		{
			if (this.ExcludeAuthenticationTypes == null && this.IncludeAuthenticationTypes == null)
				return true;

			var identity = _contextProvider.GetCurrentIdentity();
			if (this.RequireAuthentication && !identity.IsAuthenticated) return false;
			if (!identity.IsAuthenticated) return true;

			if (this.IncludeAuthenticationTypes != null)
				return IsAuthenticatedWithType(identity, this.IncludeAuthenticationTypes);

			return !IsAuthenticatedWithType(identity, this.ExcludeAuthenticationTypes);
		}

		private bool IsAuthenticatedWithType(IIdentity identity, string authenticationTypes)
		{
			return authenticationTypes
					.Split(',')
					.Select(t => t.Trim())
					.Contains(identity.AuthenticationType);
		}

		[ExcludeFromCodeCoverage] //requires AntiForgery access
		private void ValidateRequestHeader(HttpRequestMessage request)
		{
			var formToken   = string.Empty;
			var cookieToken = request.Headers
				.GetCookies()
				.Select(c => c[AntiForgeryConfig.CookieName])
				.Select(c => c.Value)
				.FirstOrDefault();

			IEnumerable<string> tokenHeaders;
			if (request.Headers.TryGetValues(RequestVerificationTokenHeader, out tokenHeaders))
				formToken = tokenHeaders.FirstOrDefault();

			AntiForgery.Validate(cookieToken, formToken);
		}
	}
}
