﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChatterService.Model;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Web.Caching;
using System.Net;
using System.Data;
using System.Configuration;
using RestSharp;

namespace ChatterService
{

    public class OAuthToken
    {
        public string id { get; set; }
        public string issued_at { get; set; }
        public string instance_url { get; set; }
        public string signature { get; set; }
        public string access_token { get; set; }
    }

    public class ChatterItem
    {
        public string id { get; set; }
        public string url { get; set; }
    }

    public class Photo
    {
        public string largePhotoUrl {get; set; }
        public string photoVersionId {get; set; }
        public string smallPhotoUrl {get; set; }
    }

    public class ChatterUser : ChatterItem
    {
        public string name { get; set; }
        public string title { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string companyName {get; set;}
        public bool isActive { get; set; }
        public ChatterItem mySubscription { get; set; }
        public Photo photo { get; set; }
        public bool isChatterGuest { get; set; }
        public string type { get; set; }
    }

    public class ChatterSubscription : ChatterItem
    {
        public ChatterUser subject { get; set; }
        public ChatterUser subscriber { get; set; }
    }

    public class ChatterResponse
    {
        public int total { get; set; }
        public string nextPageUrl { get; set; }
        public string currentPageUrl { get; set; }
        public string previousPageUrl { get; set; }
        public List<ChatterSubscription> followers { get; set; }
        public List<ChatterSubscription> following { get; set; }
    }

    public class ChatterRestService 
    {
        private RestClient _client;
        private String _userId;
        private OAuthToken _token;
        public string Url { get; set; }

        public ChatterRestService(string url)
        {
            Url = url;
        }

        public bool Login(string client_id, string grant_type, string client_secret, string username, string password)
        {
            _client = new RestClient(Url);

            var request = new RestRequest("oauth2/token", Method.POST);
            request.AddParameter("client_id", client_id);
            request.AddParameter("grant_type", grant_type);
            request.AddParameter("client_secret", client_secret);
            request.AddParameter("username", username);
            request.AddParameter("password", password);

            /// execute the request

            _token = _client.Execute<OAuthToken>(request).Data;
            //string content = _client.Execute(request).Content; // raw content as string
            return true;
        }

        public ChatterResponse GetFollowers(string userId)
        {
            return MakeUserRestCall(userId, "followers");
        }

        public ChatterResponse GetFollowing(string userId)
        {
            return MakeUserRestCall(userId, "following");
        }

        public ChatterResponse Follow(string viewerId, string ownerId)
        {
            Dictionary<string, string> postParams = new Dictionary<string, string>();
            postParams.Add("subjectId", ownerId);
            return MakeUserRestCall(viewerId, "following", Method.POST, postParams);
        }

        public ChatterResponse Unfollow(string viewerId, string ownerId)
        {
            // find the subscription for this relationship and then delete it
            ChatterResponse cresp = GetFollowing(viewerId);
            if (cresp.following != null)
            {
                foreach (ChatterSubscription csub in cresp.following)
                {
                    if (csub.subject.id.StartsWith(ownerId))
                    {
                        return MakeRestCall("subscriptions/" + csub.id, Method.DELETE, null);
                    }
                }
            }

            return cresp;
        }

        private ChatterResponse MakeUserRestCall(string userId, string call)
        {
            return MakeUserRestCall(userId, call, Method.GET, null);
        }
        
        private ChatterResponse MakeUserRestCall(string userId, string call, RestSharp.Method method, Dictionary<string, string> postParams)
        {
            return MakeRestCall("users/" + userId + "/" + call, method, postParams);
        }

        private ChatterResponse MakeRestCall(string call, RestSharp.Method method, Dictionary<string, string> postParams)
        {
            var request = new RestRequest("data/v25.0/chatter/" + call, method);
            request.AddHeader("Authorization", "Bearer " + _token.access_token);

            if (postParams != null)
            {
                foreach (KeyValuePair<String, String> entry in postParams)
                {
                    request.AddParameter(entry.Key, entry.Value);
                }
            }
            /// execute the request
            //string content = _client.Execute(request).Content; // raw content as string
            //return content;
            ChatterResponse response = _client.Execute<ChatterResponse>(request).Data;
            return response;
        }
    }


}
