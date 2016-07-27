using System.Collections.Generic;
using Google.Protobuf;
using POGOProtos.Networking.Requests;
using POGOProtos.Networking.Requests.Messages;

namespace PokemonGoApi.Source
{
	public class RequestHelper
	{
		public static IEnumerable<Request> GetDefaultRequests()
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