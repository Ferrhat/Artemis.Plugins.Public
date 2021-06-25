﻿using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System.Threading.Tasks;
using Artemis.Plugins.Modules.Json.Services.JsonDataModelServices;

namespace Artemis.Plugins.Modules.Json.Controllers
{
    public class JsonController : WebApiController
    {
        private readonly JsonDataModelServices _jsonDataModelServices;

        public JsonController(JsonDataModelServices jsonDataModelServices)
        {
            _jsonDataModelServices = jsonDataModelServices;
        }

        [Route(HttpVerbs.Get, "/json-datamodel/{key}")]
        public async Task GetDataModelByKey(string key)
        {
            if (_jsonDataModelServices.TryGetJsonByKey(key, out string json))
            {
                using var writer = HttpContext.OpenResponseText();
                await writer.WriteAsync(json);
            }
            else
            {
                throw HttpException.NotFound($"Json datamodel with key {key} not found");
            }
        }

        [Route(HttpVerbs.Delete, "/json-datamodel/{key}")]
        public string RemoveDataModelByKey(string key)
        {
            if (!_jsonDataModelServices.RemoveByKey(key))
            {
                throw HttpException.NotFound($"Json datamodel with key {key} not found");
            }
            else
            {
                return $"Json datamodel with key {key} removed";
            }
        }

        [Route(HttpVerbs.Post, "/json-datamodel/{key}")]
        public async Task AddOrReplaceJson(string key)
        {
            string json = await HttpContext.GetRequestBodyAsStringAsync();
            _jsonDataModelServices.AddOrReplaceJson(key, json, true);
            using var writer = HttpContext.OpenResponseText();
            await writer.WriteAsync(json);
        }

        [Route(HttpVerbs.Put, "/json-datamodel/{key}")]
        public async Task AddOrMergeJson(string key)
        {
            string json = await HttpContext.GetRequestBodyAsStringAsync();
            _jsonDataModelServices.AddOrMergeJson(key, json, true);
            using var writer = HttpContext.OpenResponseText();
            await writer.WriteAsync(json);
        }
    }
}