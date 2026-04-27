<%@ Page Language="C#" AutoEventWireup="true" %>

<%@ Import Namespace="System.Data" %>
<%@ Import Namespace="System.Data.SqlClient" %>

<script runat="server">
    bool isValidConnection = false;
    string connectionSting = string.Empty;

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (Session["IsValidConnection"] != null)
        {
            isValidConnection = (bool)Session["IsValidConnection"];
        }

        btnExecute.Enabled = isValidConnection;
        btnLogOff.Visible = isValidConnection;
        btnTextConnection.Visible = !isValidConnection;
        if (isValidConnection)
        {
            connectionSting = (string)Session["ConnectionString"];
            txtConnectionString.Text = connectionSting;
        }
    }

    protected void btnTextConnection_Click(object sender, EventArgs e)
    {
        connectionSting = txtConnectionString.Text;
        try
        {
            using (var oCnn = new SqlConnection(connectionSting))
            {
                oCnn.Open();
                isValidConnection = true;
                Session["ConnectionString"] = connectionSting;
            }
        }
        catch (Exception ex)
        {
            lblMessage.Text = ex.Message;
        }

        btnExecute.Enabled = isValidConnection;
        btnLogOff.Visible = isValidConnection;
        btnTextConnection.Visible = !isValidConnection;
        Session["IsValidConnection"] = isValidConnection;
    }

    protected void btnExecute_Click(object sender, EventArgs e)
    {
        string commandText = txtSQL.Text;
        DataSet ds = new DataSet();
        try
        {
            using (var oCnn = new SqlConnection(connectionSting))
            {
                oCnn.Open();
                using (var adapter = new SqlDataAdapter(commandText, oCnn))
                {
                    adapter.Fill(ds);
                }
            }
        }
        catch (Exception ex)
        {
            lblMessage.Text = ex.Message;
        }

        if (ds != null && ds.Tables != null && ds.Tables.Count > 0)
        {
            gvResult.DataSource = ds;
            gvResult.DataBind();
        }
    }

    protected void LogOff_Click(object sender, EventArgs e)
    {
        Session.Clear();
        Session.Abandon();
    }
</script>
<form id="form1" runat="server">
    <div style="width: 100%">
        <span>Connection String:</span>
        <br />
        <asp:textbox runat="server" id="txtConnectionString" width="100%"></asp:textbox>
        <br />
        <asp:button runat="server" text="TestConnection" id="btnTextConnection" onclick="btnTextConnection_Click" />
        <asp:button id="btnExecute" runat="server" enabled="False" text="Execute" onclick="btnExecute_Click" />
        <asp:button runat="server" text="LogOff" onclick="LogOff_Click" id="btnLogOff" visible="False" />
        <br />
        <asp:textbox runat="server" id="txtSQL" height="80%" textmode="MultiLine" width="100%"></asp:textbox>
        <br />
        <asp:label id="lblMessage" runat="server" text=""></asp:label>
        <asp:gridview id="gvResult" runat="server" showheaderwhenempty="True">
        </asp:gridview>
    </div>

</form>

