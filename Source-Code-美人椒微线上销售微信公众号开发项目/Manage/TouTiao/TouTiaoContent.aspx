<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TouTiaoContent.aspx.cs" Inherits="ZoomLaCMS.Manage.TouTiao.TouTiaoContent" MasterPageFile="~/Manage/I/Default.master" %>
<asp:Content runat="server" ContentPlaceHolderID="head"><title>内容管理</title></asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="Content">
    <div class="alert alert-info" style="margin:3px 0;">
        <span><strong>提示：</strong></span>
        投递成功的文章可登录头条号，在文章管理处查看， <a href="https://mp.toutiao.com/core/article/index/?source_type=0" target="_blank"><i class="fa fa-share"></i>前往查看</a>
    </div>
    <ZL:ExGridView ID="EGV" runat="server" PageSize="10" AutoGenerateColumns="false" CssClass="table table-striped table-bordered table-hover" 
        EnableTheming="False" AllowPaging="true" EmptyDataText="<%$Resources:L,当前没有信息 %>" OnPageIndexChanging="EGV_PageIndexChanging" OnRowCommand="EGV_RowCommand">
        <Columns>
            <asp:BoundField HeaderText="ID" DataField="ID" />
            <asp:BoundField HeaderText="标题" DataField="Title" />
            <asp:BoundField HeaderText="作者" DataField="Inputer" />
            <asp:TemplateField HeaderText="状态">
                <ItemTemplate>
                    <%# Eval("Status","").Equals("1") ? "<span style='color:green;'>成功</span>" : "<span style='color:red;'>失败</span>" %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="发文类型">
                <ItemTemplate>
                    <%# Eval("IsSave","").Equals("1") ? "发布" : "草稿" %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="文章分类">
                <ItemTemplate>
                    <%#GetContentType(Eval("Type","")) %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField HeaderText="备注信息" DataField="ErrMsg" />
            <asp:BoundField HeaderText="发布时间" DataField="CreateDate" />
            <asp:TemplateField HeaderText="操作">
                <ItemTemplate>
                    <asp:LinkButton runat="server" CommandArgument='<%#Eval("ID") %>' CommandName="del" OnClientClick="return confirm('确认删除这条记录吗？')" CssClass="option_style"><i class="fa fa-trash-o" title="删除"></i>删除</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </ZL:ExGridView>
</asp:Content>
<asp:Content runat="server" ContentPlaceHolderID="ScriptContent"></asp:Content>