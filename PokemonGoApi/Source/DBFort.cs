using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace PokemonGoApi.Source
{
	public class DBFort
	{
		public string FortID { get; set; }
		public string FortType { get; set; }
		public string Name { get; set; }
		public double Latitude { get; set; }
		public double Longitude { get; set; }
		public string Team { get; set; }

		public DBFort() { }

		public static List<DBFort> GetAllDBForts()
		{
			List<DBFort> dbForts = new List<DBFort>();
			using (var dbConnection = new SqlConnection(Constants.ConnectionString))
			{
				dbConnection.Open();
				var sql = "SELECT * FROM found_fort";
				var cmd = new SqlCommand(sql, dbConnection);
				var reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					DBFort found_fort = new DBFort()
					{
						FortID = reader["fortId"].ToString(),
						FortType = reader["fortType"].ToString(),
						Name = reader["name"].ToString(),
						Latitude = Convert.ToDouble(reader["latitude"]),
						Longitude = Convert.ToDouble(reader["longitude"]),
						Team = reader["team"].ToString()
					};
					dbForts.Add(found_fort);
				}
				dbConnection.Close();
			}
			return dbForts;
		}

		public void Save()
		{
			using (var dbConnection = new SqlConnection(Constants.ConnectionString))
			{
				dbConnection.Open();
				var sql = @"INSERT INTO found_fort(fortId,
													fortType,
													name,
													latitude,
													longitude,
													team)
											VALUES(@fortId,
													@fortType,
													@name,
													@latitude,
													@longitude,
													@team)";
				var cmd = new SqlCommand(sql, dbConnection);
				cmd.Parameters.AddWithValue("@fortId", FortID);
				cmd.Parameters.AddWithValue("@fortType", FortType);
				cmd.Parameters.AddWithValue("@name", Name);
				cmd.Parameters.AddWithValue("@latitude", Latitude);
				cmd.Parameters.AddWithValue("@longitude", Longitude);
				cmd.Parameters.AddWithValue("@team", Team);

				if (cmd.ExecuteNonQuery() > 0)
				{
					System.Diagnostics.Debug.WriteLine("Saved fort");
				}
			}
		}

		public static void UpdateTeam(string fortId, string team)
		{
			using (var dbConnection = new SqlConnection(Constants.ConnectionString))
			{
				dbConnection.Open();
				var sql = @"UPDATE found_fort
							SET team = @team
							WHERE fortId = @fortId";
				var cmd = new SqlCommand(sql, dbConnection);
				cmd.Parameters.AddWithValue("@team", team);
				cmd.Parameters.AddWithValue("@fortId", fortId);

				if (cmd.ExecuteNonQuery() > 0)
				{
					System.Diagnostics.Debug.WriteLine("Updated gym ownership");
				}
			}
		}
	}
}