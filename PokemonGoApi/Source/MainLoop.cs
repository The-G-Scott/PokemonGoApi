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
using PokemonGoApi.Source.Helpers;

namespace PokemonGoApi.Source
{
	public class MainLoop
	{
		private string m_googleAuthToken = String.Empty;
		private string m_apiEndPoint = "https://pgorelease.nianticlabs.com/plfe/rpc";
		private string m_location = String.Empty;
		private GetPlayerResponse m_player;
		double m_lat = 0;
		double m_lng = 0;
		double startLat = 0;
		double startLng = 0;
		List<string> seen_pokes = new List<string>();
		List<string> seen_forts = new List<string>();
		private string m_user;
		private int m_stepCount = 0;
		private string settingsHash = null;

		public void Login(string user, string pass, string location, int stepCount)
		{
			m_user = user;
			m_stepCount = stepCount;
			try
			{
				m_location = location;
				foreach (DBPoke dbPoke in DBPoke.GetAllDBPokes())
				{
					seen_pokes.Add(dbPoke.EncounterId.ToString() + dbPoke.SpawnPointId);
				}
				foreach (DBFort dbFort in DBFort.GetAllDBForts())
				{
					seen_forts.Add(dbFort.FortID);
				}
				var googleClient = new GPSOAuthClient(user, pass);
				var masterLoginResponse = googleClient.PerformMasterLogin();
				var oauthResponse = googleClient.PerformOAuth(masterLoginResponse["Token"], Constants.OAuthService, Constants.OAuthApp, Constants.OAuthClientSig);
				m_googleAuthToken = oauthResponse["Auth"];
			}
			catch
			{
				System.Diagnostics.Debug.WriteLine("Login failed. Restarting in 10 seconds");
				var worker = DBWorker.GetByUsername(m_user);
				worker.SetAvailable();
				Thread.Sleep(10000);
				Thread.CurrentThread.Abort();
			}
		}

		public InitialResponse SendInitialRequest()
		{
			var response = new InitialResponse();

			try
			{
				m_apiEndPoint = GetApiEndPoint();
				var playerResponseEnvelope = SendRequest(m_apiEndPoint, new Request { RequestType = RequestType.GetPlayer });
				m_player = GetPlayerResponse.Parser.ParseFrom(playerResponseEnvelope.Returns[0]);
				var locationRequest = WebRequest.Create(String.Format("https://maps.googleapis.com/maps/api/geocode/xml?address={0}", Uri.EscapeDataString(m_location)));
				var locationXml = XDocument.Load(locationRequest.GetResponse().GetResponseStream());
				var locationResult = locationXml.Element("GeocodeResponse").Element("result");
				var locationString = locationResult.Element("formatted_address").Value;
				startLat = Convert.ToDouble(locationResult.Element("geometry").Element("location").Element("lat").Value);
				startLng = Convert.ToDouble(locationResult.Element("geometry").Element("location").Element("lng").Value);
				m_lat = startLat;
				m_lng = startLng;

				response.Username = m_player.PlayerData.Username;
				response.FormattedAddress = locationString;
				response.Latitude = m_lat;
				response.Longitude = m_lng;

				if (!m_player.Success)
				{
					System.Diagnostics.Debug.WriteLine("GetPlayer inital request failed.");
					Thread.Sleep(1000);
					SendInitialRequest();
				}
			}
			catch { }
			return response;
		}

		public void SendSecondaryRequests()
		{
			var remoteConfigResponse = SendRequest(m_apiEndPoint, new Request
			{
				RequestType = RequestType.DownloadRemoteConfigVersion,
				RequestMessage = new DownloadRemoteConfigVersionMessage
				{
					Platform = POGOProtos.Enums.Platform.Android,
					AppVersion = 2903
				}.ToByteString()
			}).ToByteString();
			var remoteConfigParsed = DownloadRemoteConfigVersionResponse.Parser.ParseFrom(remoteConfigResponse);
			var assetDigestResponse = SendRequest(m_apiEndPoint, new Request
			{
				RequestType = RequestType.GetAssetDigest,
				RequestMessage = new GetAssetDigestMessage
				{
					Platform = POGOProtos.Enums.Platform.Android,
					AppVersion = 2903
				}.ToByteString()
			});
			var itemTemplateResponse = SendRequest(m_apiEndPoint, new Request
			{
				RequestType = RequestType.DownloadItemTemplates
			});
		}

		public void BeginRequests()
		{
			var stepSize = 0.00125;

			foreach (Location location in Location.GetSortedLocations(m_stepCount, stepSize, m_lat, m_lng))
			{
				m_lat = location.Latitude;
				m_lng = location.Longitude;

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

				try
				{
					var mapResponseEnvelope = SendRequest(m_apiEndPoint, getMapObjectRequest);
					var mapResponse = GetMapObjectsResponse.Parser.ParseFrom(mapResponseEnvelope.Returns[0]);
					CheckForPokemon(mapResponse);
				}
				catch (Exception e)
				{
					System.Diagnostics.Debug.WriteLine("Exception in BeginRequests(): " + e.Message);
				}
				// I guess the delay is 10 seconds now?
				Thread.Sleep(10100);
			}

			System.Diagnostics.Debug.WriteLine("Got map objects. Resetting.");

			// Reset
			var worker = DBWorker.GetByUsername(m_user);
			worker.SetAvailable();
			Thread.CurrentThread.Abort();
		}

