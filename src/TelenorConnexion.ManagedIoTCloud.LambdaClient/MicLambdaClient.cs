﻿using Amazon.CognitoIdentity;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TelenorConnexion.ManagedIoTCloud.CognitoIdentity;
using TelenorConnexion.ManagedIoTCloud.LambdaClient.Model;
using TelenorConnexion.ManagedIoTCloud.Model;

namespace TelenorConnexion.ManagedIoTCloud.LambdaClient
{
    [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly")]
    public class MicLambdaClient : MicClient, IMicClient, IAmazonService, IDisposable
    {
        private readonly CognitoAWSCredentials awsCredentials;
        private readonly AmazonLambdaClient lambdaClient;

        #region Constructors

        public static async Task<MicLambdaClient> CreateFromHostname(
            string hostname, CancellationToken cancelToken = default)
        {
            var manifest = await MicManifest.GetMicManifest(hostname, cancelToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            return new MicLambdaClient(manifest);
        }

        public MicLambdaClient(MicManifest manifest) : base(manifest)
        {
            awsCredentials = manifest.CreateEmptyAWSCredentials();
            lambdaClient = new AmazonLambdaClient(awsCredentials,
                manifest.AwsRegion);
        }

        #endregion // Constructors

        #region Overrides

        protected override async Task<TResponse> InvokeClientRequest<TRequest, TResponse>(string actionName, TRequest request, CancellationToken cancelToken = default)
        {
            var lambdaRequest = new InvokeRequest();

            switch (actionName)
            {
                #region Auth API
                case nameof(AuthConfirmSignup):
                    lambdaRequest.Payload = JsonConvert.SerializeObject(new MicLambdaRequest<TRequest>
                    {
                        Action = "CONFIRM_SIGN_UP",
                        Attributes = request
                    });
                    goto case nameof(Manifest.AuthLambda);
                case nameof(AuthLogin):
                    lambdaRequest.Payload = JsonConvert.SerializeObject(new MicLambdaRequest<TRequest>
                    {
                        Action = "LOGIN",
                        Attributes = request
                    });
                    goto case nameof(Manifest.AuthLambda);
                case nameof(Manifest.AuthLambda):
                    lambdaRequest.FunctionName = Manifest.AuthLambda;
                    break;
                #endregion // Auth API
                default:
                    throw new InvalidOperationException("Unknown action name: " + actionName);
            }

            var lambdaResponse = await lambdaClient.InvokeAsync(lambdaRequest, cancelToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            var response = await DeserializeOrThrow<TResponse>(lambdaResponse,
                cancelToken).ConfigureAwait(continueOnCapturedContext: false);

            if (response is MicAuthLoginResponse loginResponse)
                UpdateCredentials(loginResponse);

            return response;
        }

        #endregion // Overrides

        #region Private Helper methods

        protected override void UpdateCredentials(IMicAuthLoginResponse loginResponse)
        {
            base.UpdateCredentials(loginResponse);
            this.AddLoginToCognitoCredentials(awsCredentials);
        }

        private Task<TResponse> DeserializeOrThrow<TResponse>(InvokeResponse lambdaResponse,
            CancellationToken cancelToken)
        {
            using (var textReader = new StreamReader(lambdaResponse.Payload, Encoding.UTF8))
            using (var jsonReader = new JsonTextReader(textReader))
                return DeserializeOrThrow<TResponse>(jsonReader, cancelToken);
        }

        #endregion // Private Helper methods

        #region IAmazonService

        public AmazonLambdaConfig Config => lambdaClient.Config as AmazonLambdaConfig;

        IClientConfig IAmazonService.Config => lambdaClient.Config;

        #endregion // IAmazonService

        #region Dispose

        /// <inheritdoc />
        [DebuggerStepThrough]
        [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly")]
        [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize")]
        public void Dispose()
        {
            lambdaClient.Dispose();
        }

        #endregion // Dispose
    }
}