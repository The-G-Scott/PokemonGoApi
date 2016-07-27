using System;
using System.Data;
using PokemonGoApi.Source;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace PokemonGoApi
{
	public partial class SessionTest : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
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
					poke_row["Distance"] = Math.Sqrt(Math.Pow(Convert.ToDouble(Session["latitude"]) - dbPoke.Latitude, 2) + Math.Pow(Convert.ToDouble(Session["longitude"]) - dbPoke.Longitude, 2));
					poke_row["Expire Time"] = dbPoke.ExpireTime.ToLocalTime().ToLongTimeString();
					pokes_table.Rows.Add(poke_row);
				}
			}

			pokes_table.DefaultView.Sort = "Distance ASC";
			FoundPokesGridView.DataSource = pokes_table;
			FoundPokesGridView.DataBind();
		}
	}
}