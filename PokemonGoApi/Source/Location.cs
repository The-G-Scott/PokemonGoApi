using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PokemonGoApi.Source
{
	public class Location
	{
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public double Distance { get; set; }

		public static double GetDistance(double startLat, double startLng, double lat, double lng)
		{
			return Math.Sqrt(Math.Pow(startLat - lat, 2) + Math.Pow(startLng - lng, 2));
		}

		public static List<Location> GetSortedLocations(int numSteps, double stepSize, double startLat, double startLng)
		{
			List<Location> locations = new List<Location>();
			double curLat;
			double curLng;
			for (int i = -numSteps; i <= numSteps; i++)
			{
				curLat = startLat + (i * stepSize);
				for (int j = -numSteps; j <= numSteps; j++)
				{
					curLng = startLng + (j * stepSize);
					locations.Add(new Location
					{
						Latitude = curLat,
						Longitude = curLng,
						Distance = GetDistance(startLat, startLng, curLat, curLng)
					});
				}
			}
			return locations.OrderBy(l => l.Distance).ToList();
		}
	}
}