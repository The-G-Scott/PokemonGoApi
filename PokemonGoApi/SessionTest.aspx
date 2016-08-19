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
        <table>
            <tr style="text-align: center; font-weight:bolder;"><td>Pokemon</td><td></td><td>Pokestops/Gyms</td><td></td><td>Notify Me For</td></tr>
            <tr>
                <td style="vertical-align: top;"><asp:GridView ID="FoundPokesGridView" OnRowDataBound="FoundPokesGridView_RowDataBound" runat="server" /></td>
                <td style="width: 50px;"></td>
                <td style="vertical-align: top;"><asp:GridView ID="FoundFortsGridView" runat="server" /></td>
                <td style="width: 50px;"></td>
                <td style="vertical-align: top;">
                    <asp:CheckBoxList ID="NotifyPokesCheckList" runat="server" /><br />
                    <asp:Button ID="SaveNotifyPokesButton" Text="Save Notify Preferences" OnClick="SaveNotifyPokesButton_Click" runat="server" />
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
