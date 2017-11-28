using Neo4j.Driver.V1;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TestSample
{
    public class AssetManager
    {
        const string _uri = "bolt://192.168.39.63:7687";
        const string _userName = "neo4j";
        const string _password = "Neo4j";

        public List<string> AssetTypes { get; set; } // Nodes

        public AssetManager()
        {
            AssetTypes = new List<string>();
        }

        // Create a node in database for every asset
        internal string CreateAsset(string asset, JToken entireJson)
        {
            string node = null;
            Dictionary<string, object> valueList = entireJson.ToObject<Dictionary<string, object>>();

            string properties = string.Empty;

            foreach (var item in valueList)
            {
                properties += item.Key + ":" + $"'{item.Value}',";
            }

            properties = properties.Remove(properties.Length - 1);

            string createQuery = "CREATE (" + asset + ":" + asset + "{" + properties + "}) RETURN " + asset + ".uri AS uri";

            using (var driver = GraphDatabase.Driver(_uri, AuthTokens.Basic(_userName, _password)))
            using (var session = driver.Session())
            {
                var result = session.Run(createQuery).Select(r => r.Values).SingleOrDefault();
                node = result.Select(v => v.Value).SingleOrDefault().ToString();
            }
            return node;
        }

        // Get the existing Assets(Nodes) and
        // Create a relationship in between Assets(Nodes)
        // Note: If any of the above Assets(Nodes) doesn't exist 
        // the query neither creates a relationship nor affects any existing Assets(Nodes)
        internal dynamic CreateRelation(string baseNodeName, string baseNodeURL, string targetNodeName, string targetNodeURL, string property, JToken entireJson)
        {
            Dictionary<string, object> valueList = entireJson.ToObject<Dictionary<string, object>>();
            dynamic relation = null;

            // Query to search and return for an existing destination Asset(Node) where relationship starts
            string sourceNodeQuery = GetMatchQuery(baseNodeName, baseNodeURL);

            // Query to search and return for an existing destination Asset(Node) where relationship ends
            string destinationNodeQuery = GetMatchQuery(targetNodeName, targetNodeURL);

            // Query to create relationship if the Assets(Nodes) exist
            string createQuery = "CREATE (" + baseNodeName + ") -[:" + property.ToUpper() + "{ " + property + ":['" + targetNodeURL + "']}]->(" + targetNodeName + ")";

            string sourceQuery = RemoveReturn(sourceNodeQuery);
            string destinationQuery = RemoveReturn(destinationNodeQuery);
            string query = sourceQuery + " " + destinationQuery + " " + createQuery;

            using (var driver = GraphDatabase.Driver(_uri, AuthTokens.Basic(_userName, _password)))
            using (var session = driver.Session())
            {
                // We should pass MATCH and CREATE query in a single call because
                // the scope of variables created in MATCH query is limited to current call
                // Hence the CREATE query creates duplicate 'Nodes' with a single property "Id"

                var result = session.Run(query).Select(r => r.Values).SingleOrDefault();

                relation = result[baseNodeName.ToString()].As<IRelationship>();
            }
            return relation;
        }
        // Get all fields of an Asset
        internal dynamic GetAllFieldsOfAsset(string asset, string uri)
        {
            dynamic node = null;

            // Query to search for an Asset(Node) using "uri" property and returns the whole(all properties) Asset(Node)
            string query = "MATCH (" + asset + ":" + asset + "{uri:'" + uri + "'}) RETURN " + asset;

            node = ExecuteQuery(asset, query);
            return node;
        }

        // Get selected fields of an Asset
        internal dynamic GetSelectedFieldsOfAsset(string asset, string uri, string[] tokens)
        {
            string properties = string.Empty;
            dynamic node = null;

            for (int i = 0; i < tokens.Length; i++)
            {
                properties += asset + "." + tokens[i] + ",";
            }

            properties = properties.Remove(properties.Length - 1);

            // Query to search for an Asset(Node) using "uri" property and returns the selected properties of the Asset(Node)
            string query = "MATCH (" + asset + ":" + asset + "{uri:'" + uri + "'}) RETURN " + properties;

            node = ExecuteQuery(asset, query);
            return node;
        }

        // Get selected fields of a collection of Assets
        internal dynamic GetSelectedFieldsOfAllAsset(string asset, string[] tokens)
        {
            string properties = string.Empty;
            dynamic node = null;

            for (int i = 0; i < tokens.Length; i++)
            {
                properties += asset + "." + tokens[i] + ",";
            }

            properties = properties.Remove(properties.Length - 1);

            // Query to return the selected properties of all the Assets(Nodes) with the same 'Label'(Asset Type)
            string query = "MATCH (" + asset + ":" + asset + ") RETURN " + properties;

            node = ExecuteQuery(asset, query);
            return node;
        }

        // Get any Asset that match all the specified values
        internal dynamic GetAssetByFilter_AND(string asset, List<string[]> queryTokens)
        {
            string properties = string.Empty;
            dynamic node = null;

            foreach (var item in queryTokens)
            {
                properties += item[0] + ":'" + item[1] + "',";
            }

            properties = properties.Remove(properties.Length - 1);

            // Query to return all the Assets(Nodes) with the same 'Label'(Asset Type) and having all the specified properties
            string query = "MATCH (" + asset + ":" + asset + "{" + properties + "}) RETURN " + asset;

            node = ExecuteQuery(asset, query);
            return node;
        }

        // Get any Asset that match the specified value
        // also
        // Get any Asset that match any one of the specified values
        internal dynamic GetAssetByFilter_OR(string asset, List<string[]> queryTokens)
        {
            string properties = string.Empty;
            dynamic node = null;

            foreach (var item in queryTokens)
            {
                properties += asset + "." + item[0] + " IN ['" + item[1] + "'] OR ";
            }

            properties = properties.Substring(0, properties.Length - 3);

            // Query to return all the Assets(Nodes) with the same 'Label'(Asset Type) and having any one of the specified properties
            string query = "MATCH (" + asset + ":" + asset + ") WHERE " + properties + "RETURN " + asset;

            node = ExecuteQuery(asset, query);
            return node;
        }

        private static string GetMatchQuery(string nodeName, string nodeURL)
        {
            return "MATCH (" + nodeName + ":" + nodeName + "{uri:'" + nodeURL + "'}) RETURN " + nodeName;
        }

        public string RemoveReturn(string str)
        {
            string[] query = Regex.Split(str, @"RETURN").Where(x => !string.IsNullOrEmpty(x)).ToArray();
            return query[0];
        }

        internal string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
        }

        internal dynamic ExecuteQuery(string asset, string query)
        {
            dynamic node = null;
            //FileLog.Log(query);
            Debug.WriteLine(query);
            using (var driver = GraphDatabase.Driver(_uri, AuthTokens.Basic(_userName, _password)))
            using (var session = driver.Session())
            {
                dynamic result = session.Run(query).Select(r => r.Values).SingleOrDefault();

                //Get as an INode instance to access properties.
                node = result[asset.ToString()].As<INode>();
            }
            return node;
        }

        //internal void ExecuteQueries()
        //{
        //    string queryText = System.IO.File.ReadAllText(@"D:\Queries.txt");
        //    string[] queries = queryText.Split('|').Where(x => !string.IsNullOrEmpty(x)).ToArray();

        //    using (var driver = GraphDatabase.Driver(_uri, AuthTokens.Basic(_userName, _password)))
        //    using (var session = driver.Session())
        //    {
        //        for (int i = 0; i < queries.Length; i++)
        //        {
        //            session.Run(queries[i]);
        //        }
        //    }
        //}
    }
}
