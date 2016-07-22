<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="GoogleLogin.aspx.cs" Inherits="PokemonGoApi.GoogleLogin" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Google Login</title>
    <link rel="stylesheet" type="text/css" href="styles/main.css" />
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <h1>Logging in to Google</h1>
        <span>
            If you are seeing this page for more than a few seconds, something likely went wrong.<br />
            Try <asp:LinkButton ID="LogoutLinkButton" OnClick="LogoutLinkButton_OnClick" Text="logging out" runat="server" /> and back in.
        </span>
    </div>
    </form>
</body>
</html>
