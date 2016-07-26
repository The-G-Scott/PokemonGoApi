using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
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
		double m_lat = 0;
		double m_lng = 0;
		List<string> seen_pokes = new List<string>();
		List<ulong> explored_cells = new List<ulong>();

		protected void Page_Load(object sender, EventArgs e)
		{
			seen_pokes.Clear();
			explored_cells.Clear();
		}

		protected void GoogleLoginButton_OnClick(object sender, EventArgs e)
		{
			var googleClient = new GPSOAuthClient(GoogleUserTextBox.Text, GooglePassTextBox.Text);
			var masterLoginResponse = googleClient.PerformMasterLogin();
			var oauthResponse = googleClient.PerformOAuth(masterLoginResponse["Token"], "audience:server:client_id:848232511240-7so421jotr2609rmqakceuu1luuq0ptb.apps.googleusercontent.com", "com.nianticlabs.pokemongo", "321187995bc7cdc2b5fc91b11a96e2baa8602c62");

			GoogleLoginButton.Visible = false;
			LogoutButton.Visible = true;

			m_googleAuthToken = oauthResponse["Auth"];
			m_apiEndPoint = GetApiEndPoint();
			var playerResponseEnvelope = SendRequest(m_apiEndPoint, new Request { RequestType = RequestType.GetPlayer });
			m_player = GetPlayerResponse.Parser.ParseFrom(playerResponseEnvelope.Returns[0]);
			UserLabel.Text = "Got user: " + m_player.PlayerData.Username;

			var locationRequest = WebRequest.Create(String.Format("https://maps.googleapis.com/maps/api/geocode/xml?address={0}", Uri.EscapeDataString(LocationTextBox.Text)));
			var locationXml = XDocument.Load(locationRequest.GetResponse().GetResponseStream());
			var locationResult = locationXml.Element("GeocodeResponse").Element("result");
			var locationString = locationResult.Element("formatted_address").Value;
			m_lat = Convert.ToDouble(locationResult.Element("geometry").Element("location").Element("lat").Value);
			m_lng = Convert.ToDouble(locationResult.Element("geometry").Element("location").Element("lng").Value);

			LocationLabel.Text = String.Format("Staring at location: {0} <br /> Lat: {1}, Lng: {2}", locationString, m_lat.ToString(), m_lng.ToString());


			var stepSize = 0.0025;
			var numSteps = 4;
			var lat = m_lat;
			var lng = m_lng;

			for (double i = lat - stepSize * numSteps; i <= lat + stepSize * numSteps; i += stepSize)
			{
				m_lat = i;
				for (double j = lng - stepSize * numSteps; j <= lng + stepSize * numSteps; j += stepSize)
				{
					m_lng = j;
					SendRequest(m_apiEndPoint, new Request { RequestType = RequestType.GetPlayer });
					var cellIds = GetCellIds(m_lat, m_lng);
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
					Thread.Sleep(510);
				}
			}
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
							FoundPokesLabel.Text += String.Format("Found pokemon: {0}, coords: {1}, {2}, expires at {3}<br />", 
								poke.PokemonData.PokemonId, 
								poke.Latitude, poke.Longitude, 
								DateTime.Now.AddMilliseconds(poke.TimeTillHiddenMs).ToLocalTime().ToLongTimeString());
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

		protected ulong[] GetCellIds(double latitude, double longitude)
		{
			var latLong = S2LatLng.FromDegrees(latitude, longitude);
			var cell = S2CellId.FromLatLng(latLong);
			var cellId = cell.ParentForLevel(15);
			var cells = cellId.GetEdgeNeighbors();
			var cellIds = new List<ulong>
			{
				cellId.Id
			};

			foreach (var cellEdge1 in cells)
			{
				if (!cellIds.Contains(cellEdge1.Id)) cellIds.Add(cellEdge1.Id);

				foreach (var cellEdge2 in cellEdge1.GetEdgeNeighbors())
				{
					if (!cellIds.Contains(cellEdge2.Id)) cellIds.Add(cellEdge2.Id);
				}
			}

			return cellIds.ToArray();
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
				Requests = { GetDefaultRequests() }
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