		protected void CheckForPokemon(GetMapObjectsResponse mapResponse)
		{
			seen_pokes.Clear();
			seen_forts.Clear();
			foreach (DBPoke dbPoke in DBPoke.GetAllDBPokes())
			{
				seen_pokes.Add(dbPoke.EncounterId.ToString() + dbPoke.SpawnPointId);
			}
			foreach (DBFort dbFort in DBFort.GetAllDBForts())
			{
				seen_forts.Add(dbFort.FortID);
			}
			var notifyPokes = new List<string>();
			foreach (DBNotifyPoke poke in DBNotifyPoke.GetNotifyPokes())
			{
				notifyPokes.Add(poke.Name);
			}

			foreach (POGOProtos.Map.MapCell cell in mapResponse.MapCells)
			{
				if (cell.WildPokemons.Count > 0 || cell.CatchablePokemons.Count > 0 || cell.Forts.Count > 0 || cell.NearbyPokemons.Count > 0)
				{
					System.Diagnostics.Debug.WriteLine("FOUND SOMETHING");
				}

				if (cell.WildPokemons.Count > 0)
				{
					foreach (WildPokemon poke in cell.WildPokemons)
					{
						if (!seen_pokes.Contains(poke.EncounterId.ToString() + poke.SpawnPointId) && poke.TimeTillHiddenMs > 0)
						{
							System.Diagnostics.Debug.WriteLine("Found pokemon");
							seen_pokes.Add(poke.EncounterId.ToString() + poke.SpawnPointId);

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

							var distance = Location.GetDistance(startLat, startLng, poke.Latitude, poke.Longitude);
							if (distance < .00045)
							{
								string subject = String.Format("Found {0} in catchable distance", poke.PokemonData.PokemonId.ToString());
								string body = String.Format("Poke: {0}\nLat/Long: {1}, {2}\nDist: {3}\nExpires: {4}", 
									poke.PokemonData.PokemonId.ToString(), poke.Latitude, poke.Longitude, distance, found_poke.ExpireTime.ToLocalTime().ToLongTimeString());
								GmailHelper.SendEmail(subject, body);
							}
							else if (notifyPokes.Contains(poke.PokemonData.PokemonId.ToString()))
							{
								string subject = String.Format("FOUND {0} FROM NOTIFY LIST", poke.PokemonData.PokemonId.ToString());
								string body = String.Format("Poke: {0}\nLat/Long: {1}, {2}\nDist: {3}\nExpires: {4}",
									poke.PokemonData.PokemonId.ToString(), poke.Latitude, poke.Longitude, distance, found_poke.ExpireTime.ToLocalTime().ToLongTimeString());
								GmailHelper.SendEmail(subject, body);
							}
						}
					}
				}
				if (cell.Forts.Count > 0)
				{
					foreach (FortData fort in cell.Forts)
					{
						if (!seen_forts.Contains(fort.Id) && fort.Enabled)
						{
							System.Diagnostics.Debug.WriteLine("Found fort");
							var fortDetailsRequest = new Request
							{
								RequestType = RequestType.FortDetails,
								RequestMessage = new FortDetailsMessage
								{
									FortId = fort.Id,
									Latitude = fort.Latitude,
									Longitude = fort.Longitude
								}.ToByteString()
							};
							try
							{
								var fortDetailsResponseEnvelope = SendRequest(m_apiEndPoint, fortDetailsRequest);
								var fortDetails = FortDetailsResponse.Parser.ParseFrom(fortDetailsResponseEnvelope.Returns[0]);
								DBFort found_fort = new DBFort
								{
									FortID = fort.Id,
									FortType = fort.Type.ToString(),
									Name = fortDetails.Name,
									Latitude = fort.Latitude,
									Longitude = fort.Longitude,
									Team = fort.OwnedByTeam.ToString()
								};
								found_fort.Save();
								seen_forts.Add(fort.Id);
							}
							catch
							{

							}
							
						}
						else if (seen_forts.Contains(fort.Id) && fort.Type == FortType.Gym)
						{
							DBFort.UpdateTeam(fort.Id, fort.OwnedByTeam.ToString());
						}
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
				Altitude = 100,
				Unknown12 = 123,
				Requests = { RequestHelper.GetDefaultRequests(settingsHash) }
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

			EncryptionHelper encryptionHelper = new EncryptionHelper();
			requestEnvelope.Unknown6 = encryptionHelper.GenerateSignature(requestEnvelope, m_lat, m_lng);

			try
			{
				using (var memoryStream = new System.IO.MemoryStream())
				{
					requestEnvelope.WriteTo(memoryStream);
					var httpClient = new HttpClient();
					httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("Dalvik/2.1.0 (Linux; U; Android 5.0; Nexus 5 Build/LPX13D)");
					httpClient.DefaultRequestHeaders.ExpectContinue = false;
					using (var response = httpClient.PostAsync(apiUrl, new ByteArrayContent(memoryStream.ToArray())).Result)
					{
						var responseBytes = response.Content.ReadAsByteArrayAsync().Result;
						var responseEnvelope = ResponseEnvelope.Parser.ParseFrom(responseBytes);
						if (string.IsNullOrEmpty(settingsHash) && responseEnvelope.StatusCode != 53)
						{
							var downloadSettings = DownloadSettingsResponse.Parser.ParseFrom(responseEnvelope.Returns[4]);
							settingsHash = downloadSettings.Hash;
						}
						System.Diagnostics.Debug.WriteLine("Received" + responseBytes.Length + "bytes. Status code: " + responseEnvelope.StatusCode + ", settings hash " + settingsHash);

						return responseEnvelope;
					}
				}
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.WriteLine("Exception in SendRequest(): " + e.Message);
			}
			return new ResponseEnvelope();
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
						seen_pokes.RemoveAll(spId => spId == dbPoke.EncounterId.ToString() + dbPoke.SpawnPointId);
					}
				}
				System.Diagnostics.Debug.WriteLine("Cleared stale pokes. Waiting one minute.");
				Thread.Sleep(60000);
			}
		}
	}
}