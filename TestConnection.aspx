<%@ Page Language="vb" AutoEventWireup="false" CodeFile="TestConnection.aspx.vb" Inherits="TestConnection" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Fabric Warehouse Connectivity Test</title>
    <style>
        body { font-family: Segoe UI, Arial, sans-serif; margin: 24px; color: #222; }
        h1 { font-size: 20px; }
        .ok { color: #107c10; font-weight: 600; }
        .err { color: #a4262c; font-weight: 600; white-space: pre-wrap; }
        table { border-collapse: collapse; margin-top: 12px; }
        th, td { border: 1px solid #ccc; padding: 4px 8px; font-size: 13px; }
        th { background: #f3f2f1; text-align: left; }
        .banner { background: #f3f2f1; padding: 8px; border-radius: 4px; margin: 8px 0; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <h1>Microsoft Fabric Warehouse - Connectivity Test</h1>
        <p>Runs <strong>SELECT @@VERSION</strong> and an optional sample query.</p>
        <asp:Button ID="btnTest" runat="server" Text="Run connectivity test" />
        <div class="banner">
            <asp:Literal ID="litStatus" runat="server" />
        </div>
        <asp:Literal ID="litVersion" runat="server" />
        <asp:GridView ID="gvResults" runat="server" AutoGenerateColumns="true" />
    </form>
</body>
</html>
