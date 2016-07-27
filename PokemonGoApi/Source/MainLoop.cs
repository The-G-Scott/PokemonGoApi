using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using POGOProtos.Networking.Envelopes;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;
using POGOProtos.Networking.Responses;
using POGOProtos.Map.Pokemon;
using POGOProtos.Map.Fort;
using Google.Protobuf;
using DankMemes.GPSOAuthSharp;
using System.Xml.Linq;

namespace PokemonGoApi.Source
{
	public class MainLoop
	{
		private string m_googleAuthToken = String.Empty;
		private string m_apiEndPoint = "https://pgorelease.nianticlabs.com/plfe/rpc";
		private string m_location = String.Empty;
		private GetPlayerResponse m_player;
		double starting_lat;
		double starting_lng;
		double m_lat = 0;
		double m_lng = 0;
		List<string> seen_pokes = new List<string>();
		List<ulong> explored_cells = new List<ulong>();

		public void Login(string user, string pass, string location)
		{
			m_location = location;
			var googleClient = new GPSOAuthClient(user, pass);
			var masterLoginResponse = googleClient.PerformMasterLogin();
			var oauthResponse = googleClient.PerformOAuth(masterLoginResponse["Token"], "audience:server:client_id:848232511240-7so421jotr2609rmqakceuu1luuq0ptb.apps.googleusercontent.com", "com.nianticlabs.pokemongo", "321187995bc7cdc2b5fc91b11a96e2baa8602c62");
			m_googleAuthToken = oauthResponse["Auth"];
		}

		public InitialResponse SendInitialRequest()
		{
			var response = new InitialResponse();

			m_apiEndPoint = GetApiEndPoint();
			var playerResponseEnvelope = SendRequest(m_apiEndPoint, new Request { RequestType = RequestType.GetPlayer });
			m_player = GetPlayerResponse.Parser.ParseFrom(playerResponseEnvelope.Returns[0]);

			var locationRequest = WebRequest.Create(String.Format("https://maps.googleapis.com/maps/api/geocode/xml?address={0}", Uri.EscapeDataString(m_location)));
			var locationXml = XDocument.Load(locationRequest.GetResponse().GetResponseStream());
			var locationResult = locationXml.Element("GeocodeResponse").Element("result");
			var locationString = locationResult.Element("formatted_address").Value;
			starting_lat = Convert.ToDouble(locationResult.Element("geometry").Element("location").Element("lat").Value);
			starting_lng = Convert.ToDouble(locationResult.Element("geometry").Element("location").Element("lng").Value);
			m_lat = starting_lat;
			m_lng = starting_lng;

			response.Username = m_player.PlayerData.Username;
			response.FormattedAddress = locationString;
			response.Latitude = starting_lat;
			response.Longitude = starting_lng;
			return response;
		}

		public void BeginRequests()
		{
			while (true)
			{
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
					}
				}
				System.Diagnostics.Debug.WriteLine("Got map objects. Sleeping for one minute");
				Thread.Sleep(60000);
			}
			System.Diagnostics.Debug.WriteLine("BeginRequests parent died, stopping.");
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

							DBPoke found_poke = new DBPoke()
							{
								EncounterId = poke.EncounterId,
								LastModifiedTimeStampMs = poke.LastModifiedTimestampMs,
								Latitude = poke.Latitude,
								Longitude = poke.Longitude,
								SpawnPointId = poke.SpawnPointId,
								Name = poke.PokemonData.PokemonId.ToString(),
								ExpireTime = DateTime.UtcNow.AddMilliseconds(poke.TimeTillHiddenMs)
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

		public void BeginClearStalePokes()
		{
			while (true)
			{
				foreach (DBPoke dbPoke in DBPoke.GetAllDBPokes())
				{
					if (dbPoke.ExpireTime < DateTime.UtcNow)
					{
						dbPoke.Delete();
					}
				}
				System.Diagnostics.Debug.WriteLine("Cleared stale pokes. Waiting one minute.");
				Thread.Sleep(60000);
			}
			System.Diagnostics.Debug.WriteLine("BeginClearStalePokes parent died, stopping.");
		}
	}
}