using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace PokemonGoApi.Source
{
	public class DBPoke
	{
		public ulong EncounterId { get; set; }
		public long LastModifiedTimeStampMs { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public string SpawnPointId { get; set; }
		public string Name { get; set; }
		public DateTime ExpireTime { get; set; }

		public DBPoke() { }

		public DBPoke(ulong encounterId, long lastModifiedTimeStampMs, double latitude, double longitude, string spawnPointId, string name, DateTime expireTime)
		{
			EncounterId = encounterId;
			LastModifiedTimeStampMs = lastModifiedTimeStampMs;
			Latitude = latitude;
			Longitude = longitude;
			SpawnPointId = spawnPointId;
			Name = name;
			ExpireTime = expireTime;
		}

		public static List<DBPoke> GetAllDBPokes()
		{
			List<DBPoke> dbPokes = new List<DBPoke>();
			using (var dbConnection = new SqlConnection(Constants.ConnectionString))
			{
				dbConnection.Open();
				var sql = "SELECT * FROM found_pokemon";
				var cmd = new SqlCommand(sql, dbConnection);
				var reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					DBPoke found_poke = new DBPoke()
					{
						EncounterId = Convert.ToUInt64(reader["encounterId"]),
						LastModifiedTimeStampMs = Convert.ToInt64(reader["lastModifiedTimeStampMs"]),
						Latitude = Convert.ToDouble(reader["latitude"]),
						Longitude = Convert.ToDouble(reader["longitude"]),
						SpawnPointId = reader["spawnPointId"].ToString(),
						Name = reader["name"].ToString(),
						ExpireTime = Convert.ToDateTime(reader["expireTime"])
					};
					dbPokes.Add(found_poke);
				}
				dbConnection.Close();
			}
			return dbPokes;
		}

		public void Delete()
		{
			using (var dbConnection = new SqlConnection(Constants.ConnectionString))
			{
				dbConnection.Open();
				var sql = @"DELETE FROM found_pokemon 
							WHERE encounterId = @encounterId
							AND spawnPointId = @spawnPointId";
				var cmd = new SqlCommand(sql, dbConnection);
				cmd.Parameters.AddWithValue("@encounterId", EncounterId.ToString());
				cmd.Parameters.AddWithValue("@spawnPointId", SpawnPointId);

				if (cmd.ExecuteNonQuery() > 0)
				{
					System.Diagnostics.Debug.WriteLine(String.Format("Deleted stale pokemon {0}", Name));
				}
			}
		}

		public void Save()
		{
			using (var dbConnection = new SqlConnection(Constants.ConnectionString))
			{
				dbConnection.Open();
				var sql = @"INSERT INTO found_pokemon(encounterId,
													  lastModifiedTimeStampMs,
													  latitude,
													  longitude,
													  spawnPointId,
													  name,
													  expireTime)
											   VALUES(@encounterId,
													  @lastModifiedTimeStampMs,
													  @latitude,
													  @longitude,
													  @spawnPointId,
													  @name,
													  @expireTime)";
				var cmd = new SqlCommand(sql, dbConnection);
				cmd.Parameters.AddWithValue("@encounterId", EncounterId.ToString());
				cmd.Parameters.AddWithValue("@lastModifiedTimeStampMs", LastModifiedTimeStampMs);
				cmd.Parameters.AddWithValue("@latitude", Latitude);
				cmd.Parameters.AddWithValue("@longitude", Longitude);
				cmd.Parameters.AddWithValue("@spawnPointId", SpawnPointId);
				cmd.Parameters.AddWithValue("@name", Name);
				cmd.Parameters.AddWithValue("@expireTime", ExpireTime);

				if (cmd.ExecuteNonQuery() > 0)
				{
					System.Diagnostics.Debug.WriteLine("Saved pokemon");
				}
			}
		}
	}
}