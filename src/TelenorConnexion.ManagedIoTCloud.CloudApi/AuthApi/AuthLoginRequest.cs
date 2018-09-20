﻿using Newtonsoft.Json;

namespace TelenorConnexion.ManagedIoTCloud.CloudApi.AuthApi
{
    public class AuthLoginRequest : ICloudApiRequestAttributes
    {
        public const string Action = "LOGIN";

        [JsonProperty("userName")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        public CloudApiRequest CreateRequest() =>
            new CloudApiRequest
            {
                Action = Action,
                Attributes = this
            };
    }
}