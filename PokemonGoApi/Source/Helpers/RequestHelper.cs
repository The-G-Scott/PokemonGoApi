using System.Collections.Generic;
using Google.Protobuf;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;

namespace PokemonGoApi.Source
{
	public class RequestHelper
	{
		public static IEnumerable<Request> GetDefaultRequests(string settingsHash)
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

				string.IsNullOrEmpty(settingsHash) ?
				new Request
				{
					RequestType = RequestType.DownloadSettings
				}
				:
				new Request
				{
					RequestType = RequestType.DownloadSettings,
					RequestMessage = new DownloadSettingsMessage
					{
						Hash = settingsHash
					}.ToByteString()
				}
			};
		}
	}
}