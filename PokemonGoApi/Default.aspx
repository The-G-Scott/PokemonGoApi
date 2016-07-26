<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="PokemonGoApi.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Pokemon Go API Test</title>
    <link rel="icon" runat="server" href="~/favicon.ico" type="image/x-icon" />
    <link rel="stylesheet" href="~/styles/main.css" type="text/css" runat="server" />
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <table>
                <tr><td>Username:</td><td><asp:TextBox ID="GoogleUserTextBox" runat="server" /></td></tr>
                <tr><td>Password:</td><td><asp:TextBox ID="GooglePassTextBox" TextMode="Password" runat="server" /></td></tr>
                <tr><td>Location:</td><td><asp:TextBox ID="LocationTextBox" runat="server" /></td></tr>
                <tr><td colspan="2">
                    <asp:Button ID="GoogleLoginButton" Text="Google Login" OnClick="GoogleLoginButton_OnClick" runat="server" />
                    <asp:Button ID="LogoutButton" Text="Log Out" OnClick="LogoutButton_OnClick" runat="server" Visible="false" />
                </td></tr>
            </table>
        </div>
        <div>
            <asp:Label ID="UserLabel" runat="server" /><br />
            <asp:Label ID="LocationLabel" runat="server" /><br />
            <asp:Label ID="FoundPokesLabel" runat="server" />
        </div>
    </form>
</body>
</html>
