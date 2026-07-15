<%@ Page Language="vb" AutoEventWireup="false" CodeFile="Default.aspx.vb" Inherits="_Default" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Fabric Warehouse Demo - Weather</title>
    <style>
        body { font-family: Segoe UI, Arial, sans-serif; margin: 24px; color: #222; }
        h1 { font-size: 22px; margin-bottom: 4px; }
        .sub { color: #555; margin-top: 0; }
        .bar { background: #f3f2f1; padding: 10px 12px; border-radius: 6px; margin: 12px 0; }
        .ok { color: #107c10; font-weight: 600; }
        .err { color: #a4262c; font-weight: 600; white-space: pre-wrap; }
        .btn { background: #0078d4; color: #fff; border: 0; padding: 8px 16px; border-radius: 4px; cursor: pointer; font-size: 14px; }
        .btn:hover { background: #106ebe; }
        table.grid { border-collapse: collapse; margin-top: 12px; width: 100%; max-width: 720px; }
        table.grid th, table.grid td { border: 1px solid #d0d0d0; padding: 6px 12px; font-size: 13px; text-align: left; }
        table.grid th { background: #0078d4; color: #fff; }
        table.grid tr:nth-child(even) td { background: #f7f9fb; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <h1>Microsoft Fabric Warehouse - End-to-End Demo</h1>
        <p class="sub">Source: <strong>[dbo].[Weather]</strong></p>

        <div class="bar">
            <asp:Button ID="btnLoad" runat="server" CssClass="btn" Text="Connect to Fabric &amp; load Weather" />
            &nbsp;&nbsp;
            <asp:Literal ID="litStatus" runat="server" />
        </div>

        <asp:GridView ID="gvWeather" runat="server" CssClass="grid" AutoGenerateColumns="true"
            GridLines="None" EmptyDataText="No rows loaded yet. Click the button above." />
    </form>
</body>
</html>
