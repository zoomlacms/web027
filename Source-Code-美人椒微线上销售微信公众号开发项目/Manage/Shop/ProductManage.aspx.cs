﻿using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Text;
using System.Web.UI.WebControls;
using System.Xml;
using ZoomLa.BLL;
using ZoomLa.BLL.Helper;
using ZoomLa.BLL.Shop;
using ZoomLa.Common;
using ZoomLa.Model;
using ZoomLa.SQLDAL;

namespace ZoomLaCMS.Manage.Shop
{
    public partial class ProductManage : CustomerPageAction
    {
        B_Product bll = new B_Product();
        B_Node nodeBll = new B_Node();
        B_Model bmode = new B_Model();
        B_ModelField bfield = new B_ModelField();
        private string rightSP = "<i class='fa fa-check' style='color:green;'></i>", errorSP = "<i class='fa fa-remove' style='color:red;'></i>", unknownSP = "<i class='fa fa-warning' style='color:orange;'></i>";
        public int NodeID { get { return DataConverter.CLng(Request.QueryString["NodeID"]); } }
        //-1所有店铺商品,==0自营商品,>0店
        public int StoreID {
            get {
                if (!string.IsNullOrEmpty(Request.QueryString["StoreID"]))
                {
                    StoreType_DP.SelectedValue = Request.QueryString["StoreID"];
                }
                return DataConverter.CLng(StoreType_DP.SelectedValue, -100);
            }
        }
        public int QuickSouch
        {
            get
            {
                if (!string.IsNullOrEmpty(Request.QueryString["quicksouch"]))
                {
                    quicksouch.SelectedValue = Request.QueryString["quicksouch"];
                    return DataConverter.CLng(Request.QueryString["quicksouch"]);
                }
                else { return DataConverter.CLng(quicksouch.SelectedValue); }
            }
        }
        public string KeyWord
        {
            get
            {
                if (ViewState["KeyWord"] == null)
                { ViewState["KeyWord"] = string.IsNullOrEmpty(Request.QueryString["KeyWord"]) ? "" : Request.QueryString["KeyWord"]; }
                return ViewState["KeyWord"].ToString();
            }
            set { ViewState["KeyWord"] = value; }
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!B_ARoleAuth.Check(ZLEnum.Auth.shop, "ProductManage"))
            {
                function.WriteErrMsg(Resources.L.没有权限进行此项操作);
            }
            if (!IsPostBack)
            {
                M_Node nodeMod = null;
                if (NodeID > 0)
                {
                    nodeMod = nodeBll.SelReturnModel(NodeID);
                    NodeName_L.Text = nodeMod.NodeName;
                    NodeName_L1.Text = nodeMod.NodeName;
                    Item_L1.Text = "";
                    string tlp = " <div class='btn-group'><button type='button' class='btn btn-default dropdown-toggle' data-toggle='dropdown'>{0}管理<span class='caret'></span></button><ul class='dropdown-menu' role='menu'><li><a href='AddProduct.aspx?ModelID={1}&NodeID={2}'>添加{0}</a></li><li><a href='javascript:;' onclick='ShowImport();'>导入{0}</a></li></ul></div>";
                    string[] modArr = nodeMod.ContentModel.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < modArr.Length; i++)
                    {
                        M_ModelInfo model = bmode.SelReturnModel(DataConverter.CLng(modArr[i]));
                        if (model == null || model.IsNull) { continue; }
                        string itemName = string.IsNullOrEmpty(model.ItemName) ? model.ModelName : model.ItemName;
                        //第一个模型,支持导入导出
                        if (string.IsNullOrEmpty(ModelID_Hid.Value))
                        {
                            Item_L1.Text = itemName;
                            ModelID_Hid.Value = model.ModelID.ToString();
                        }
                        lblAddContent.Text += string.Format(tlp, itemName, modArr[i], NodeID);
                    }
                }
                MyBind();
            }
        }
        public void MyBind()
        {
            Filter_Product filter = new Filter_Product() { NodeID = NodeID, storeid = StoreID, type = QuickSouch, pid = 0, stype = SType_DP.SelectedValue, skey = KeyWord, hasRecycle = 0, proclass = "" };
            filter.orderSort = filter.SetSort(txtbyfilde.SelectedValue, txtbyOrder.SelectedValue);
            DataTable dt = bll.GetProductAll(filter);
            countsp.InnerText = dt.Rows.Count.ToString();
            EGV.DataSource = dt;
            EGV.DataBind();
        }
        protected void Egv_PageIndexChanging(object sender, GridViewPageEventArgs e)
        {
            EGV.PageIndex = e.NewPageIndex;
            MyBind();
        }
        protected void Egv_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView dr = e.Row.DataItem as DataRowView;
                e.Row.Attributes.Add("ondblclick", "location='ShowProduct.aspx?id=" + dr["ID"] + "';");
                int pid = DataConverter.CLng(dr["ParentID"]);
                if (pid > 0 && GroupPro_L.InnerHtml.IndexOf("sgp_" + pid) < 0)
                {
                    StringWriter sw = new StringWriter();
                    Server.Execute("/Common/Comp/GroupPro.aspx?ID=" + pid, sw);
                    GroupPro_L.InnerHtml += "<div id='sgp_" + pid + "'>" + sw.ToString() + "</div>";
                }
            }
        }
        protected void Egv_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            M_Product product = new M_Product();
            M_Product productPre = new M_Product();
            switch (e.CommandName.ToLower())
            {
                case "upmove":
                    product = bll.GetproductByid(Convert.ToInt32(e.CommandArgument.ToString()));
                    productPre = bll.GetNearID(NodeID, product.OrderID, 1);           //int NodeID, int CurrentID, int UporDown
                    if (productPre.OrderID != 0)
                    {
                        int CurrOrder = product.OrderID;
                        product.OrderID = productPre.OrderID;
                        productPre.OrderID = CurrOrder;
                        bll.UpdateOrder(product);
                        bll.UpdateOrder(productPre);
                    }
                    break;
                case "downmove":
                    product = bll.GetproductByid(Convert.ToInt32(e.CommandArgument.ToString()));
                    productPre = bll.GetNearID(NodeID, product.OrderID, 0);
                    if (productPre.ID != 0)
                    {
                        int CurrOrder = product.OrderID;
                        product.OrderID = productPre.OrderID;
                        productPre.OrderID = CurrOrder;
                        bll.UpdateOrder(product);
                        bll.UpdateOrder(productPre);
                    }
                    break;
                case "del1":
                    int pid = DataConverter.CLng(e.CommandArgument.ToString());
                    bool delok = bll.RealDelByIDS(pid.ToString());
                    break;
                default:
                    break;
            }
            MyBind();
        }
        protected void Search_Btn_Click(object sender, EventArgs e)
        {
            KeyWord = SKey_T.Text.Trim();
            MyBind();
        }
        //开始销售
        protected void Button1_Click(object sender, EventArgs e)
        {
            string CID = Request.Form["idchk"];
            if (!String.IsNullOrEmpty(CID) && bll.setproduct(1, CID))
            {
                function.WriteSuccessMsg(Resources.L.批量设置为开始销售成功);
            }
            else
            {
                function.WriteSuccessMsg(Resources.L.批量设置为开始销售失败 + "!" + Resources.L.请选择您要设置的数据);
            }
        }
        //热卖
        protected void Button2_Click(object sender, EventArgs e)
        {
            string CID = Request.Form["idchk"];
            if (!String.IsNullOrEmpty(CID) && bll.setproduct(2, CID))
            {
                function.WriteSuccessMsg(Resources.L.批量设置为热卖成功 + "!");
            }
            else
            {
                function.WriteErrMsg(Resources.L.批量设置为热卖失败 + "!" + Resources.L.请选择您要设置的数据);
            }
        }
        //精品
        protected void Button6_Click(object sender, EventArgs e)
        {
            string CID = Request.Form["idchk"];
            if (!String.IsNullOrEmpty(CID) && bll.setproduct(3, CID))
            {
                function.WriteSuccessMsg(Resources.L.批量设为精品成功 + "!");
            }
            else
            {
                function.WriteErrMsg("批量设为精品失败!请选择您要设置的数据");
            }
        }
        //新品
        protected void Button5_Click(object sender, EventArgs e)
        {
            string CID = Request.Form["idchk"];
            if (!String.IsNullOrEmpty(CID) && bll.setproduct(4, CID))
            {
                function.WriteSuccessMsg("批量设为新品成功!");
            }
            else
            {
                function.WriteErrMsg("批量设为新品失败!请选择您要设置的数据");
            }
        }
        protected void Button3_Click(object sender, EventArgs e)
        {
            string CID = Request.Form["idchk"];
            if (!String.IsNullOrEmpty(CID) && bll.setproduct(13, CID))
            {
                function.WriteSuccessMsg("批量删除成功!");
            }
            else
            {
                function.WriteErrMsg("批量删除失败!请选择您要删除的数据");
            }
        }
        protected void Button4_Click(object sender, EventArgs e)
        {
            string CID = Request.Form["idchk"];
            if (!String.IsNullOrEmpty(CID) && bll.setproduct(6, CID))
            {
                function.WriteSuccessMsg("批量停止销售成功!");
            }
            else
            {
                function.WriteErrMsg("批量停止销售失败!请选择您要设置的数据");
            }
        }
        protected void Button7_Click(object sender, EventArgs e)
        {
            string CID = Request.Form["idchk"];
            if (!String.IsNullOrEmpty(CID) && bll.setproduct(7, CID))
            {
                function.WriteSuccessMsg("批量取消热卖成功!");
            }
            else
            {
                function.WriteErrMsg("批量取消热卖失败!请选择您要设置的数据");
            }
        }
        protected void Button8_Click(object sender, EventArgs e)
        {
            string CID = Request.Form["idchk"];
            if (!String.IsNullOrEmpty(CID) && bll.setproduct(8, CID))
            {
                function.WriteSuccessMsg("批量取消精品成功!");
            }
            else
            {
                function.WriteErrMsg("批量取消精品失败!请选择您要设置的数据");
            }
        }
        protected void Button9_Click(object sender, EventArgs e)
        {
            string CID = Request.Form["idchk"];
            if (!String.IsNullOrEmpty(CID) && bll.setproduct(9, CID))
            {
                function.WriteSuccessMsg("批量取消新品成功!");
            }
            else
            {
                function.WriteErrMsg("批量取消新品失败!请选择您要设置的数据");
            }
        }
        protected void btnMove_Click(object sender, EventArgs e)
        {
            string CID = Request.Form["idchk"];
            if (!string.IsNullOrEmpty(CID))
            {
                Response.Redirect("ProductMove.aspx?id=" + CID.ToString());
            }
            else
                Response.Redirect(Request.RawUrl);
        }
        //审核
        protected void Button11_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(Request.Form["idchk"]) && bll.setproduct(10, Request.Form["idchk"]))
            {
                function.WriteSuccessMsg("批量审核成功", Request.RawUrl);
            }
        }
        //取消审核
        protected void Button12_Click(object sender, EventArgs e)
        {
            string CID = Request.Form["idchk"];
            if (!String.IsNullOrEmpty(CID) && bll.setproduct(11, CID))
            {
                function.WriteSuccessMsg("批量取消审核成功", Request.RawUrl);
            }
        }
        private void Educe(DataTable dt, int type)
        {
            string str = "";
            if (dt != null)
            {
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    str += dt.Columns[i].ColumnName;

                    if ((DataConverter.CLng(dt.Columns.Count) - 1) != i)
                        str += ",";
                }
                str += "\\n";
                foreach (DataRow dr in dt.Rows)
                {
                    for (int i = 0; i < DataConverter.CLng(dt.Columns.Count); i++)
                    {
                        if (dr.ItemArray[i].ToString().Trim().IndexOf(",") != -1)
                        {
                            str += dr.ItemArray[i].ToString().Trim().Replace(",", "，");
                        }
                        else
                        {
                            str += dr.ItemArray[i].ToString().Trim();

                        }
                        if ((DataConverter.CLng(dt.Columns.Count) - 1) != i)
                            str += ",";
                    }
                    str += "\\n";
                }
            }
            string filesname = "";
            if (type == 1)
                filesname = DateTime.Now.ToShortDateString() + "_Output";
            else
                filesname = DateTime.Now.ToShortDateString() + "_childOutput";
            Response.Write("<script>var winname = window.open('', '_blank', 'top=10000');winname.document.open('text/html', 'replace');winname.document.write('" + str.ToString() + "');winname.document.execCommand('saveas','','" + filesname + ".csv');winname.close();</script>");
            str = "";
        }
        protected void lbtnSaveTempter_Click(object sender, EventArgs e)
        {
            DataTable dt = bfield.GetModelFieldList(Convert.ToInt32(ModelID_Hid.Value));
            if (dt != null && dt.Rows.Count >= 1)
                Educe(dt);
            else
            {
                function.WriteErrMsg("该模型存在相关未设置好的项，请核对后再下载！");
            }
        }
        private void Educe(DataTable dt)
        {
            string str = "商品编号,条形码,商品名称,关键字,商品单位,商品重量,销售状态(1),属性设置(1),点击数,创建时间,更新时间,商品简介,详细介绍,商品清晰图,商品缩略图,生产商,品牌/商标,缺货时允许购买(0),限购数量,最低购买数量,市场参考价,当前零售价,";
            int i = 0;
            foreach (DataRow dr in dt.Rows)
            {
                str += dr["FieldAlias"].ToString();
                i = i + 1;
                if (i < dt.Rows.Count)
                {
                    str += ",";
                }
            }
            DataGrid dg = new DataGrid();
            dg.DataSource = dt.DefaultView;
            dg.DataBind();
            //str += "\\n1\\n2\\n3";
            string filesname = bmode.SelReturnModel(ZoomLa.Common.DataConverter.CLng(int.Parse(ModelID_Hid.Value))).ItemName + "模型导入模板";

            Encoding gb = System.Text.Encoding.GetEncoding("GB2312");
            StringWriter sw = new StringWriter();
            sw.WriteLine(str);
            Response.AddHeader("Content-Disposition", "attachment; filename=" + Server.UrlEncode(filesname) + ".csv");
            Response.ContentEncoding = System.Text.Encoding.GetEncoding("GB2312");
            Response.Write(sw.ToString());
            Response.End();
        }
        protected void btImport_Click(object sender, EventArgs e)
        {
            if (FileUpload1.HasFile)
            {
                int FileLen = FileUpload1.PostedFile.ContentLength;
                byte[] input = new byte[FileLen];
                System.IO.Stream UpLoadStream = FileUpload1.PostedFile.InputStream;
                UpLoadStream.Read(input, 0, FileLen);
                UpLoadStream.Position = 0;
                System.IO.StreamReader sr = new System.IO.StreamReader(UpLoadStream, System.Text.Encoding.Default);
                //文件内容
                string filecontent = sr.ReadToEnd().ToString();
                sr.Close();
                filecontent = Server.HtmlEncode(filecontent);
                ArrayList myAL = new ArrayList();
                DataTable table1 = new DataTable();
                table1.Clear();
                string[] filearr = filecontent.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < filearr.Length; i++)
                {
                    string[] danarr = (filearr[i]).Split(new string[] { "," }, StringSplitOptions.None);

                    if (i == 0)
                    {
                        for (int j = 0; j < danarr.Length; j++)
                        {
                            table1.Columns.Add(danarr[j].Trim());
                        }
                    }
                    else
                    {
                        DataRow dr = table1.NewRow();
                        for (int d = 0; d < danarr.Length; d++)
                        {
                            dr[d] = danarr[d].ToString();
                        }
                        table1.Rows.Add(dr);
                    }
                }
                if (bll.ImportProducts(table1, Convert.ToInt32(ModelID_Hid.Value), NodeID))
                {
                    function.WriteSuccessMsg("导入成功");
                }
                else
                {
                    function.WriteErrMsg("导入失败");
                }
            }
        }
        protected void txtbyfilde_SelectedIndexChanged(object sender, EventArgs e)
        {
            MyBind();
        }
        protected void txtbyOrder_SelectedIndexChanged(object sender, EventArgs e)
        {
            MyBind();
        }
        protected void quicksouch_SelectedIndexChanged(object sender, EventArgs e)
        {
            MyBind();
        }
        #region View Get
        public string formattype(string type)
        {
            switch (type)
            {
                case "0":
                    return errorSP;
                case "1":
                    return rightSP;
                default:
                    return unknownSP;
            }
        }
        public string GetStock()
        {
            string str = "";
            int stock = DataConverter.CLng(Eval("stock"));
            int stockDown = DataConverter.CLng(Eval("StockDown"));
            if (stock <= stockDown)
            {
                str = "<font color=red>" + stock + " [警]</font>";
            }
            else
            {
                str = stockDown.ToString();
            }
            return str;
        }
        public string formatnewstype(string type)
        {
            int newtype = DataConverter.CLng(type);
            string restring = "";
            if (newtype == 2)
            {
                restring = "<font color=red>" + Resources.L.特价 + "</font>";
            }
            else if (newtype == 3)
            {
                restring = "<font color=blue>" + Resources.L.积分商品 + "</font>";
            }
            if (!Eval("IsTrue", "").Equals("1"))
            {
                restring = "<font color=#999999>" + Resources.L.待审核 + "</font>";
            }
            if (!DataConverter.CBool(Eval("Recycler", "")))
            {
                restring = "<font color=blue>" + Resources.L.正常 + "</font>";
            }
            else
            {
                restring = "<font color=#999999>" + Resources.L.已删除 + "</font>";
            }
            return restring;
        }
        public string GetPrice()
        {
            //return OrderHelper.GetPriceStr(Convert.ToDouble(Eval("LinPrice")), Eval("LinPrice_Json").ToString());
            return Convert.ToDouble(Eval("LinPrice")).ToString("f2");
        }
        public string GetProAttr()
        {
            string html = "";
            html += Convert.ToInt32(Eval("isbest")) == 1 ? "<span style='color:blue;margin-left:5px;'>" + Resources.L.精 + "</span>" : "";
            html += Convert.ToInt32(Eval("ishot")) == 1 ? "<span style='color:red;margin-left:5px;'>" + Resources.L.热 + "</span>" : "";
            html += Convert.ToInt32(Eval("isnew")) == 1 ? "<span style='color:green;margin-left:5px;'>" + Resources.L.新 + "</span>" : "";
            html += Convert.ToInt32(Eval("Largess")) == 1 ? "<span style='color:#99CC00;margin-left:5px;'>特</span>" : "";
            return html;
        }
        public string getproimg()
        {
            return function.GetImgUrl(Eval("Thumbnails"));
        }
        public string GetShopUrl()
        {
            return OrderHelper.GetShopUrl(Eval("UserShopID"), Eval("ID"));
        }
        public string GetStatus()
        {
            string status = Eval("istrue").ToString();
            return status.Equals("1") ? Resources.L.已审核 : Resources.L.未审核;
        }

        protected void StoreType_SelectedIndexChanged(object sender, EventArgs e)
        {
            MyBind();
        }

        public string GetNode()
        {
            string result = "";
            if (NodeID == 0)
            {
                result = "[<a href='?NodeID=" + Eval("NodeID") + "'>" + Eval("NodeName") + "</a>] ";
            }
            return result;
        }
        public string GetGroupPro()
        {
            string html = "";
            if (DataConverter.CLng(Eval("ParentID")) > 0)
            {
                html += " <button type=\"button\" class=\"btn btn-s pro_btn grouppro_btn\" data-pid=\"" + Eval("ParentID") + "\">单品组合</button>";
            }
            if (DataConverter.CLng(Eval("Class")) == 2)
            {
                html += " <button type=\"button\" class=\"btn btn-s pro_btn suitpro_btn\" onclick=\"showsuit(" + Eval("ID") + ")\">促销组合</button>";
            }
            return html;
        }
        #endregion
    }
}