<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="AddTouTiaoContent.aspx.cs" Inherits="ZoomLaCMS.Manage.TouTiao.AddTouTiaoContent" MasterPageFile="~/Manage/I/Default.master" %>
<asp:Content runat="server" ContentPlaceHolderID="head"><title>内容投递</title></asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Content">
    <table class="table table-bordered">
        <tr>
            <td class="td_m text-right">文章标题：</td>
            <td>
                <ZL:TextBox ID="Title_T" runat="server" AllowEmpty="false" MaxLength="10" CssClass="form-control text_300"></ZL:TextBox><span style="color:green;">*5-10字</span>
            </td>
        </tr>
        <tr>
            <td class="text-right">文章摘要：</td>
            <td>
                <asp:TextBox ID="Abs_T" runat="server" CssClass="form-control" Width="400px" Height="80px" TextMode="MultiLine"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td class="text-right">文章内容：</td>
            <td>
                <asp:TextBox ID="Content_T" runat="server" AllowEmpty="false" CssClass="form-control" Width="400" Height="200" TextMode="MultiLine"></asp:TextBox>
            </td>
        </tr>
        <tr>
            <td class="text-right">文章分类：</td>
            <td>
                <asp:DropDownList ID="Types_DP" runat="server" CssClass="form-control text_300"></asp:DropDownList>
            </td>
        </tr>
        <tr>
            <td class="text-right">发文类型：</td>
            <td>
                <asp:RadioButtonList ID="IsSave_Rad" runat="server" RepeatDirection="Horizontal">
                    <asp:ListItem Text="草稿" Value="0" Selected="True" />
                    <asp:ListItem Text="发布" Value="1" />
                </asp:RadioButtonList>
            </td>
        </tr>
        <tr>
            <td></td>
            <td>
                <asp:Button ID="Add_B" runat="server" CssClass="btn btn-info" Text="发表文章" OnClick="Add_B_Click" />
            </td>
        </tr>
    </table>    

    <asp:Label ID="Result_L" runat="server"></asp:Label>
    <asp:Image ID="img_url" runat="server" />
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="ScriptContent"></asp:Content>
