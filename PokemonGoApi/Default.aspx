<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="PokemonGoApi.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Pokemon Go API Test</title>
    <link rel="icon" runat="server" href="~/favicon.ico" type="image/x-icon" />
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:Button ID="GoogleLoginButton" Text="Google Login" OnClick="GoogleLoginButton_OnClick" runat="server" />
        <asp:Button ID="LogoutButton" Text="Log Out" OnClick="LogoutButton_OnClick" runat="server" Visible="false" />
    </div>
    </form>
</body>
</html>
