using CardinalEventStationApp.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CardinalEventStationApp.Services
{
    /// <summary>
    /// Requires NuGet Packages:
    /// -- Newtonsoft.Json v11.02 - Shared project only
    /// </summary>
    public class UnAuthenticatedRequestService : IRequestService
    {
        private readonly JsonSerializerSettings _serializerSettings;
        private readonly HttpClient _client;
        private readonly String _endPoint;

        private bool _initialized { get; set; }

        public UnAuthenticatedRequestService()
        {
            _initialized = false;
            _serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Ignore
            };
            _serializerSettings.Converters.Add(new StringEnumConverter());
            _client = new HttpClient
            {
                MaxResponseContentBufferSize = 256000
            };
            _endPoint = Constants.CardinalWebApiBase;
            _initialized = true;
        }

        public async Task<Boolean> PostAuthenticationRequestAsync(string username, string password, bool persistent)
        {
            var parameters = new List<KeyValuePair<string, string>>();
            parameters.Add(new KeyValuePair<string, string>("username", username));
            parameters.Add(new KeyValuePair<string, string>("password", password));
            parameters.Add(new KeyValuePair<string, string>("persistent", persistent.ToString()));

            var content = new FormUrlEncodedContent(parameters);
            HttpResponseMessage response = await _client.PostAsync(_endPoint + "Token", content);
            if (response.IsSuccessStatusCode)
            {
                _initialized = true;
                return true;
            }
            return false;
        }

        private void CheckInitialization()
        {
            if (!_initialized)
                throw new InvalidOperationException("UnAuthenticatedRequestService has not been initialized");

            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<TResult> GetAsync<TResult>(string uri)
        {
            CheckInitialization();
            if (!String.IsNullOrEmpty(_endPoint))
            {
                uri = _endPoint + uri;
            }
            HttpResponseMessage response = await _client.GetAsync(uri);
            await HandleResponse(response);
            string serialized = await response.Content.ReadAsStringAsync();
            TResult result = await Task.Run(() => JsonConvert.DeserializeObject<TResult>(serialized, _serializerSettings));
            return result;
        }

        public Task<TResult> PostAsync<TResult>(string uri, TResult data)
        {
            return PostAsync<TResult, TResult>(uri, data);
        }

        public async Task<TResult> PostAsync<TRequest, TResult>(string uri, TRequest data, bool auth = true)
        {
            if (auth)
            {
                CheckInitialization();
            }
            if (!String.IsNullOrEmpty(_endPoint))
            {
                uri = _endPoint + uri;
            }
            HttpResponseMessage response;
            // Special-case for login
            if (data is IEnumerable<KeyValuePair<string, string>> parameters)
            {
                var content = new FormUrlEncodedContent(parameters);
                response = await _client.PostAsync(uri, content);
                if (!response.IsSuccessStatusCode)
                {
                    return await Task.Run(() => JsonConvert.DeserializeObject<TResult>("false", _serializerSettings));
                }
            }
            else
            {
                string serialized = await Task.Run(() => JsonConvert.SerializeObject(data, _serializerSettings));
                response = await _client.PostAsync(uri, new StringContent(serialized, Encoding.UTF8, "application/json"));
            }
            await HandleResponse(response);
            string responseData = await response.Content.ReadAsStringAsync();
            //handle special case for login -- assumes TResult is bool
            if (!auth && uri.EndsWith("api/Token"))
            {
                _initialized = true;
                responseData = "true";
            }
            return await Task.Run(() => JsonConvert.DeserializeObject<TResult>(responseData, _serializerSettings));
        }

        public async Task<TResult> PutAsync<TResult>(string uri, TResult data)
        {
            return await PutAsync<TResult, TResult>(uri, data);
        }

        public async Task<TResult> PutAsync<TRequest, TResult>(string uri, TRequest data)
        {
            CheckInitialization();
            if (!String.IsNullOrEmpty(_endPoint))
            {
                uri = _endPoint + uri;
            }
            string serialized = await Task.Run(() => JsonConvert.SerializeObject(data, _serializerSettings));
            HttpResponseMessage response = await _client.PutAsync(uri, new StringContent(serialized, Encoding.UTF8, "application/json"));
            await HandleResponse(response);
            string responseData = await response.Content.ReadAsStringAsync();
            return await Task.Run(() => JsonConvert.DeserializeObject<TResult>(responseData, _serializerSettings));
        }

        public async Task<TResult> DeleteAsync<TResult>(string uri)
        {
            CheckInitialization();
            if (!String.IsNullOrEmpty(_endPoint))
            {
                uri = _endPoint + uri;
            }
            HttpResponseMessage response = await _client.DeleteAsync(uri);
            await HandleResponse(response);
            string serialized = await response.Content.ReadAsStringAsync();
            TResult result = await Task.Run(() => JsonConvert.DeserializeObject<TResult>(serialized, _serializerSettings));
            return result;
        }


        private async Task HandleResponse(HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.Forbidden || response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException(content);
                }
                throw new HttpRequestException(content);
            }
        }
    }
}
