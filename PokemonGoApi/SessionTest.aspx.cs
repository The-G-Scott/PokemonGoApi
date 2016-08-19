using System;
using System.Data;
using PokemonGoApi.Source;
using System.Collections.Generic;
using System.Web.UI.WebControls;

namespace PokemonGoApi
{
	public partial class SessionTest : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			if (!IsPostBack)
			{
				List<ulong> encounterIds = new List<ulong>();
				DataTable pokes_table = new DataTable();
				pokes_table.Columns.Add("Pokemon");
				pokes_table.Columns.Add("Location");
				pokes_table.Columns.Add("Distance");
				pokes_table.Columns.Add("Expire Time");
				var dbPokes = DBPoke.GetAllDBPokes();
				foreach (DBPoke dbPoke in dbPokes)
				{
					if (dbPoke.ExpireTime > DateTime.UtcNow && !encounterIds.Contains(dbPoke.EncounterId))
					{
						encounterIds.Add(dbPoke.EncounterId);

						DataRow poke_row = pokes_table.NewRow();
						poke_row["Pokemon"] = dbPoke.Name;
						poke_row["Location"] = String.Format("{0}, {1}", dbPoke.Latitude, dbPoke.Longitude);
						poke_row["Distance"] = Location.GetDistance(Convert.ToDouble(Session["latitude"]), Convert.ToDouble(Session["longitude"]), dbPoke.Latitude, dbPoke.Longitude);
						poke_row["Expire Time"] = dbPoke.ExpireTime.ToLocalTime().ToLongTimeString();
						pokes_table.Rows.Add(poke_row);
					}
				}
				pokes_table.DefaultView.Sort = "Distance ASC";
				FoundPokesGridView.DataSource = pokes_table;
				FoundPokesGridView.AllowPaging = true;
				FoundPokesGridView.PageSize = 45;
				FoundPokesGridView.DataBind();

				List<string> fortIds = new List<string>();
				DataTable forts_table = new DataTable();
				forts_table.Columns.Add("Name");
				forts_table.Columns.Add("Type");
				forts_table.Columns.Add("Owned By");
				forts_table.Columns.Add("Location");
				forts_table.Columns.Add("Distance");
				var dbForts = DBFort.GetAllDBForts();
				foreach (DBFort dbFort in dbForts)
				{
					if (!fortIds.Contains(dbFort.FortID))
					{
						fortIds.Add(dbFort.FortID);

						DataRow fort_row = forts_table.NewRow();
						fort_row["Name"] = dbFort.Name;
						fort_row["Type"] = dbFort.FortType == "Checkpoint" ? "Pokestop" : "Gym";
						fort_row["Owned By"] = GetTeamName(dbFort.Team);
						fort_row["Location"] = String.Format("{0}, {1}", dbFort.Latitude, dbFort.Longitude);
						fort_row["Distance"] = Location.GetDistance(Convert.ToDouble(Session["latitude"]), Convert.ToDouble(Session["longitude"]), dbFort.Latitude, dbFort.Longitude);
						forts_table.Rows.Add(fort_row);
					}
				}
				forts_table.DefaultView.Sort = "Distance ASC";
				FoundFortsGridView.DataSource = forts_table;
				FoundFortsGridView.AllowPaging = true;
				FoundFortsGridView.PageSize = 45;
				FoundFortsGridView.DataBind();

				PopulateNotifyPokesCheckList();
			}
		}

		protected void PopulateNotifyPokesCheckList()
		{
			NotifyPokesCheckList.RepeatColumns = 4;

			var notifyPokes = new List<string>();
			foreach (DBNotifyPoke poke in DBNotifyPoke.GetNotifyPokes())
			{
				notifyPokes.Add(poke.Name);
			}

			foreach (DBNotifyPoke poke in DBNotifyPoke.GetAllPokes())
			{
				NotifyPokesCheckList.Items.Add(new ListItem { Value = poke.Name, Selected = notifyPokes.Contains(poke.Name) });
			}
		}

		protected string GetTeamName(string color)
		{
			switch (color)
			{
				case "Blue":
					return "Mystic";
				case "Red":
					return "Valor";
				case "Yellow":
					return "Instinct";
				default:
					return "";
			}
		}

		protected void FoundPokesGridView_RowDataBound(object sender, GridViewRowEventArgs e)
		{
			if (e.Row.RowType == DataControlRowType.DataRow)
			{
				if (Convert.ToDouble(e.Row.Cells[2].Text) < .0004)
				{
					e.Row.Style.Add("background-color", "#6666ff");
				}
			}
		}

		protected void SaveNotifyPokesButton_Click(object sender, EventArgs e)
		{
			var notifyPokes = new List<string>();
			foreach (DBNotifyPoke poke in DBNotifyPoke.GetNotifyPokes())
			{
				notifyPokes.Add(poke.Name);
			}

			foreach (ListItem pokeLi in NotifyPokesCheckList.Items)
			{
				if (pokeLi.Selected)
				{
					if (!notifyPokes.Contains(pokeLi.Value))
					{
						new DBNotifyPoke { Name = pokeLi.Value }.Add();
					}
				}
				else if (notifyPokes.Contains(pokeLi.Value))
				{
					new DBNotifyPoke { Name = pokeLi.Value }.Delete();
				}
			}
		}
	}
}