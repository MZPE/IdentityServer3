﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Claims;
using Thinktecture.IdentityServer.Core.Authentication;
using Thinktecture.IdentityServer.Core.Connect.Models;
using Thinktecture.IdentityServer.Core.Services;

namespace Thinktecture.IdentityServer.Core.Connect
{
    public class AuthorizeInteractionResponseGenerator
    {
        private SignInMessage _signIn;
        private ICoreSettings _core;
        
        private IConsentService _consent;

        public AuthorizeInteractionResponseGenerator(ICoreSettings core, IConsentService consent)
        {
            _signIn = new SignInMessage();
            
            _core = core;
            _consent = consent;
        }

        public InteractionResponse ProcessLogin(ValidatedAuthorizeRequest request, ClaimsPrincipal user)
        {
            // pass through display mode to signin service
            if (request.DisplayMode.IsPresent())
            {
                _signIn.DisplayMode = request.DisplayMode;
            }

            // pass through ui locales to signin service
            if (request.UiLocales.IsPresent())
            {
                _signIn.UILocales = request.UiLocales;
            }

            // unauthenticated user
            if (!user.Identity.IsAuthenticated)
            {
                // prompt=none means user must be signed in already
                if (request.PromptMode == Constants.PromptModes.None)
                {
                    return new InteractionResponse
                    {
                        IsError = true,
                        Error = new AuthorizeError
                        {
                            ErrorType = ErrorTypes.Client,
                            Error = Constants.AuthorizeErrors.InteractionRequired,
                            ResponseMode = request.ResponseMode,
                            ErrorUri = request.RedirectUri,
                            State = request.State
                        }
                    };
                }

                return new InteractionResponse
                {
                    IsLogin = true,
                    SignInMessage = _signIn
                };
            }

            // prompt=login

            // clear the auth cookie
            // remove the prompt=login
            // redirect to login page

            // check authentication freshness
            if (request.MaxAge.HasValue)
            {
                var authTime = user.GetAuthenticationTime();
                if (DateTime.UtcNow > authTime.AddSeconds(request.MaxAge.Value))
                {
                    return new InteractionResponse
                    {
                        IsLogin = true,
                        SignInMessage = _signIn
                    };
                }
            }
    
            return new InteractionResponse();
        }

        public InteractionResponse ProcessConsent(ValidatedAuthorizeRequest request, ClaimsPrincipal user, UserConsent consent)
        {
            if (request.PromptMode == Constants.PromptModes.Consent ||
                _consent.RequiresConsent(request.Client, user, request.RequestedScopes))
            {
                var response = new InteractionResponse();

                // did user provide consent
                if (consent == null)
                {
                    // user was not yet shown conset screen
                    response.IsConsent = true;
                }
                else
                {
                    request.WasConsentShown = true;

                    // user was shown consent -- did they say yes or no
                    if (consent.WasConsentGranted == false)
                    {
                        // no need to show consent screen again
                        // build access denied error to return to client
                        response.IsError = true;
                        response.Error = new AuthorizeError { 
                            ErrorType = ErrorTypes.Client,
                            Error = Constants.AuthorizeErrors.AccessDenied,
                            ResponseMode = request.ResponseMode,
                            ErrorUri = request.RedirectUri, 
                            State = request.State
                        };
                    }
                    else
                    {
                        // user said yes, so let's validate the scopes they granted
                        var requestedScopes =
                            from s in _core.GetScopes()
                            where request.RequestedScopes.Contains(s.Name)
                            select s;

                        // the user has consented to all required scopes requested from client
                        // and then any others they picked on the consent screen
                        var consentedScopes = 
                            from s in requestedScopes
                            where s.Required || consent.ScopedConsented.Contains(s.Name)
                            select s;
                        
                        if (!consentedScopes.Any())
                        {
                            // they said yes, but didn't pick any scopes
                            // show consent again and provide error message
                            response.IsConsent = true;
                            response.ConsentError = "Must select at least one permission.";
                        }
                        else
                        {
                            // they said yes, and chose scopes
                            // so adjust requested scopes to consented scopes
                            request.RequestedScopes = consentedScopes.Select(x => x.Name).ToList();
                        }
                    }
                }
                
                return response;
            }

            return new InteractionResponse();
        }
    }
}
