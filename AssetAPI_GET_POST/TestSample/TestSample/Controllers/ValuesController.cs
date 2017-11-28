using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Dynamic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace TestSample.Controllers
{
    //[Route("api/[controller]")]
    public class assetserviceController : Controller
    {
        const string _uri = "bolt://192.168.39.63:7687";
        const string _userName = "neo4j";
        const string _password = "Neo4j";
        public AssetManager _assetManager;

        public assetserviceController()
        {
            _assetManager = new AssetManager();
        }

        // POST api/values
        [HttpPost("{asset}")]
        public void PostAssets([FromBody]object jsonvalue)
        {
            List<string> result = new List<string>();
            var serialized = JsonConvert.SerializeObject(jsonvalue);
            JToken entireJson = JToken.Parse(serialized);

            var baseNodeName = string.Empty;
            var targetNodeName = string.Empty;
            var baseNodeURL = string.Empty;

            foreach (var item in entireJson)
            {
                if (item is JProperty)
                {
                    var _key = (item as JProperty).Name;
                    var _value = (item as JProperty).Value.ToString();
                    if (_key.ToString() == "uri")
                    {
                        baseNodeURL = _value;
                        string[] tokens = _value.Split('/').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                        baseNodeName = tokens[0];
                        if (!_assetManager.AssetTypes.Contains(baseNodeName))
                        {
                            _assetManager.AssetTypes.Add(baseNodeName);
                        }
                        result.Add("Created Asset with uri: " + _assetManager.CreateAsset(baseNodeName, entireJson));
                    }
                    else
                    {
                        if (!DateTime.TryParse(_value.ToString(), out DateTime _dt))
                        {
                            if (_value.Contains("/"))
                            {
                                string[] tokens = _value.Split('/').Where(x => !string.IsNullOrEmpty(x)).ToArray();
                                targetNodeName = tokens[0];
                                if (!_assetManager.AssetTypes.Contains(targetNodeName))
                                {
                                    _assetManager.AssetTypes.Add(targetNodeName);
                                }
                                _assetManager.CreateRelation(baseNodeName, baseNodeURL, targetNodeName, _value, _key, entireJson);
                            }
                        }
                    }
                }
                else
                {
                    var v = item.HasValues;
                    var x = item.Value<JObject>();
                    foreach (var y in x)
                    {
                        var _key = y.Key;
                        var _value = y.Value;
                        if (_key.ToString() == "uri")
                        {
                            baseNodeURL = _value.ToString();
                            string[] tokens = _value.ToString().Split('/').Where(x1 => !string.IsNullOrEmpty(x1)).ToArray();
                            baseNodeName = tokens[0];
                            if (!_assetManager.AssetTypes.Contains(baseNodeName))
                            {
                                _assetManager.AssetTypes.Add(baseNodeName);
                            }
                            _assetManager.CreateAsset(baseNodeName, item);
                        }
                        else
                        {
                            if (!DateTime.TryParse(_value.ToString(), out DateTime _dt))
                            {
                                if (_value.ToString().Contains("/"))
                                {
                                    string[] tokens = _value.ToString().Split('/').Where(x1 => !string.IsNullOrEmpty(x1)).ToArray();
                                    targetNodeName = tokens[0];
                                    if (!_assetManager.AssetTypes.Contains(targetNodeName))
                                    {
                                        _assetManager.AssetTypes.Add(targetNodeName);
                                    }
                                    _assetManager.CreateRelation(baseNodeName, baseNodeURL, targetNodeName, _value.ToString(), _key, item);
                                }
                            }
                        }
                    }
                }
            }
            foreach (var item in result)
            {
                Debug.WriteLine(item.ToString());
            }
        }

        //<asset-app-url>/locomotives/1
        //// OR ////
        //<asset-app-url>/locomotives/1?fields=uri,type,manufacturer
        [HttpGet("{asset}/{id}")]
        public JsonResult GetSingleOrMutipleAssets(string asset, string id, [FromQuery] string fields)
        {
            dynamic node = null;
            string uri = "/" + asset + "/" + id;

            if (fields != null)
            {
                string[] tokens = fields.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToArray();

                // Get selected fields of an Asset
                node = _assetManager.GetSelectedFieldsOfAsset(asset, uri, tokens);
            }
            else
            {
                // Get all fields of an Asset
                node = _assetManager.GetAllFieldsOfAsset(asset, uri);
            }

            var jsonResult = JsonConvert.SerializeObject(node);

            return Json(jsonResult);
        }

        //<asset-app-url>/locomotives?filter=serial_no=0084
        //<asset-app-url>/locomotives?filter=model=SD70ACe:fleet=/fleets/up-5
        //<asset-app-url>/locomotives?filter=engine=/engines/v16-2-5|fleet=/fleets/csx-1
        //// OR ////
        //<asset-app-url>/locomotives?fields=uri,type,manufacturer
        [HttpGet("{asset}")]
        public JsonResult GetAssetsByFilterOrFields(string asset, [FromQuery] string filter, [FromQuery] string fields)
        {
            dynamic node = null;
            List<string[]> queryTokens = null;

            if (filter != null)
            {
                if (filter.Contains("|"))
                {
                    // Get any Asset that match the specified value
                    // also
                    // Get any Asset that match any one of the specified values
                    // (Here the the pipe character '|' represents the OR condition)

                    queryTokens = filter.Split('|').Select(s => s.Split('=')).ToList();
                    node = _assetManager.GetAssetByFilter_OR(asset, queryTokens);
                }
                else
                {
                    // Get any Asset that match all the specified values
                    // (Here the the colon character ':' represents the AND condition)

                    queryTokens = filter.Split(':').Select(s => s.Split('=')).ToList();
                    node = _assetManager.GetAssetByFilter_AND(asset, queryTokens);
                }
            }
            else if (fields != null)
            {
                string[] tokens = fields.Split(',').Where(x => !string.IsNullOrEmpty(x)).ToArray();

                // Get selected fields of a collection of Assets
                node = _assetManager.GetSelectedFieldsOfAllAsset(asset, tokens);
            }
            var jsonResult = JsonConvert.SerializeObject(node);

            return Json(jsonResult);
        }
    }
}

