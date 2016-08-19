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
                <tr>
                    <td><asp:Label ID="LocationTitleLabel" Text="Location:" runat="server"/></td><td><asp:TextBox ID="LocationTextBox" runat="server" /></td>
                    <td><asp:Label ID="StepCountLabel" Text="Step Count:" runat="server" /></td><td><asp:TextBox ID="StepCountTextBox" runat="server" /></td>
                    <td><asp:Button ID="StartButton" Text="Start" OnClick="StartButton_OnClick" runat="server" /></td>
                </tr>
                <tr>
                    <td><asp:Label ID="UserLabel" runat="server" /></td>
                    <td><asp:Label ID="LocationLabel" runat="server" /></td>
                </tr>
            </table>
            <iframe id="ResultsIFrame" src="SessionTest.aspx" runat="server" visible="false" />
        </div>
    </form>
</body>
</html>
