<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SessionTest.aspx.cs" Inherits="PokemonGoApi.SessionTest" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <a href="SessionTest.aspx">Refresh</a><br />
        <asp:GridView ID="FoundPokesGridView" runat="server" />
    </div>
    </form>
</body>
</html>
