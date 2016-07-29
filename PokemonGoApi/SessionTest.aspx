<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SessionTest.aspx.cs" Inherits="PokemonGoApi.SessionTest" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <link rel="stylesheet" href="~/styles/main.css" type="text/css" runat="server" />
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <a href="SessionTest.aspx">Refresh</a><br />
        <asp:GridView ID="FoundPokesGridView" OnRowDataBound="FoundPokesGridView_RowDataBound" runat="server" />
    </div>
    </form>
</body>
</html>
