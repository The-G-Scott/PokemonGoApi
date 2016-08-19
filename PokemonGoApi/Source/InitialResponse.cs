namespace PokemonGoApi.Source
{
	public class InitialResponse
	{
		public string Username { get; set; }
		public string FormattedAddress { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }

		public InitialResponse() { }
	}
}