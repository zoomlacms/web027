using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ZoomLa.BLL;
using ZoomLa.Common;
using ZoomLa.Model;

namespace ZoomLaCMS.Common.Master
{
    public partial class UserEmpty : System.Web.UI.MasterPage
    {
        B_User buser = new B_User();
        B_Group groupBll = new B_Group();
        M_Group groupMod = new M_Group();
        M_UserInfo mu = new M_UserInfo();
        public string uname = "";
        public string gname = "";
        public string uface = "";
        public string Purse = "";
        public string Score = "";
        protected void Page_Init(object sender, EventArgs e)
        {
            if (buser.CheckLogin())
            {
                mu = buser.GetLogin();
                if (mu == null || mu.IsNull || mu.UserID < 1)
                {
                    Response.Redirect("/User/Login");
                }
                else if (mu.Status != 0) { function.WriteErrMsg("你的帐户未通过验证或被锁定，请与网站管理员联系", "/User/Login"); }
                uname = string.IsNullOrEmpty(mu.TrueName) ? mu.UserName : mu.TrueName;
                groupMod = groupBll.SelReturnModel(mu.GroupID);
                gname = groupMod.GroupName;
                uface = mu.UserFace;
                Purse = mu.Purse.ToString("f2");
                Score = mu.UserExp.ToString("f2");
            }
            else
            {
                B_User.CheckIsLogged(Request.RawUrl);
            }
        }
        protected void Page_Load(object sender, EventArgs e)
        {

        }
    }
}