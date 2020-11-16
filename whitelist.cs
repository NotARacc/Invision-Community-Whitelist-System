using System;
using RestSharp;
using System.Net;
using CitizenFX.Core;
using Newtonsoft.Json.Linq;

namespace invision_whitelist
{
    public class whitelist : BaseScript
    {
        string communityURL = "http://WEB_URL/api/";
        string apiKey = "API_KEY";
        string groupID = "25";
        string steamHex { get; set; }
        JObject jsonRet;
        IRestClient client;
        IRestRequest request;
        IRestResponse response;
        public whitelist()
        {
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(handleConnection);
        }
        
        private async void handleConnection([FromSource]Player player, string plrName, dynamic kickReason, dynamic deferrals)
        {

            deferrals.defer();
            await Delay(0);
            deferrals.update("We're checking your whitelist. Please standby");
            Debug.WriteLine($"Connecting user: {player.Name} (Steam: {player.Identifiers["steam"]}, Discord: {player.Identifiers["discord"]}, IP: {player.Identifiers["ip"]})");
            
            if(player.Identifiers["steam"] == null)
            {
                deferrals.done("Couldn't find your steam hex! Try re-opening FiveM with Steam open.");
                return;
            } else
            {
                steamHex = player.Identifiers["steam"].ToLower().Replace("steam:", "");
            }

            client = new RestClient(communityURL);
            request = new RestRequest("core/members?group=" + groupID + "&perPage=500&key=" + apiKey);
            response = client.Execute(request);
            if (response.StatusCode == (HttpStatusCode)200)
            {
                try
                {
                    jsonRet = JObject.Parse(response.Content);
                    foreach(JToken obj in jsonRet["results"])
                    {
                        string retrievedHex = obj["customFields"]["1"]["fields"]["3"]["value"].ToString().ToLower().Replace("steam:", "");
                        if(retrievedHex == steamHex)
                        {
                            Debug.WriteLine($"{player.Name} is whitelisted. Allowing connection.");
                            deferrals.update("You are whitelisted. Redirecting you!");
                            await Delay(500);
                            deferrals.done();
                            return;
                        }
                    }
                    Debug.WriteLine($"{player.Name} is not whitelisted. Terminating connection.");
                    deferrals.done("You are not whitelisted!");
                    return;
                } catch(Exception e)
                {
                    Debug.WriteLine(e.ToString());
                    deferrals.done("An error occured with the whitelist.");
                    return;
                }
            }
            else
            {
                Debug.WriteLine(response.ErrorMessage);
                deferrals.done("Could not query API. Try again later.");
                return;
            }
        }
    }
}
