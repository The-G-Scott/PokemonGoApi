using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using Google.Protobuf;
using DankMemes.GPSOAuthSharp;
using System.IdentityModel.Tokens.Jwt;
using PokemonGoApi.Source;

namespace PokemonGoApi
{
	public partial class Default : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (Session["idToken"] != null)
			{
				Response.Write(Session["idToken"] + "<br />" + Session["idToken"].ToString().Length +"<br />");
				var googleClient = new GPSOAuthClient("cuapb.5115.test", "5115test");
				var masterLoginResponse = googleClient.PerformMasterLogin();
				var oauthResponse = googleClient.PerformOAuth(masterLoginResponse["Token"], "audience:server:client_id:848232511240-7so421jotr2609rmqakceuu1luuq0ptb.apps.googleusercontent.com", "com.nianticlabs.pokemongo", "321187995bc7cdc2b5fc91b11a96e2baa8602c62");
				Response.Write(oauthResponse["Auth"] + "<br />" + oauthResponse["Auth"].ToString().Length);

				var testAuth = oauthResponse["Auth"];
				var testAuth2 = Session["idToken"].ToString().Substring(0, 848);
				var pwep = Session["accessToken"];
				var pewp = masterLoginResponse["Token"];
				var token = (GooglePlusAccessToken)Session["GooglePlusAccessToken"];

				var tit = new JwtSecurityToken(Session["idToken"].ToString());
				bool gah = Session["idToken"].ToString().Contains(tit.Payload["at_hash"].ToString());
									
				tit.Payload.Remove("at_hash");
				tit.Header.Remove("typ");

				var titty = tit.EncodedHeader + "." + tit.EncodedPayload + "." + tit.RawSignature;
				var foop = new JwtSecurityToken(titty);

				//var tat = tit.ToString();
				//var titty = new JwtSecurityToken(tat);
				//Response.End();

				var workinglen = testAuth.Length;
				var tittylen = titty.Length;
				GoogleLoginButton.Visible = false;
				LogoutButton.Visible = true;

				var apiEndPoint = "https://pgorelease.nianticlabs.com/plfe/rpc";


				var requestEnvelope = new RequestEnvelope
				{
					StatusCode = 2,
					RequestId = (ulong) new Random().Next(100000000, 999999999),
					Latitude = 0,
					Longitude = 0,
					Altitude = 0,
					Unknown12 = 123,
					Requests = { GetDefaultRequests() }
				};
				//requestEnvelope.Requests.Add(new Request() { RequestType = RequestType.GetPlayer });
				//requestEnvelope.StatusCode = 2; // REQUEST ?
				//requestEnvelope.RequestId = 7309341774315520108; // ???
				//requestEnvelope.Latitude = 0;
				//requestEnvelope.Longitude = 0;
				//requestEnvelope.Altitude = 0;
				requestEnvelope.AuthInfo = new RequestEnvelope.Types.AuthInfo()
				{
					Provider = "google",
					Token = new RequestEnvelope.Types.AuthInfo.Types.JWT()
					{
						Contents = Session["idToken"].ToString(),
						Unknown2 = 59 // ???
					}
				};

				var testLen = 0;

				requestEnvelope.Requests.Insert(0, new Request() { RequestType = RequestType.GetPlayer });
				using (var memoryStream = new System.IO.MemoryStream())
				{
					requestEnvelope.WriteTo(memoryStream);
					var httpClient = new HttpClient();
					httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("Dalvik/2.1.0 (Linux; U; Android 6.0.1; ONEPLUS A3003 Build/MMB29M)");
					httpClient.DefaultRequestHeaders.ExpectContinue = false;
					using (var response = httpClient.PostAsync(apiEndPoint, new ByteArrayContent(memoryStream.ToArray())).Result)
					{
						var responseBytes = response.Content.ReadAsByteArrayAsync().Result;
						var responseEnvelope = ResponseEnvelope.Parser.ParseFrom(responseBytes);
						testLen = responseBytes.Length;
					}
				}
				//while (responseEnvelope.StatusCode == 102)
				//{
				//	responseBytes = response.Content.ReadAsByteArrayAsync().Result;
				//	responseEnvelope = ResponseEnvelope.Parser.ParseFrom(responseBytes);
				//}


				//var memoryStream = new System.IO.MemoryStream();
				//var outStream = new CodedOutputStream(memoryStream, false);
				//requestEnvelope.WriteTo(outStream);
				//outStream.Flush();
				//memoryStream.Position = 0;
				//var memoryReader = new System.IO.BinaryReader(memoryStream);
				//byte[] data = memoryReader.ReadBytes((int)memoryStream.Length);
				//var request = (HttpWebRequest)WebRequest.Create(apiEndPoint);
				//request.Method = "POST";
				//request.CookieContainer = new CookieContainer();
				//request.ContentLength = data != null ? data.Length : 0;
				////request.ServicePoint.Expect100Continue = false;
				//request.UserAgent = "Niantic App";
				//request.Host = "pgorelease.nianticlabs.com";
				//request.KeepAlive = true;
				////request.AllowAutoRedirect = false;
				//request.Accept = "*/*";
				//var stream = request.GetRequestStream();
				//if (data != null && data.Length > 0)
				//{
				//	stream.Write(data, 0, data.Length);
				//}
				//var response = (HttpWebResponse)request.GetResponse();
				//data = new System.IO.BinaryReader(response.GetResponseStream()).ReadBytes((int)response.ContentLength);
				//var responseEnvelope = new ResponseEnvelope();
				//var messageParser = new MessageParser<ResponseEnvelope>(() => { return new ResponseEnvelope(); });
				//responseEnvelope = messageParser.ParseFrom(data);

				//while (responseEnvelope.StatusCode == 102)
				//{
				//	//poop
				//}
				var titties = 1;
			}
		}

		protected void GoogleLoginButton_OnClick(object sender, EventArgs e)
		{
			var Googleurl = "https://accounts.google.com/o/oauth2/v2/auth?" +
				"response_type=code" +
				"&redirect_uri=" + ConfigurationManager.AppSettings["GoogleRedirectUrl"] + 
				"&scope=https://www.googleapis.com/auth/userinfo.email%20https://www.googleapis.com/auth/userinfo.profile" +
				"&client_id=" + ConfigurationManager.AppSettings["GoogleClientId"];
			Session["loginWith"] = "google";
			Response.Redirect(Googleurl);
		}

		protected void LogoutButton_OnClick(object sender, EventArgs e)
		{
			Session["idToken"] = null;
			Session["loginWith"] = null;
			Response.Redirect("Default.aspx");
		}

		private IEnumerable<Request> GetDefaultRequests()
		{
			return new[]
			{
				new Request
				{
					RequestType = RequestType.GetHatchedEggs
				},
				new Request
				{
					RequestType = RequestType.GetInventory,
					RequestMessage = new GetInventoryMessage
					{
					   LastTimestampMs = 0
					}.ToByteString()
				},
				new Request
				{
					RequestType = RequestType.CheckAwardedBadges
				},
				new Request
				{
					RequestType = RequestType.DownloadSettings,
					RequestMessage = new DownloadSettingsMessage
					{
						Hash = "4a2e9bc330dae60e7b74fc85b98868ab4700802e"
					}.ToByteString()
				}
			};
		}
	}
}