using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Data;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using POGOProtos.Data.Player;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;
using POGOProtos.Map.Pokemon;
using POGOProtos.Map.Fort;
using Google.Protobuf;
using DankMemes.GPSOAuthSharp;
using System.IdentityModel.Tokens.Jwt;
using PokemonGoApi.Source;
using Google.Common.Geometry;
using System.Xml.Linq;

namespace PokemonGoApi
{
	public partial class Default : System.Web.UI.Page
	{
		private string m_googleAuthToken = String.Empty;
		private string m_apiEndPoint = "https://pgorelease.nianticlabs.com/plfe/rpc";
		private GetPlayerResponse m_player;
		double starting_lat;
		double starting_lng;
		double m_lat = 0;
		double m_lng = 0;
		List<string> seen_pokes = new List<string>();
		List<ulong> explored_cells = new List<ulong>();
		DataTable pokes_table = new DataTable();

		protected void Page_Load(object sender, EventArgs e)
		{
			seen_pokes.Clear();
			explored_cells.Clear();
			pokes_table.Columns.Add("Pokemon");
			pokes_table.Columns.Add("Location");
			pokes_table.Columns.Add("Distance");
			pokes_table.Columns.Add("Expire Time");

			Session["test"] = new List<string>();

			//if (Response.Cookies["authCookie"] != null && Response.Cookies["gAuthCookie"].Expires > DateTime.Now.ToUniversalTime())
			//{
			//	m_googleAuthToken = Response.Cookies["gAuthCookie"].Value;
			//}

			if (!String.IsNullOrEmpty(m_googleAuthToken))
			{
				GoogleLoginButton.Visible = false;
				LogoutButton.Visible = true;

				SendInitialRequest();
			}
		}

		protected void Login(string user, string pass)
		{
			var googleClient = new GPSOAuthClient(user, pass);
			var masterLoginResponse = googleClient.PerformMasterLogin();
			var oauthResponse = googleClient.PerformOAuth(masterLoginResponse["Token"], "audience:server:client_id:848232511240-7so421jotr2609rmqakceuu1luuq0ptb.apps.googleusercontent.com", "com.nianticlabs.pokemongo", "321187995bc7cdc2b5fc91b11a96e2baa8602c62");
			m_googleAuthToken = oauthResponse["Auth"];

			//var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			//HttpCookie authCookie = new HttpCookie("gAuthCookie");
			//authCookie.Value = oauthResponse["Auth"];
			//authCookie.Expires = epoch.AddSeconds(Convert.ToInt32(oauthResponse["Expiry"]));
			//Response.Cookies.Add(authCookie);

			GoogleLoginButton.Visible = false;
			LogoutButton.Visible = true;

			SendInitialRequest();
		}

		protected void GoogleLoginButton_OnClick(object sender, EventArgs e)
		{
			Login(GoogleUserTextBox.Text, GooglePassTextBox.Text);
		}

		protected void SendInitialRequest()
		{
			m_apiEndPoint = GetApiEndPoint();
			var playerResponseEnvelope = SendRequest(m_apiEndPoint, new Request { RequestType = RequestType.GetPlayer });
			m_player = GetPlayerResponse.Parser.ParseFrom(playerResponseEnvelope.Returns[0]);
			UserLabel.Text = "Got user: " + m_player.PlayerData.Username;

			var locationRequest = WebRequest.Create(String.Format("https://maps.googleapis.com/maps/api/geocode/xml?address={0}", Uri.EscapeDataString(LocationTextBox.Text)));
			var locationXml = XDocument.Load(locationRequest.GetResponse().GetResponseStream());
			var locationResult = locationXml.Element("GeocodeResponse").Element("result");
			var locationString = locationResult.Element("formatted_address").Value;
			starting_lat = Convert.ToDouble(locationResult.Element("geometry").Element("location").Element("lat").Value);
			starting_lng = Convert.ToDouble(locationResult.Element("geometry").Element("location").Element("lng").Value);
			m_lat = starting_lat;
			m_lng = starting_lng;

			LocationLabel.Text = String.Format("Staring at location: {0} <br /> Lat: {1}, Lng: {2}", locationString, m_lat.ToString(), m_lng.ToString());


			var stepSize = 0.00125;
			var numSteps = 5;
			var lat = m_lat;
			var lng = m_lng;

			for (double i = lat - stepSize * numSteps; i <= lat + stepSize * numSteps; i += stepSize)
			{
				m_lat = i;
				for (double j = lng - stepSize * numSteps; j <= lng + stepSize * numSteps; j += stepSize)
				{
					m_lng = j;
					SendRequest(m_apiEndPoint, new Request { RequestType = RequestType.GetPlayer });
					var cellIds = S2Helper.GetCellIds(m_lat, m_lng);
					var getMapObjectRequest = new Request
					{
						RequestType = RequestType.GetMapObjects,
						RequestMessage = new GetMapObjectsMessage
						{
							CellId = { cellIds },
							SinceTimestampMs = { new List<long>(cellIds.Length).ToArray() },
							Latitude = m_lat,
							Longitude = m_lng
						}.ToByteString()
					};

					var mapResponseEnvelope = SendRequest(m_apiEndPoint, getMapObjectRequest);
					var mapResponse = GetMapObjectsResponse.Parser.ParseFrom(mapResponseEnvelope.Returns[0]);
					CheckForPokemon(mapResponse);
					//Thread.Sleep(510);
				}
			}

			pokes_table.DefaultView.Sort = "Distance ASC";
			FoundPokesGridView.DataSource = pokes_table;
			FoundPokesGridView.DataBind();
		}

