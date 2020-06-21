<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="StoreUpdate.aspx.cs" Inherits="ZoomLaCMS.Manage.UserShopMannger.StoreUpdate" ValidateRequest="false" MasterPageFile="~/Manage/I/Default.master" %>
<%@ Register Src="~/Manage/I/ASCX/TlpDP.ascx" TagPrefix="ZL" TagName="TlpDP" %>
<asp:Content runat="server" ContentPlaceHolderID="head">
<title>商家店铺修改</title>
<script type="text/javascript" charset="utf-8" src="/Plugins/Ueditor/ueditor.config.js"></script>
<script type="text/javascript" charset="utf-8" src="/Plugins/Ueditor/ueditor.all.js"></script>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Content">
    <div>
        <table class="table table-striped table-bordered table-hover">
            <tr>
                <td class="text-right">店铺名称</td>
                <td>
                    <asp:TextBox ID="UserShopName_T" runat="server" CssClass="form-control text_300" />
                    <a href="/Store/StoreIndex?id=<%:storeMod.GeneralID %>" class="btn btn-info margin_l5" target="_blank"><i class="fa fa-eye"></i></a>
                </td>
            </tr>
            <tr><td class="text-right">首页模板</td><td>
                <%=PageCommon.GetTlpDP("IndexTemplate",!string.IsNullOrEmpty(IndexTemplate_hid.Value)) %>
                <asp:HiddenField ID="IndexTemplate_hid" runat="server" />
            </td></tr>
            <asp:Literal ID="ModelHtml" runat="server"></asp:Literal>
        </table>
    </div>
    <div style="margin-left: 400px">
        <asp:Button ID="Esubmit" CssClass="btn btn-primary" runat="server" Text="保存信息" OnClick="Esubmit_Click" />
    </div>
<ZL:TlpDP runat="server" />
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="ScriptContent">
<script>
    var zlconfig = {
        updir: "<%=ZoomLa.Components.SiteConfig.SiteOption.UploadDir.ToLower()%>",
        duptitlenum: "<%=ZoomLa.Components.SiteConfig.SiteOption.DupTitleNum%>",
        modelid: "<%=0%>"
        };
</script>
<script type="text/javascript" src="/JS/OAKeyWord.js"></script>
<script type="text/javascript" src="/JS/DatePicker/WdatePicker.js"></script>
<script type="text/javascript" src="/JS/chinese.js"></script>
<script type="text/javascript" src="/JS/Common.js"></script>
<script type="text/javascript" src="/JS/Controls/ZL_Dialog.js"></script>
<script type="text/javascript" src="/JS/ICMS/tags.json"></script>
<script type="text/javascript" src="/JS/ZL_Content.js"></script>
<script>
    $(function () { Tlp_initTemp(); })
</script>
</asp:Content>

