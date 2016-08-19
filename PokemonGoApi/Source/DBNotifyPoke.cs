using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;

namespace PokemonGoApi.Source
{
	public class DBNotifyPoke
	{
		public string Name { get; set; }

		public static List<DBNotifyPoke> GetAllPokes()
		{
			List<DBNotifyPoke> pokes = new List<DBNotifyPoke>();
			using (var conn = new SqlConnection(Constants.ConnectionString))
			{
				conn.Open();
				var cmd = new SqlCommand("SELECT * FROM pokemon", conn);
				var dataReader = cmd.ExecuteReader();
				while (dataReader.Read())
				{
					pokes.Add(new DBNotifyPoke { Name = dataReader["name"].ToString() });
				}
			}
			return pokes;
		}

		public static List<DBNotifyPoke> GetNotifyPokes()
		{
			List<DBNotifyPoke> pokes = new List<DBNotifyPoke>();
			using (var conn = new SqlConnection(Constants.ConnectionString))
			{
				conn.Open();
				var cmd = new SqlCommand("SELECT * FROM pokemonNotify", conn);
				var dataReader = cmd.ExecuteReader();
				while (dataReader.Read())
				{
					pokes.Add(new DBNotifyPoke { Name = dataReader["name"].ToString() });
				}
			}
			return pokes;
		}

		public void Add()
		{
			using (var conn = new SqlConnection(Constants.ConnectionString))
			{
				conn.Open();
				var cmd = new SqlCommand("INSERT INTO pokemonNotify(name) VALUES(@name)", conn);
				cmd.Parameters.AddWithValue("@name", Name);
				cmd.ExecuteNonQuery();
			}
		}

		public void Delete()
		{
			using (var conn = new SqlConnection(Constants.ConnectionString))
			{
				conn.Open();
				var cmd = new SqlCommand("DELETE FROM pokemonNotify WHERE name=@name", conn);
				cmd.Parameters.AddWithValue("@name", Name);
				cmd.ExecuteNonQuery();
			}
		}
	}
}