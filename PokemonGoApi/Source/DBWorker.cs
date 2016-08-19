using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;

namespace PokemonGoApi.Source
{
	public class DBWorker
	{
		public string Username { get; set; }
		public string Password { get; set; }
		public bool IsAvailable { get; set; }

		public static List<DBWorker> GetAvailableWorkers()
		{
			List<DBWorker> availableWorkers = new List<DBWorker>();
			using (var dbConnection = new SqlConnection(Constants.ConnectionString))
			{
				dbConnection.Open();
				var sql = @"SELECT *
							FROM workerAccounts
							WHERE available = 1";
				var cmd = new SqlCommand(sql, dbConnection);
				var reader = cmd.ExecuteReader();
				while (reader.Read())
				{
					DBWorker worker = new DBWorker
					{
						Username = reader["username"].ToString(),
						Password = reader["password"].ToString(),
						IsAvailable = Convert.ToBoolean(reader["available"])
					};
					availableWorkers.Add(worker);
				}
			}
			return availableWorkers;
		}

		public static DBWorker GetByUsername(string username)
		{
			DBWorker worker = new DBWorker();
			using (var dbConnection = new SqlConnection(Constants.ConnectionString))
			{
				dbConnection.Open();
				var sql = @"SELECT *
							FROM workerAccounts
							WHERE username = @username";
				var cmd = new SqlCommand(sql, dbConnection);
				cmd.Parameters.AddWithValue("@username", username);
				var reader = cmd.ExecuteReader();
				if (reader.Read())
				{
					worker.Username = reader["username"].ToString();
					worker.Password = reader["password"].ToString();
					worker.IsAvailable = Convert.ToBoolean(reader["available"]);
				}
			}
			return worker;
		}

		public void SetBusy()
		{
			using (var dbConnection = new SqlConnection(Constants.ConnectionString))
			{
				dbConnection.Open();
				var sql = @"UPDATE workerAccounts
							SET available = 0
							WHERE username = @username";
				var cmd = new SqlCommand(sql, dbConnection);
				cmd.Parameters.AddWithValue("@username", Username);
				cmd.ExecuteNonQuery();
			}
		}

		public void SetAvailable()
		{
			using (var dbConnection = new SqlConnection(Constants.ConnectionString))
			{
				dbConnection.Open();
				var sql = @"UPDATE workerAccounts
							SET available = 1
							WHERE username = @username";
				var cmd = new SqlCommand(sql, dbConnection);
				cmd.Parameters.AddWithValue("@username", Username);
				cmd.ExecuteNonQuery();
			}
		}
	}
}