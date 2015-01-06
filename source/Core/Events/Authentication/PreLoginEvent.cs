﻿/*
 * Copyright 2014 Dominick Baier, Brock Allen
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Thinktecture.IdentityServer.Core.Events
{
    /// <summary>
    /// Event class for pre-login events
    /// </summary>
    public class PreLoginEvent : AuthenticationEventBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PreLoginEvent"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        public PreLoginEvent(EventType type)
            : base(EventConstants.Ids.PreLogin, type)
        {
            if (type == EventType.Success)
            {
                Message = Resources.Events.PreLoginSuccess;
            }
            else if (type == EventType.Failure)
            {
                Message = Resources.Events.PreLoginFailure;
            }
            else
            {
                Message = "Pre-login event";
            }
        }
    }
}