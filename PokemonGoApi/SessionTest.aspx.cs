using System;
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
			var asd = PokemonGoApi.Source.DBPoke.GetAllDBPokes();
			foreach (PokemonGoApi.Source.DBPoke dsa in asd)
			{
				SessionTestLabel.Text += dsa.Name + "<br />";
			}
		}
	}
}