		protected void CheckForPokemon(GetMapObjectsResponse mapResponse)
		{
			foreach (POGOProtos.Map.MapCell cell in mapResponse.MapCells)
			{
				if (cell.WildPokemons.Count > 0)
				{
					foreach (WildPokemon poke in cell.WildPokemons)
					{
						if (!seen_pokes.Contains(poke.SpawnPointId))
						{
							System.Diagnostics.Debug.WriteLine("Found pokemon");
							seen_pokes.Add(poke.SpawnPointId);

							DataRow poke_row = pokes_table.NewRow();
							poke_row["Pokemon"] = poke.PokemonData.PokemonId;
							poke_row["Location"] = String.Format("{0}, {1}", poke.Latitude, poke.Longitude);
							poke_row["Distance"] = Math.Sqrt(Math.Pow(starting_lat - poke.Latitude, 2) + Math.Pow(starting_lng - poke.Longitude, 2));
							poke_row["Expire Time"] = DateTime.Now.AddMilliseconds(poke.TimeTillHiddenMs).ToLocalTime().ToLongTimeString();
							pokes_table.Rows.Add(poke_row);
							var asd = (List<string>)Session["test"];
							asd.Add(poke.PokemonData.PokemonId.ToString());
							Session["test"] = asd;

							DBPoke found_poke = new DBPoke()
							{
								EncounterId = poke.EncounterId,
								LastModifiedTimeStampMs = poke.LastModifiedTimestampMs,
								Latitude = poke.Latitude,
								Longitude = poke.Longitude,
								SpawnPointId = poke.SpawnPointId,
								Name = poke.PokemonData.PokemonId.ToString(),
								TimeTillHiddenMs = poke.TimeTillHiddenMs,
								FoundTimeSeconds = Convert.ToInt32((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds)
						};
							found_poke.Save();
						}
					}
				}
				if (cell.Forts.Count > 0)
				{
					foreach (FortData fort in cell.Forts)
					{
						System.Diagnostics.Debug.WriteLine("Found fort");
					}
				}
			}
		}

		protected string GetApiEndPoint()
		{
			var responseEnvelope = SendRequest(m_apiEndPoint, new Request { RequestType = RequestType.GetPlayer });
			return String.Format("https://{0}/rpc", responseEnvelope.ApiUrl);
		}

		protected ResponseEnvelope SendRequest(string apiUrl, Request request)
		{
			var requestEnvelope = new RequestEnvelope
			{
				StatusCode = 2,
				RequestId = (ulong)new Random().Next(100000000, 999999999),
				Latitude = m_lat,
				Longitude = m_lng,
				Altitude = 50,
				Unknown12 = 123,
				Requests = { RequestHelper.GetDefaultRequests() }
			};
			requestEnvelope.AuthInfo = new RequestEnvelope.Types.AuthInfo()
			{
				Provider = "google",
				Token = new RequestEnvelope.Types.AuthInfo.Types.JWT()
				{
					Contents = m_googleAuthToken,
					Unknown2 = 59 // ???
				}
			};

			requestEnvelope.Requests.Insert(0, request);
			using (var memoryStream = new System.IO.MemoryStream())
			{
				requestEnvelope.WriteTo(memoryStream);
				var httpClient = new HttpClient();
				httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("Niantic App");
				httpClient.DefaultRequestHeaders.ExpectContinue = false;
				using (var response = httpClient.PostAsync(apiUrl, new ByteArrayContent(memoryStream.ToArray())).Result)
				{
					var responseBytes = response.Content.ReadAsByteArrayAsync().Result;
					var responseEnvelope = ResponseEnvelope.Parser.ParseFrom(responseBytes);
					System.Diagnostics.Debug.WriteLine("Received" + responseBytes.Length + "bytes.");

					return responseEnvelope;
				}
			}
		}

		protected void LogoutButton_OnClick(object sender, EventArgs e)
		{
			Session["idToken"] = null;
			Session["loginWith"] = null;
			Response.Redirect("Default.aspx");
		}

		
	}
}